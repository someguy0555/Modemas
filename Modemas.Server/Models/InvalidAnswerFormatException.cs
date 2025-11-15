namespace Modemas.Server.Models;

public class InvalidAnswerFormatException : Exception
{
    public string QuestionType { get; }
    public object? ProvidedAnswer { get; }

    public InvalidAnswerFormatException(
        string questionType,
        object? providedAnswer,
        string message,
        Exception? inner = null)
        : base(message, inner)
    {
        QuestionType = questionType;
        ProvidedAnswer = providedAnswer;
    }
}
