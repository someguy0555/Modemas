namespace Modemas.Server
{
    /// <summary>
    /// Class is responsible for storing lobby data locally on the backend.
    /// </summary>
    public class Lobby
    {
        public string LobbyId = "";
        public string HostConnectionId = "";
        public List<Player> Players = new();
        public LobbyState State = LobbyState.Waiting;
    }
}
