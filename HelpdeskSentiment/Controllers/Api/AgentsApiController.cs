using HelpdeskSentiment.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpdeskSentiment.Controllers.Api
{
    // DTO returned by the agents endpoint. Exposes only safe fields; never password hashes or entities.
    public record AgentDto(int Id, string Username, string Role);

    // REST API for agent accounts, using attribute routing and HTTP Basic authentication.
    // Restricted to administrators.
    [ApiController]
    [Route("api/agents")]
    [Authorize(AuthenticationSchemes = BasicAuthenticationHandler.SchemeName, Roles = Roles.Admin)]
    public class AgentsApiController : ControllerBase
    {
        private readonly IUserStore _userStore;

        public AgentsApiController(IUserStore userStore)
        {
            _userStore = userStore;
        }

        // GET api/agents   (admin only: 200 OK, 401 Unauthorized if unauthenticated, 403 Forbidden if not Admin)
        [HttpGet]
        public ActionResult<IEnumerable<AgentDto>> GetAll()
        {
            var agents = _userStore.GetAll()
                .Select(u => new AgentDto(u.Id, u.Username, u.Role));

            return Ok(agents);
        }
    }
}
