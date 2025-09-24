namespace Modemas.Server
{
    /// <summary>
    /// Class is responsible for storing lobby data locally on the backend.
    /// </summary>
    public class Lobby
    {
        public string LobbyId { get; set; } = "";
        public string HostConnectionId { get; set; } = "";
        public List<Player> Players { get; set; } = new();
        public LobbyState State { get; set; } = LobbyState.Waiting;
    }
}
