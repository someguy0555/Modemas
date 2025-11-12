using Microsoft.AspNetCore.SignalR;
using Modemas.Server.Models;
using System.Linq;

using Modemas.Server.Interfaces;

namespace Modemas.Server.Services;

/// <summary>
/// Contains core business logic for managing lobbies.
/// - Create / Join / Leave lobby
/// - Start voting and tally votes
/// - Track players and lobby state
/// </summary>
public class LobbyService
{
    private readonly LobbyStore _store;
    private readonly MatchService _matchService;
    private readonly QuestionGenerationService _questionGenerationService;
    private readonly IQuestionRepository _repo;

    public LobbyService(
        LobbyStore store,
        MatchService matchService,
        QuestionGenerationService questionGenerationService,
        IQuestionRepository repo)
    {
        _store = store;
        _matchService = matchService;
        _questionGenerationService = questionGenerationService;
        _repo = repo;
    }

    /// <summary>
    /// Creates a new lobby and adds the host.
    /// </summary>
    public async Task CreateLobby(HubCallerContext context, IHubCallerClients clients, IGroupManager groups, string hostName)
    {
        var lobbyId = Guid.NewGuid().ToString("N")[..8];
        var lobby = new Lobby
        {
            LobbyId = lobbyId,
            HostConnectionId = context.ConnectionId
        };

        _store.Add(lobby);
        await groups.AddToGroupAsync(context.ConnectionId, lobbyId);
        await clients.Caller.SendAsync("LobbyCreated", lobbyId);

        await JoinLobby(context, clients, groups, lobbyId, hostName);
        Console.WriteLine($"Lobby {lobbyId} created by {hostName}");
    }

    /// <summary>
    /// Adds a player to an existing lobby.
    /// </summary>
    public async Task JoinLobby(HubCallerContext context, IHubCallerClients clients, IGroupManager groups, string lobbyId, string playerName)
    {
        var lobby = _store.Get(lobbyId);
        if (lobby == null)
        {
            await clients.Caller.SendAsync("Error", "Lobby not found");
            return;
        }

        if (lobby.Players.Any(p => p.ConnectionId == context.ConnectionId))
            return;

        if (lobby.Players.Any(p => p.Name == playerName))
        {
            await clients.Caller.SendAsync("Error", $"Name '{playerName}' already taken");
            return;
        }

        var player = new Player { Name = playerName, ConnectionId = context.ConnectionId };
        lobby.Players.Add(player);

        await groups.AddToGroupAsync(context.ConnectionId, lobbyId);
        await clients.Group(lobbyId).SendAsync("LobbyAddPlayer", playerName);
        await clients.Caller.SendAsync("LobbyJoined", lobbyId, playerName, lobby.Players.Select(p => p.Name), lobby.State);

        Console.WriteLine($"Player {playerName} joined lobby {lobbyId}");
    }

    public async Task StartVoting(IHubCallerClients clients, string lobbyId)
    {
        // notify clients voting (or generation) started
        await clients.Group(lobbyId).SendAsync("VotingStarted", lobbyId, 0);

        // Attempt to load or generate questions
        var ok = await WaitForQuestionsAsync(lobbyId);

        // voting ended (or generation completed/failed)
        await clients.Group(lobbyId).SendAsync("VotingEnded", lobbyId);

        if (!ok)
        {
            // Do not start match if questions are missing
            await clients.Group(lobbyId).SendAsync("MatchStartFailed", lobbyId, "Questions unavailable. Please generate a topic.");
            return;
        }

        await _matchService.StartMatch(clients, lobbyId);
    }

