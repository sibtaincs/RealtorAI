using System.Collections.Concurrent;
using System.Diagnostics;

namespace RealEstateAI.Api.Middleware;

public sealed class RequestMetrics
{
    private long _total;
    private long _errors;
    private readonly ConcurrentDictionary<string, long> _byStatusClass = new();

    public DateTimeOffset StartedAt { get; } = DateTimeOffset.UtcNow;

    public void Record(int statusCode)
    {
        Interlocked.Increment(ref _total);
        if (statusCode >= 400) Interlocked.Increment(ref _errors);
        var statusClass = $"{statusCode / 100}xx";
        _byStatusClass.AddOrUpdate(statusClass, 1, (_, count) => count + 1);
    }

    public object Snapshot() => new
    {
        total = Interlocked.Read(ref _total),
        errors = Interlocked.Read(ref _errors),
        byStatusClass = _byStatusClass.OrderBy(pair => pair.Key)
            .ToDictionary(pair => pair.Key, pair => pair.Value),
        uptimeSeconds = Math.Round((DateTimeOffset.UtcNow - StartedAt).TotalSeconds, 1)
    };
}

public sealed class RequestObservabilityMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestObservabilityMiddleware> _logger;
    private readonly RequestMetrics _metrics;

    public RequestObservabilityMiddleware(
        RequestDelegate next,
        ILogger<RequestObservabilityMiddleware> logger,
        RequestMetrics metrics)
    {
        _next = next;
        _logger = logger;
        _metrics = metrics;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var suppliedId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault();
        var correlationId = string.IsNullOrWhiteSpace(suppliedId) || suppliedId.Length > 128
            ? Guid.NewGuid().ToString("N")
            : suppliedId;

        context.Response.Headers["X-Correlation-Id"] = correlationId;
        Activity.Current?.SetTag("correlation.id", correlationId);

        using (_logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();
                _metrics.Record(context.Response.StatusCode);
                // Log the route, never the query string, headers or body — those can carry tokens.
                _logger.LogInformation(
                    "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs}ms",
                    context.Request.Method,
                    context.Request.Path.Value,
                    context.Response.StatusCode,
                    stopwatch.ElapsedMilliseconds);
            }
        }
    }
}
