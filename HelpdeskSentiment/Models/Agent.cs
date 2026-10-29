using System.ComponentModel.DataAnnotations;

namespace HelpdeskSentiment.Models
{
    // A helpdesk agent who receives calls.
    public class Agent
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(150)]
        public string Email { get; set; } = string.Empty;

        public ICollection<CallRecord> Calls { get; set; } = new List<CallRecord>();
    }
}
