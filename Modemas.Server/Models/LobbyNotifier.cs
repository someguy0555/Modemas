using Microsoft.AspNetCore.SignalR;
namespace Modemas.Server.Models;

public class LobbyNotifier
{
    private readonly IHubClients _clients;
    private readonly IGroupManager _groups;

    public LobbyNotifier(IHubClients clients, IGroupManager groups)
    {
        _clients = clients;
        _groups = groups;
    }

    public Task AddPlayerToGroup(string connectionId, string lobbyId)
        => _groups.AddToGroupAsync(connectionId, lobbyId);

    public Task NotifyLobbyCreated(string connectionId, string lobbyId)
        => _clients.Client(connectionId).SendAsync("LobbyCreated", lobbyId);

    public Task NotifyPlayerJoined(string lobbyId, string playerName)
        => _clients.Group(lobbyId).SendAsync("LobbyAddPlayer", playerName);

    public Task NotifyClient(string connectionId, string method, params object[] args)
        => _clients.Client(connectionId).SendCoreAsync(method, args);

    public Task NotifyGroup(string lobbyId, string eventName, params object[] args)
        => _clients.Group(lobbyId).SendCoreAsync(eventName, args);

    public Task NotifyError(string connectionId, string message)
        => _clients.Client(connectionId).SendAsync("Error", message);

    public Task NotifyKicked(string connectionId, string message)
        => _clients.Client(connectionId).SendAsync("KickedFromLobby", message);

    public Task NotifyLobbySettingsUpdated(string lobbyId, LobbySettings settings)
        => _clients.Group(lobbyId).SendAsync("LobbySettingsUpdated", settings);
}
