namespace FND.Domain.Entities;

public sealed class NewsArticle
{
    private NewsArticle() { }

    public NewsArticle(string title, string content, string? source = null)
    {
        Id = Guid.NewGuid();
        Title = string.IsNullOrWhiteSpace(title) ? throw new ArgumentException("Title is required.", nameof(title)) : title.Trim();
        Content = string.IsNullOrWhiteSpace(content) ? throw new ArgumentException("Content is required.", nameof(content)) : content.Trim();
        Source = source?.Trim();
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public string Title { get; private set; } = null!;
    public string Content { get; private set; } = null!;
    public string? Source { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
}
