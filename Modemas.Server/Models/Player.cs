namespace Modemas.Server.Models;

public class Player
{
    public string Name = "";
    public string ConnectionId = "";
    public Dictionary<int, int> QuestionScores = new(); // Since this is a dictionary, we don't have to set it's size when the match starts >:)
    public bool HasAnsweredCurrent = false;

    public int TotalPoints => QuestionScores.Values.Sum();
}
