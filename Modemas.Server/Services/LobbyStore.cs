using Microsoft.AspNetCore.SignalR;
using Modemas.Server.Models;
using Modemas.Server.Interfaces;
using System.Collections;
using System.Collections.Concurrent;

namespace Modemas.Server.Services;

/// <summary>
/// Singleton service holding all active lobbies in memory.
/// Provides thread-safe access to lobby data.
/// </summary>
public class LobbyStore : ILobbyStore
{
    private readonly ConcurrentDictionary<string, Lobby> _lobbies = new();
    public IHubCallerClients Clients { get; private set; } = null!;

    public void SetClients(IHubCallerClients clients)
    {
        Clients = clients;
    }

    public Lobby? Get(string id) =>
        _lobbies.TryGetValue(id, out var lobby) ? lobby : null;

    public void Add(Lobby lobby) =>
        _lobbies[lobby.LobbyId] = lobby;

    public void Remove(string id) =>
        _lobbies.TryRemove(id, out _);

    public Lobby? FindByConnection(string connectionId)
        => _lobbies.Values.FirstOrDefault(l =>
            l.HostConnectionId == connectionId ||
            l.Players.Any(p => p.ConnectionId == connectionId));

    public IEnumerator<Lobby> GetEnumerator() => _lobbies.Values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}


