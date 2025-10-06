using System.Text.Json.Serialization;

namespace Modemas.Server.Models
{
    /// <summary>
    /// This is a class for storing questions.
    /// Questions are stored in Json
    /// </summary>
    public record Question
    {
        [JsonPropertyName("text")]
        public string Text { get; init; } = "";

        [JsonPropertyName("choices")]
        public List<string> Choices { get; init; } = new();

        [JsonPropertyName("correctAnswer")]
        public int CorrectAnswer { get; init; } = 0;

        [JsonPropertyName("timeLimit")]
        public int TimeLimit { get; init; } = 10;
    }
}
