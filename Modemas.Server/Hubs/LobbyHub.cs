using Microsoft.AspNetCore.SignalR;

using Modemas.Server.Interfaces;
using Modemas.Server.Models;

namespace Modemas.Server.Hubs;

/// <summary>
/// SignalR Hub for managing lobbies and matches.
/// Receives requests from clients and delegates logic to LobbyService/MatchService.
/// </summary>
public class LobbyHub : Hub
{
    private readonly ILobbyService _lobbyService;
    private readonly IMatchService _matchService;

    public LobbyHub(ILobbyService lobbyService, IMatchService matchService)
    {
        _lobbyService = lobbyService;
        _matchService = matchService;
    }

    public async Task CreateLobby(string hostName)
        => await _lobbyService.CreateLobby(Context.ConnectionId, hostName);

    public async Task JoinLobby(string lobbyId, string playerName)
        => await _lobbyService.JoinLobby(Context.ConnectionId, lobbyId, playerName);

    public async Task StartVoting(string lobbyId)
        => await _lobbyService.StartVoting(lobbyId);

    public async Task AnswerQuestion(string lobbyId, object answer)
        => await _matchService.AnswerQuestion(Context.ConnectionId, lobbyId, answer);

    public async Task UpdateLobbySettings(string lobbyId, LobbySettings settings)
        => await _lobbyService.UpdateLobbySettings(Context.ConnectionId, lobbyId, settings);

    public async Task KickPlayer(string lobbyId, string playerName)
        => await _lobbyService.KickPlayer(Context.ConnectionId, lobbyId, playerName);

    public override async Task OnDisconnectedAsync(Exception? ex)
    {
        await _lobbyService.HandleDisconnect(Context.ConnectionId);
        await base.OnDisconnectedAsync(ex);
    }
}
