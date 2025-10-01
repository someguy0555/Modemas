using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.IO;

namespace Modemas.Server
{
    public class LobbyHub : Hub
    {
        private static readonly ConcurrentDictionary<string, Lobby> Lobbies = new();
        
        public async Task CreateLobby()
        {
            var lobbyId = Guid.NewGuid().ToString("N").Substring(0, 8); // short code
            var lobby = new Lobby
            {
                LobbyId = lobbyId,
                HostConnectionId = Context.ConnectionId
            };
            Lobbies[lobbyId] = lobby;

            await Groups.AddToGroupAsync(Context.ConnectionId, lobbyId);
            await Clients.Caller.SendAsync("LobbyCreated", lobbyId, LobbyState.Waiting);
        }
        
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
        
        public async Task StartMatch(string lobbyId, string topic)
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


            var topicFile = $"{topic}.json";
            var fullPath = Path.GetFullPath(topicFile);
            Console.WriteLine($"Looking for topic file at: {fullPath}");

            if (!File.Exists(topicFile))
            {
                await Clients.Caller.SendAsync("Error", $"Topic '{topic}' not found at {fullPath}.");
                return;
            }

            var json = File.ReadAllText(topicFile);
            var questions = System.Text.Json.JsonSerializer.Deserialize<List<Question>>(json);

            if (questions == null)
            {
                await Clients.Caller.SendAsync("Error", "There are no questions available for this topic.");
                return;
            }

            lobby.Questions = questions;
            lobby.CurrentQuestionIndex = 0;
            lobby.State = LobbyState.Started;

            await Clients.Group(lobbyId).SendAsync("LobbyMatchStarted", lobby.State, topic);
            await RunMatch(lobby);
        }
        
        private async Task RunMatch(Lobby lobby)
        {
            lobby.State = LobbyState.Started;

            while (lobby.CurrentQuestionIndex < lobby.Questions.Count)
            {
                var question = lobby.Questions[lobby.CurrentQuestionIndex];
                await Clients.Group(lobby.LobbyId).SendAsync("NewQuestion", question);
                await Clients.Group(lobby.LobbyId).SendAsync("NewQuestion", new
                {
                    question.Text,
                    question.Choices,
                    question.TimeLimit
                });
                Console.WriteLine($"New Question: {question.Text}, {question.Choices}, {question.TimeLimit}");

                await Task.Delay(question.TimeLimit * 1000);

                await Clients.Group(lobby.LobbyId).SendAsync("QuestionTimeout", $"Timeout for question {lobby.CurrentQuestionIndex}!");

                lobby.CurrentQuestionIndex++;
            }

            lobby.State = LobbyState.Waiting;
            await Clients.Group(lobby.LobbyId).SendAsync("MatchEnded", lobby.LobbyId);
        }
        
        public async Task AnswerQuestion(string lobbyId, int answerIndex)
        {
            if (!Lobbies.TryGetValue(lobbyId, out var lobby))
            {
                await Clients.Caller.SendAsync("Error", "Lobby not found.");
                return;
            }

            if (lobby.State != LobbyState.Started)
            {
                await Clients.Caller.SendAsync("Error", "No active match is running.");
                return;
            }

            var player = lobby.Players.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId);
            if (player == null)
            {
                await Clients.Caller.SendAsync("Error", "You are not part of this lobby.");
                return;
            }

            // Ensure index is valid
            if (lobby.CurrentQuestionIndex >= lobby.Questions.Count)
            {
                await Clients.Caller.SendAsync("Error", "No current question to answer.");
                return;
            }

            var currentQuestion = lobby.Questions[lobby.CurrentQuestionIndex];
            if (answerIndex < 0 || answerIndex >= currentQuestion.Choices.Count)
            {
                await Clients.Caller.SendAsync("Error", "Invalid answer choice.");
                return;
            }

            // Send feedback to the client who answered
            await Clients.Caller.SendAsync("AnswerReceived", lobby.CurrentQuestionIndex, answerIndex);

            // Optionally broadcast to all clients in the lobby for demo purposes
            await Clients.Group(lobbyId).SendAsync("PlayerAnswered", player.Name, lobby.CurrentQuestionIndex, answerIndex);

            Console.WriteLine($"Player '{player.Name}' answered question {lobby.CurrentQuestionIndex} with option {answerIndex}.");
        }

        /// <summary>
        /// Called automatically when a client disconnects from the hub.
        /// <para>This method checks whether the disconnecting client is a player or host in any lobby. 
        /// If a player disconnects, they are removed from the lobby and the remaining players are notified. 
        /// If the host disconnects, all players are kicked, the lobby is closed, and the lobby is removed from the global list.</para>
        /// </summary>
        /// <param name="exception">The exception that triggered the disconnect, if any.</param>
        /// <returns>A task representing the async operation.</returns>
        /// <remarks> This method ensures proper cleanup of lobbies and notification of clients when a disconnect occurs. </remarks>
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

        /// <summary>
        /// Allows the host to update lobby customization settings.
        /// </summary>
        public async Task UpdateLobbySettings(string lobbyId, int numberOfQuestions, string theme, int questionTimer)
        {
            if (!Lobbies.TryGetValue(lobbyId, out var lobby))
            {
                await Clients.Caller.SendAsync("Error", "Lobby not found.");
                return;
            }
            if (lobby.HostConnectionId != Context.ConnectionId)
            {
                await Clients.Caller.SendAsync("Error", "Only the host can update settings.");
                return;
            }
            lobby.NumberOfQuestions = numberOfQuestions;
            lobby.Theme = theme;
            lobby.QuestionTimer = questionTimer;
            await Clients.Group(lobbyId).SendAsync("LobbySettingsUpdated", numberOfQuestions, theme, questionTimer);
        }
    }
}
