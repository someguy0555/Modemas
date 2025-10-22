namespace Modemas.Server.Models;

public class QuestionTopicGroup
{
    private string _topic = string.Empty;
    private List<Question> _questions = new();

    public string Topic
    {
        get => _topic;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Topic cannot be empty.");
            _topic = value.Trim();
        }
    }

    public List<Question> Questions
    {
        get => _questions;
        set => _questions = value ?? new List<Question>();
    }
}
