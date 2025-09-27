using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace Modemas.Server
{
    /// <summary>
    /// A SignalR Hub that is responsible for managing lobbies.
    /// <para>It's responsible for:</para>
    /// <list type="bullet">
    ///   <item><description>Creating lobbies</description></item>
    ///   <item><description>Joining lobbies</description></item>
    ///   <item><description>Tracking lobby membership</description></item>
    ///   <item><description>Cleaning up when players disconnect</description></item>
    ///   <item><description>Broadcasting updates to all players in a lobby</description></item>
    /// </list>
    /// </summary>
    public class LobbyHub : Hub
    {
        private static readonly ConcurrentDictionary<string, Lobby> Lobbies = new();

        /// <summary>
        /// Creates a new lobby. Adds the connection calling this method as a host.
        /// </summary>
        /// <returns>A task representing the async operation.</returns>
        public async Task CreateLobby()
        {
            var lobbyId = Guid.NewGuid().ToString("N").Substring(0, 8); // short code
            var lobby = new Lobby
            {
                LobbyId = lobbyId,
                HostConnectionId = Context.ConnectionId
            };
            Lobbies[lobbyId] = lobby;

            // Console.WriteLine($"Created lobby {lobbyId}");

            await Groups.AddToGroupAsync(Context.ConnectionId, lobbyId);
            await Clients.Caller.SendAsync("LobbyCreated", lobbyId, LobbyState.Waiting);
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

            var player = new Player
            {
                Name = playerName,
                ConnectionId = Context.ConnectionId
            };

            lobby.Players.Add(player);

            await Groups.AddToGroupAsync(Context.ConnectionId, lobbyId);
            await Clients.Group(lobbyId).SendAsync("LobbyAddPlayer", playerName);
            await Clients.Caller.SendAsync("LobbyJoined", lobbyId, playerName, lobby.Players.Select(p => p.Name), lobby.State);
        }

        /// <summary>
        /// Starts a the match in a lobby. Currently doesn't have much use other then setting lobbyState to LobbyState.Started.
        /// </summary>
        /// <param name="lobbyId">The ID of the lobby to join.</param>
        /// <returns>A task representing the async operation.</returns>
        public async Task StartMatch(String lobbyId)
        {
            if (!Lobbies.TryGetValue(lobbyId, out var lobby))
            {
                await Clients.Caller.SendAsync("Error", "Lobby not found");
                return;
            }

            if (lobby.State == LobbyState.Started)
            {
                await Clients.Caller.SendAsync("Error", "Cannot start a match while another is in progress.");
                return;
            }

            if (lobby.HostConnectionId != Context.ConnectionId)
            {
                await Clients.Caller.SendAsync("Error", "Only the host can start the match");
                return;
            }

            var json = System.IO.File.ReadAllText("questions.json");
            var questions = System.Text.Json.JsonSerializer.Deserialize<List<Question>>(json);

            if (questions == null)
            {
                await Clients.Caller.SendAsync("Error", "There are no questions available.");
                return;
            }

            lobby.Questions = questions;
            lobby.CurrentQuestionIndex = 0;
            lobby.State = LobbyState.Started;

            await Clients.Group(lobbyId).SendAsync("LobbyMatchStarted", lobby.State);
            await SendNextQuestion(lobby);
        }

        private async Task SendNextQuestion(Lobby lobby)
        {
            if (lobby.CurrentQuestionIndex >= lobby.Questions.Count)
            {
                lobby.State = LobbyState.Waiting;
                await Clients.Group(lobby.LobbyId).SendAsync("MatchEnded", lobby.LobbyId);
                return;
            }

            var question = lobby.Questions[lobby.CurrentQuestionIndex];
            await Clients.Group(lobby.LobbyId).SendAsync("NewQuestion", new
            {
                question.Text,
                question.Choices,
                question.TimeLimit
            });
            Console.WriteLine($"New Question: {question.Text}, {question.Choices}, {question.TimeLimit}");

            var timeLimit = question.TimeLimit > 0 ? question.TimeLimit : 15;
            await Task.Delay(timeLimit * 1000);

            await Clients.Group(lobby.LobbyId).SendAsync("QuestionTimeout", $"Timeout for question: {lobby.CurrentQuestionIndex}!");

            lobby.CurrentQuestionIndex++;
            await SendNextQuestion(lobby);
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
                    // Inform all players they are kicked
                    foreach (var p in lobby.Players.ToList())
                    {
                        await Clients.Client(p.ConnectionId).SendAsync("KickedFromLobby", "Host disconnected");
                        await Groups.RemoveFromGroupAsync(p.ConnectionId, lobby.LobbyId);
                    }
                    // Inform host too (if needed)
                    await Clients.Client(lobby.HostConnectionId).SendAsync("KickedFromLobby", "Host disconnected");
                    await Groups.RemoveFromGroupAsync(lobby.HostConnectionId, lobby.LobbyId);

                    await Clients.Group(lobby.LobbyId).SendAsync("Lobby closed", "Host disconnected, lobby closed");
                    Lobbies.TryRemove(lobby.LobbyId, out _);
                    break;
                }
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
