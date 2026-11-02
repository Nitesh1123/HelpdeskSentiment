using System.ComponentModel.DataAnnotations;

namespace HelpdeskSentiment.Models
{
    public enum LoginEvent
    {
        SignIn,
        SignInFailed,
        SignUp,
        SignOut
    }

    // One row per sign-in, failed sign-in, sign-up or sign-out on the website.
    public class LoginActivity
    {
        public int Id { get; set; }

        public LoginEvent Event { get; set; }

        // For a failed sign-in this is the username that was typed (it may not exist).
        [Required, StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [StringLength(100)]
        public string? DisplayName { get; set; }

        [StringLength(20)]
        public string? Role { get; set; }

        [StringLength(45)]
        public string? IpAddress { get; set; }

        [StringLength(200)]
        public string? UserAgent { get; set; }

        public DateTime OccurredAt { get; set; } = DateTime.Now;
    }
}
