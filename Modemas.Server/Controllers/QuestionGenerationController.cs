using Microsoft.AspNetCore.Mvc;
using Modemas.Server.Services;

using Modemas.Server.Interfaces;

namespace Modemas.Server.Controllers;

[ApiController]
[Route("api/questions")]
public class QuestionGenerationController : ControllerBase
{
    private readonly QuestionGenerationService _service;

    public QuestionGenerationController(QuestionGenerationService service)
    {
        _service = service;
    }

    [HttpPost("get")]
    public async Task<IActionResult> GetQuestions([FromBody] GenerateRequest request)
    {
        var questions = await _service.GetOrGenerateQuestionsAsync(request.Topic, request.Count);
        return Ok(questions);
    }
}

public record GenerateRequest(string Topic, int Count);
