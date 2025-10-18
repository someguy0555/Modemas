using Modemas.Server.Models;

namespace Modemas.Server.Interfaces;

public interface IQuestionRepository
{
    Task SaveAsync(List<Question> questions);
}
