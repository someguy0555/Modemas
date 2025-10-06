using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Modemas.Server.Models;

namespace Modemas.Server.Models;

public class QuestionConverter : JsonConverter<Question>
{
    public override Question Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var jsonDoc = JsonDocument.ParseValue(ref reader);
        var json = jsonDoc.RootElement.GetRawText();

        var typeProp = jsonDoc.RootElement.GetProperty("type").GetString();
        return typeProp switch
        {
            "MultipleChoice" => JsonSerializer.Deserialize<MultipleChoiceQuestion>(json, options)!,
            "MultipleAnswer" => JsonSerializer.Deserialize<MultipleAnswerQuestion>(json, options)!,
            "TrueFalse" => JsonSerializer.Deserialize<TrueFalseQuestion>(json, options)!,
            _ => throw new NotSupportedException($"Unknown question type: {typeProp}")
        };
    }

    public override void Write(Utf8JsonWriter writer, Question value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, (object)value, options);
    }
}
