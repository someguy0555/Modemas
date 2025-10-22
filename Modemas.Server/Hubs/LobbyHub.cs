using Microsoft.AspNetCore.SignalR;
using Modemas.Server.Services;

namespace Modemas.Server.Hubs;

/// <summary>
/// SignalR Hub for managing lobbies and matches.
/// Receives requests from clients and delegates logic to LobbyService/MatchService.
/// </summary>
public class LobbyHub : Hub
{
    private readonly LobbyService _lobbyService;
    private readonly MatchService _matchService;

    public LobbyHub(LobbyService lobbyService, MatchService matchService)
    {
        _lobbyService = lobbyService;
        _matchService = matchService;
    }

    public async Task CreateLobby(string hostName)
        => await _lobbyService.CreateLobby(Context, Clients, Groups, hostName);

    public async Task JoinLobby(string lobbyId, string playerName)
        => await _lobbyService.JoinLobby(Context, Clients, Groups, lobbyId, playerName);

    public async Task StartVoting(string lobbyId)
        => await _lobbyService.StartVoting(Clients, lobbyId);

    public async Task AnswerQuestion(string lobbyId, object answer)
        => await _matchService.AnswerQuestion(Context, Clients, lobbyId, answer);

    public async Task UpdateLobbySettings(string lobbyId, int numberOfQuestions = 10, int questionTimerInSeconds = 10, string theme = "general knowledge")
        => await _lobbyService.UpdateLobbySettings(Context, Clients, lobbyId, numberOfQuestions, questionTimerInSeconds, theme);
    
    public async Task KickPlayer(string lobbyId, string targetPlayerName)
        => await _lobbyService.KickPlayer(Context, Clients, Groups, lobbyId, targetPlayerName);

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await _lobbyService.HandleDisconnect(Context, Clients, Groups);
        await base.OnDisconnectedAsync(exception);
    }
}
