using Microsoft.AspNetCore.Mvc;
using Modemas.Server.Services;

using Modemas.Server.Interfaces;

namespace Modemas.Server.Controllers;

[ApiController]
[Route("api/questions")]
public class QuestionGenerationController : ControllerBase
{
    private readonly QuestionGenerationService _generator;
    private readonly IQuestionRepository _repo;

    public QuestionGenerationController(QuestionGenerationService generator, IQuestionRepository repo)
    {
        _generator = generator;
        _repo = repo;
    }

    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] GenerateRequest request)
    {
        var questions = await _generator.GenerateQuestionsAsync(request.Topic, request.Count);
        await _repo.SaveAsync(questions);
        return Ok(questions);
    }
}

public record GenerateRequest(string Topic, int Count);
