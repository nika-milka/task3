using task3.Config;
using task3.Middleware;
using task3.Services;
using task3.Tests;

// Проверка аргумента для запуска тестов
if (args.Contains("--test") || args.Contains("-t"))
{
    await Tests.RunAllAsync();
    return;
}

var builder = WebApplication.CreateBuilder(args);

// 1. Чтение из файла appsettings.json (низший приоритет)
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// 2. Чтение из переменных окружения с префиксом APP_ (средний приоритет)
builder.Configuration.AddEnvironmentVariables(prefix: "APP_");

// 3. Чтение из аргументов командной строки (высший приоритет)
builder.Configuration.AddCommandLine(args);

// Привязываем настройки к объекту
var appSettings = new AppSettings();
builder.Configuration.Bind(appSettings);

// Ранняя валидация настроек (запрещаем запуск при ошибках)
var validator = new ConfigValidator();
if (!validator.Validate(appSettings, out var errors))
{
    Console.WriteLine("╔═══════════════════════════════════════════════════════════════════╗");
    Console.WriteLine("║                     ОШИБКИ КОНФИГУРАЦИИ                           ║");
    Console.WriteLine("║                      ЗАПУСК НЕВОЗМОЖЕН                             ║");
    Console.WriteLine("╚═══════════════════════════════════════════════════════════════════╝");
    foreach (var err in errors)
    {
        Console.WriteLine($"  ❌ {err}");
    }
    return; // Запрещаем запуск приложения
}

// Вывод информации об успешной загрузке конфигурации
Console.WriteLine("╔═══════════════════════════════════════════════════════════════════╗");
Console.WriteLine("║                  КОНФИГУРАЦИЯ УСПЕШНО ЗАГРУЖЕНА                   ║");
Console.WriteLine("╚═══════════════════════════════════════════════════════════════════╝");
Console.WriteLine($"  📌 Режим работы: {appSettings.Mode}");
Console.WriteLine($"  🔒 Доверенные источники: {string.Join(", ", appSettings.TrustedOrigins)}");
Console.WriteLine($"  📊 Общий лимит запросов: {appSettings.RateLimits.GeneralRequestsPerMinute} зап/мин");
Console.WriteLine($"  ✏️ Лимит создания элементов: {appSettings.RateLimits.CreateRequestsPerMinute} зап/мин");
Console.WriteLine($"  📝 Подробные ошибки: {(appSettings.EnableDetailedErrors ? "Да" : "Нет")}");
Console.WriteLine("");

// Регистрация сервисов
builder.Services.AddSingleton(appSettings);
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Добавляем middleware для защитных заголовков и проверки доверенных источников
app.UseMiddleware<SecurityHeadersMiddleware>();

// Добавляем middleware для ограничения частоты запросов
app.UseMiddleware<RateLimitingMiddleware>();

// ============================================================================
// ЭНДПОИНТЫ ДЛЯ ТЕСТИРОВАНИЯ
// ============================================================================

// Главная страница
app.MapGet("/", () => Results.Ok(new
{
    message = "Сервер работает",
    mode = appSettings.Mode,
    version = "1.0.0",
    endpoints = new[] { "/", "/items", "/health", "/info" }
}));

// Получение списка элементов (GET) - высокий лимит
app.MapGet("/items", () => Results.Ok(new[]
{
    "item1",
    "item2",
    "item3",
    "item4",
    "item5"
}));

// Создание нового элемента (POST) - низкий лимит
app.MapPost("/items", () => Results.Created("/items/1", new
{
    id = 1,
    name = "new item",
    createdAt = DateTime.UtcNow
}));

// Проверка здоровья сервера
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    mode = appSettings.Mode,
    uptime = DateTime.UtcNow,
    rateLimits = new
    {
        general = appSettings.RateLimits.GeneralRequestsPerMinute,
        create = appSettings.RateLimits.CreateRequestsPerMinute
    }
}));

// Информация о конфигурации (только для Educational режима)
app.MapGet("/info", () =>
{
    if (appSettings.Mode == "Educational")
    {
        return Results.Ok(new
        {
            mode = appSettings.Mode,
            trustedOrigins = appSettings.TrustedOrigins,
            rateLimits = appSettings.RateLimits,
            enableDetailedErrors = appSettings.EnableDetailedErrors,
            note = "Эта информация доступна только в Educational режиме"
        });
    }
    else
    {
        return Results.Ok(new { message = "Информация о конфигурации скрыта в Production режиме" });
    }
});

// Тестовый эндпоинт для проверки rate limiting (быстро генерирует много запросов)
app.MapGet("/test/rate-limit", async (HttpContext context) =>
{
    var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    return Results.Ok(new
    {
        message = "Для тестирования rate limiting используйте множество GET/POST запросов",
        tip = "Выполните 61 GET запрос к /items или 11 POST запросов к /items",
        yourIp = clientIp
    });
});

// Тестовый эндпоинт для проверки заголовков безопасности
app.MapGet("/test/headers", (HttpContext context) =>
{
    var headers = context.Response.Headers
        .Where(h => h.Key.StartsWith("X-") || h.Key == "Cache-Control")
        .ToDictionary(h => h.Key, h => h.Value.ToString());

    return Results.Ok(new
    {
        message = "Проверьте ответ на наличие защитных заголовков",
        expectedHeaders = new[] { "X-Content-Type-Options", "X-Frame-Options", "X-XSS-Protection", "Cache-Control" },
        actualHeaders = headers
    });
});

app.Run();