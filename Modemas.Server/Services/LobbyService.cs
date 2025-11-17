using Modemas.Server.Interfaces;
using Modemas.Server.Models;

namespace Modemas.Server.Services;

/// <summary>
/// Core business logic for lobby lifecycle:
/// - Create / Join / Leave / Kick / Disconnect
/// - Start voting and handle question generation
/// </summary>
public class LobbyService : ILobbyService
{
    private readonly ILobbyManager _manager;
    private readonly ILobbyNotifier _notifier;
    private readonly IMatchService _matchService;
    private readonly IQuestionGenerationService _questionGenerationService;
    private readonly IQuestionRepository _repo;

    public LobbyService(
        ILobbyManager manager,
        ILobbyNotifier notifier,
        IMatchService matchService,
        IQuestionGenerationService questionGenerationService,
        IQuestionRepository repo)
    {
        _manager = manager;
        _notifier = notifier;
        _matchService = matchService;
        _questionGenerationService = questionGenerationService;
        _repo = repo;
    }

    /// <summary>
    /// Creates a new lobby for the given host connection and host name.
    /// </summary>
    public async Task CreateLobby(string connectionId, string hostName)
    {
        var lobby = _manager.CreateLobby(connectionId);

        // await _notifier.AddPlayerToGroup(connectionId, lobby.LobbyId);
        await _notifier.NotifyLobbyCreated(connectionId, lobby.LobbyId);

        await JoinLobby(connectionId, lobby.LobbyId, hostName);
        Console.WriteLine($"Lobby {lobby.LobbyId} created by {hostName}");
    }

    /// <summary>
    /// Adds a player to an existing lobby.
    /// </summary>
    public async Task JoinLobby(string connectionId, string lobbyId, string playerName)
    {
        var lobby = _manager.GetLobby(lobbyId);
        if (lobby == null)
        {
            await _notifier.NotifyError(connectionId, "Lobby not found");
            return;
        }

        bool added = _manager.AddPlayer(lobby, connectionId, playerName);
        if (!added)
        {
            await _notifier.NotifyError(connectionId, "Name already taken or duplicate connection");
            return;
        }

        await _notifier.AddPlayerToGroup(connectionId, lobby.LobbyId);
        
        var playerNames = lobby.Players.Select(p => p.Name).ToList();
        await _notifier.NotifyClient(connectionId, "LobbyJoined", lobby.LobbyId, playerName, playerNames, lobby.State.ToString());
        
        var settingsPayload = new
        {
            numberOfQuestions = lobby.LobbySettings.NumberOfQuestions,
            questionTimerInSeconds = lobby.LobbySettings.QuestionTimerInSeconds,
            topic = lobby.LobbySettings.Topic
        };
        await _notifier.NotifyClient(connectionId, "LobbySettingsUpdated", settingsPayload);
        
        await _notifier.NotifyPlayerJoined(lobby.LobbyId, playerName);

        Console.WriteLine($"Player {playerName} joined lobby {lobbyId}");
    }

    /// <summary>
    /// Updates lobby settings (host-only).
    /// </summary>
    public async Task UpdateLobbySettings(string connectionId, string lobbyId, LobbySettings settings)
    {
        var lobby = _manager.GetLobby(lobbyId);
        if (lobby == null)
        {
            await _notifier.NotifyError(connectionId, "Lobby not found");
            return;
        }

        if (lobby.HostConnectionId != connectionId)
        {
            await _notifier.NotifyError(connectionId, "Only the host can update settings");
            return;
        }
        
        var safeNumber = Math.Max(1, settings.NumberOfQuestions);
        var safeTimer = Math.Max(1, settings.QuestionTimerInSeconds);
        var safeTopic = (settings.Topic ?? "").Trim();

        var sanitized = new LobbySettings(safeNumber, safeTimer, safeTopic);
        
        _manager.UpdateSettings(lobby, sanitized);
        
        lobby.LobbySettings = sanitized;
        
        try
        {
            await EnsureMatchQuestionsAsync(lobby);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error while adjusting match questions for lobby {lobbyId}: {ex}");
            await _notifier.NotifyError(connectionId, "Failed to apply settings to current questions.");
        }
        

        await _notifier.NotifyLobbySettingsUpdated(lobbyId, sanitized);
        Console.WriteLine($"Settings updated for lobby {lobbyId}");
    }

    /// <summary>
    /// Removes a player from a lobby (host-only).
    /// </summary>
    public async Task KickPlayer(string connectionId, string lobbyId, string targetPlayerName)
    {
        var lobby = _manager.GetLobby(lobbyId);
        if (lobby == null)
        {
            await _notifier.NotifyError(connectionId, "Lobby not found");
            return;
        }

        if (lobby.HostConnectionId != connectionId)
        {
            await _notifier.NotifyError(connectionId, "Only the host can kick players");
            return;
        }

        var target = lobby.Players.FirstOrDefault(p => p.Name == targetPlayerName);
        if (target == null)
        {
            await _notifier.NotifyError(connectionId, $"Player '{targetPlayerName}' not found");
            return;
        }

        _manager.RemovePlayer(lobby, target.ConnectionId);
        await _notifier.NotifyKicked(target.ConnectionId, $"You were kicked from lobby {lobbyId} by host.");
        await _notifier.NotifyGroup(lobbyId, "LobbyRemovePlayer", targetPlayerName);

        Console.WriteLine($"Player {targetPlayerName} kicked from lobby {lobbyId}");
    }

