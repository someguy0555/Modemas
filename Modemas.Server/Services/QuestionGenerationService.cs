using Modemas.Server.Models;
using Modemas.Server.Interfaces;

namespace Modemas.Server.Services;

public class QuestionGenerationService
{
    private readonly HttpClient _http;
    private readonly IQuestionParser _parser;

    public QuestionGenerationService(HttpClient http, IQuestionParser parser)
    {
        _http = http;
        _parser = parser;
    }

    public async Task<List<Question>> GenerateQuestionsAsync(string topic, int count)
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

        return questions;
    }
}
