using System.ComponentModel.DataAnnotations;

namespace HelpdeskSentiment.Models
{
    public class ClerkLoginRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string? FullName { get; set; }

        public string? UserId { get; set; }

        public string? Provider { get; set; } = "Clerk";

        public string? SessionToken { get; set; }

        public string? ReturnUrl { get; set; }
    }

    public class ClerkAuthResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? RedirectUrl { get; set; }
        public string? Username { get; set; }
    }
}
