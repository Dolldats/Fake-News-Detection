using FND.Application.Interfaces;
using FND.Domain.Entities;
using System.Text.Json;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using FND.Domain.Enum;

namespace FND.Application.Services;

public sealed class DetectionEngine : IDetectionEngine
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public DetectionEngine(HttpClient httpClient, string apiKey)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
    }

    public Task<AnalysisResult> AnalyzeAsync(NewsArticle article, CancellationToken cancellationToken = default)
    {
        return AnalyzeWithFactCheckAsync(article, cancellationToken);
    }

    private async Task<AnalysisResult> AnalyzeWithFactCheckAsync(NewsArticle article, CancellationToken cancellationToken)
    {
        // --- Stage 1: Google Fact Check API (strongest signal) ---
        if (!string.IsNullOrWhiteSpace(_apiKey))
        {
            var claimsToSearch = new[]
            {
                article.Title,
                ExtractFirstClaim(article.Content)
            }.Where(claim => !string.IsNullOrWhiteSpace(claim)).Distinct();

            ClaimReview? review = null;
            foreach (var claimText in claimsToSearch)
            {
                review = await SearchClaimAsync(claimText, cancellationToken);
                if (review is not null) break;
            }

            if (review is not null)
            {
                string rating = (review.TextualRating ?? string.Empty).ToLowerInvariant();
                var verdict = rating.Contains("false") || rating.Contains("fake") || rating.Contains("incorrect") || rating.Contains("misleading")
                    ? NewsVerdict.Fake
                    : rating.Contains("true") || rating.Contains("correct")
                        ? NewsVerdict.Real
                        : NewsVerdict.Uncertain;
                var confidence = verdict == NewsVerdict.Uncertain ? 0.50m : 0.85m;
                return new AnalysisResult(article.Id, verdict, confidence,
                    $"Fact-check rating: {review.TextualRating}. Source: {review.Publisher?.Name ?? "published fact-check"}.");
            }
        }

        // --- Stage 2: Content-based multi-signal analysis (fallback) ---
        return AnalyzeContent(article);
    }

    // ---------------------------------------------------------------
    //  Content-based analysis
    // ---------------------------------------------------------------

    private static AnalysisResult AnalyzeContent(NewsArticle article)
    {
        var signals = new List<(string Name, double Score, double Weight)>();
        string fullText = $"{article.Title} {article.Content}";
        string lowerText = fullText.ToLowerInvariant();
        string lowerTitle = (article.Title ?? string.Empty).ToLowerInvariant();
        string source = (article.Source ?? string.Empty).ToLowerInvariant().Trim();

        // 1. Source credibility
        var sourceScore = ScoreSourceCredibility(source);
        signals.Add(("Source credibility", sourceScore, 0.30));

        // 2. Sensationalism
        var sensationalismScore = ScoreSensationalism(fullText, lowerText, lowerTitle);
        signals.Add(("Sensationalism", sensationalismScore, 0.20));

        // 3. Writing quality
        var qualityScore = ScoreWritingQuality(fullText, article.Content);
        signals.Add(("Writing quality", qualityScore, 0.15));

        // 4. Credibility indicators
        var credibilityScore = ScoreCredibilityIndicators(lowerText);
        signals.Add(("Credibility indicators", credibilityScore, 0.20));

        // 5. Manipulation patterns
        var manipulationScore = ScoreManipulationPatterns(lowerText);
        signals.Add(("Manipulation patterns", manipulationScore, 0.15));

        // Compute weighted composite score (-1.0 to +1.0)
        double composite = signals.Sum(s => s.Score * s.Weight);

        // Map to verdict
        NewsVerdict verdict;
        if (composite >= 0.25)
            verdict = NewsVerdict.Real;
        else if (composite <= -0.25)
            verdict = NewsVerdict.Fake;
        else
            verdict = NewsVerdict.Uncertain;

        // Confidence: distance from zero, scaled into 0.40 – 0.80 range
        decimal confidence = Math.Clamp((decimal)(0.40 + Math.Abs(composite) * 0.50), 0.40m, 0.80m);

        // Build explanation
        string explanation = BuildExplanation(signals, verdict);

        return new AnalysisResult(article.Id, verdict, confidence, explanation);
    }

    // ---------------------------------------------------------------
    //  Signal 1: Source credibility
    // ---------------------------------------------------------------

    private static readonly HashSet<string> TrustedSources = new(StringComparer.OrdinalIgnoreCase)
    {
        // International wire services & major outlets
        "reuters.com", "apnews.com", "bbc.com", "bbc.co.uk",
        "nytimes.com", "washingtonpost.com", "theguardian.com",
        "cnn.com", "npr.org", "pbs.org", "cbsnews.com",
        "nbcnews.com", "abcnews.go.com", "usatoday.com",
        "economist.com", "ft.com", "bloomberg.com",
        "nature.com", "sciencemag.org", "thelancet.com",
        "aljazeera.com", "dw.com", "france24.com",
        "time.com", "politico.com", "thehill.com",
        "snopes.com", "factcheck.org", "politifact.com",
        "afp.com", "news.sky.com", "independent.co.uk",
        "axios.com", "propublica.org", "theatlantic.com"
    };

    private static readonly HashSet<string> UnreliableSources = new(StringComparer.OrdinalIgnoreCase)
    {
        "infowars.com", "naturalnews.com", "beforeitsnews.com",
        "worldnewsdailyreport.com", "yournewswire.com", "newspunch.com",
        "thegatewaypundit.com", "zerohedge.com", "globalresearch.ca",
        "dailybuzzlive.com", "empirenews.net", "huzlers.com",
        "nationalreport.net", "now8news.com", "worldtruth.tv",
        "neonnettle.com", "realfarmacy.com", "thereisnews.com",
        "abcnews.com.co", "washingtonpost.com.co"
    };

    private static double ScoreSourceCredibility(string source)
    {
        if (string.IsNullOrWhiteSpace(source)) return 0.0;

        // Strip leading "www."
        source = source.StartsWith("www.") ? source[4..] : source;

        if (TrustedSources.Contains(source)) return 1.0;
        if (UnreliableSources.Contains(source)) return -1.0;

        // Typo-squat detection: lookalikes of major domains (e.g. "abcnews.com.co")
        foreach (var trusted in TrustedSources)
        {
            if (source.Contains(trusted) && source != trusted)
                return -0.6; // looks like impersonation
        }

        return 0.0; // unknown source, neutral
    }

    // ---------------------------------------------------------------
    //  Signal 2: Sensationalism
    // ---------------------------------------------------------------

    private static readonly string[] ClickbaitPhrases =
    {
        "you won't believe", "what they don't want you to know",
        "this will blow your mind", "shocking truth", "jaw-dropping",
        "mind-blowing", "doctors hate", "one weird trick",
        "is this the end", "the real truth about", "what really happened",
        "they don't want you to see", "you need to see this",
        "this changes everything", "unbelievable", "bombshell",
        "exposed", "gone viral", "the media won't show you",
        "you have been lied to", "secret they", "banned video"
    };

    private static double ScoreSensationalism(string fullText, string lowerText, string lowerTitle)
    {
        double penalty = 0.0;

        // Count ALL-CAPS words (3+ letters)
        int capsWords = Regex.Matches(fullText, @"\b[A-Z]{3,}\b").Count;
        if (capsWords > 5) penalty -= 0.4;
        else if (capsWords > 3) penalty -= 0.2;

        // Excessive exclamation marks
        int exclamations = fullText.Count(c => c == '!');
        if (exclamations > 5) penalty -= 0.3;
        else if (exclamations > 2) penalty -= 0.15;

        // Clickbait phrases (check title and body)
        int clickbaitHits = ClickbaitPhrases.Count(phrase => lowerText.Contains(phrase));
        penalty -= Math.Min(clickbaitHits * 0.15, 0.6);

        // Title in all caps is a red flag
        if (lowerTitle.Length > 10 && lowerTitle.ToUpperInvariant() == (fullText.Length >= lowerTitle.Length ? fullText[..lowerTitle.Length] : ""))
        {
            // title was typed in all-caps
            bool titleAllCaps = lowerTitle.Length > 10
                && Regex.IsMatch(fullText[..Math.Min(fullText.Length, lowerTitle.Length + 1)], @"^[A-Z\s\d\W]+$");
            if (titleAllCaps) penalty -= 0.2;
        }

        // Clamp to [-1, 0] — sensationalism can only penalize, not boost
        return Math.Clamp(penalty, -1.0, 0.0);
    }

    // ---------------------------------------------------------------
    //  Signal 3: Writing quality
    // ---------------------------------------------------------------

    private static double ScoreWritingQuality(string fullText, string content)
    {
        double score = 0.0;

        // Very short content is suspicious
        if (content.Length < 200)
            score -= 0.4;
        else if (content.Length > 800)
            score += 0.2; // longer, more detailed articles are generally more credible

        // Excessive repeated punctuation (!!!, ???, ...)
        int repeatedPunctuation = Regex.Matches(fullText, @"[!?]{2,}|\.{4,}").Count;
        if (repeatedPunctuation > 3) score -= 0.3;
        else if (repeatedPunctuation > 1) score -= 0.15;

        // High proportion of uppercase letters (excluding normal sentence starts)
        if (fullText.Length > 50)
        {
            double capsRatio = (double)fullText.Count(char.IsUpper) / fullText.Count(char.IsLetter);
            if (capsRatio > 0.4) score -= 0.3;
        }

        // Paragraphs / line breaks suggest structured writing
        int paragraphs = fullText.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length;
        if (paragraphs >= 4) score += 0.15;

        return Math.Clamp(score, -1.0, 1.0);
    }

    // ---------------------------------------------------------------
    //  Signal 4: Credibility indicators
    // ---------------------------------------------------------------

    private static readonly string[] AttributionPhrases =
    {
        "according to", "said in a statement", "told reporters",
        "officials say", "a spokesperson", "in an interview",
        "reported by", "confirmed that", "a study published",
        "research shows", "data from", "statistics show",
        "the report states", "in a press release", "sources say",
        "experts say", "researchers found", "the study found",
        "peer-reviewed", "published in the journal"
    };

    private static double ScoreCredibilityIndicators(string lowerText)
    {
        double score = 0.0;

        // Quotation marks suggest sourced quotes
        int quoteCount = lowerText.Count(c => c == '"' || c == '\u201C' || c == '\u201D');
        if (quoteCount >= 4) score += 0.3;
        else if (quoteCount >= 2) score += 0.15;

        // Attribution phrases
        int attributionHits = AttributionPhrases.Count(phrase => lowerText.Contains(phrase));
        score += Math.Min(attributionHits * 0.1, 0.4);

        // Numeric data (dates, percentages, statistics) suggest factual reporting
        int numbers = Regex.Matches(lowerText, @"\d{2,}").Count;
        if (numbers >= 5) score += 0.2;
        else if (numbers >= 2) score += 0.1;

        return Math.Clamp(score, -1.0, 1.0);
    }

    // ---------------------------------------------------------------
    //  Signal 5: Manipulation patterns
    // ---------------------------------------------------------------

    private static readonly string[] FearOutragePhrases =
    {
        "destroy", "catastrophe", "agenda", "hoax", "cover-up",
        "cover up", "they don't want you to know", "the truth is being hidden",
        "wake up people", "open your eyes", "they are lying",
        "the government is hiding", "exposed the truth", "this is war",
        "we are being played", "puppet master", "controlled by"
    };

    private static readonly string[] ConspiracyPhrases =
    {
        "deep state", "big pharma", "mainstream media lies", "wake up",
        "sheeple", "new world order", "plandemic", "false flag",
        "crisis actor", "chemtrails", "5g causes", "microchip",
        "depopulation", "globalist", "they control", "illuminati",
        "the elites", "shadow government"
    };

    private static readonly string[] UrgencyPhrases =
    {
        "share before deleted", "spread the word", "they're hiding this",
        "share this everywhere", "going to be censored", "banned from",
        "they tried to silence", "must watch before removed",
        "share before it's too late", "this is being suppressed",
        "don't let them bury this", "repost this"
    };

    private static double ScoreManipulationPatterns(string lowerText)
    {
        double penalty = 0.0;

        int fearHits = FearOutragePhrases.Count(phrase => lowerText.Contains(phrase));
        penalty -= Math.Min(fearHits * 0.12, 0.4);

        int conspiracyHits = ConspiracyPhrases.Count(phrase => lowerText.Contains(phrase));
        penalty -= Math.Min(conspiracyHits * 0.15, 0.5);

        int urgencyHits = UrgencyPhrases.Count(phrase => lowerText.Contains(phrase));
        penalty -= Math.Min(urgencyHits * 0.15, 0.4);

        return Math.Clamp(penalty, -1.0, 0.0);
    }

    // ---------------------------------------------------------------
    //  Explanation builder
    // ---------------------------------------------------------------

    private static string BuildExplanation(List<(string Name, double Score, double Weight)> signals, NewsVerdict verdict)
    {
        var parts = new List<string>();

        parts.Add("No published fact-check was found. Verdict is based on content analysis.");

        foreach (var (name, score, _) in signals)
        {
            if (Math.Abs(score) < 0.05) continue; // skip negligible signals

            string direction = score > 0 ? "positive" : "negative";
            string strength = Math.Abs(score) switch
            {
                >= 0.7 => "Strong",
                >= 0.3 => "Moderate",
                _ => "Slight"
            };
            parts.Add($"{strength} {direction} signal from {name.ToLowerInvariant()}.");
        }

        if (verdict == NewsVerdict.Uncertain && parts.Count == 1)
            parts.Add("Insufficient signals to determine authenticity with confidence.");

        return string.Join(" ", parts);
    }

    // ---------------------------------------------------------------
    //  Fact-check helpers (unchanged)
    // ---------------------------------------------------------------

    private async Task<ClaimReview?> SearchClaimAsync(string claimText, CancellationToken cancellationToken)
    {
        var query = Uri.EscapeDataString(claimText.Trim());
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync(
                $"https://factchecktools.googleapis.com/v1alpha1/claims:search?query={query}&languageCode=en&pageSize=5&key={Uri.EscapeDataString(_apiKey)}",
                cancellationToken);
        }
        catch (HttpRequestException)
        {
            throw new HttpRequestException("The fact-checking service is unreachable.");
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode) return null;

            var payload = await response.Content.ReadFromJsonAsync<FactCheckResponse>(cancellationToken: cancellationToken);
            return payload?.Claims?.SelectMany(claim => claim.ClaimReview ?? []).FirstOrDefault();
        }
    }

    private static string ExtractFirstClaim(string content)
    {
        var sentence = content.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(value => value.Trim())
            .FirstOrDefault(value => value.Length >= 35);
        return sentence?[..Math.Min(sentence.Length, 300)] ?? content[..Math.Min(content.Length, 300)];
    }

    private sealed record FactCheckResponse(List<Claim>? Claims);
    private sealed record Claim(string? Text, List<ClaimReview>? ClaimReview);
    private sealed record ClaimReview(Publisher? Publisher, string? TextualRating, string? Url);
    private sealed record Publisher(string? Name, string? Site);
}
