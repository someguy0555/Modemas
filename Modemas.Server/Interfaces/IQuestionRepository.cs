using Modemas.Server.Models;

namespace Modemas.Server.Interfaces;

public interface IQuestionRepository
{
    Task<IEnumerable<Question>> GetByTopicAsync(string topic);
    Task SaveAsync(string topic, IEnumerable<Question> questions);
    Task<IEnumerable<string>> GetAllTopicsAsync();
    Task DeleteAsync(string topic);
}
