namespace Modemas.Server.Models;

/// <summary>
/// Represents an individual player in a lobby.
/// </summary>
public class Player
{
    private string _name = string.Empty;
    private string _connectionId = string.Empty;
    private List<ScoreEntry> _questionScores = new();

    public string Name
    {
        get => _name;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Player name cannot be empty.");
            _name = value.Trim();
        }
    }

    public string ConnectionId
    {
        get => _connectionId;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Connection ID cannot be empty.");
            _connectionId = value.Trim();
        }
    }

    public List<ScoreEntry> QuestionScores
    {
        get => _questionScores;
        set => _questionScores = value ?? new List<ScoreEntry>();
    }

    public bool HasAnsweredCurrent { get; set; }

    public int TotalPoints => QuestionScores.Sum(s => s.Points);
}
