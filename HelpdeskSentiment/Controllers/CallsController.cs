using HelpdeskSentiment.Authentication;
using HelpdeskSentiment.Models;
using HelpdeskSentiment.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HelpdeskSentiment.Controllers
{
    [Authorize]
    public class CallsController : Controller
    {
        private const string RecentCallsKey = "RecentCalls";
        private const int MaxRecentCalls = 5;

        private readonly ICallService _calls;

        // ICallService is injected by the DI container.
        public CallsController(ICallService calls)
        {
            _calls = calls;
        }

        // GET /Calls?sentiment=Negative&search=refund   (filters come from the QueryString)
        public async Task<IActionResult> Index(SentimentLabel? sentiment, string? search)
        {
            ViewData["Title"] = "Incoming calls";
            ViewBag.Sentiment = sentiment;
            ViewBag.Search = search;
            ViewBag.RecentCalls = await GetRecentCallsAsync();

            List<CallRecord> calls = await _calls.GetAllAsync(sentiment, search);
            return View(calls);
        }

        public async Task<IActionResult> Details(int id)
        {
            CallRecord? call = await _calls.GetByIdAsync(id);
            if (call == null)
            {
                return NotFound();
            }

            RememberRecentCall(id);
            return View(call);
        }

        public async Task<IActionResult> Create()
        {
            await LoadAgentsAsync();
            return View(new CallRecord());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("CallerName,PhoneNumber,Category,AgentId,Transcript,CallTime,DurationSeconds")] CallRecord call)
        {
            if (!ModelState.IsValid)
            {
                await LoadAgentsAsync();
                return View(call);
            }

            await _calls.CreateAsync(call);
            TempData["Message"] = $"Call from {call.CallerName} saved. Detected sentiment: {call.Sentiment} ({call.SentimentScore:+0.00;-0.00;0})"
                                  + (call.IsEscalated ? " — flagged for escalation!" : "");
            TempData["MessageType"] = call.Sentiment == SentimentLabel.Negative ? "warning" : "success";
            return RedirectToAction(nameof(Details), new { id = call.Id });
        }

        public async Task<IActionResult> Edit(int id)
        {
            CallRecord? call = await _calls.GetByIdAsync(id);
            if (call == null)
            {
                return NotFound();
            }

            await LoadAgentsAsync();
            return View(call);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,
            [Bind("Id,CallerName,PhoneNumber,Category,AgentId,Transcript,CallTime,DurationSeconds")] CallRecord call)
        {
            if (id != call.Id)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                await LoadAgentsAsync();
                return View(call);
            }

            if (!await _calls.UpdateAsync(call))
            {
                return NotFound();
            }

            TempData["Message"] = "Call updated and sentiment re-analysed.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> Delete(int id)
        {
            CallRecord? call = await _calls.GetByIdAsync(id);
            if (call == null)
            {
                return NotFound();
            }

            return View(call);
        }

        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = Roles.Admin)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _calls.DeleteAsync(id);
            TempData["Message"] = "Call deleted.";
            return RedirectToAction(nameof(Index));
        }

        private async Task LoadAgentsAsync()
        {
            List<Agent> agents = await _calls.GetAgentsAsync();
            ViewBag.Agents = new SelectList(agents, nameof(Agent.Id), nameof(Agent.Name));
        }

        // Session: keep the ids of the last few calls this user opened.
        private void RememberRecentCall(int id)
        {
            List<int> recent = ReadRecentIds();
            recent.Remove(id);
            recent.Insert(0, id);
            if (recent.Count > MaxRecentCalls)
            {
                recent.RemoveRange(MaxRecentCalls, recent.Count - MaxRecentCalls);
            }

            HttpContext.Session.SetString(RecentCallsKey, string.Join(",", recent));
        }

        private List<int> ReadRecentIds()
        {
            string? stored = HttpContext.Session.GetString(RecentCallsKey);
            if (string.IsNullOrEmpty(stored))
            {
                return new List<int>();
            }

            return stored.Split(',', StringSplitOptions.RemoveEmptyEntries)
                         .Select(s => int.TryParse(s, out int n) ? n : 0)
                         .Where(n => n > 0)
                         .ToList();
        }

        private async Task<List<CallRecord>> GetRecentCallsAsync()
        {
            var result = new List<CallRecord>();
            foreach (int id in ReadRecentIds())
            {
                CallRecord? call = await _calls.GetByIdAsync(id);
                if (call != null)
                {
                    result.Add(call);
                }
            }

            return result;
        }
    }
}
