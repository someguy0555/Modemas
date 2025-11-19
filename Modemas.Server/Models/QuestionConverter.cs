using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Modemas.Server.Models;

public class QuestionConverter : JsonConverter<Question>
{
    private static readonly Type BaseType = typeof(Question);
    private static readonly Lazy<Dictionary<string, Type>> TypeMap = new(() =>
        Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => BaseType.IsAssignableFrom(t) && !t.IsAbstract)
                .ToDictionary(
                    t => t.Name,
                    t => t,
                    StringComparer.OrdinalIgnoreCase)
    );

    public override Question Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var jsonDoc = JsonDocument.ParseValue(ref reader);
        var json = jsonDoc.RootElement.GetRawText();

        if (!jsonDoc.RootElement.TryGetProperty("type", out var typeProp))
            throw new JsonException("Missing 'type' property on Question JSON.");

        var typeName = typeProp.GetString() ?? throw new JsonException("Question 'type' property is null.");

        if (!TypeMap.Value.TryGetValue(typeName, out var targetType))
            throw new NotSupportedException($"Unknown question type: {typeName}");

        return (Question)JsonSerializer.Deserialize(json, targetType, options)!;
    }

    public override void Write(Utf8JsonWriter writer, Question value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, (object)value, options);
    }
}
