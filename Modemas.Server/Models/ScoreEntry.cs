namespace Modemas.Server.Models;

public struct ScoreEntry
{
    public int QuestionIndex { get; set; }
    public int Points { get; set; }
    public bool IsCorrect { get; set; }

    public ScoreEntry(int questionIndex, int points, bool isCorrect)
    {
        QuestionIndex = questionIndex;
        Points = points;
        IsCorrect = isCorrect;
    }
}
