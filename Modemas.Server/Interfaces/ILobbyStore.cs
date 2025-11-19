using Modemas.Server.Models;

namespace Modemas.Server.Interfaces;

public interface ILobbyStore : IEnumerable<Lobby>
{
    void Add(Lobby lobby);
    Lobby? Get(string lobbyId);
    void Remove(string lobbyId);
    Lobby? FindByConnection(string connectionId);
    IEnumerable<Lobby> GetAll();
}
