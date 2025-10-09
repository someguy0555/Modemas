using Microsoft.AspNetCore.SignalR;
using Modemas.Server.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Modemas.Server.Services;

/// <summary>
/// Handles match logic, including sending questions, timing, and ending matches.
/// </summary>
public class MatchService
{
    private readonly LobbyStore _store;

    public MatchService(LobbyStore store)
    {
        _store = store;
    }

    /// <summary>
    /// Starts a match for the given lobby.
    /// </summary>
    public async Task StartMatch(IHubCallerClients clients, string lobbyId)
    {
        var lobby = _store.Get(lobbyId);
        if (lobby == null || lobby.State == LobbyState.Started)
            return;

        var json = "questions.json".ReadAllFileText();

        // var options = new JsonSerializerOptions
        // {
        //     PropertyNameCaseInsensitive = true,
        //     Converters = { new QuestionConverter() },
        // };

        var options = new JsonSerializerOptions();
        options.Converters.Add(new JsonStringEnumConverter());
        var questions = JsonSerializer.Deserialize<List<Question>>(json, options) ?? new();
        // var questions = JsonSerializer.Deserialize<List<Question>>(json) ?? new();

        lobby.Match.Questions = questions;
        lobby.Match.CurrentQuestionIndex = 0;
        lobby.State = LobbyState.Started;

        // Reset players’ answers
        foreach (var p in lobby.Players)
        {
            p.HasAnsweredCurrent = false;
            p.QuestionScores.Clear();
        }

        Console.WriteLine($"Match started in lobby {lobbyId}");
        await clients.Group(lobbyId).SendAsync("LobbyMatchStarted", lobbyId);

        _ = RunMatchLoop(clients, lobby);
    }

    /// <summary>
    /// Sends each question and waits for answers.
    /// </summary>
    private async Task RunMatchLoop(IHubCallerClients clients, Lobby lobby)
    {
        Console.WriteLine($"[RunMatchLoop] Starting match for lobby {lobby.LobbyId}. Total questions: {lobby.Match.Questions.Count}");

        while (lobby.Match.CurrentQuestionIndex < lobby.Match.Questions.Count)
        {
            var question = lobby.Match.Questions[lobby.Match.CurrentQuestionIndex];
            Console.WriteLine($"[RunMatchLoop] Preparing question {lobby.Match.CurrentQuestionIndex} ({question.Type}) for lobby {lobby.LobbyId}");

            // Reset HasAnsweredCurrent for all players
            foreach (var player in lobby.Players)
            {
                player.HasAnsweredCurrent = false;
                Console.WriteLine($"[RunMatchLoop] Reset HasAnsweredCurrent for player {player.Name}");
            }

            // int duration = lobby.LobbySettings.QuestionTimerInSeconds > 0
            //                ? lobby.LobbySettings.QuestionTimerInSeconds
            //                : question.TimeLimit;
            int duration = question.TimeLimit;

            Console.WriteLine($"[RunMatchLoop] Sending question {lobby.Match.CurrentQuestionIndex} to lobby {lobby.LobbyId}: \"{question.Text}\" with timer {duration}s");
            await clients.Group(lobby.LobbyId).SendAsync("NewQuestion", question, duration);

            Console.WriteLine($"[RunMatchLoop] Waiting {duration}s for answers...");
            await Task.Delay(duration * 1000);

            Console.WriteLine($"[RunMatchLoop] Timeout reached for question {lobby.Match.CurrentQuestionIndex} in lobby {lobby.LobbyId}");
            await clients.Group(lobby.LobbyId).SendAsync("QuestionTimeout", $"Timeout for question {lobby.Match.CurrentQuestionIndex}");

            lobby.Match.CurrentQuestionIndex++;
        }

        Console.WriteLine($"[RunMatchLoop] Match complete for lobby {lobby.LobbyId}. Notifying clients...");
        lobby.State = LobbyState.Waiting;

        await clients.Group(lobby.LobbyId).SendAsync("MatchEndStarted", lobby.LobbyId);
        await Task.Delay(10000);
        await clients.Group(lobby.LobbyId).SendAsync("MatchEndEnded", lobby.LobbyId);

        Console.WriteLine($"[RunMatchLoop] Match ended in lobby {lobby.LobbyId}");
    }
}
