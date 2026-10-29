using System.Diagnostics;

namespace HelpdeskSentiment.Middleware
{
    // Custom middleware: logs every request with its status code and how long it took,
    // and adds an X-Response-Time-ms header to the response.
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();

            context.Response.OnStarting(() =>
            {
                context.Response.Headers["X-Response-Time-ms"] = stopwatch.ElapsedMilliseconds.ToString();
                return Task.CompletedTask;
            });

            try
            {
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();
                _logger.LogInformation("{Method} {Path}{Query} -> {StatusCode} in {Elapsed} ms (user: {User})",
                    context.Request.Method,
                    context.Request.Path,
                    context.Request.QueryString,
                    context.Response.StatusCode,
                    stopwatch.ElapsedMilliseconds,
                    context.User.Identity?.IsAuthenticated == true ? context.User.Identity.Name : "anonymous");
            }
        }
    }

    public static class RequestLoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app) =>
            app.UseMiddleware<RequestLoggingMiddleware>();
    }
}
