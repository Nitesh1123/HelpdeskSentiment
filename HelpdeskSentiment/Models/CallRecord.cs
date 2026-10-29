using System.ComponentModel.DataAnnotations;

namespace HelpdeskSentiment.Models
{
    // One incoming helpdesk call. Sentiment fields are filled by the sentiment analyser, not by the user.
    public class CallRecord
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Caller name is required")]
        [StringLength(100, MinimumLength = 2)]
        [Display(Name = "Caller name")]
        public string CallerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required")]
        [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Enter a valid 10 digit mobile number")]
        [Display(Name = "Phone number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        public CallCategory Category { get; set; } = CallCategory.General;

        [Required(ErrorMessage = "Select the agent who took the call")]
        [Display(Name = "Agent")]
        public int? AgentId { get; set; }

        public Agent? Agent { get; set; }

        [Required(ErrorMessage = "Transcript is required")]
        [StringLength(4000, MinimumLength = 10, ErrorMessage = "Transcript must be between 10 and 4000 characters")]
        [DataType(DataType.MultilineText)]
        [Display(Name = "Call transcript / notes")]
        public string Transcript { get; set; } = string.Empty;

        [Display(Name = "Call time")]
        public DateTime CallTime { get; set; } = DateTime.Now;

        [Range(1, 7200, ErrorMessage = "Duration must be between 1 and 7200 seconds")]
        [Display(Name = "Duration (sec)")]
        public int DurationSeconds { get; set; } = 60;

        [Display(Name = "Score")]
        public double SentimentScore { get; set; }

        public SentimentLabel Sentiment { get; set; } = SentimentLabel.Neutral;

        [Display(Name = "Escalated")]
        public bool IsEscalated { get; set; }

        [StringLength(500)]
        [Display(Name = "Matched words")]
        public string? Keywords { get; set; }
    }
}
