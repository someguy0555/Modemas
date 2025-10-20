namespace Modemas.Server.Models;

public record LobbySettings(
    int NumberOfQuestions = 10,
    int QuestionTimerInSeconds = 10,
    string Topic = ""
);
