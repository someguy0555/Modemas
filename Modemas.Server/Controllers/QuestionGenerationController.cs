using Microsoft.AspNetCore.Mvc;
using Modemas.Server.Services;

using Modemas.Server.Interfaces;

namespace Modemas.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QuestionsController : ControllerBase
{
    private readonly IQuestionRepository _repository;
    private readonly QuestionGenerationService _generator;

    public QuestionsController(IQuestionRepository repository, QuestionGenerationService generator)
    {
        _repository = repository;
        _generator = generator;
    }

    [HttpGet("topics")]
    public async Task<IActionResult> GetTopics()
    {
        var topics = await _repository.GetAllTopicsAsync();
        return Ok(topics);
    }

    [HttpGet("{topic}")]
    public async Task<IActionResult> GetByTopic(string topic)
    {
        var questions = await _repository.GetByTopicAsync(topic);
        return Ok(questions);
    }

    [HttpPost("{topic}/generate")]
    public async Task<IActionResult> GenerateTopic(string topic, [FromQuery] int count = 5)
    {
        var generated = await _generator.GenerateQuestionsAsync(topic, count);
        await _repository.SaveAsync(topic, generated);
        return Ok(generated);
    }

    [HttpDelete("{topic}")]
    public async Task<IActionResult> DeleteTopic(string topic)
    {
        await _repository.DeleteAsync(topic);
        return NoContent();
    }
}
