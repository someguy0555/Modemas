namespace Modemas.Server
{
    /// <summary>
    /// Class is responsible for storing lobby data locally on the backend.
    /// </summary>
    public class Lobby
    {
        public string LobbyId = "";
        public string HostConnectionId = "";
        public string CreatorName = ""; // Name of the lobby's creator
        public List<Player> Players = new();
        public LobbyState State = LobbyState.Waiting;
        public List<Question> Questions = [];
        public int CurrentQuestionIndex = 0;
        
        // Customization options
        public int NumberOfQuestions = 10;
        public string Theme = "";
        public int QuestionTimer = 3; //Seconds
    }
}
