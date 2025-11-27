using System.Text.Json;
using Modemas.Server.Models;

public class QuestionTests
{
    private JsonElement Json(string raw)
        => JsonDocument.Parse(raw).RootElement;

    // ================================================================================
    // Base Question
    // ================================================================================

    private class DummyQuestion : Question<int>
    {
        public DummyQuestion() => Type = QuestionType.MultipleChoice;

        public override int ParseTyped(JsonElement json) => 123;
        public override int IsCorrect(object answer) => 0;
    }

    [Fact]
    public void Points_CannotBeNegative()
    {
        var q = new DummyQuestion();

        Assert.Throws<ArgumentException>(() => q.Points = -1);
    }

    [Fact]
    public void ParseAnswer_ForwardsToParseTyped()
    {
        var q = new DummyQuestion();
        var result = q.ParseAnswer(Json("123"));

        Assert.Equal(123, result);
    }

    // ================================================================================
    // MultipleChoiceQuestion
    // ================================================================================

    [Fact]
    public void MCQ_ParseTyped_ParsesInteger()
    {
        var q = new MultipleChoiceQuestion();
        int result = q.ParseTyped(Json("2"));

        Assert.Equal(2, result);
    }

    [Fact]
    public void MCQ_ParseTyped_ThrowsOnInvalidJson()
    {
        var q = new MultipleChoiceQuestion();

        Assert.Throws<InvalidAnswerFormatException>(() =>
            q.ParseTyped(Json("\"invalid\""))
        );
    }

    [Fact]
    public void MCQ_IsCorrect_ReturnsFullPoints()
    {
        var q = new MultipleChoiceQuestion
        {
            Points = 200,
            CorrectAnswerIndex = 1
        };

        int score = q.IsCorrect(1);

        Assert.Equal(200, score);
    }

    [Fact]
    public void MCQ_IsCorrect_ReturnsZero_WhenWrong()
    {
        var q = new MultipleChoiceQuestion { CorrectAnswerIndex = 2 };

        int score = q.IsCorrect(1);

        Assert.Equal(0, score);
    }

    [Fact]
    public void MCQ_Indexer_ReturnsChoice()
    {
        var q = new MultipleChoiceQuestion
        {
            Choices = new List<string> { "A", "B", "C" }
        };

        Assert.Equal("B", q[1]);
    }

    // ================================================================================
    // MultipleAnswerQuestion
    // ================================================================================

    [Fact]
    public void MA_ParseTyped_ParsesArray()
    {
        var q = new MultipleAnswerQuestion();

        var result = q.ParseTyped(Json("[1,2,3]"));

        Assert.Equal(new List<int> { 1, 2, 3 }, result);
    }

    [Fact]
    public void MA_ParseTyped_RemovesDuplicates()
    {
        var q = new MultipleAnswerQuestion();

        var result = q.ParseTyped(Json("[1,1,2]"));

        Assert.Equal(new List<int> { 1, 2 }, result);
    }

    [Fact]
    public void MA_ParseTyped_ThrowsOnInvalidType()
    {
        var q = new MultipleAnswerQuestion();

        Assert.Throws<InvalidAnswerFormatException>(() =>
            q.ParseTyped(Json("\"nope\""))
        );
    }

    [Fact]
    public void MA_IsCorrect_ReturnsProportionalScore()
    {
        var q = new MultipleAnswerQuestion
        {
            Points = 100,
            CorrectAnswerIndices = new() { 1, 2 }
        };

        int score = q.IsCorrect(new List<int> { 1 });

        Assert.Equal(50, score);
    }

    [Fact]
    public void MA_IsCorrect_ReturnsZero_WhenNoCorrectAnswersConfigured()
    {
        var q = new MultipleAnswerQuestion
        {
            Points = 100,
            CorrectAnswerIndices = new()
        };

        int score = q.IsCorrect(new List<int> { 1, 2 });

        Assert.Equal(0, score);
    }

    // ================================================================================
    // TrueFalseQuestion
    // ================================================================================

    [Fact]
    public void TF_ParseTyped_ParsesBoolean()
    {
        var q = new TrueFalseQuestion();

        Assert.True(q.ParseTyped(Json("true")));
        Assert.False(q.ParseTyped(Json("false")));
    }

    [Fact]
    public void TF_ParseTyped_ThrowsOnInvalidJson()
    {
        var q = new TrueFalseQuestion();

        Assert.Throws<InvalidAnswerFormatException>(() =>
            q.ParseTyped(Json("\"nope\""))
        );
    }

    [Fact]
    public void TF_IsCorrect_ReturnsPointsIfCorrect()
    {
        var q = new TrueFalseQuestion
        {
            Points = 75,
            CorrectAnswer = true
        };

        int score = q.IsCorrect(true);

        Assert.Equal(75, score);
    }

    [Fact]
    public void TF_IsCorrect_ReturnsZeroIfWrong()
    {
        var q = new TrueFalseQuestion { CorrectAnswer = false };

        int score = q.IsCorrect(true);

        Assert.Equal(0, score);
    }
}
