using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Moq;
using Moq.Protected;
using Modemas.Server.Models;
using Modemas.Server.Interfaces;
using Modemas.Server.Services;

public class QuestionGenerationServiceTests
{
    private readonly Mock<IQuestionParser> _parserMock;
    private readonly Mock<IQuestionRepository> _repoMock;
    private readonly HttpClient _http;
    private readonly Mock<IHostEnvironment> _hostEnvMock;
    private readonly Mock<ILogger<QuestionGenerationService>> _loggerMock;
    private readonly IConfiguration _config;

    public QuestionGenerationServiceTests()
    {
        _parserMock = new Mock<IQuestionParser>();
        _repoMock = new Mock<IQuestionRepository>();
        _hostEnvMock = new Mock<IHostEnvironment>();
        _loggerMock = new Mock<ILogger<QuestionGenerationService>>();

        _hostEnvMock.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        var handler = new Mock<HttpMessageHandler>();

        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{ \"response\": \"[]\" }")
            });

        _http = new HttpClient(handler.Object);

        _config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["QuestionGeneration:Endpoint"] = "http://localhost:11435/api/generate"
        }).Build();
    }

    private QuestionGenerationService CreateService() =>
        new QuestionGenerationService(_http, _parserMock.Object, _repoMock.Object, _loggerMock.Object, _config, _hostEnvMock.Object);


    private QuestionGenerationService CreateService(
        HttpClient? http = null,
        bool isDevelopment = true)
    {
        _hostEnvMock.Reset();

        _hostEnvMock
            .Setup(e => e.EnvironmentName)
            .Returns(isDevelopment
                ? Environments.Development
                : Environments.Production);

        return new QuestionGenerationService(
            http ?? _http,
            _parserMock.Object,
            _repoMock.Object,
            _loggerMock.Object,
            _config,
            _hostEnvMock.Object
        );
    }

    [Fact]
    public async Task GetOrGenerateQuestionsAsync_ReturnsExistingQuestions_IfCached()
    {
        var existing = new List<Question>
        {
            new MultipleAnswerQuestion { Text = "Cached" }
        };

        _repoMock.Setup(r => r.GetByTopicAsync("math"))
            .ReturnsAsync(existing);

        var service = CreateService();

        var result = await service.GetOrGenerateQuestionsAsync(5, "math");

        Assert.Single(result);
        Assert.Equal("Cached", result.First().Text);

        _parserMock.Verify(p => p.Parse(It.IsAny<string>()), Times.Never);
        _repoMock.Verify(r => r.SaveAsync(It.IsAny<string>(), It.IsAny<IEnumerable<Question>>()), Times.Never);
    }

    [Fact]
    public async Task GetOrGenerateQuestionsAsync_GeneratesNewQuestions_WhenNoneCached()
    {
        _repoMock.Setup(r => r.GetByTopicAsync("history"))
            .ReturnsAsync(new List<Question>());

        var parsedQuestions = new List<Question>
        {
            new MultipleAnswerQuestion { Text = "Generated" }
        };

        _parserMock.Setup(p => p.Parse(It.IsAny<string>()))
            .Returns(parsedQuestions);

        _repoMock.Setup(r => r.SaveAsync("history", parsedQuestions))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var result = await service.GetOrGenerateQuestionsAsync(5, "history");

        Assert.Single(result);
        Assert.Equal("Generated", result.First().Text);

        _repoMock.Verify(r => r.SaveAsync("history", parsedQuestions), Times.Once);
    }

    [Fact]
    public async Task GenerateQuestionsAsync_SendsHttpRequest_AndParsesResponse()
    {
        var parsed = new List<Question>
        {
            new MultipleAnswerQuestion { Text = "API Question" }
        };

        _parserMock.Setup(p => p.Parse(It.IsAny<string>()))
            .Returns(parsed);

        var service = CreateService();

        var result = await service.GenerateQuestionsAsync(3, "science");

        Assert.Single(result);
        Assert.Equal("API Question", result.First().Text);

        _parserMock.Verify(p => p.Parse(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task GenerateQuestionsAsync_UsesCorrectPayload()
    {
        string? sentBody = null;

        var handler = new Mock<HttpMessageHandler>();

        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString().Contains("api/generate")
                ),
                ItExpr.IsAny<CancellationToken>()
            )
            .Callback<HttpRequestMessage, CancellationToken>(async (req, _) =>
            {
                sentBody = await req.Content!.ReadAsStringAsync();
            })
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"response\":\"[]\"}")
            });

        var httpClient = new HttpClient(handler.Object);
        var service = new QuestionGenerationService(httpClient, _parserMock.Object, _repoMock.Object, _loggerMock.Object, _config, _hostEnvMock.Object);

        _parserMock.Setup(p => p.Parse(It.IsAny<string>()))
            .Returns(new List<Question> { new MultipleAnswerQuestion { Text = "OK" } });

        await service.GenerateQuestionsAsync(7, "physics");

        Assert.NotNull(sentBody);

        Assert.Contains("\"model\":\"deepseek\"", sentBody);
        Assert.Contains("Generate 7 Kahoot-style questions about physics", sentBody);
        Assert.Contains("Output valid JSON only.", sentBody);
    }

    [Fact]
    public async Task GenerateQuestionsAsync_HttpException_UsesFallback_InDevelopment()
    {
        var handler = new Mock<HttpMessageHandler>();

        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection failed"));

        var httpClient = new HttpClient(handler.Object);
        var service = CreateService(httpClient, isDevelopment: true);

        var result = await service.GenerateQuestionsAsync(3, "biology");

        Assert.Equal(3, result.Count());
        Assert.All(result, q =>
            Assert.Contains("biology", q.Text));
    }

    [Fact]
    public async Task GenerateQuestionsAsync_HttpException_Throws_InProduction()
    {
        var handler = new Mock<HttpMessageHandler>();

        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException());

        var httpClient = new HttpClient(handler.Object);
        var service = CreateService(httpClient, isDevelopment: false);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GenerateQuestionsAsync(2, "chemistry"));
    }

    [Fact]
    public async Task GenerateQuestionsAsync_NonSuccessStatus_UsesFallback()
    {
        var handler = new Mock<HttpMessageHandler>();

        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent("LLM crashed")
            });

        var service = CreateService(new HttpClient(handler.Object));

        var result = await service.GenerateQuestionsAsync(2, "geography");

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GenerateQuestionsAsync_ParserThrows_UsesFallback()
    {
        _parserMock
            .Setup(p => p.Parse(It.IsAny<string>()))
            .Throws(new Exception("Invalid JSON"));

        var service = CreateService();

        var result = await service.GenerateQuestionsAsync(1, "art");

        Assert.Single(result);
        Assert.Contains("art", result.First().Text);
    }

    [Fact]
    public async Task GenerateQuestionsAsync_EmptyParsedResult_UsesFallback()
    {
        _parserMock
            .Setup(p => p.Parse(It.IsAny<string>()))
            .Returns(new List<Question>());

        var service = CreateService();

        var result = await service.GenerateQuestionsAsync(4, "music");

        Assert.Equal(4, result.Count());
    }

    [Fact]
    public async Task PlaceholderQuestions_AreDeterministicAndValid()
    {
        _parserMock
            .Setup(p => p.Parse(It.IsAny<string>()))
            .Returns(new List<Question>());

        var service = CreateService();

        var result = await service.GenerateQuestionsAsync(2, "logic");

        var mc = Assert.IsType<MultipleChoiceQuestion>(result.First());

        Assert.Equal(4, mc.Choices.Count);
        Assert.Equal(0, mc.CorrectAnswerIndex);
        Assert.Equal(15, mc.TimeLimit);
        Assert.Equal(100, mc.Points);
    }

    [Fact]
    public async Task GetOrGenerateQuestionsAsync_SavesGeneratedQuestions()
    {
        _repoMock.Setup(r => r.GetByTopicAsync("cs"))
            .ReturnsAsync(new List<Question>());

        var generated = new List<Question>
    {
        new MultipleChoiceQuestion { Text = "Generated Q" }
    };

        _parserMock.Setup(p => p.Parse(It.IsAny<string>()))
            .Returns(generated);

        var service = CreateService();

        await service.GetOrGenerateQuestionsAsync(1, "cs");

        _repoMock.Verify(r =>
            r.SaveAsync("cs", generated),
            Times.Once);
    }
}
