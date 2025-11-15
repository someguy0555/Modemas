using System.Text.Json;
using Modemas.Server.Models;
using Modemas.Server.Interfaces;

namespace Modemas.Server.Services;

/// <summary>
/// Handles match logic, including sending questions, timing, and ending matches.
/// </summary>
public class MatchService : IMatchService
{
    private readonly ILobbyStore _store;
    private readonly ILobbyNotifier _notifier;

    public MatchService(ILobbyStore store, ILobbyNotifier notifier)
    {
        _store = store;
        _notifier = notifier;
    }

    /// <summary>
    /// Starts a match for the given lobby.
    /// </summary>
    public async Task StartMatch(string lobbyId)
    {
        var lobby = _store.Get(lobbyId);
        if (lobby == null || lobby.State == LobbyState.Started) 
            return;

        var questions = lobby.Match?.Questions ?? new List<Question>();
        if (!questions.Any())
        {
            await _notifier.NotifyGroup(lobbyId, "MatchStartFailed", lobbyId, "No questions available.");
            return;
        }

        lobby.Match.CurrentQuestionIndex = 0;
        lobby.State = LobbyState.Started;

        foreach (var p in lobby.Players)
        {
            p.HasAnsweredCurrent = false;
            p.QuestionScores.Clear();
        }

        await _notifier.NotifyGroup(lobbyId, "LobbyMatchStarted", lobbyId);

        _ = RunMatchLoop(lobby);
    }

    private async Task RunMatchLoop(Lobby lobby)
    {
        while (lobby.Match.CurrentQuestionIndex < lobby.Match.Questions.Count)
        {
            var question = lobby.Match.Questions[lobby.Match.CurrentQuestionIndex];

            foreach (var player in lobby.Players)
                player.HasAnsweredCurrent = false;

            int duration = question.TimeLimit;

            await _notifier.NotifyGroup(lobby.LobbyId, "NewQuestion", question);

            await Task.Delay(duration * 1000);

            await _notifier.NotifyGroup(lobby.LobbyId, "QuestionTimeout", $"Timeout for question {lobby.Match.CurrentQuestionIndex}");

            lobby.Match.CurrentQuestionIndex++;
        }

        // Match ended
        lobby.State = LobbyState.Waiting;
        int matchEndDurationInSeconds = 10;
        var playerResults = lobby.Players
            .Select(p => new
            {
                Name = p.Name,
                TotalPoints = p.TotalPoints,
                Scores = p.QuestionScores
            })
            .ToList();

        await _notifier.NotifyGroup(lobby.LobbyId, "MatchEndStarted", lobby.LobbyId, matchEndDurationInSeconds, playerResults);
        await Task.Delay(matchEndDurationInSeconds * 1000);
        await _notifier.NotifyGroup(lobby.LobbyId, "MatchEndEnded");
    }

    public async Task AnswerQuestion(string connectionId, string lobbyId, object answer)
    {
        var lobby = _store.Get(lobbyId);
        if (lobby == null) 
        {
            await _notifier.NotifyClient(connectionId, "Error", "Lobby not found");
            return;
        }

        var player = lobby.Players.FirstOrDefault(p => p.ConnectionId == connectionId);
        if (player == null) 
        {
            await _notifier.NotifyClient(connectionId, "Error", "You are not in this lobby");
            return;
        }

        var question = lobby.Match.Questions.ElementAtOrDefault(lobby.Match.CurrentQuestionIndex);
        if (question == null) 
        {
            await _notifier.NotifyClient(connectionId, "Error", "No active question");
            return;
        }

        if (player.HasAnsweredCurrent)
        {
            await _notifier.NotifyClient(connectionId, "Error", "You already answered this question");
            return;
        }

        try
        {
            if (answer is JsonElement json)
                answer = question.ParseAnswer((JsonElement) answer);
            // if (answer is JsonElement json)
            // {
            //     switch (question.Type)
            //     {
            //         case QuestionType.MultipleChoice:
            //             if (json.ValueKind == JsonValueKind.Number && json.TryGetInt32(out var intVal))
            //                 answer = intVal;
            //             break;
            //
            //         case QuestionType.MultipleAnswer:
            //             if (json.ValueKind == JsonValueKind.Array)
            //                 answer = json.EnumerateArray()
            //                              .Where(e => e.ValueKind == JsonValueKind.Number)
            //                              .Select(e => e.GetInt32())
            //                              .ToList();
            //             break;
            //
            //         case QuestionType.TrueFalse:
            //             if (json.ValueKind == JsonValueKind.True || json.ValueKind == JsonValueKind.False)
            //                 answer = json.GetBoolean();
            //             break;
            //     }
            // }

            int points = question.IsCorrect(answer);
            bool isCorrect = points > 0;

            var entry = new ScoreEntry(lobby.Match.CurrentQuestionIndex, points, isCorrect);
            player.QuestionScores.Add(entry);
            player.HasAnsweredCurrent = true;

            await _notifier.NotifyClient(connectionId, "AnswerAccepted", entry);

        }
        catch (ArgumentException ex)
        {
            await _notifier.NotifyClient(connectionId, "Error", ex.Message);
        }
        catch (Exception)
        {
            await _notifier.NotifyClient(connectionId, "Error", "An unexpected error occurred while processing your answer.");
        }
    }
}
