using FND.Domain.Entities;
using FND.Infastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FND.Infastructure.Repositories;

public sealed class NewsRepository : INewsRepository
{
    private readonly FNDDbContext _dbContext;

    public NewsRepository(FNDDbContext dbContext) => _dbContext = dbContext;

    public Task AddAsync(NewsArticle article, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _dbContext.NewsArticles.Add(article);
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<NewsArticle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _dbContext.NewsArticles.FirstOrDefaultAsync(article => article.Id == id, cancellationToken);
    }

    public Task AddAnalysisAsync(AnalysisResult result, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _dbContext.AnalysisResults.Add(result);
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<AnalysisResult?> GetAnalysisByArticleIdAsync(Guid articleId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _dbContext.AnalysisResults.FirstOrDefaultAsync(result => result.NewsArticleId == articleId, cancellationToken);
    }
}
