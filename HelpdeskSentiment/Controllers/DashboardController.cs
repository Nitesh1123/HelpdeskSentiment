using HelpdeskSentiment.Authentication;
using HelpdeskSentiment.Models;
using HelpdeskSentiment.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpdeskSentiment.Controllers
{
    [Authorize(Roles = Roles.Admin)]
    public class DashboardController : Controller
    {
        private readonly ICallService _calls;

        public DashboardController(ICallService calls)
        {
            _calls = calls;
        }

        public async Task<IActionResult> Index()
        {
            SentimentSummary summary = await _calls.GetSummaryAsync();

            // ViewData and ViewBag both pass data from controller to view without a model.
            ViewData["Title"] = "Supervisor dashboard";
            ViewData["TotalCalls"] = summary.TotalCalls;
            ViewData["AverageScore"] = summary.AverageScore;
            ViewBag.Positive = summary.Positive;
            ViewBag.Neutral = summary.Neutral;
            ViewBag.Negative = summary.Negative;
            ViewBag.Escalated = summary.Escalated;
            ViewBag.ByCategory = summary.ByCategory;
            ViewBag.ByAgent = summary.AverageScoreByAgent;

            List<CallRecord> escalated = (await _calls.GetAllAsync(SentimentLabel.Negative))
                .Where(c => c.IsEscalated)
                .Take(5)
                .ToList();
            return View(escalated);
        }
    }
}
