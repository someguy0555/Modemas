using Modemas.Server.Models;

namespace Modemas.Server.Interfaces;

public interface IQuestionParser
{
    List<Question> Parse(string llmOutput);
}
