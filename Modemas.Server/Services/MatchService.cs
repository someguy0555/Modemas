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
            await clients.Group(lobby.LobbyId).SendAsync("NewQuestion", question);

            Console.WriteLine($"[RunMatchLoop] Waiting {duration}s for answers...");
            await Task.Delay(duration * 1000);

            Console.WriteLine($"[RunMatchLoop] Timeout reached for question {lobby.Match.CurrentQuestionIndex} in lobby {lobby.LobbyId}");
            await clients.Group(lobby.LobbyId).SendAsync("QuestionTimeout", $"Timeout for question {lobby.Match.CurrentQuestionIndex}");

            lobby.Match.CurrentQuestionIndex++;
        }

        Console.WriteLine($"[RunMatchLoop] Match complete for lobby {lobby.LobbyId}. Notifying clients...");
        lobby.State = LobbyState.Waiting;

        int matchEndDurationInSeconds = 10;
        var playerResults = lobby.Players
            .Select(p => new
            {
                Name = p.Name,
                Points = p.TotalPoints
            })
            .ToList();

        await clients.Group(lobby.LobbyId).SendAsync("MatchEndStarted", lobby.LobbyId, matchEndDurationInSeconds, playerResults);
        await Task.Delay(matchEndDurationInSeconds * 1000);
        await clients.Group(lobby.LobbyId).SendAsync("MatchEndEnded", lobby.LobbyId);

        Console.WriteLine($"[RunMatchLoop] Match ended in lobby {lobby.LobbyId}");
    }

    public async Task AnswerQuestion(HubCallerContext context, IHubCallerClients clients, string lobbyId, object answer)
    {
        var lobby = _store.Get(lobbyId);
        if (lobby == null)
        {
            Console.WriteLine($"AnswerQuestion: Lobby {lobbyId} not found for connection {context.ConnectionId}");
            await clients.Caller.SendAsync("Error", "Lobby not found");
            return;
        }

        var player = lobby.Players.FirstOrDefault(p => p.ConnectionId == context.ConnectionId);
        if (player == null)
        {
            Console.WriteLine($"AnswerQuestion: Player not found in lobby {lobbyId} (Connection {context.ConnectionId})");
            await clients.Caller.SendAsync("Error", "You are not in this lobby");
            return;
        }

        var question = lobby.Match.Questions.ElementAtOrDefault(lobby.Match.CurrentQuestionIndex);
        if (question == null)
        {
            Console.WriteLine($"AnswerQuestion: No active question in lobby {lobbyId}");
            await clients.Caller.SendAsync("Error", "No active question");
            return;
        }

        if (player.HasAnsweredCurrent)
        {
            Console.WriteLine($"AnswerQuestion: Player {player.Name} already answered question {lobby.Match.CurrentQuestionIndex} in lobby {lobbyId}");
            await clients.Caller.SendAsync("Error", "You already answered this question");
            return;
        }

        try
        {
            if (answer is JsonElement json)
            {
                switch (question.Type)
                {
                    case QuestionType.MultipleChoice:
                        if (json.ValueKind == JsonValueKind.Number && json.TryGetInt32(out var intVal))
                            answer = intVal;
                        break;

                    case QuestionType.MultipleAnswer:
                        if (json.ValueKind == JsonValueKind.Array)
                            answer = json.EnumerateArray()
                                         .Where(e => e.ValueKind == JsonValueKind.Number)
                                         .Select(e => e.GetInt32())
                                         .ToList();
                        break;

                    case QuestionType.TrueFalse:
                        if (json.ValueKind == JsonValueKind.True || json.ValueKind == JsonValueKind.False)
                            answer = json.GetBoolean();
                        break;
                }
            }

            int points = question.IsCorrect(answer);
            player.QuestionScores[lobby.Match.CurrentQuestionIndex] = points;
            player.HasAnsweredCurrent = true;

            bool isCorrect = points > 0;
            await clients.Caller.SendAsync("AnswerAccepted", points, isCorrect);
            Console.WriteLine($"Player {player.Name} answered question {lobby.Match.CurrentQuestionIndex} in lobby {lobbyId} earning {points} points. Correct: {isCorrect}");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"AnswerQuestion: Invalid answer from player {player.Name} in lobby {lobbyId}. Exception: {ex.Message}");
            await clients.Caller.SendAsync("Error", ex.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"AnswerQuestion: Unexpected error for player {player.Name} in lobby {lobbyId}. Exception: {ex.Message}");
            await clients.Caller.SendAsync("Error", "An unexpected error occurred while processing your answer.");
        }
    }

}
