using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace Modemas.Server
{
    public class LobbyHub : Hub
    {
        // Store all active lobbies (key = LobbyId)
        private static readonly ConcurrentDictionary<string, Lobby> Lobbies = new();

        // Host creates a new lobby
        public async Task CreateLobby()
        {
            var lobbyId = Guid.NewGuid().ToString("N").Substring(0, 6); // short code
            var lobby = new Lobby
            {
                LobbyId = lobbyId,
                HostConnectionId = Context.ConnectionId
            };

            Lobbies[lobbyId] = lobby;

            // Add host to group
            await Groups.AddToGroupAsync(Context.ConnectionId, lobbyId);

            // Notify host only
            await Clients.Caller.SendAsync("LobbyCreated", lobbyId);
        }

        // Player joins an existing lobby
        public async Task JoinLobby(string lobbyId, string playerName)
        {
            if (Lobbies.TryGetValue(lobbyId, out var lobby))
            {
                var player = new Player
                {
                    Name = playerName,
                    ConnectionId = Context.ConnectionId
                };

                lobby.Players.Add(player);

                // Add player to group
                await Groups.AddToGroupAsync(Context.ConnectionId, lobbyId);

                // Notify everyone in the lobby
                await Clients.Group(lobbyId).SendAsync("PlayerJoined", playerName);
            }
            else
            {
                await Clients.Caller.SendAsync("Error", "Lobby not found");
            }
        }

        // Handle player disconnection (cleanup)
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            foreach (var kvp in Lobbies)
            {
                var lobby = kvp.Value;
                var player = lobby.Players.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId);

                if (player != null)
                {
                    lobby.Players.Remove(player);

                    // Notify remaining lobby members
                    await Clients.Group(lobby.LobbyId).SendAsync("PlayerLeft", player.Name);

                    break;
                }

                // If host disconnected, close the lobby
                if (lobby.HostConnectionId == Context.ConnectionId)
                {
                    Lobbies.TryRemove(lobby.LobbyId, out _);
                    await Clients.Group(lobby.LobbyId).SendAsync("Error", "Host disconnected, lobby closed");
                    break;
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        // Host starts the game
        public async Task StartGame(string lobbyId)
        {
            if (Lobbies.TryGetValue(lobbyId, out var lobby))
            {
                lobby.State = LobbyState.Started;
                await Clients.Group(lobbyId).SendAsync("GameStarted");
            }
        }
    }
}
