using Microsoft.AspNetCore.SignalR;

namespace Modemas.Server.Services
{
    public class LobbyService
    {
        private readonly LobbyStore _store;
        private readonly MatchService _matchService;

        public LobbyService(LobbyStore store, MatchService matchService)
        {
            _store = store;
            _matchService = matchService;
        }

        public async Task CreateLobby(HubCallerContext context, IHubCallerClients clients, IGroupManager groups, string hostName)
        {
            var lobbyId = Guid.NewGuid().ToString("N")[..8];
            var lobby = new Lobby
            {
                LobbyId = lobbyId,
                HostConnectionId = context.ConnectionId
            };

            _store.Add(lobby);
            await groups.AddToGroupAsync(context.ConnectionId, lobbyId);
            await clients.Caller.SendAsync("LobbyCreated", lobbyId);

            await JoinLobby(context, clients, groups, lobbyId, hostName);
            Console.WriteLine($"Lobby {lobbyId} created by {hostName}");
        }

        public async Task JoinLobby(HubCallerContext context, IHubCallerClients clients, IGroupManager groups, string lobbyId, string playerName)
        {
            var lobby = _store.Get(lobbyId);
            if (lobby == null)
            {
                await clients.Caller.SendAsync("Error", "Lobby not found");
                return;
            }

            if (lobby.Players.Any(p => p.ConnectionId == context.ConnectionId))
                return;

            if (lobby.Players.Any(p => p.Name == playerName))
            {
                await clients.Caller.SendAsync("Error", $"Name '{playerName}' already taken");
                return;
            }

            var player = new Player { Name = playerName, ConnectionId = context.ConnectionId };
            lobby.Players.Add(player);

            await groups.AddToGroupAsync(context.ConnectionId, lobbyId);
            await clients.Group(lobbyId).SendAsync("LobbyAddPlayer", playerName);
            await clients.Caller.SendAsync("LobbyJoined", lobbyId, playerName, lobby.Players.Select(p => p.Name), lobby.State);

            Console.WriteLine($"Player {playerName} joined lobby {lobbyId}");
        }

        public async Task StartVoting(IHubCallerClients clients, string lobbyId)
        {
            const int duration = 10;

            await clients.Group(lobbyId).SendAsync("VotingStarted", lobbyId, duration);
            await Task.Delay(duration * 1000);

            await clients.Group(lobbyId).SendAsync("VotingEnded", lobbyId);
            // await _matchService.StartMatch(lobbyId);
            await _matchService.StartMatch(clients, lobbyId);
        }

        public async Task AnswerQuestion(HubCallerContext context, IHubCallerClients clients, string lobbyId, int answerIndex)
        {
            var lobby = _store.Get(lobbyId);
            if (lobby == null)
            {
                await clients.Caller.SendAsync("Error", "Lobby not found");
                return;
            }

            var player = lobby.Players.FirstOrDefault(p => p.ConnectionId == context.ConnectionId);
            if (player == null)
            {
                await clients.Caller.SendAsync("Error", "You are not in this lobby");
                return;
            }

            var currentQuestion = lobby.Match.Questions.ElementAtOrDefault(lobby.Match.CurrentQuestionIndex);
            if (currentQuestion == null || answerIndex < 0 || answerIndex >= currentQuestion.Choices.Count)
            {
                await clients.Caller.SendAsync("Error", "Invalid answer");
                return;
            }

            Console.WriteLine($"Player {player.Name} answered {answerIndex} in lobby {lobbyId}");
        }

        public async Task HandleDisconnect(HubCallerContext context, IHubCallerClients clients, IGroupManager groups)
        {
            var lobby = _store.FindByConnection(context.ConnectionId);
            if (lobby == null) return;

            if (lobby.HostConnectionId == context.ConnectionId)
            {
                foreach (var player in lobby.Players)
                {
                    await clients.Client(player.ConnectionId).SendAsync("KickedFromLobby", "Host disconnected");
                    await groups.RemoveFromGroupAsync(player.ConnectionId, lobby.LobbyId);
                }
                _store.Remove(lobby.LobbyId);
                Console.WriteLine($"Lobby {lobby.LobbyId} closed (host disconnected)");
                return;
            }

            var leavingPlayer = lobby.Players.FirstOrDefault(p => p.ConnectionId == context.ConnectionId);
            if (leavingPlayer != null)
            {
                lobby.Players.Remove(leavingPlayer);
                await clients.Group(lobby.LobbyId).SendAsync("PlayerLeft", leavingPlayer.Name);
                Console.WriteLine($"Player {leavingPlayer.Name} left lobby {lobby.LobbyId}");
            }
        }

        public async Task UpdateLobbySettings(HubCallerContext context, IHubCallerClients clients, string lobbyId, int numberOfQuestions, string theme, int questionTimerInSeconds)
        {
            var lobby = _store.Get(lobbyId);
            if (lobby == null)
            {
                await clients.Caller.SendAsync("Error", "Lobby not found");
                return;
            }
            if (lobby.HostConnectionId != context.ConnectionId)
            {
                await clients.Caller.SendAsync("Error", "Only the host can change settings");
                return;
            }

            lobby.LobbySettings.NumberOfQuestions = numberOfQuestions;
            lobby.LobbySettings.QuestionTimerInSeconds = questionTimerInSeconds;
            await clients.Group(lobbyId).SendAsync("LobbySettingsUpdated", numberOfQuestions, theme, questionTimerInSeconds);

            Console.WriteLine($"Settings updated in lobby {lobbyId}");
        }
    }
}
