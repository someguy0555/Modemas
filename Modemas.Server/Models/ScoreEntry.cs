namespace Modemas.Server.Models;

/// <summary>
/// Immutable value struct representing a player's score for a single question.
/// </summary>
public readonly struct ScoreEntry
{
    public int QuestionIndex { get; init; }
    public int Points { get; init; }
    public bool IsCorrect { get; init; }

    public ScoreEntry(int questionIndex, int points, bool isCorrect)
    {
        if (questionIndex < 0)
            throw new ArgumentException("Question index cannot be negative.");
        if (points < 0)
            throw new ArgumentException("Points cannot be negative.");

        QuestionIndex = questionIndex;
        Points = points;
        IsCorrect = isCorrect;
    }
}
