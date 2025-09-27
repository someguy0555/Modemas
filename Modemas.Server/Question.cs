namespace Modemas.Server
{
    /// <summary>
    /// This is a class for storing questions.
    /// </summary>
    public class Question
    {
        public string Text = "";
        public List<string> Choices = [];
        public int CorrectAnswer = 0;
        public int TimeLimit = 0;
    }
}
