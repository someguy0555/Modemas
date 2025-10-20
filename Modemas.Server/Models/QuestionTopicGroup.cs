namespace Modemas.Server.Models
{
    public class QuestionTopicGroup
    {
        public string Topic { get; set; } = string.Empty;
        public List<Question> Questions { get; set; } = new();
    }
}
