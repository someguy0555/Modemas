using Microsoft.AspNetCore.SignalR;
using Modemas.Server.Models;
using System.Collections;
using System.Collections.Concurrent;

namespace Modemas.Server.Services;

/// <summary>
/// Singleton service holding all active lobbies in memory.
/// Provides thread-safe access to lobby data.
/// </summary>
public class LobbyStore : IEnumerable<Lobby>
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

    // public void Add(Lobby lobby)
    // {
    //     Console.WriteLine($"[LobbyStore.Add] Added");
    //     _lobbies[lobby.LobbyId] = lobby;
    // }

    public void Remove(string id) =>
        _lobbies.TryRemove(id, out _);

    // public Lobby? FindByConnection(string connectionId) =>
    //     _lobbies.Values.FirstOrDefault(l =>
    //         l.HostConnectionId == connectionId ||
    //         l.Players.Any(p => p.ConnectionId == connectionId));

    public Lobby? FindByConnection(string connectionId)
    {
        // Console.WriteLine("=== Current Lobbies ===");
        // foreach (var lobby in _lobbies.Values)
        // {
        //     Console.WriteLine($"Lobby ID: {lobby.LobbyId}, Host: {lobby.HostConnectionId}, Players Count: {lobby.Players.Count}");
        //     foreach (var player in lobby.Players)
        //     {
        //         Console.WriteLine($"  Player: {player.Name}, ConnectionId: {player.ConnectionId}");
        //     }
        // }
        // Console.WriteLine("=======================");

        var lobbyFound = _lobbies.Values.FirstOrDefault(l =>
            l.HostConnectionId == connectionId ||
            l.Players.Any(p => p.ConnectionId == connectionId));

        // if (lobbyFound != null)
        // {
        //     Console.WriteLine($"[FindByConnection] Found lobby {lobbyFound.LobbyId} for connection {connectionId}");
        // }
        // else
        // {
        //     Console.WriteLine($"[FindByConnection] No lobby found for connection {connectionId}");
        // }

        return lobbyFound;
    }

    public IEnumerator<Lobby> GetEnumerator() => _lobbies.Values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
