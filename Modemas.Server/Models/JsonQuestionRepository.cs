using System.Text.Json;
using Modemas.Server.Interfaces;

namespace Modemas.Server.Models;

public class JsonQuestionRepository : IQuestionRepository
{
    private readonly string _path = "Data/questions.json";

    public async Task SaveAsync(List<Question> questions)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        var json = JsonSerializer.Serialize(questions, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_path, json);
    }
}
