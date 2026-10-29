using HelpdeskSentiment.Models;

namespace HelpdeskSentiment.Services.Sentiment
{
    public class SentimentResult
    {
        // Normalised score between -1 (very negative) and +1 (very positive).
        public double Score { get; set; }
        public SentimentLabel Label { get; set; }
        public bool ShouldEscalate { get; set; }
        public List<string> PositiveWords { get; } = new();
        public List<string> NegativeWords { get; } = new();
        public int TokenCount { get; set; }

        public string MatchedWords =>
            string.Join(", ", PositiveWords.Concat(NegativeWords).Distinct());
    }
}
