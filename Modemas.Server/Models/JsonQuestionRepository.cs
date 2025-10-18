using System.Text.Json;
using System.Text.Json.Serialization;
using Modemas.Server.Interfaces;

namespace Modemas.Server.Models;

public class JsonQuestionRepository : IQuestionRepository
{
    private readonly string _basePath = Path.Combine(AppContext.BaseDirectory, "QuestionBank");

    public JsonQuestionRepository()
    {
        Directory.CreateDirectory(_basePath);
    }

    public async Task<IEnumerable<Question>?> GetByTopicAsync(string topic)
    {
        var file = Path.Combine(_basePath, $"{topic.ToLowerInvariant()}.json");
        if (!File.Exists(file))
            return null;

        var json = await File.ReadAllTextAsync(file);
        var options = new JsonSerializerOptions
        {
            Converters = { new QuestionConverter(), new JsonStringEnumConverter() }
        };
        return JsonSerializer.Deserialize<List<Question>>(json, options);
    }

    public async Task SaveAsync(string topic, IEnumerable<Question> questions)
    {
        var file = Path.Combine(_basePath, $"{topic.ToLowerInvariant()}.json");

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new QuestionConverter(), new JsonStringEnumConverter() }
        };

        var json = JsonSerializer.Serialize(questions, options);
        await File.WriteAllTextAsync(file, json);
    }
}
