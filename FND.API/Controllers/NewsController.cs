using FND.Application.DTOs;
using FND.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FND.API.Controllers;

[ApiController]
[Route("api/news")]
public sealed class NewsController : ControllerBase
{
    private readonly INewsService _newsService;

    public NewsController(INewsService newsService)
    {
        _newsService = newsService;
    }

    [HttpPost("analyze")]
    [ProducesResponseType(typeof(AnalysisResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AnalysisResponse>> Analyze(
        AnalyzeNewsRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Content))
            return BadRequest("Title and content are required.");

        try
        {
            return Ok(await _newsService.AnalyzeAsync(request, cancellationToken));
        }
        catch (HttpRequestException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                "No internet connection. The fact-checking service is unavailable.");
        }
    }
}
