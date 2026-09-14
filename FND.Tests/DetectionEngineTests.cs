//using FND.Application.Services;
//using FND.Domain.Entities;
//using FND.Domain.Enum;

//namespace FND.Tests;

//public sealed class DetectionEngineTests
//{
//    private static DetectionEngine CreateEngine() => new(new HttpClient(), string.Empty);

//    [Fact]
//    public async Task Trusted_source_with_sourced_article_is_not_marked_fake()
//    {
//        var article = new NewsArticle("Officials announce new public health data",
//            "According to officials, researchers found data from 2024 and 2025. The report states that the study was published in a peer-reviewed journal.\nA spokesperson said in a statement that the findings remain under review.",
//            "reuters.com");

//        var result = await CreateEngine().AnalyzeAsync(article);

//        Assert.NotEqual(NewsVerdict.Fake, result.Verdict);
//    }

//    [Fact]
//    public async Task Clickbait_and_conspiracy_language_is_flagged_as_fake()
//    {
//        var article = new NewsArticle("SHOCKING TRUTH THEY DON'T WANT YOU TO KNOW!!!",
//            "Wake up people! The deep state and big pharma are hiding this. Share before deleted! This is a cover-up and they are lying.",
//            "unknown.example");

//        var result = await CreateEngine().AnalyzeAsync(article);

//        Assert.Equal(NewsVerdict.Fake, result.Verdict);
//    }
//}
