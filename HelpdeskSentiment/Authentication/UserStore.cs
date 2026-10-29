using System.Security.Claims;

namespace HelpdeskSentiment.Authentication
{
    public class AppUser
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Role { get; set; } = Roles.Agent;
    }

    public static class Roles
    {
        public const string Admin = "Admin";
        public const string Agent = "Agent";
    }

    public interface IUserStore
    {
        AppUser? Validate(string username, string password);
        ClaimsPrincipal CreatePrincipal(AppUser user, string authenticationScheme);
    }

    // Demo users are read from the "DemoUsers" section of appsettings.json.
    // A real system would store hashed passwords in the database (e.g. ASP.NET Core Identity).
    public class ConfigUserStore : IUserStore
    {
        private readonly List<AppUser> _users;

        public ConfigUserStore(IConfiguration configuration)
        {
            _users = configuration.GetSection("DemoUsers").Get<List<AppUser>>() ?? new List<AppUser>();
        }

        public AppUser? Validate(string username, string password)
        {
            return _users.FirstOrDefault(u =>
                string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase) &&
                u.Password == password);
        }

        public ClaimsPrincipal CreatePrincipal(AppUser user, string authenticationScheme)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Username),
                new Claim(ClaimTypes.Name, user.DisplayName),
                new Claim(ClaimTypes.Role, user.Role)
            };

            return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationScheme));
        }
    }
}
