using HelpdeskSentiment.Data;
using HelpdeskSentiment.Models;
using HelpdeskSentiment.Services.Sentiment;
using Microsoft.EntityFrameworkCore;

namespace HelpdeskSentiment.Services
{
    // Business logic for calls. Injected into controllers through DI (see Program.cs).
    public class CallService : ICallService
    {
        private readonly HelpdeskDbContext _db;
        private readonly ISentimentAnalyzer _analyzer;

        public CallService(HelpdeskDbContext db, ISentimentAnalyzer analyzer)
        {
            _db = db;
            _analyzer = analyzer;
        }

        public async Task<List<CallRecord>> GetAllAsync(SentimentLabel? sentiment = null, string? search = null)
        {
            IQueryable<CallRecord> query = _db.CallRecords.Include(c => c.Agent);

            if (sentiment.HasValue)
            {
                query = query.Where(c => c.Sentiment == sentiment.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string term = search.Trim();
                query = query.Where(c => c.CallerName.Contains(term)
                                      || c.PhoneNumber.Contains(term)
                                      || c.Transcript.Contains(term));
            }

            return await query.OrderByDescending(c => c.CallTime).ToListAsync();
        }

        public Task<CallRecord?> GetByIdAsync(int id) =>
            _db.CallRecords.Include(c => c.Agent).FirstOrDefaultAsync(c => c.Id == id);

        public async Task<CallRecord> CreateAsync(CallRecord call)
        {
            ApplySentiment(call);
            _db.CallRecords.Add(call);
            await _db.SaveChangesAsync();
            return call;
        }

        public async Task<bool> UpdateAsync(CallRecord call)
        {
            CallRecord? existing = await _db.CallRecords.FindAsync(call.Id);
            if (existing == null)
            {
                return false;
            }

            existing.CallerName = call.CallerName;
            existing.PhoneNumber = call.PhoneNumber;
            existing.Category = call.Category;
            existing.AgentId = call.AgentId;
            existing.Transcript = call.Transcript;
            existing.CallTime = call.CallTime;
            existing.DurationSeconds = call.DurationSeconds;
            ApplySentiment(existing);

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            CallRecord? existing = await _db.CallRecords.FindAsync(id);
            if (existing == null)
            {
                return false;
            }

            _db.CallRecords.Remove(existing);
            await _db.SaveChangesAsync();
            return true;
        }

        public Task<List<Agent>> GetAgentsAsync() =>
            _db.Agents.OrderBy(a => a.Name).ToListAsync();

        public async Task<SentimentSummary> GetSummaryAsync()
        {
            List<CallRecord> calls = await _db.CallRecords.Include(c => c.Agent).ToListAsync();

            return new SentimentSummary
            {
                TotalCalls = calls.Count,
                Positive = calls.Count(c => c.Sentiment == SentimentLabel.Positive),
                Neutral = calls.Count(c => c.Sentiment == SentimentLabel.Neutral),
                Negative = calls.Count(c => c.Sentiment == SentimentLabel.Negative),
                Escalated = calls.Count(c => c.IsEscalated),
                AverageScore = calls.Count == 0 ? 0 : Math.Round(calls.Average(c => c.SentimentScore), 3),
                ByCategory = calls.GroupBy(c => c.Category.ToString())
                                  .ToDictionary(g => g.Key, g => g.Count()),
                AverageScoreByAgent = calls.Where(c => c.Agent != null)
                                           .GroupBy(c => c.Agent!.Name)
                                           .ToDictionary(g => g.Key, g => Math.Round(g.Average(c => c.SentimentScore), 3))
            };
        }

        private void ApplySentiment(CallRecord call)
        {
            try
            {
                SentimentResult result = _analyzer.Analyze(call.Transcript);
                call.SentimentScore = result.Score;
                call.Sentiment = result.Label;
                call.IsEscalated = result.ShouldEscalate;
                string keywords = result.MatchedWords;
                call.Keywords = keywords.Length > 500 ? keywords.Substring(0, 500) : keywords;
            }
            catch (SentimentException)
            {
                call.SentimentScore = 0;
                call.Sentiment = SentimentLabel.Neutral;
                call.IsEscalated = false;
                call.Keywords = null;
            }
        }
    }
}
