using HelpdeskSentiment.Authentication;
using HelpdeskSentiment.Models;
using HelpdeskSentiment.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpdeskSentiment.Controllers.Api
{
    // REST API for calls, using attribute routing. Test with Postman (docs/HelpdeskSentiment.postman_collection.json).
    [ApiController]
    [Route("api/calls")]
    [Authorize(AuthenticationSchemes = BasicAuthenticationHandler.SchemeName)]
    public class CallsApiController : ControllerBase
    {
        private readonly ICallService _calls;

        public CallsApiController(ICallService calls)
        {
            _calls = calls;
        }

        // GET api/calls?sentiment=Negative&search=bill
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CallResponse>>> GetAll(
            [FromQuery] SentimentLabel? sentiment, [FromQuery] string? search)
        {
            List<CallRecord> calls = await _calls.GetAllAsync(sentiment, search);
            return Ok(calls.Select(CallResponse.From));
        }

        // GET api/calls/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<CallResponse>> GetById(int id)
        {
            CallRecord? call = await _calls.GetByIdAsync(id);
            if (call == null)
            {
                return NotFound();
            }

            return Ok(CallResponse.From(call));
        }

        // GET api/calls/summary
        [HttpGet("summary")]
        public async Task<ActionResult<SentimentSummary>> Summary()
        {
            return Ok(await _calls.GetSummaryAsync());
        }

        // POST api/calls
        [HttpPost]
        public async Task<ActionResult<CallResponse>> Create(CallRequest request)
        {
            CallRecord created = await _calls.CreateAsync(request.ToEntity());
            CallRecord? saved = await _calls.GetByIdAsync(created.Id);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, CallResponse.From(saved!));
        }

        // PUT api/calls/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, CallRequest request)
        {
            if (!await _calls.UpdateAsync(request.ToEntity(id)))
            {
                return NotFound();
            }

            return NoContent();
        }

        // DELETE api/calls/5   (admins only)
        [HttpDelete("{id:int}")]
        [Authorize(AuthenticationSchemes = BasicAuthenticationHandler.SchemeName, Roles = Roles.Admin)]
        public async Task<IActionResult> Delete(int id)
        {
            if (!await _calls.DeleteAsync(id))
            {
                return NotFound();
            }

            return NoContent();
        }
    }
}
