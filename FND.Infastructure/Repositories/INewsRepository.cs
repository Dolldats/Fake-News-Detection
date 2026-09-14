using FND.Domain.Entities;

namespace FND.Infastructure.Repositories;

public interface INewsRepository
{
    Task AddAsync(NewsArticle article, CancellationToken cancellationToken = default);
    Task<NewsArticle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAnalysisAsync(AnalysisResult result, CancellationToken cancellationToken = default);
    Task<AnalysisResult?> GetAnalysisByArticleIdAsync(Guid articleId, CancellationToken cancellationToken = default);
}
