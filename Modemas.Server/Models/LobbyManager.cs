using Modemas.Server.Interfaces;

namespace Modemas.Server.Models;

public class LobbyManager : ILobbyManager
{
    private readonly ILobbyStore _store;

    public LobbyManager(ILobbyStore store)
    {
        _store = store;
    }

    public Lobby CreateLobby(string hostConnectionId)
    {
        var lobbyId = Guid.NewGuid().ToString("N")[..8];
        var lobby = new Lobby
        {
            LobbyId = lobbyId,
            HostConnectionId = hostConnectionId
        };
        _store.Add(lobby);
        return lobby;
    }

    public bool AddPlayer(Lobby lobby, string connectionId, string playerName)
    {
        if (lobby.Players.Any(p => p.ConnectionId == connectionId || p.Name == playerName))
            return false;

        var player = new Player { Name = playerName, ConnectionId = connectionId };
        lobby.Players.Add(player);
        return true;
    }

    public void RemovePlayer(Lobby lobby, string connectionId)
    {
        var player = lobby.Players.FirstOrDefault(p => p.ConnectionId == connectionId);
        if (player != null)
            lobby.Players.Remove(player);
    }

    public void UpdateSettings(Lobby lobby, LobbySettings settings)
    {
        lobby.LobbySettings = settings;
    }

    public void RemoveLobby(string lobbyId)
    {
         _store.Remove(lobbyId);
    }

    public Lobby? GetLobby(string lobbyId) => _store.Get(lobbyId);

    public Lobby? FindLobbyByConnection(string connectionId) => _store.FindByConnection(connectionId);
}
