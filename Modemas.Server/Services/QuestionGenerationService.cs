using Modemas.Server.Models;
using Modemas.Server.Interfaces;

namespace Modemas.Server.Services;

public class QuestionGenerationService
{
    private readonly HttpClient _http;
    private readonly IQuestionParser _parser;
    private readonly IQuestionRepository _repo;

    public QuestionGenerationService(HttpClient http, IQuestionParser parser, IQuestionRepository repo)
    {
        _http = http;
        _parser = parser;
        _repo = repo;
    }

    /// <summary>
    /// Returns questions for the given topic.
    /// Checks the repository first, generates new ones via DeepSeek if none found.
    /// </summary>
    public async Task<IEnumerable<Question>> GetOrGenerateQuestionsAsync(int count = 5, string topic = "general knowledge")
    {
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
    public async Task<List<Question>> GenerateQuestionsAsync(int count = 5, string topic = "general knowledge")
    {
        var payload = new
        {
            model = "deepseek",
            prompt = $"Generate {count} Kahoot-style questions about {topic}. Output valid JSON only."
        };

        var response = await _http.PostAsJsonAsync("http://localhost:11435/api/generate", payload);

        response.EnsureSuccessStatusCode();
        string content = await response.Content.ReadAsStringAsync();

        var questions = _parser.Parse(content);

        return questions.ToList();
    }
}
