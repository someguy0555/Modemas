using Microsoft.AspNetCore.SignalR;
using Modemas.Server.Models;
using System.Collections.Concurrent;

namespace Modemas.Server.Services
{
    public class LobbyStore
    {
        private readonly ConcurrentDictionary<string, Lobby> _lobbies = new();
        public IHubCallerClients Clients { get; private set; } = null!;

        // This is called by DI in Startup/Program
        public void SetClients(IHubCallerClients clients)
        {
            Clients = clients;
        }

        public Lobby? Get(string id) => _lobbies.TryGetValue(id, out var lobby) ? lobby : null;

        public void Add(Lobby lobby) => _lobbies[lobby.LobbyId] = lobby;

        public void Remove(string id) => _lobbies.TryRemove(id, out _);

        public Lobby? FindByConnection(string connectionId) =>
            _lobbies.Values.FirstOrDefault(l =>
                l.HostConnectionId == connectionId ||
                l.Players.Any(p => p.ConnectionId == connectionId));
    }
}
