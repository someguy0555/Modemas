using Modemas.Server.Models;

namespace Modemas.Server.Interfaces;

public interface IQuestionGenerationService
{
    Task<IEnumerable<Question>> GetOrGenerateQuestionsAsync(int count = 5, string topic = "general knowledge");
    Task<IEnumerable<Question>> GenerateQuestionsAsync(int count = 5, string topic = "general knowledge");
}
