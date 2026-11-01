using System.Security.Claims;

namespace HelpdeskSentiment.Authentication
{
    public class AppUser
    {
        public int Id { get; set; }
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
        IReadOnlyList<AppUser> GetAll();
        bool UserExists(string username);
        AppUser CreateUser(string username, string password, string displayName, string role = Roles.Agent);
    }

    // Demo users are read from the "DemoUsers" section of appsettings.json.
    // In-memory registration allows adding accounts during runtime.
    public class ConfigUserStore : IUserStore
    {
        private readonly List<AppUser> _users;
        private readonly object _lock = new();

        public ConfigUserStore(IConfiguration configuration)
        {
            _users = configuration.GetSection("DemoUsers").Get<List<AppUser>>() ?? new List<AppUser>();
            for (int i = 0; i < _users.Count; i++)
            {
                if (_users[i].Id == 0)
                {
                    _users[i].Id = i + 1;
                }
            }
        }

        public IReadOnlyList<AppUser> GetAll()
        {
            lock (_lock)
            {
                return _users.ToList().AsReadOnly();
            }
        }

        public AppUser? Validate(string username, string password)
        {
            lock (_lock)
            {
                return _users.FirstOrDefault(u =>
                    string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase) &&
                    u.Password == password);
            }
        }

        public bool UserExists(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return false;
            }

            lock (_lock)
            {
                return _users.Any(u =>
                    string.Equals(u.Username, username.Trim(), StringComparison.OrdinalIgnoreCase));
            }
        }

        public AppUser CreateUser(string username, string password, string displayName, string role = Roles.Agent)
        {
            lock (_lock)
            {
                int nextId = _users.Count > 0 ? _users.Max(u => u.Id) + 1 : 1;
                var newUser = new AppUser
                {
                    Id = nextId,
                    Username = username.Trim(),
                    Password = password,
                    DisplayName = string.IsNullOrWhiteSpace(displayName) ? username.Trim() : displayName.Trim(),
                    Role = string.IsNullOrWhiteSpace(role) ? Roles.Agent : role
                };
                _users.Add(newUser);
                return newUser;
            }
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
