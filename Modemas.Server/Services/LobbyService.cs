using Microsoft.AspNetCore.SignalR;
using Modemas.Server.Models;

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
    private readonly LobbyManager _manager;
    private readonly LobbyNotifier _notifier;
    private readonly MatchService _matchService;
    private readonly QuestionGenerationService _questionGenerationService;
    private readonly IQuestionRepository _repo;

    public LobbyService(
        LobbyManager manager,
        LobbyNotifier notifier,
        MatchService matchService,
        QuestionGenerationService questionGenerationService,
        IQuestionRepository repo)
    {
        _manager = manager;
        _notifier = notifier;
        _matchService = matchService;
        _questionGenerationService = questionGenerationService;
        _repo = repo;
    }

    public async Task CreateLobby(HubCallerContext context, string hostName)
    {
        var lobby = _manager.CreateLobby(context.ConnectionId);

        await _notifier.AddPlayerToGroup(context.ConnectionId, lobby.LobbyId);
        await _notifier.NotifyLobbyCreated(context.ConnectionId, lobby.LobbyId);

        await JoinLobby(context, lobby.LobbyId, hostName);
        Console.WriteLine($"Lobby {lobby.LobbyId} created by {hostName}");
    }

    public async Task JoinLobby(HubCallerContext context, string lobbyId, string playerName)
    {
        var lobby = _manager.GetLobby(lobbyId);
        if (lobby == null)
        {
            await _notifier.NotifyError(context.ConnectionId, "Lobby not found");
            return;
        }

        bool added = _manager.AddPlayer(lobby, context.ConnectionId, playerName);
        if (!added)
        {
            await _notifier.NotifyError(context.ConnectionId, "Name already taken or duplicate connection");
            return;
        }

        await _notifier.AddPlayerToGroup(context.ConnectionId, lobby.LobbyId);
        await _notifier.NotifyPlayerJoined(lobby.LobbyId, playerName);
        Console.WriteLine($"Player {playerName} joined lobby {lobbyId}");
    }

    public async Task KickPlayer(HubCallerContext context, string lobbyId, string targetPlayerName)
    {
        var lobby = _manager.GetLobby(lobbyId);
        if (lobby == null)
        {
            await _notifier.NotifyError(context.ConnectionId, "Lobby not found");
            return;
        }

        if (lobby.HostConnectionId != context.ConnectionId)
        {
            await _notifier.NotifyError(context.ConnectionId, "Only the host can kick players");
            return;
        }

        var target = lobby.Players.FirstOrDefault(p => p.Name == targetPlayerName);
        if (target == null)
        {
            await _notifier.NotifyError(context.ConnectionId, $"Player '{targetPlayerName}' not found");
            return;
        }

        _manager.RemovePlayer(lobby, target.ConnectionId);
        await _notifier.NotifyKicked(target.ConnectionId, $"You were kicked from lobby {lobbyId} by host.");
        Console.WriteLine($"Player {targetPlayerName} kicked from lobby {lobbyId}");
    }

    public async Task UpdateLobbySettings(HubCallerContext context, string lobbyId, LobbySettings settings)
    {
        var lobby = _manager.GetLobby(lobbyId);
        if (lobby == null)
        {
            await _notifier.NotifyError(context.ConnectionId, "Lobby not found");
            return;
        }

        if (lobby.HostConnectionId != context.ConnectionId)
        {
            await _notifier.NotifyError(context.ConnectionId, "Only the host can update settings");
            return;
        }

        _manager.UpdateSettings(lobby, settings);
        await _notifier.NotifyLobbySettingsUpdated(lobbyId, settings);
        Console.WriteLine($"Settings updated for lobby {lobbyId}");
    }

    public async Task<bool> WaitForQuestionsAsync(string lobbyId)
    {
        var lobby = _manager.GetLobby(lobbyId);
        if (lobby == null || string.IsNullOrWhiteSpace(lobby.LobbySettings?.Topic))
            return false;

        var topic = lobby.LobbySettings.Topic.Trim();
        var count = lobby.LobbySettings.NumberOfQuestions;

        // 1️⃣ Check repository
        var existing = (await _repo.GetByTopicAsync(topic)).ToList();
        if (existing.Any())
        {
            lobby.Match ??= new LobbyMatch();
            lobby.Match.Questions = existing;
            return true;
        }

        // 2️⃣ Generate if not in repo
        try
        {
            var questions = await _questionGenerationService.GenerateQuestionsAsync(count: count, topic: topic);
            if (!questions.Any()) return false;

            await _repo.SaveAsync(topic, questions);
            lobby.Match ??= new LobbyMatch();
            lobby.Match.Questions = questions;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task StartVoting(string lobbyId)
    {
        var lobby = _manager.GetLobby(lobbyId);
        if (lobby == null) return;

        // Notify clients
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

    public async Task HandleDisconnect(string connectionId)
    {
        var lobby = _store.FindByConnection(connectionId);
        // handle disconnect logic
    }
}
