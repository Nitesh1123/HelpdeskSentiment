using System.ComponentModel.DataAnnotations;
using HelpdeskSentiment.Models;

namespace HelpdeskSentiment.Controllers.Api
{
    // What the API returns for a call (keeps the JSON flat and avoids Agent <-> Calls loops).
    public record CallResponse(
        int Id,
        string CallerName,
        string PhoneNumber,
        string Category,
        int? AgentId,
        string? AgentName,
        string Transcript,
        DateTime CallTime,
        int DurationSeconds,
        double SentimentScore,
        string Sentiment,
        bool IsEscalated,
        string? Keywords)
    {
        public static CallResponse From(CallRecord c) => new(
            c.Id, c.CallerName, c.PhoneNumber, c.Category.ToString(), c.AgentId, c.Agent?.Name,
            c.Transcript, c.CallTime, c.DurationSeconds, c.SentimentScore, c.Sentiment.ToString(),
            c.IsEscalated, c.Keywords);
    }

    // What the API accepts when creating or updating a call. Sentiment is always calculated by the server.
    public class CallRequest
    {
        [Required, StringLength(100, MinimumLength = 2)]
        public string CallerName { get; set; } = string.Empty;

        [Required, RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Enter a valid 10 digit mobile number")]
        public string PhoneNumber { get; set; } = string.Empty;

        public CallCategory Category { get; set; } = CallCategory.General;

        [Required]
        public int? AgentId { get; set; }

        [Required, StringLength(4000, MinimumLength = 10)]
        public string Transcript { get; set; } = string.Empty;

        public DateTime? CallTime { get; set; }

        [Range(1, 7200)]
        public int DurationSeconds { get; set; } = 60;

        public CallRecord ToEntity(int id = 0) => new()
        {
            Id = id,
            CallerName = CallerName,
            PhoneNumber = PhoneNumber,
            Category = Category,
            AgentId = AgentId,
            Transcript = Transcript,
            CallTime = CallTime ?? DateTime.Now,
            DurationSeconds = DurationSeconds
        };
    }

    public class AnalyzeRequest
    {
        [Required, StringLength(4000)]
        public string Text { get; set; } = string.Empty;
    }
}
