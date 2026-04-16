using task3.Config;

namespace task3.Middleware;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly AppSettings _settings;

    public SecurityHeadersMiddleware(RequestDelegate next, AppSettings settings)
    {
        _next = next;
        _settings = settings;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var origin = context.Request.Headers.Origin.ToString();

        // Проверка доверенного источника
        if (!string.IsNullOrEmpty(origin) && _settings.TrustedOrigins != null && !_settings.TrustedOrigins.Contains(origin))
        {
            context.Response.StatusCode = 403;
            var msg = _settings.Mode == "Educational"
                ? $"Forbidden: Источник {origin} не в списке доверенных"
                : "Forbidden";
            await context.Response.WriteAsync(msg);
            return;
        }

        // Добавление защитных заголовков (используем Append вместо Add)
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Append("X-Frame-Options", "DENY");
        context.Response.Headers.Append("Cache-Control", "no-store, no-cache, must-revalidate");
        context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");

        await _next(context);
    }
}