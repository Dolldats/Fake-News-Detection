namespace FND.Application.DTOs;

public sealed class AnalyzeNewsRequest
{
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string? Source { get; init; }
}
