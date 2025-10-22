namespace Modemas.Server.Models;

public record LobbySettings(
    int NumberOfQuestions = 5,
    int QuestionTimerInSeconds = 15,
    string Topic = ""
);
