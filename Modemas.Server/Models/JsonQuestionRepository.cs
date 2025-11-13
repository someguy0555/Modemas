// using System.Text.Json;
// using System.Text.Json.Serialization;
// using Modemas.Server.Models;
//
// using Modemas.Server.Interfaces;
//
// namespace Modemas.Server.Services;
//
// /// <summary>
// /// JSON-backed repository for managing question sets per topic.
// /// Supports CRUD operations via file-stream-based IO.
// /// </summary>
// public class JsonQuestionRepository : IQuestionRepository
// {
//     private const string FilePath = "questions.json";
//
//     private readonly JsonSerializerOptions _options = new()
//     {
//         WriteIndented = true,
//         Converters = { new JsonStringEnumConverter(), new QuestionConverter() }
//     };
//
//     private async Task<List<QuestionTopicGroup>> LoadAllAsync()
//     {
//         if (!File.Exists(FilePath))
//             return new List<QuestionTopicGroup>();
//
//         try
//         {
//             using var stream = new FileStream(
//                 FilePath,
//                 FileMode.Open,
//                 FileAccess.Read,
//                 FileShare.Read,
//                 bufferSize: 4096,
//                 useAsync: true
//             );
//
//             if (stream.Length == 0)
//                 return new List<QuestionTopicGroup>();
//
//             var data = await JsonSerializer.DeserializeAsync<List<QuestionTopicGroup>>(stream, _options);
//             return data ?? new List<QuestionTopicGroup>();
//         }
//         catch (JsonException ex)
//         {
//             Console.WriteLine($"[JsonQuestionRepository] JSON parse error: {ex.Message}");
//             return new List<QuestionTopicGroup>();
//         }
//         catch (IOException ex)
//         {
//             Console.WriteLine($"[JsonQuestionRepository] File read error: {ex.Message}");
//             return new List<QuestionTopicGroup>();
//         }
//     }
//
//     private async Task SaveAllAsync(List<QuestionTopicGroup> groups)
//     {
//         var tempPath = $"{FilePath}.tmp";
//
//         using (var stream = new FileStream(
//             tempPath,
//             FileMode.Create,
//             FileAccess.Write,
//             FileShare.None,
//             bufferSize: 4096,
//             useAsync: true))
//         {
//             await JsonSerializer.SerializeAsync(stream, groups, _options);
//         }
//
//         File.Copy(tempPath, FilePath, overwrite: true);
//         File.Delete(tempPath);
//     }
//
//     /// <summary>
//     /// Retrieves all questions for a specific topic.
//     /// </summary>
//     public async Task<IEnumerable<Question>> GetByTopicAsync(string topic)
//     {
//         var all = await LoadAllAsync();
//         var match = all.FirstOrDefault(g =>
//             string.Equals(g.Topic, topic, StringComparison.OrdinalIgnoreCase));
//
//         return match?.Questions ?? new List<Question>();
//     }
//
//     /// <summary>
//     /// Saves (creates or updates) a topic’s question list.
//     /// </summary>
//     public async Task SaveAsync(string topic, IEnumerable<Question> questions)
//     {
//         var all = await LoadAllAsync();
//         var existing = all.FirstOrDefault(g =>
//             string.Equals(g.Topic, topic, StringComparison.OrdinalIgnoreCase));
//
//         if (existing is not null)
//         {
//             existing.Questions = questions.ToList();
//         }
//         else
//         {
//             all.Add(new QuestionTopicGroup
//             {
//                 Topic = topic,
//                 Questions = questions.ToList()
//             });
//         }
//
//         await SaveAllAsync(all);
//     }
//
//     /// <summary>
//     /// Lists all available topic names.
//     /// </summary>
//     public async Task<IEnumerable<string>> GetAllTopicsAsync()
//     {
//         var all = await LoadAllAsync();
//         return all.Select(g => g.Topic).Distinct(StringComparer.OrdinalIgnoreCase);
//     }
//
//     /// <summary>
//     /// Deletes all questions for a specific topic.
//     /// </summary>
//     public async Task DeleteAsync(string topic)
//     {
//         var all = await LoadAllAsync();
//         var newList = all
//             .Where(g => !string.Equals(g.Topic, topic, StringComparison.OrdinalIgnoreCase))
//             .ToList();
//
//         if (newList.Count == all.Count)
//             return;
//
//         await SaveAllAsync(newList);
//     }
//
//     /// <summary>
//     /// Adds a new topic (if not already present).
//     /// </summary>
//     public async Task CreateTopicAsync(string topic)
//     {
//         var all = await LoadAllAsync();
//         if (all.Any(g => string.Equals(g.Topic, topic, StringComparison.OrdinalIgnoreCase)))
//             return;
//
//         all.Add(new QuestionTopicGroup
//         {
//             Topic = topic,
//             Questions = new List<Question>()
//         });
//
//         await SaveAllAsync(all);
//     }
//
//     /// <summary>
//     /// Renames an existing topic.
//     /// </summary>
//     public async Task<bool> RenameTopicAsync(string oldTopic, string newTopic)
//     {
//         var all = await LoadAllAsync();
//         var existing = all.FirstOrDefault(g =>
//             string.Equals(g.Topic, oldTopic, StringComparison.OrdinalIgnoreCase));
//
//         if (existing == null)
//             return false;
//
//         existing.Topic = newTopic;
//         await SaveAllAsync(all);
//         return true;
//     }
// }
