using FND.Domain.Entities;
using FND.Domain.Enum;

namespace FND.Application.DTOs;

public sealed record AnalysisResponse(
    Guid ArticleId,
    Guid AnalysisId,
    NewsVerdict Verdict,
    decimal ConfidenceScore,
    string Explanation,
    DateTime AnalyzedAtUtc);
