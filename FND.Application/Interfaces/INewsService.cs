using FND.Application.DTOs;

namespace FND.Application.Interfaces;

public interface INewsService
{
    Task<AnalysisResponse> AnalyzeAsync(
        AnalyzeNewsRequest request,
        CancellationToken cancellationToken = default);
}
