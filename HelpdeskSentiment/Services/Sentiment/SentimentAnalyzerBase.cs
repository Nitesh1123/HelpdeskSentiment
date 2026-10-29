using System.Text;
using HelpdeskSentiment.Models;

namespace HelpdeskSentiment.Services.Sentiment
{
    // Abstraction: the base class fixes the steps (validate -> tokenise -> score -> classify)
    // and lets each derived analyser decide how words are scored.
    public abstract class SentimentAnalyzerBase : ISentimentAnalyzer
    {
        protected const double PositiveThreshold = 0.15;
        protected const double NegativeThreshold = -0.15;

        public abstract string Name { get; }

        public SentimentResult Analyze(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new SentimentException("Transcript is empty, nothing to analyse.");
            }

            string[] tokens = Tokenize(text);
            if (tokens.Length == 0)
            {
                throw new SentimentException("Transcript does not contain any words.");
            }

            SentimentResult result = Score(tokens);
            result.TokenCount = tokens.Length;
            result.Label = Classify(result.Score);
            return result;
        }

        protected abstract SentimentResult Score(string[] tokens);

        // Lower-cases the text and keeps only letters and apostrophes, so "Not happy!!" -> ["not", "happy"].
        protected virtual string[] Tokenize(string text)
        {
            var sb = new StringBuilder(text.Length);
            foreach (char ch in text.ToLowerInvariant())
            {
                sb.Append(char.IsLetter(ch) || ch == '\'' ? ch : ' ');
            }

            return sb.ToString()
                     .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                     .Select(t => t.Trim('\''))
                     .Where(t => t.Length > 0)
                     .ToArray();
        }

        protected virtual SentimentLabel Classify(double score)
        {
            if (score >= PositiveThreshold) return SentimentLabel.Positive;
            if (score <= NegativeThreshold) return SentimentLabel.Negative;
            return SentimentLabel.Neutral;
        }
    }
}
