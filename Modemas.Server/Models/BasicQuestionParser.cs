using System.Text.Json;
using System.Text.Json.Serialization;
using Modemas.Server.Interfaces;

namespace Modemas.Server.Models;

public class QuestionParser : IQuestionParser
{
    public List<Question> Parse(string llmOutput)
    {
        if (string.IsNullOrWhiteSpace(llmOutput))
            throw new InvalidOperationException("No valid JSON array found in input.");

        var lines = llmOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var allItems = new List<JsonElement>();

        foreach (var line in lines)
        {
            try
            {
                using var doc = JsonDocument.Parse(line);
                if (doc.RootElement.TryGetProperty("response", out var responseProp))
                {
                    using var responseDoc = JsonDocument.Parse(responseProp.GetString()!);
                    if (responseDoc.RootElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in responseDoc.RootElement.EnumerateArray())
                        {
                            allItems.Add(item.Clone());
                        }
                    }
                }
            }
            catch (JsonException) {}
        }

        if (allItems.Count == 0)
            throw new InvalidOperationException("No questions parsed from model output.");

        var mergedJson = JsonSerializer.Serialize(allItems);

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        options.Converters.Add(new JsonStringEnumConverter());
        options.Converters.Add(new QuestionConverter());

        try
        {
            var questions = JsonSerializer.Deserialize<List<Question>>(mergedJson, options);
            if (questions == null || questions.Count == 0)
                throw new InvalidOperationException("No questions parsed from model output.");

            return questions;
        }
        catch (JsonException ex)
        {
            Console.WriteLine("Failed JSON: " + mergedJson);
            throw new InvalidOperationException("Failed to parse LLM JSON.", ex);
        }
    }
}
