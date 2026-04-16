using System.Collections.Concurrent;
using task3.Config;

namespace task3.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly AppSettings _settings;
    private static readonly ConcurrentDictionary<string, (int Count, DateTime ResetTime)> _requests = new();

    public RateLimitingMiddleware(RequestDelegate next, AppSettings settings)
    {
        _next = next;
        _settings = settings;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var path = context.Request.Path.ToString();
        var method = context.Request.Method;

        // Определяем лимит в зависимости от маршрута и метода
        int limit;
        if (path.StartsWith("/items") && method == "POST")
        {
            limit = _settings.RateLimits.CreateRequestsPerMinute;
        }
        else
        {
            limit = _settings.RateLimits.GeneralRequestsPerMinute;
        }

        var key = $"{clientIp}:{path}:{method}";
        var now = DateTime.UtcNow;

        if (_requests.TryGetValue(key, out var entry) && now < entry.ResetTime)
        {
            if (entry.Count >= limit)
            {
                context.Response.StatusCode = 429;
                var msg = _settings.Mode == "Educational"
                    ? $"Лимит {limit} запросов в минуту превышен. Подождите {Math.Ceiling((entry.ResetTime - now).TotalSeconds)} секунд."
                    : "Too many requests";
                await context.Response.WriteAsync(msg);
                return;
            }
            _requests[key] = (entry.Count + 1, entry.ResetTime);
        }
        else
        {
            _requests[key] = (1, now.AddMinutes(1));
        }

        await _next(context);
    }
}