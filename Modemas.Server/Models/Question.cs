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
    [JsonPropertyName("text")]
    public string Text { get; set; } = "";

    [JsonPropertyName("timeLimit")]
    public int TimeLimit { get; set; } = 10;

    [JsonPropertyName("points")]
    public int Points { get; set; } = 100;

    [JsonPropertyName("type")]
    public QuestionType Type { get; set; }

    /// <summary>
    /// Returns points awarded for the given answer.
    /// Throws an exception if answer type is invalid.
    /// </summary>
    public abstract int IsCorrect(object answer);
}

public class MultipleChoiceQuestion : Question
{
    [JsonPropertyName("choices")]
    public List<string> Choices { get; set; } = new();

    [JsonPropertyName("correctAnswer")]
    public int CorrectAnswerIndex { get; set; }

    public MultipleChoiceQuestion() => Type = QuestionType.MultipleChoice;

    public override int IsCorrect(object answer)
    {
        if (answer is not int idx)
            throw new ArgumentException("Answer must be an integer index for MultipleChoiceQuestion.");

        return idx == CorrectAnswerIndex ? Points : 0;
    }
}

public class MultipleAnswerQuestion : Question
{
    [JsonPropertyName("choices")]
    public List<string> Choices { get; set; } = new();

    [JsonPropertyName("correctAnswers")]
    public List<int> CorrectAnswerIndices { get; set; } = new();

    public MultipleAnswerQuestion() => Type = QuestionType.MultipleAnswer;

    public override int IsCorrect(object answer)
    {
        if (answer is not IEnumerable<int> indices)
            throw new ArgumentException("Answer must be a collection of indices for MultipleAnswerQuestion.");

        var playerAnswers = indices.Distinct().ToList();
        int correctSelections = playerAnswers.Intersect(CorrectAnswerIndices).Count();

        return correctSelections / Choices.Count;
    }
}

public class TrueFalseQuestion : Question
{
    [JsonPropertyName("correctAnswer")]
    public bool CorrectAnswer { get; set; }

    public TrueFalseQuestion() => Type = QuestionType.TrueFalse;

    public override int IsCorrect(object answer)
    {
        if (answer is not bool b)
            throw new ArgumentException("Answer must be a boolean for TrueFalseQuestion.");

        return b == CorrectAnswer ? Points : 0;
    }
}
