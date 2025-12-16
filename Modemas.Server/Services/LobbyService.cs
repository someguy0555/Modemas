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

        await HostJoinLobby(connectionId, lobby.LobbyId, hostName);
        Console.WriteLine($"Lobby {lobby.LobbyId} created by {hostName}");
    }

    private async Task HostJoinLobby(string connectionId, string lobbyId, string playerName)
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
        await _notifier.NotifyPlayerJoined(lobby.LobbyId, playerName);
        Console.WriteLine($"Player {playerName} joined lobby {lobbyId}");
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
        await _notifier.NotifyPlayerJoined(lobby.LobbyId, playerName);
        await _notifier.NotifyClient(
            connectionId,
            "LobbyJoined",
            lobby.LobbyId,
            playerName,
            lobby.Players.Select(p => p.Name),
            lobby.State
        );
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

        _manager.UpdateSettings(lobby, settings);
        await _notifier.NotifyLobbySettingsUpdated(lobbyId, settings);
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
        // foreach (var q in existing) {
        //     Console.WriteLine("TimeLimit: " + q.TimeLimit);
        // }
        if (existing.Any())
        {
            lobby.Match ??= new LobbyMatch();
            lobby.Match.Questions = existing;
            return true;
        }

        // Generate new questions
        try
        {
            throw new Exception("Failed to generate questions.");
            var questions = await _questionGenerationService.GetOrGenerateQuestionsAsync(count, topic);
            // foreach (var q in questions) {
            //     Console.WriteLine("TimeLimit: " + q.TimeLimit);
            // }
            Console.WriteLine("Questions were generated or something.", questions);
            if (!questions.Any()) return false;

            await _repo.SaveAsync(topic, questions);
            Console.WriteLine("Saved questions");

            lobby.Match ??= new LobbyMatch();
            lobby.Match.Questions = questions.ToList();
            Console.WriteLine("Saved questions end: " + lobby.Match.Questions.ToString());
            // foreach (var q in lobby.Match.Questions) {
            //     Console.WriteLine("Questions: " + q.Text);
            // }
            return true;
        }
        catch
        {
            Console.WriteLine("ExceptionQuestions: " + lobby.Match.Questions);
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
            await _notifier.NotifyGroup(lobbyId, "MatchStartFailed", lobbyId, "Unable to generate questions.");
            // await _notifier.NotifyGroup(lobbyId, "MatchEndEnded", lobbyId);
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
}
