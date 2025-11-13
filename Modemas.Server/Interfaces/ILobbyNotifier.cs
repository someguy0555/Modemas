using Modemas.Server.Models;

namespace Modemas.Server.Interfaces;

public interface ILobbyNotifier
{
    Task AddPlayerToGroup(string connectionId, string lobbyId);
    Task NotifyLobbyCreated(string connectionId, string lobbyId);
    Task NotifyPlayerJoined(string lobbyId, string playerName);
    Task NotifyClient(string connectionId, string method, params object[] args);
    Task NotifyGroup(string lobbyId, string eventName, params object[] args);
    Task NotifyError(string connectionId, string message);
    Task NotifyKicked(string connectionId, string message);
    Task NotifyLobbySettingsUpdated(string lobbyId, LobbySettings settings);
}
