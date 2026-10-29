namespace HelpdeskSentiment.Services.Sentiment
{
    public interface ISentimentAnalyzer
    {
        string Name { get; }
        SentimentResult Analyze(string text);
    }
}
