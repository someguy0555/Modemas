namespace Modemas.Server.Models;

/// <summary>
/// Represents an active lobby, containing its state, host, players, and match data.
/// </summary>
public class Lobby
{
    private string _lobbyId = string.Empty;
    private string _hostConnectionId = string.Empty;
    private LobbyState _state = LobbyState.Waiting;
    private LobbyMatch _match = new();
    private List<Player> _players = new();
    private LobbySettings _lobbySettings = new();

    public string LobbyId
    {
        get => _lobbyId;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Lobby ID cannot be empty.");
            _lobbyId = value.Trim();
        }
    }

    public string HostConnectionId
    {
        get => _hostConnectionId;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Host connection ID cannot be empty.");
            _hostConnectionId = value.Trim();
        }
    }

    public LobbyState State
    {
        get => _state;
        set
        {
            if (!Enum.IsDefined(typeof(LobbyState), value))
                throw new ArgumentException("Invalid lobby state.");
            _state = value;
        }
    }

    public LobbyMatch Match
    {
        get => _match;
        set => _match = value ?? throw new ArgumentNullException(nameof(Match));
    }

    public List<Player> Players
    {
        get => _players;
        set => _players = value ?? new List<Player>();
    }

    public LobbySettings LobbySettings
    {
        get => _lobbySettings;
        set => _lobbySettings = value ?? new LobbySettings();
    }

    public int TotalPlayers => Players.Count;
}
