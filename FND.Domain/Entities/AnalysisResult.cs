using FND.Domain.Enum;

namespace FND.Domain.Entities;

public sealed class AnalysisResult
{
    private AnalysisResult() { }

    public AnalysisResult(Guid newsArticleId, NewsVerdict verdict, decimal confidenceScore, string explanation)
    {
        if (newsArticleId == Guid.Empty) throw new ArgumentException("News article ID is required.", nameof(newsArticleId));
        if (confidenceScore is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(confidenceScore), "Confidence must be between 0 and 1.");

        Id = Guid.NewGuid();
        NewsArticleId = newsArticleId;
        Verdict = verdict;
        ConfidenceScore = confidenceScore;
        Explanation = string.IsNullOrWhiteSpace(explanation) ? throw new ArgumentException("Explanation is required.", nameof(explanation)) : explanation.Trim();
        AnalyzedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid NewsArticleId { get; private set; }
    public NewsVerdict Verdict { get; private set; }
    public decimal ConfidenceScore { get; private set; }
    public string Explanation { get; private set; } = null!;
    public DateTime AnalyzedAtUtc { get; private set; }
}

