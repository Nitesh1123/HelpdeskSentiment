using HelpdeskSentiment.Models;

namespace HelpdeskSentiment.Services
{
    public interface ICallService
    {
        Task<List<CallRecord>> GetAllAsync(SentimentLabel? sentiment = null, string? search = null);
        Task<CallRecord?> GetByIdAsync(int id);
        Task<CallRecord> CreateAsync(CallRecord call);
        Task<bool> UpdateAsync(CallRecord call);
        Task<bool> DeleteAsync(int id);
        Task<List<Agent>> GetAgentsAsync();
        Task<SentimentSummary> GetSummaryAsync();
    }

    public class SentimentSummary
    {
        public int TotalCalls { get; set; }
        public int Positive { get; set; }
        public int Neutral { get; set; }
        public int Negative { get; set; }
        public int Escalated { get; set; }
        public double AverageScore { get; set; }
        public Dictionary<string, int> ByCategory { get; set; } = new();
        public Dictionary<string, double> AverageScoreByAgent { get; set; } = new();
    }
}