    /// <summary>
    /// Generates or retrieves questions for a lobby topic.
    /// </summary>
    public async Task<bool> WaitForQuestionsAsync(string lobbyId)
    {
        var lobby = _manager.GetLobby(lobbyId);
        if (lobby == null || string.IsNullOrWhiteSpace(lobby.LobbySettings?.Topic))
            return false;

        var topic = lobby.LobbySettings.Topic.Trim();
        var count = lobby.LobbySettings.NumberOfQuestions;

        // Check repository
        var existing = (await _repo.GetByTopicAsync(topic)).ToList();
        if (existing.Any())
        {
            lobby.Match ??= new LobbyMatch();
            lobby.Match.Questions = existing.ToList();
            
            await EnsureMatchQuestionsAsync(lobby);
            return true;
        }

        // Generate new questions
        try
        {
            var questions = await _questionGenerationService.GenerateQuestionsAsync(count, topic);
            if (!questions.Any()) return false;
            
            await _repo.SaveAsync(topic, questions);
            lobby.Match ??= new LobbyMatch();
            lobby.Match.Questions = questions.ToList();
            
            await EnsureMatchQuestionsAsync(lobby);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Starts voting and prepares questions.
    /// </summary>
    public async Task StartVoting(string lobbyId)
    {
        var lobby = _manager.GetLobby(lobbyId);
        if (lobby == null) return;

        await _notifier.NotifyGroup(lobbyId, "VotingStarted", lobbyId, 0);

        bool ok = await WaitForQuestionsAsync(lobbyId);

        await _notifier.NotifyGroup(lobbyId, "VotingEnded", lobbyId);

        if (!ok)
        {
            await _notifier.NotifyGroup(lobbyId, "MatchStartFailed", lobbyId, "Questions unavailable");
            return;
        }

        await _matchService.StartMatch(lobbyId);
    }

    /// <summary>
    /// Handles player disconnection. If host disconnects, closes the lobby.
    /// </summary>
    public async Task HandleDisconnect(string connectionId)
    {
        var lobby = _manager.FindLobbyByConnection(connectionId);
        if (lobby == null) return;

        if (lobby.HostConnectionId == connectionId)
        {
            // Close lobby on entire lobby disconnect.
            foreach (var p in lobby.Players.ToList())
            {
                await _notifier.NotifyKicked(p.ConnectionId, "Host disconnected. Lobby closed.");
            }

            _manager.RemoveLobby(lobby.LobbyId);
            Console.WriteLine($"Lobby {lobby.LobbyId} closed due to host disconnect.");
            return;
        }

        // Handle non-host disconnect
        var disconnectedPlayer = lobby.Players.FirstOrDefault(p => p.ConnectionId == connectionId);
        if (disconnectedPlayer != null)
        {
            _manager.RemovePlayer(lobby, connectionId);
            await _notifier.NotifyGroup(lobby.LobbyId, "LobbyRemovePlayer", disconnectedPlayer.Name);
            Console.WriteLine($"Player {disconnectedPlayer.Name} disconnected from lobby {lobby.LobbyId}");
        }
    }
    
    private async Task EnsureMatchQuestionsAsync(Lobby lobby)
    {
        if (lobby == null || lobby.LobbySettings == null || string.IsNullOrWhiteSpace(lobby.LobbySettings.Topic))
            return;

        lobby.Match ??= new LobbyMatch();

        var topic = lobby.LobbySettings.Topic.Trim();
        var desiredCount = Math.Max(1, lobby.LobbySettings.NumberOfQuestions);
        var desiredTime = Math.Max(1, lobby.LobbySettings.QuestionTimerInSeconds);

        var current = lobby.Match.Questions ?? new List<Question>();
        
        if (current.Count > desiredCount)
        {
            lobby.Match.Questions = current.Take(desiredCount).ToList();
            current = lobby.Match.Questions;
        }
        
        if (current.Count < desiredCount)
        {
            var missing = desiredCount - current.Count;
            var generated = new List<Question>();
            try
            {
                generated = (await _questionGenerationService.GenerateQuestionsAsync(missing, topic)).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to generate additional questions for topic '{topic}': {ex}");
            }

            if (generated.Any())
            {
                current.AddRange(generated);
                try
                {
                    await _repo.SaveAsync(topic, current);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to save questions for topic '{topic}': {ex}");
                }
            }
        }
        
        foreach (var q in current)
        {
            q.TimeLimit = desiredTime;
        }
        
        lobby.Match.Questions = current;
    }
}
