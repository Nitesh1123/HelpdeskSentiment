using System.Net.Http.Headers;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace HelpdeskSentiment.Authentication
{
    // HTTP Basic authentication for the Web API: "Authorization: Basic base64(username:password)".
    public class BasicAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string SchemeName = "Basic";

        private readonly IUserStore _users;

        public BasicAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            IUserStore users)
            : base(options, logger, encoder)
        {
            _users = users;
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.ContainsKey("Authorization"))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            string username;
            string password;
            try
            {
                var header = AuthenticationHeaderValue.Parse(Request.Headers.Authorization!);
                if (!SchemeName.Equals(header.Scheme, StringComparison.OrdinalIgnoreCase) || header.Parameter == null)
                {
                    return Task.FromResult(AuthenticateResult.NoResult());
                }

                string decoded = Encoding.UTF8.GetString(Convert.FromBase64String(header.Parameter));
                int colon = decoded.IndexOf(':');
                if (colon < 0)
                {
                    return Task.FromResult(AuthenticateResult.Fail("Invalid Basic credentials format"));
                }

                username = decoded.Substring(0, colon);
                password = decoded.Substring(colon + 1);
            }
            catch (FormatException)
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization header"));
            }

            AppUser? user = _users.Validate(username, password);
            if (user == null)
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid username or password"));
            }

            var ticket = new AuthenticationTicket(_users.CreatePrincipal(user, SchemeName), SchemeName);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            Response.Headers.WWWAuthenticate = "Basic realm=\"HelpdeskSentiment\", charset=\"UTF-8\"";
            return base.HandleChallengeAsync(properties);
        }
    }
}
