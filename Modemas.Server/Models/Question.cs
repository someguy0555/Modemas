using System.Text.Json.Serialization;

namespace Modemas.Server.Models;

public enum QuestionType
{
    MultipleChoice,
    MultipleAnswer,
    TrueFalse
}

[JsonConverter(typeof(QuestionConverter))]
public abstract class Question
{
    public int Id { get; set; }
    private int _points = 100;

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("timeLimit")]
    public int TimeLimit { get; set; } = 10;

    [JsonPropertyName("points")]
    public int Points
    {
        get => _points;
        set
        {
            if (value < 0)
                throw new ArgumentException("Points cannot be negative.");
            _points = value;
        }
    }

    [JsonPropertyName("type")]
    public QuestionType Type { get; protected set; }

    public abstract int IsCorrect(object answer);
}

public class MultipleChoiceQuestion : Question
{
    [JsonPropertyName("choices")]
    public List<string> Choices { get; init; } = new();

    [JsonPropertyName("correctAnswer")]
    public int CorrectAnswerIndex { get; init; }

    public string this[int index] => Choices[index];

    public MultipleChoiceQuestion() => Type = QuestionType.MultipleChoice;

    public override int IsCorrect(object answer)
    {
        if (answer is not int idx)
            throw new ArgumentException("Answer must be an integer index.");
        return idx == CorrectAnswerIndex ? Points : 0;
    }
}

public class MultipleAnswerQuestion : Question
{
    [JsonPropertyName("choices")]
    public List<string> Choices { get; init; } = new();

    [JsonPropertyName("correctAnswerIndices")]
    public List<int> CorrectAnswerIndices { get; init; } = new();

    public MultipleAnswerQuestion() => Type = QuestionType.MultipleAnswer;

    public override int IsCorrect(object answer)
    {
        if (answer is not IEnumerable<int> indices)
            throw new ArgumentException("Answer must be a collection of indices.");

        var answers = indices.Distinct().ToList();
        int correct = answers.Intersect(CorrectAnswerIndices).Count();

        return CorrectAnswerIndices.Count == 0 ? 0 : (correct * Points) / CorrectAnswerIndices.Count;
    }
}

public class TrueFalseQuestion : Question
{
    [JsonPropertyName("correctAnswer")]
    public bool CorrectAnswer { get; init; }

    public TrueFalseQuestion() => Type = QuestionType.TrueFalse;

    public override int IsCorrect(object answer)
    {
        if (answer is not bool b)
            throw new ArgumentException("Answer must be a boolean.");
        return b == CorrectAnswer ? Points : 0;
    }
}
