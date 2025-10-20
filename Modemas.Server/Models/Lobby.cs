namespace Modemas.Server.Models;

/// <summary>
/// Class is responsible for storing lobby data locally on the backend.
/// </summary>
public class Lobby
{
    public string LobbyId = "";
    public string HostConnectionId = "";
    public LobbyState State = LobbyState.Waiting;
    public LobbyMatch Match = new();
    public List<Player> Players = new();
    public LobbySettings LobbySettings = new();
};
