using HelpdeskSentiment.Models;
using HelpdeskSentiment.Services.Sentiment;
using Microsoft.AspNetCore.Mvc;

namespace HelpdeskSentiment.Controllers
{
    // Playground page: paste any text and see how the analyser scores it, without saving a call.
    public class AnalyzeController : Controller
    {
        private readonly ISentimentAnalyzer _analyzer;

        public AnalyzeController(ISentimentAnalyzer analyzer)
        {
            _analyzer = analyzer;
        }

        public IActionResult Index()
        {
            ViewBag.AnalyzerName = _analyzer.Name;
            return View(new AnalyzeViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(AnalyzeViewModel model)
        {
            ViewBag.AnalyzerName = _analyzer.Name;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                model.Result = _analyzer.Analyze(model.Text);
            }
            catch (SentimentException ex)
            {
                ModelState.AddModelError(nameof(model.Text), ex.Message);
            }

            return View(model);
        }
    }
}
