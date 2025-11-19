using System.Text.Json;
using System.Text.Json.Serialization;
using Modemas.Server.Interfaces;

namespace Modemas.Server.Models;

public class QuestionParser : IQuestionParser
{
    public List<Question> Parse(string llmOutput)
    {
        var lines = llmOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var sb = new System.Text.StringBuilder();

        foreach (var line in lines)
        {
            try
            {
                using var doc = JsonDocument.Parse(line);
                if (doc.RootElement.TryGetProperty("response", out var responseProp))
                {
                    sb.Append(responseProp.GetString());
                }
            }
            catch (JsonException) { }
        }

        var fullJson = sb.ToString();

        int start = fullJson.IndexOf('[');
        int end = fullJson.LastIndexOf(']');
        if (start < 0 || end < 0)
            throw new InvalidOperationException("No valid JSON array found in reconstructed LLM output.");

        var jsonArray = fullJson[start..(end + 1)];

        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            options.Converters.Add(new JsonStringEnumConverter());
            options.Converters.Add(new QuestionConverter());

            var questions = JsonSerializer.Deserialize<List<Question>>(jsonArray, options);

            if (questions == null || questions.Count == 0)
                throw new InvalidOperationException("No questions parsed from model output.");

            Console.WriteLine("[Parse] Everything went well.");
            return questions;
        }
        catch (JsonException ex)
        {
            Console.WriteLine("fullJson: " + fullJson);
            Console.WriteLine("jsonArray: " + jsonArray);
            throw new InvalidOperationException("Failed to parse LLM JSON.", ex);
        }
    }
}
