using FND.Domain.Entities;

namespace FND.Application.Interfaces;

public interface IDetectionEngine
{
    Task<AnalysisResult> AnalyzeAsync(
        NewsArticle article,
        CancellationToken cancellationToken = default);
}
