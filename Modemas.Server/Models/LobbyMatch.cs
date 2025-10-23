namespace Modemas.Server.Models;

/// <summary>
/// Represents a single match session inside a lobby.
/// </summary>
public class LobbyMatch
{
    private List<Question> _questions = new();
    private int _currentQuestionIndex = 0;

    public List<Question> Questions
    {
        get => _questions;
        set => _questions = value ?? new List<Question>();
    }

    public int CurrentQuestionIndex
    {
        get => _currentQuestionIndex;
        set => _currentQuestionIndex = value;
    }

    public Question? CurrentQuestion => 
        Questions.Count > 0 && CurrentQuestionIndex < Questions.Count ? Questions[CurrentQuestionIndex] : null;

    public bool HasNextQuestion => CurrentQuestionIndex + 1 < Questions.Count;
}
