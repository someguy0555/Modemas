using Modemas.Server.Models;

namespace Modemas.Server.Interfaces;

public interface ILobbyService
{
    Task CreateLobby(string connectionId, string hostName);

    Task JoinLobby(string connectionId, string lobbyId, string playerName);

    Task UpdateLobbySettings(string connectionId, string lobbyId, LobbySettings settings);

    Task KickPlayer(string connectionId, string lobbyId, string targetPlayerName);

    Task<bool> WaitForQuestionsAsync(string lobbyId);

    Task StartVoting(string lobbyId);

    Task HandleDisconnect(string connectionId);
}
