using Microsoft.AspNetCore.SignalR;
using Modemas.Server.Models;

namespace Modemas.Server.Services;

public class MatchService
{
    private readonly LobbyStore _store;

    public MatchService(LobbyStore store)
    {
        _store = store;
    }

    public async Task StartMatch(IHubCallerClients clients, string lobbyId)
    {
        var lobby = _store.Get(lobbyId);
        if (lobby == null)
            return;

        if (lobby.State == LobbyState.Started)
            return;

        var json = File.ReadAllText("questions.json");
        var questions = System.Text.Json.JsonSerializer.Deserialize<List<Question>>(json) ?? new();
        lobby.Match.Questions = questions;
        lobby.Match.CurrentQuestionIndex = 0;
        lobby.State = LobbyState.Started;

        Console.WriteLine($"Match started in lobby {lobbyId}");
        await clients.Group(lobbyId).SendAsync("LobbyMatchStarted", lobbyId);

        _ = RunMatchLoop(clients, lobby);
    }

    private async Task RunMatchLoop(IHubCallerClients clients, Lobby lobby)
    {
        foreach (var question in lobby.Match.Questions)
        {
            await clients.Group(lobby.LobbyId).SendAsync("NewQuestion", question);
            await Task.Delay(question.TimeLimit * 1000);
            await clients.Group(lobby.LobbyId).SendAsync("QuestionTimeout", $"Timeout for question {question.Text}");
            lobby.Match.CurrentQuestionIndex++;
        }

        lobby.State = LobbyState.Waiting;
        await clients.Group(lobby.LobbyId).SendAsync("MatchEndStarted", lobby.LobbyId);
        await Task.Delay(10000);
        await clients.Group(lobby.LobbyId).SendAsync("MatchEndEnded", lobby.LobbyId);

        Console.WriteLine($"Match ended in lobby {lobby.LobbyId}");
    }

    private async Task RunMatchLoop(Lobby lobby)
    {
        foreach (var question in lobby.Match.Questions)
        {
            await _store.Clients.Group(lobby.LobbyId).SendAsync("NewQuestion", question);
            await Task.Delay(question.TimeLimit * 1000);
            await _store.Clients.Group(lobby.LobbyId).SendAsync("QuestionTimeout", $"Timeout for question {question.Text}");
            lobby.Match.CurrentQuestionIndex++;
        }

        lobby.State = LobbyState.Waiting;
        await _store.Clients.Group(lobby.LobbyId).SendAsync("MatchEndStarted", lobby.LobbyId);
        await Task.Delay(10000);
        await _store.Clients.Group(lobby.LobbyId).SendAsync("MatchEndEnded", lobby.LobbyId);

        Console.WriteLine($"Match ended in lobby {lobby.LobbyId}");
    }
}
