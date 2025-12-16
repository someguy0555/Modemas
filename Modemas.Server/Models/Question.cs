using System.Text.Json;
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
    public int TimeLimit { get; set; } = 20;

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
    public abstract object ParseAnswer(JsonElement json);
}

public abstract class Question<TAnswer> : Question
    where TAnswer : notnull
{
    /// <summary>
    /// Implemented in derived types to parse the JsonElement into TAnswer.
    /// </summary>
    public abstract TAnswer ParseTyped(JsonElement json);

    public override object ParseAnswer(JsonElement json)
        => ParseTyped(json);

    protected TAnswer EnsureType(object answer)
    {
        if (answer is TAnswer t)
            return t;

        throw new InvalidAnswerFormatException(
            Type.ToString(),
            answer,
            $"Expected answer type {typeof(TAnswer).Name} but received {answer?.GetType().Name ?? "null"}."
        );
    }
}

public class MultipleChoiceQuestion : Question<int>
{
    [JsonPropertyName("choices")]
    public List<string> Choices { get; init; } = new();

    [JsonPropertyName("correctAnswer")]
    public int CorrectAnswerIndex { get; init; }

    public string this[int index] => Choices[index];

    public MultipleChoiceQuestion() => Type = QuestionType.MultipleChoice;

    public override int ParseTyped(JsonElement json)
    {
        if (json.ValueKind == JsonValueKind.Number && json.TryGetInt32(out int value))
            return value;

        throw new InvalidAnswerFormatException(
            Type.ToString(),
            json.ToString(),
            "Multiple choice answers must be a single integer index."
        );
    }

    public override int IsCorrect(object answer)
    {
        int idx = EnsureType(answer);
        return idx == CorrectAnswerIndex ? Points : 0;
    }
}

public class MultipleAnswerQuestion : Question<List<int>>
{
    [JsonPropertyName("choices")]
    public List<string> Choices { get; init; } = new();

    [JsonPropertyName("correctAnswerIndices")]
    public List<int> CorrectAnswerIndices { get; init; } = new();

    public MultipleAnswerQuestion() => Type = QuestionType.MultipleAnswer;

    public override List<int> ParseTyped(JsonElement json)
    {
        if (json.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidAnswerFormatException(
                Type.ToString(),
                json.ToString(),
                "Multiple answer question requires an array of integers."
            );
        }

        try
        {
            return json
                .EnumerateArray()
                .Where(e => e.ValueKind == JsonValueKind.Number)
                .Select(e => e.GetInt32())
                .Distinct()
                .ToList();
        }
        catch (Exception ex)
        {
            throw new InvalidAnswerFormatException(
                Type.ToString(),
                json.ToString(),
                "Failed to parse multiple answer answer array.",
                ex
            );
        }
    }

    public override int IsCorrect(object answer)
    {
        var indices = EnsureType(answer);

        int correct = indices.Intersect(CorrectAnswerIndices).Count();

        return CorrectAnswerIndices.Count == 0
            ? 0
            : (correct * Points) / CorrectAnswerIndices.Count;
    }
}

public class TrueFalseQuestion : Question<bool>
{
    [JsonPropertyName("correctAnswer")]
    public bool CorrectAnswer { get; init; }

    public TrueFalseQuestion() => Type = QuestionType.TrueFalse;

    public override bool ParseTyped(JsonElement json)
    {
        if (json.ValueKind is JsonValueKind.True or JsonValueKind.False)
            return json.GetBoolean();

        throw new InvalidAnswerFormatException(
            Type.ToString(),
            json.ToString(),
            "True/false answer must be a boolean."
        );
    }

    public override int IsCorrect(object answer)
    {
        bool b = EnsureType(answer);
        return b == CorrectAnswer ? Points : 0;
    }
}
