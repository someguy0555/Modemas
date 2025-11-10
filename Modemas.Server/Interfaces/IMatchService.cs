namespace Modemas.Server.Interfaces;

public interface IMatchService
{
    Task StartMatch(string lobbyId);
    Task AnswerQuestion(string connectionId, string lobbyId, object answer);
}
