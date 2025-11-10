using Modemas.Server.Models;

namespace Modemas.Server.Interfaces;

public interface ILobbyManager
{
    Lobby CreateLobby(string hostConnectionId);
    bool AddPlayer(Lobby lobby, string connectionId, string playerName);
    void RemovePlayer(Lobby lobby, string connectionId);
    void UpdateSettings(Lobby lobby, LobbySettings settings);
    void RemoveLobby(string lobbyId);
    Lobby? GetLobby(string lobbyId);
    Lobby? FindLobbyByConnection(string connectionId);
}
