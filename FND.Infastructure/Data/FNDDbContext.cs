using FND.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FND.Infastructure.Data;

public sealed class FNDDbContext : DbContext
{
    public FNDDbContext(DbContextOptions<FNDDbContext> options) : base(options) { }
    public DbSet<NewsArticle> NewsArticles => Set<NewsArticle>();
    public DbSet<AnalysisResult> AnalysisResults => Set<AnalysisResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NewsArticle>(entity =>
        {
            entity.HasKey(article => article.Id);
            entity.Property(article => article.Title).HasMaxLength(500).IsRequired();
            entity.Property(article => article.Content).IsRequired();
            entity.Property(article => article.Source).HasMaxLength(500);
        });
        modelBuilder.Entity<AnalysisResult>(entity =>
        {
            entity.HasKey(result => result.Id);
            entity.Property(result => result.Verdict).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(result => result.ConfidenceScore).HasPrecision(5, 4);
            entity.Property(result => result.Explanation).IsRequired();
            entity.HasIndex(result => result.NewsArticleId);
        });
    }
}
