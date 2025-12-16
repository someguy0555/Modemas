using Modemas.Server.Models;

public class LobbyMatchTests
{
    [Fact]
    public void Questions_SetValidList_SetsCorrectly()
    {
        var match = new LobbyMatch();
        var questions = new List<Question> { new MultipleChoiceQuestion { Text = "Q1" } };
        match.Questions = questions;
        Assert.Equal(questions, match.Questions);
    }

    [Fact]
    public void Questions_SetNull_SetsEmptyList()
    {
        var match = new LobbyMatch();
        match.Questions = null!;
        Assert.NotNull(match.Questions);
        Assert.Empty(match.Questions);
    }

    [Fact]
    public void CurrentQuestion_ReturnsNull_WhenNoQuestions()
    {
        var match = new LobbyMatch();
        match.Questions.Clear();
        Assert.Null(match.CurrentQuestion);
    }

    [Fact]
    public void CurrentQuestion_ReturnsNull_WhenIndexOutOfRange()
    {
        var match = new LobbyMatch();
        match.Questions = new List<Question> { new MultipleChoiceQuestion { Text = "Q1" } };
        match.CurrentQuestionIndex = 5;
        Assert.Null(match.CurrentQuestion);
    }

    [Fact]
    public void CurrentQuestion_ReturnsQuestion_WhenIndexValid()
    {
        var match = new LobbyMatch();
        var question = new MultipleChoiceQuestion { Text = "Q1" };
        match.Questions = new List<Question> { question };
        match.CurrentQuestionIndex = 0;
        Assert.Equal(question, match.CurrentQuestion);
    }

    [Fact]
    public void HasNextQuestion_ReturnsFalse_WhenNoQuestions()
    {
        var match = new LobbyMatch();
        match.Questions.Clear();
        match.CurrentQuestionIndex = 0;
        Assert.False(match.HasNextQuestion);
    }

    [Fact]
    public void HasNextQuestion_ReturnsFalse_WhenOnLastQuestion()
    {
        var match = new LobbyMatch();
        match.Questions = new List<Question>
        {
            new MultipleChoiceQuestion { Text = "Q1" }
        };
        match.CurrentQuestionIndex = 0;
        Assert.False(match.HasNextQuestion);
    }

    [Fact]
    public void HasNextQuestion_ReturnsTrue_WhenMoreQuestionsAvailable()
    {
        var match = new LobbyMatch();
        match.Questions = new List<Question>
        {
            new MultipleChoiceQuestion { Text = "Q1" },
            new MultipleChoiceQuestion { Text = "Q2" }
        };
        match.CurrentQuestionIndex = 0;
        Assert.True(match.HasNextQuestion);
    }

    [Fact]
    public void CurrentQuestionIndex_SetAndGet_WorksCorrectly()
    {
        var match = new LobbyMatch();
        match.CurrentQuestionIndex = 2;
        Assert.Equal(2, match.CurrentQuestionIndex);
    }
}
