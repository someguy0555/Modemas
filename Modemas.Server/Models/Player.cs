namespace Modemas.Server.Models;

public class Player
{
    public string Name { get; set; } = "";
    public string ConnectionId { get; set; } = "";
    public List<ScoreEntry> QuestionScores { get; set; } = new();
    public bool HasAnsweredCurrent { get; set; } = false;

    public int TotalPoints => QuestionScores.Sum(s => s.Points);
}
