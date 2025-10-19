using System.Text.Json;
using System.Text.Json.Serialization;
using Modemas.Server.Models;

using Modemas.Server.Interfaces;

namespace Modemas.Server.Services
{
    public class JsonQuestionRepository : IQuestionRepository
    {
        private const string FilePath = "questions.json";

        private readonly JsonSerializerOptions _options = new()
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter(), new QuestionConverter() }
        };

        private async Task<List<QuestionTopicGroup>> LoadAllAsync()
        {
            if (!File.Exists(FilePath))
                return new List<QuestionTopicGroup>();

            var json = await File.ReadAllTextAsync(FilePath);
            if (string.IsNullOrWhiteSpace(json))
                return new List<QuestionTopicGroup>();

            return JsonSerializer.Deserialize<List<QuestionTopicGroup>>(json, _options)
                ?? new List<QuestionTopicGroup>();
        }

        private async Task SaveAllAsync(List<QuestionTopicGroup> groups)
        {
            var json = JsonSerializer.Serialize(groups, _options);
            await File.WriteAllTextAsync(FilePath, json);
        }

        public async Task<IEnumerable<Question>> GetByTopicAsync(string topic)
        {
            var all = await LoadAllAsync();
            return all.FirstOrDefault(g =>
                string.Equals(g.Topic, topic, StringComparison.OrdinalIgnoreCase)
            )?.Questions ?? new List<Question>();
        }

        public async Task SaveAsync(string topic, IEnumerable<Question> questions)
        {
            var all = await LoadAllAsync();
            var existing = all.FirstOrDefault(g =>
                string.Equals(g.Topic, topic, StringComparison.OrdinalIgnoreCase)
            );

            if (existing != null)
            {
                existing.Questions = questions.ToList();
            }
            else
            {
                all.Add(new QuestionTopicGroup
                {
                    Topic = topic,
                    Questions = questions.ToList()
                });
            }

            await SaveAllAsync(all);
        }

        public async Task<IEnumerable<string>> GetAllTopicsAsync()
        {
            var all = await LoadAllAsync();
            return all.Select(g => g.Topic).Distinct(StringComparer.OrdinalIgnoreCase);
        }

        public async Task DeleteAsync(string topic)
        {
            var all = await LoadAllAsync();
            var newList = all
                .Where(g => !string.Equals(g.Topic, topic, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (newList.Count != all.Count)
            {
                await SaveAllAsync(newList);
            }
        }
    }
}
