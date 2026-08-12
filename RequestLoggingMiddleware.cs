using System.Diagnostics;

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
        string correlationId = Guid.NewGuid().ToString("N")[..8];

        context.Response.Headers["X-correlation-Id"] = correlationId;

        _logger.LogInformation("--> START {Method} {Path} [ID: {Id}]", context.Request.Method, context.Request.Path, correlationId);

        var sw = Stopwatch.StartNew();
        await _next(context);

        sw.Stop();
        _logger.LogInformation("<-- END {StatusCode} in {Elapsed}ms [ID: {Id}]", context.Response.StatusCode, sw.ElapsedMilliseconds, correlationId);
    }
}

