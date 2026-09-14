using FND.Application.DTOs;
using FND.Application.Interfaces;
using FND.Domain.Entities;
using FND.Infastructure.Repositories;

namespace FND.Application.Services;

public sealed class NewsService : INewsService
{
    private readonly IDetectionEngine _detectionEngine;
    private readonly INewsRepository _newsRepository;

    public NewsService(IDetectionEngine detectionEngine, INewsRepository newsRepository)
    {
        _detectionEngine = detectionEngine;
        _newsRepository = newsRepository;
    }

    public async Task<AnalysisResponse> AnalyzeAsync(AnalyzeNewsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var article = new NewsArticle(request.Title, request.Content, request.Source);
        var result = await _detectionEngine.AnalyzeAsync(article, cancellationToken);
        await _newsRepository.AddAsync(article, cancellationToken);
        await _newsRepository.AddAnalysisAsync(result, cancellationToken);

        return new AnalysisResponse(article.Id, result.Id, result.Verdict, result.ConfidenceScore, result.Explanation, result.AnalyzedAtUtc);
    }
}
