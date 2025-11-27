using Modemas.Server.Models;
using Modemas.Server.Interfaces;

namespace Modemas.Server.Services;

public class QuestionGenerationService : IQuestionGenerationService
{
    private readonly HttpClient _http;
    private readonly IQuestionParser _parser;
    private readonly IQuestionRepository _repo;

    public QuestionGenerationService(HttpClient http, IQuestionParser parser, IQuestionRepository repo)
    {
        _http = http;
        _http.Timeout = TimeSpan.FromMinutes(10);
        Console.WriteLine($"http timeout: {http.Timeout}");
        _parser = parser;
        _repo = repo;
    }

    /// <summary>
    /// Returns questions for the given topic.
    /// Checks the repository first, generates new ones via DeepSeek if none found.
    /// </summary>
    public async Task<IEnumerable<Question>> GetOrGenerateQuestionsAsync(int count = 5, string topic = "general knowledge")
    {
        Console.WriteLine("GetOrGenerate: " + topic);
        var existing = await _repo.GetByTopicAsync(topic);
        if (existing != null && existing.Any())
            return existing;

        var newQuestions = await GenerateQuestionsAsync(count, topic);

        await _repo.SaveAsync(topic, newQuestions);

        return newQuestions;
    }

    /// <summary>
    /// Calls the local DeepSeek API to generate new questions.
    /// </summary>
    public async Task<IEnumerable<Question>> GenerateQuestionsAsync(int count = 5, string topic = "general knowledge")
    {
        var payload = new
        {
            model = "deepseek",
            prompt = $"Generate {count} Kahoot-style questions about {topic}. Output valid JSON only."
        };

        Console.WriteLine($"pre-response: {payload}");
        var response = await _http.PostAsJsonAsync("http://localhost:11435/api/generate", payload);
        string content = await response.Content.ReadAsStringAsync();
        // response.EnsureSuccessStatusCode();

        Console.WriteLine($"Response content: {content}");
        var questions = _parser.Parse(content);
        Console.WriteLine($"Parsed {questions} questions successfully.");

        Console.WriteLine($"questions: {questions}");
        return questions;
    }
}
