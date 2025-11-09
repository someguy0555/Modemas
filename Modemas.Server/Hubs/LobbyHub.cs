using Microsoft.AspNetCore.SignalR;

using Modemas.Server.Services;
using Modemas.Server.Models;

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
        => await _lobbyService.CreateLobby(hostName);

    public async Task JoinLobby(string lobbyId, string playerName)
        => await _lobbyService.JoinLobby(lobbyId, playerName);

    public async Task StartVoting(string lobbyId)
        => await _lobbyService.StartVoting(lobbyId);

    public async Task AnswerQuestion(string lobbyId, object answer)
        => await _matchService.AnswerQuestion(Context.ConnectionId, lobbyId, answer);

    public async Task UpdateLobbySettings(string lobbyId, LobbySettings lobbySettings)
        => await _lobbyService.UpdateLobbySettings(lobbyId, lobbySettings);

    public async Task KickPlayer(string lobbyId, string targetPlayerName)
        => await _lobbyService.KickPlayer(lobbyId, targetPlayerName);

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await _lobbyService.HandleDisconnect(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}
