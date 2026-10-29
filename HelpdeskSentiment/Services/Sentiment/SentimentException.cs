namespace HelpdeskSentiment.Services.Sentiment
{
    // Custom exception thrown when text cannot be analysed (for example an empty transcript).
    public class SentimentException : Exception
    {
        public SentimentException(string message) : base(message)
        {
        }
    }
}
