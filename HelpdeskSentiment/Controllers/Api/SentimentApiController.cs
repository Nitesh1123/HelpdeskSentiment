using HelpdeskSentiment.Services.Sentiment;
using Microsoft.AspNetCore.Mvc;

namespace HelpdeskSentiment.Controllers.Api
{
    // Public endpoint: analyse any text without saving it.
    [ApiController]
    [Route("api/sentiment")]
    public class SentimentApiController : ControllerBase
    {
        private readonly ISentimentAnalyzer _analyzer;

        public SentimentApiController(ISentimentAnalyzer analyzer)
        {
            _analyzer = analyzer;
        }

        // POST api/sentiment/analyze   { "text": "I am very happy with the service" }
        [HttpPost("analyze")]
        public ActionResult<SentimentResult> Analyze(AnalyzeRequest request)
        {
            try
            {
                return Ok(_analyzer.Analyze(request.Text));
            }
            catch (SentimentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
