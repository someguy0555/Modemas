using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace Modemas.Server
{
    public class LobbyHub : Hub
    {
        private static readonly ConcurrentDictionary<string, Lobby> Lobbies = new();

        /// <summary>
        /// Creates a new lobby. Adds the connection calling this method as a host.
        /// </summary>
        /// <returns>A task representing the async operation.</returns>
        public async Task CreateLobby()
        {
            var lobbyId = Guid.NewGuid().ToString("N").Substring(0, 6); // short code
            var lobby = new Lobby
            {
                LobbyId = lobbyId,
                HostConnectionId = Context.ConnectionId
            };
            Lobbies[lobbyId] = lobby;

            // Console.WriteLine($"Created lobby {lobbyId}");

            await Groups.AddToGroupAsync(Context.ConnectionId, lobbyId);
            await Clients.Caller.SendAsync("LobbyCreated", lobbyId);
        }

        /// <summary>
        /// Adds a player to a lobby, if not already present.
        /// </summary>
        /// <param name="lobbyId">The ID of the lobby to join.</param>
        /// <param name="playerName">The display name of the player. Cannot have multiple repeating names in the same lobby.</param>
        /// <returns>A task representing the async operation.</returns>
        public async Task JoinLobby(string lobbyId, string playerName)
        {
            if (!Lobbies.TryGetValue(lobbyId, out var lobby))
            {
                await Clients.Caller.SendAsync("Error", "Lobby not found");
                return;
            }

            if (lobby.HostConnectionId == Context.ConnectionId)
            {
                await Clients.Caller.SendAsync("Error", "Hosts can't join their own lobby.");
                return;
            }

            if (lobby.Players.Any(p => p.ConnectionId == Context.ConnectionId))
            {
                await Clients.Caller.SendAsync("Error", "You are already part of the lobby");
                return;
            }

            if (lobby.Players.Any(p => p.Name == playerName))
            {
                await Clients.Caller.SendAsync("Error", $"A player with the name {playerName} already exists");
                return;
            }

            // Console.WriteLine($"Joined lobby {lobbyId}, as {playerName}");

            var player = new Player
            {
                Name = playerName,
                ConnectionId = Context.ConnectionId
            };

            lobby.Players.Add(player);

            await Groups.AddToGroupAsync(Context.ConnectionId, lobbyId);
            await Clients.Group(lobbyId).SendAsync("PlayerJoined", playerName, lobbyId);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            foreach (var kvp in Lobbies)
            {
                var lobby = kvp.Value;
                var player = lobby.Players.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId);

                if (player != null)
                {
                    lobby.Players.Remove(player);
                    await Clients.Group(lobby.LobbyId).SendAsync("PlayerLeft", player.Name);
                    break;
                }

                if (lobby.HostConnectionId == Context.ConnectionId)
                {
                    Lobbies.TryRemove(lobby.LobbyId, out _);
                    await Clients.Group(lobby.LobbyId).SendAsync("Error", "Host disconnected, lobby closed");
                    break;
                }
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
