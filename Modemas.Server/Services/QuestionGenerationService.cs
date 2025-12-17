using Modemas.Server.Models;
using Modemas.Server.Interfaces;

namespace Modemas.Server.Services;

public class QuestionGenerationService : IQuestionGenerationService
{
    private readonly HttpClient _http;
    private readonly IQuestionParser _parser;
    private readonly IQuestionRepository _repo;
    private readonly ILogger<QuestionGenerationService> _logger;
    private readonly bool _allowFallback;

    // Configurable endpoint; defaults to local dev Ollama-compatible API.
    private const string DefaultGenerationEndpoint = "http://localhost:11435/api/generate";

    public QuestionGenerationService(
        HttpClient http,
        IQuestionParser parser,
        IQuestionRepository repo,
        ILogger<QuestionGenerationService> logger,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        _http = http;
        _http.Timeout = TimeSpan.FromMinutes(10);
        _parser = parser;
        _repo = repo;
        _logger = logger;

        GenerationEndpoint = configuration["QuestionGeneration:Endpoint"] ?? DefaultGenerationEndpoint;

        // Only allow placeholder fallback in Development (and tests which use Development by default).
        _allowFallback = environment.IsDevelopment();
    }

    public string GenerationEndpoint { get; }

    /// <summary>
    /// Returns questions for the given topic.
    /// Checks the repository first, generates new ones via LLM if none found.
    /// </summary>
    public async Task<IEnumerable<Question>> GetOrGenerateQuestionsAsync(int count = 5, string topic = "general knowledge")
    {
        var existing = (await _repo.GetByTopicAsync(topic)).ToList();
        if (existing.Count > 0)
        {
            return existing;
        }

        var newQuestions = (await GenerateQuestionsAsync(count, topic)).ToList();

        await _repo.SaveAsync(topic, newQuestions);

        return newQuestions;
    }

    /// <summary>
    /// Calls the configured LLM endpoint to generate new questions.
    /// </summary>
    public async Task<IEnumerable<Question>> GenerateQuestionsAsync(int count = 5, string topic = "general knowledge")
    {
        var payload = new
        {
            model = "deepseek",
            prompt = $"Generate {count} Kahoot-style questions about {topic}. Output valid JSON only."
        };

        HttpResponseMessage response;
        try
        {
            response = await _http.PostAsJsonAsync(GenerationEndpoint, payload);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to call question generation endpoint {Endpoint}.", GenerationEndpoint);
            if (_allowFallback)
            {
                _logger.LogInformation("Using placeholder questions because environment is Development.");
                return CreatePlaceholderQuestions(count, topic);
            }

            throw new InvalidOperationException("Question generation service is currently unavailable.");
        }

        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Question generation endpoint returned non-success status {StatusCode}. Body: {Body}",
                (int)response.StatusCode,
                content);

            if (_allowFallback)
            {
                _logger.LogInformation("Using placeholder questions because environment is Development.");
                return CreatePlaceholderQuestions(count, topic);
            }

            throw new InvalidOperationException("Question generation service returned an error.");
        }

        try
        {
            var questions = _parser.Parse(content);
            if (questions.Count == 0)
            {
                if (_allowFallback)
                    return CreatePlaceholderQuestions(count, topic);

                throw new InvalidOperationException("Question generation service returned invalid data.");
            }

            return questions;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse question generation response.");

            if (_allowFallback)
            {
                _logger.LogInformation("Using placeholder questions because environment is Development.");
                return CreatePlaceholderQuestions(count, topic);
            }

            throw new InvalidOperationException("Question generation service returned invalid data.");
        }
    }

    private static List<Question> CreatePlaceholderQuestions(int count, string topic)
    {
        // Deterministic fallback so the app works even if the external generator is down.
        // Keep it simple: MultipleChoiceQuestion with 4 choices.
        var safeTopic = string.IsNullOrWhiteSpace(topic) ? "general knowledge" : topic.Trim();

        var list = new List<Question>(Math.Max(count, 1));
        for (int i = 0; i < Math.Max(count, 1); i++)
        {
            list.Add(new MultipleChoiceQuestion
            {
                Text = $"({safeTopic}) Placeholder question #{i + 1}",
                Choices = new List<string> { "A", "B", "C", "D" },
                CorrectAnswerIndex = 0,
                TimeLimit = 15,
                Points = 100
            });
        }

        return list;
    }
}
