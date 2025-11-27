using Modemas.Server.Models;

public class QuestionParserTests
{
    private readonly QuestionParser _parser;

    public QuestionParserTests()
    {
        _parser = new QuestionParser();
    }

    [Fact]
    public void Parse_ValidSingleJsonLine_ReturnsQuestions()
    {
        string llmOutput = "{\"response\":\"[{\\\"text\\\":\\\"What is 2+2?\\\",\\\"type\\\":\\\"MultipleChoice\\\"}]\"}";

        var result = _parser.Parse(llmOutput);

        Assert.Single(result);
        Assert.Equal("What is 2+2?", result[0].Text);
    }

    [Fact]
    public void Parse_InvalidJsonLine_IgnoresLine()
    {
        string llmOutput =
            "invalid json\n" +
            "{\"response\":\"[{\\\"text\\\":\\\"ValidQ\\\",\\\"type\\\":\\\"MultipleChoice\\\"}]\"}";

        var result = _parser.Parse(llmOutput);

        Assert.Single(result);
        Assert.Equal("ValidQ", result[0].Text);
    }

    [Fact]
    public void Parse_NoArrayInResponse_ThrowsInvalidOperationException()
    {
        string llmOutput = "{\"response\":\"{\\\"Text\\\":\\\"Not an array\\\"}\"}";

        var ex = Assert.Throws<InvalidOperationException>(() => _parser.Parse(llmOutput));
        
        Assert.Contains("No valid JSON array found", ex.Message);
    }

    [Fact]
    public void Parse_EmptyArrayInResponse_ThrowsInvalidOperationException()
    {
        string llmOutput = "{\"response\":\"[]\"}";

        var ex = Assert.Throws<InvalidOperationException>(() => _parser.Parse(llmOutput));

        Assert.Contains("No questions parsed", ex.Message);
    }

    [Fact]
    public void Parse_NullOrEmptyInput_ThrowsInvalidOperationException()
    {
        string llmOutput = "";

        var ex = Assert.Throws<InvalidOperationException>(() => _parser.Parse(llmOutput));
        Assert.Contains("No valid JSON array found", ex.Message);
    }
}