    /// <summary>
    /// Returns true if questions are loaded into lobby.Match.Questions; false otherwise.
    /// Actively generates questions if needed.
    /// </summary>
    public async Task<bool> WaitForQuestionsAsync(string lobbyId)
    {
        Console.WriteLine($"[WaitForQuestionsAsync] {lobbyId} started waiting.");
        var lobby = _store.Get(lobbyId);
        if (lobby == null) return false;

        var topic = lobby.LobbySettings?.Topic?.Trim();
        var count = lobby.LobbySettings?.NumberOfQuestions ?? 0;

        // Validate topic
        if (string.IsNullOrWhiteSpace(topic))
        {
            Console.WriteLine($"[WaitForQuestionsAsync] No topic set for lobby {lobbyId}");
            return false;
        }

        // 1️⃣ Try loading from repository
        var existing = (await _repo.GetByTopicAsync(topic)).ToList();
        var timer = lobby.LobbySettings.QuestionTimerInSeconds;

        if (existing.Any())
        {
            if (existing.Count >= count)
            {
                var chosen = existing.Take(count).ToList();
                foreach (var q in chosen) q.TimeLimit = timer;
                lobby.Match ??= new LobbyMatch();
                lobby.Match.Questions = chosen;
                Console.WriteLine($"[WaitForQuestionsAsync] Loaded {chosen.Count} questions for topic '{topic}' from repository.");
                return true;
            }
            else
            {
                // repo has fewer than requested: generate missing
                foreach (var q in existing) q.TimeLimit = timer;
                var needed = count - existing.Count;
                var generated = await _questionGenerationService.GenerateQuestionsAsync(needed, topic);
                foreach (var q in generated) q.TimeLimit = timer;

                var combined = existing.Concat(generated).ToList();

                if (generated.Any())
                    await _repo.SaveAsync(topic, combined);

                lobby.Match ??= new LobbyMatch();
                lobby.Match.Questions = combined;
                Console.WriteLine($"[WaitForQuestionsAsync] Combined repo ({existing.Count}) + generated ({generated?.Count() ?? 0}) = {lobby.Match.Questions.Count} for topic '{topic}'.");
                return lobby.Match.Questions.Any();
            }
        }

        // 2️⃣ No questions in repo — generate dynamically
        try
        {
            Console.WriteLine($"[WaitForQuestionsAsync] Generating {count} questions for topic '{topic}'...");

            var questions = await _questionGenerationService.GenerateQuestionsAsync(count, topic);
            if (questions == null || !questions.Any())
            {
                Console.WriteLine($"WaitForQuestionsAsync: Generation returned no questions for '{topic}'.");
                return false;
            }

            // Ensure generated questions use the lobby's timer
            foreach (var q in questions)
                q.TimeLimit = timer;

            await _repo.SaveAsync(topic, questions);
            lobby.Match ??= new LobbyMatch();
            lobby.Match.Questions = questions.ToList();

            Console.WriteLine($"WaitForQuestionsAsync: Generated and saved {lobby.Match.Questions.Count} questions for topic '{topic}'.");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"WaitForQuestionsAsync: Exception generating questions for '{topic}': {ex.Message}");
            return false;
        }
    }

    public async Task HandleDisconnect(HubCallerContext context, IHubCallerClients clients, IGroupManager groups)
    {
        var lobby = _store.FindByConnection(context.ConnectionId);
        if (lobby == null) return;
        Console.WriteLine($"Disconnecting from {lobby.LobbyId} by {context.ConnectionId}.");

        if (lobby.HostConnectionId == context.ConnectionId)
        {
            foreach (var player in lobby.Players)
            {
                await clients.Client(player.ConnectionId).SendAsync("KickedFromLobby", "Host disconnected");
                await groups.RemoveFromGroupAsync(player.ConnectionId, lobby.LobbyId);
            }
            _store.Remove(lobby.LobbyId);
            Console.WriteLine($"Lobby {lobby.LobbyId} closed (host disconnected)");
            return;
        }

        var leavingPlayer = lobby.Players.FirstOrDefault(p => p.ConnectionId == context.ConnectionId);
        if (leavingPlayer != null)
        {
            lobby.Players.Remove(leavingPlayer);
            await clients.Group(lobby.LobbyId).SendAsync("LobbyRemovePlayer", leavingPlayer.Name);
            Console.WriteLine($"Player {leavingPlayer.Name} left lobby {lobby.LobbyId}");
        }
    }

    public async Task UpdateLobbySettings(HubCallerContext context, IHubCallerClients clients, string lobbyId, LobbySettings lobbySettings)
    {
        var lobby = _store.Get(lobbyId);
        if (lobby == null)
        {
            await clients.Caller.SendAsync("Error", "Lobby not found");
            return;
        }
        if (lobby.HostConnectionId != context.ConnectionId)
        {
            await clients.Caller.SendAsync("Error", "Only the host can change settings");
            return;
        }

        // Sanitize incoming settings
        var sanitizedNumber = Math.Max(1, lobbySettings.NumberOfQuestions);
        var sanitizedTimer = Math.Max(1, lobbySettings.QuestionTimerInSeconds);
        var sanitizedTopic = (lobbySettings.Topic ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(sanitizedTopic))
        {
            await clients.Caller.SendAsync("Error", "Topic cannot be empty");
            return;
        }

        // Apply settings to lobby
        lobby.LobbySettings = new LobbySettings(
            NumberOfQuestions: sanitizedNumber,
            QuestionTimerInSeconds: sanitizedTimer,
            Topic: sanitizedTopic
        );

        // If a match already has questions loaded, update their time limits and adjust count to match requested number
        if (lobby.Match?.Questions != null && lobby.Match.Questions.Any())
        {
            // update time limits
            foreach (var q in lobby.Match.Questions)
                q.TimeLimit = sanitizedTimer;

            // trim if too many
            if (lobby.Match.Questions.Count > sanitizedNumber)
            {
                lobby.Match.Questions = lobby.Match.Questions.Take(sanitizedNumber).ToList();
            }
            // extend if too few
            else if (lobby.Match.Questions.Count < sanitizedNumber)
            {
                var needed = sanitizedNumber - lobby.Match.Questions.Count;

                // try to get additional unique questions from repo
                var repoList = (await _repo.GetByTopicAsync(sanitizedTopic)).ToList();
                var additionalFromRepo = repoList
                    .Where(r => !lobby.Match.Questions.Any(c => c.Text == r.Text))
                    .Take(needed)
                    .ToList();

                foreach (var q in additionalFromRepo)
                    q.TimeLimit = sanitizedTimer;

                lobby.Match.Questions.AddRange(additionalFromRepo);
                needed -= additionalFromRepo.Count;

                // if still need more, generate new ones
                if (needed > 0)
                {
                    var gen = await _questionGenerationService.GenerateQuestionsAsync(needed, sanitizedTopic);
                    foreach (var q in gen)
                        q.TimeLimit = sanitizedTimer;

                    if (gen.Any())
                    {
                        // persist combined repository (existing repo + newly generated)
                        var merged = repoList.Concat(gen).ToList();
                        await _repo.SaveAsync(sanitizedTopic, merged);
                        lobby.Match.Questions.AddRange(gen);
                    }
                }
            }
        }

        // Notify clients with camelCase payload as client expects
        await clients.Group(lobbyId).SendAsync("LobbySettingsUpdated", new
        {
            numberOfQuestions = sanitizedNumber,
            questionTimerInSeconds = sanitizedTimer,
            topic = sanitizedTopic
        });

        Console.WriteLine($"Settings updated in lobby {lobbyId}: questions={sanitizedNumber}, timer={sanitizedTimer}, topic='{sanitizedTopic}'");
    }

    public async Task KickPlayer(HubCallerContext context, IHubCallerClients clients, IGroupManager groups, string lobbyId, string targetPlayerName)
    {
        var lobby = _store.Get(lobbyId);
        if (lobby == null)
        {
            await clients.Caller.SendAsync("Error", "Lobby not found");
            Console.WriteLine($"KickPlayer: Lobby {lobbyId} not found (requested by {context.ConnectionId})");
            return;
        }

        if (lobby.HostConnectionId != context.ConnectionId)
        {
            await clients.Caller.SendAsync("Error", "Only the host can kick players");
            Console.WriteLine($"KickPlayer: Unauthorized kick attempt in lobby {lobbyId} by {context.ConnectionId}");
            return;
        }

        var target = lobby.Players.FirstOrDefault(p => p.Name == targetPlayerName);
        if (target == null)
        {
            await clients.Caller.SendAsync("Error", $"Player '{targetPlayerName}' not found in lobby");
            Console.WriteLine($"KickPlayer: Player '{targetPlayerName}' not found in lobby {lobbyId}");
            return;
        }

        // Remove player from lobby storage
        lobby.Players.Remove(target);

        try
        {
            // Notify the kicked client and remove from group
            await clients.Client(target.ConnectionId).SendAsync("KickedFromLobby", $"You were kicked from lobby {lobbyId} by the host.");
            await groups.RemoveFromGroupAsync(target.ConnectionId, lobbyId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"KickPlayer: Error notifying/removing kicked client {target.Name} ({target.ConnectionId}): {ex.Message}");
            // continue to inform remaining clients even if notify/removal fails
        }

        // Notify remaining clients that the player was removed
        await clients.Group(lobbyId).SendAsync("LobbyRemovePlayer", targetPlayerName);

        Console.WriteLine($"Player {targetPlayerName} kicked from lobby {lobbyId} by host {context.ConnectionId}");
    }
}
