using System.ComponentModel.DataAnnotations;
using HelpdeskSentiment.Services.Sentiment;

namespace HelpdeskSentiment.Models
{
    public class AnalyzeViewModel
    {
        [Required(ErrorMessage = "Type or paste some text to analyse")]
        [StringLength(4000)]
        [DataType(DataType.MultilineText)]
        [Display(Name = "Text to analyse")]
        public string Text { get; set; } = string.Empty;

        public SentimentResult? Result { get; set; }
    }
}
