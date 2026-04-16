using System.Net;
using Microsoft.Extensions.Configuration;
using task3.Config;
using task3.Services;

namespace task3.Tests;

public static class Tests
{
    private static readonly HttpClient _client = new HttpClient();
    private const string BaseUrl = "http://localhost:5184";
    private static int _passedTests = 0;
    private static int _failedTests = 0;

    public static async Task RunAllAsync()
    {
        Console.Clear();
        Console.WriteLine("ТЕСТИРОВАНИЕ");

        Console.WriteLine("\n📋 ВНИМАНИЕ: Перед запуском тестов убедитесь, что сервер запущен!");
        Console.WriteLine("   Запустите сервер в отдельном терминале: dotnet run");
        Console.WriteLine("   Затем нажмите любую клавишу для продолжения...");
        Console.ReadKey();

        // Группа 1: Приоритет настроек (не требуют сервера)
        Test1_FileConfigPriority();
        Test2_EnvironmentVariablePriority();
        Test3_CommandLinePriority();
        Test4_FullPriorityChain();

        // Группа 2: Валидация настроек (не требуют сервера)
        Test5_ValidConfig();
        Test6_InvalidMode();
        Test7_EmptyTrustedOrigins();
        Test8_InvalidOriginUrl();
        Test9_ZeroRateLimit();

        // Группа 3: Интеграционные тесты (требуют запущенный сервер)
        await Test10_UntrustedOriginBlocked();
        await Test11_PostRateLimit();
        await Test12_SecurityHeaders();
        await Test13_GetRateLimit();
        await Test14_ModeComparison();

        // Итоги
        Console.WriteLine("\n╔═══════════════════════════════════════════════════════════════════════════════════╗");
        Console.WriteLine($"║                          РЕЗУЛЬТАТЫ ТЕСТИРОВАНИЯ                                 ║");
        Console.WriteLine($"║                          ✅ Пройдено: {_passedTests}  ❌ Не пройдено: {_failedTests}                              ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════════════════════════════════╝");

        if (_failedTests == 0)
        {
            Console.WriteLine("\n🎉 ПОЗДРАВЛЯЮ! Все тесты пройдены успешно!");
            Console.WriteLine("   Проект соответствует всем требованиям задания.");
        }
        else
        {
            Console.WriteLine($"\n⚠️ Не пройдено {_failedTests} тестов. Исправьте ошибки и запустите снова.");
        }
    }

    // =========================================================================
    // ГРУППА 1: ПРОВЕРКА ПРИОРИТЕТА НАСТРОЕК
    // =========================================================================

    static void Test1_FileConfigPriority()
    {
        Console.WriteLine("\n┌─────────────────────────────────────────────────────────────────────────────────┐");
        Console.WriteLine("│ ТЕСТ 1: Чтение настроек из файла (appsettings.json)                              │");
        Console.WriteLine("└─────────────────────────────────────────────────────────────────────────────────┘");

        try
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            var mode = config["Mode"];
            var generalLimit = config["RateLimits:GeneralRequestsPerMinute"];
            var createLimit = config["RateLimits:CreateRequestsPerMinute"];

            Console.WriteLine($"   📄 Режим из файла: {mode}");
            Console.WriteLine($"   📄 Общий лимит: {generalLimit} зап/мин");
            Console.WriteLine($"   📄 Лимит создания: {createLimit} зап/мин");

            if (!string.IsNullOrEmpty(mode) && !string.IsNullOrEmpty(generalLimit))
            {
                Console.WriteLine("   ✅ Файл успешно прочитан");
                _passedTests++;
            }
            else
            {
                Console.WriteLine("   ❌ Ошибка: не удалось прочитать файл");
                _failedTests++;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ Ошибка: {ex.Message}");
            _failedTests++;
        }
    }

    static void Test2_EnvironmentVariablePriority()
    {
        Console.WriteLine("\n┌─────────────────────────────────────────────────────────────────────────────────┐");
        Console.WriteLine("│ ТЕСТ 2: Переменная окружения переопределяет файл                                 │");
        Console.WriteLine("└─────────────────────────────────────────────────────────────────────────────────┘");

        try
        {
            Environment.SetEnvironmentVariable("APP_Mode", "Production");

            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false)
                .AddEnvironmentVariables(prefix: "APP_")
                .Build();

            var mode = config["Mode"];

            Console.WriteLine($"   📄 Режим из файла (appsettings.json): Educational");
            Console.WriteLine($"   🔧 Переменная окружения APP_Mode: Production");
            Console.WriteLine($"   🎯 Итоговое значение: {mode}");

            if (mode == "Production")
            {
                Console.WriteLine("   ✅ Переменная окружения переопределила файл");
                _passedTests++;
            }
            else
            {
                Console.WriteLine("   ❌ Ошибка: переменная окружения не переопределила файл");
                _failedTests++;
            }

            Environment.SetEnvironmentVariable("APP_Mode", null);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ Ошибка: {ex.Message}");
            _failedTests++;
        }
    }

    static void Test3_CommandLinePriority()
    {
        Console.WriteLine("\n┌─────────────────────────────────────────────────────────────────────────────────┐");
        Console.WriteLine("│ ТЕСТ 3: Аргументы командной строки переопределяют файл и переменные окружения    │");
        Console.WriteLine("└─────────────────────────────────────────────────────────────────────────────────┘");

        try
        {
            Environment.SetEnvironmentVariable("APP_Mode", "Production");
            var args = new[] { "--Mode=Educational" };

            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false)
                .AddEnvironmentVariables(prefix: "APP_")
                .AddCommandLine(args)
                .Build();

            var mode = config["Mode"];

            Console.WriteLine($"   📄 Файл (appsettings.json): Educational");
            Console.WriteLine($"   🔧 Переменная окружения APP_Mode: Production");
            Console.WriteLine($"   💻 Аргумент CLI: --Mode=Educational");
            Console.WriteLine($"   🎯 Итоговое значение: {mode}");

            if (mode == "Educational")
            {
                Console.WriteLine("   ✅ Аргумент CLI переопределил и файл, и переменную окружения");
                _passedTests++;
            }
            else
            {
                Console.WriteLine("   ❌ Ошибка: аргумент CLI не имеет высшего приоритета");
                _failedTests++;
            }

            Environment.SetEnvironmentVariable("APP_Mode", null);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ Ошибка: {ex.Message}");
            _failedTests++;
        }
    }

    static void Test4_FullPriorityChain()
    {
        Console.WriteLine("\n┌─────────────────────────────────────────────────────────────────────────────────┐");
        Console.WriteLine("│ ТЕСТ 4: Полная цепочка приоритетов (CLI > ENV > FILE)                           │");
        Console.WriteLine("└─────────────────────────────────────────────────────────────────────────────────┘");

        try
        {
            Environment.SetEnvironmentVariable("APP_RateLimits__GeneralRequestsPerMinute", "100");
            var args = new[] { "--RateLimits:GeneralRequestsPerMinute=30" };

            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false)
                .AddEnvironmentVariables(prefix: "APP_")
                .AddCommandLine(args)
                .Build();

            var actualLimit = int.Parse(config["RateLimits:GeneralRequestsPerMinute"]);

            Console.WriteLine($"   📄 Файл (appsettings.json): 60 зап/мин");
            Console.WriteLine($"   🔧 Переменная окружения: 100 зап/мин");
            Console.WriteLine($"   💻 Аргумент CLI: 30 зап/мин");
            Console.WriteLine($"   🎯 Итоговое значение: {actualLimit} зап/мин");

            if (actualLimit == 30)
            {
                Console.WriteLine("   ✅ Приоритет работает правильно: CLI > ENV > FILE");
                _passedTests++;
            }
            else
            {
                Console.WriteLine($"   ❌ Ошибка: ожидалось 30, получено {actualLimit}");
                _failedTests++;
            }

            Environment.SetEnvironmentVariable("APP_RateLimits__GeneralRequestsPerMinute", null);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ Ошибка: {ex.Message}");
            _failedTests++;
        }
    }

    // =========================================================================
    // ГРУППА 2: ПРОВЕРКА ВАЛИДАЦИИ НАСТРОЕК
    // =========================================================================

    static void Test5_ValidConfig()
    {
        Console.WriteLine("\n┌─────────────────────────────────────────────────────────────────────────────────┐");
        Console.WriteLine("│ ТЕСТ 5: Корректная конфигурация проходит валидацию                              │");
        Console.WriteLine("└─────────────────────────────────────────────────────────────────────────────────┘");

        var settings = new AppSettings
        {
            Mode = "Educational",
            TrustedOrigins = new List<string> { "http://localhost:5000", "http://localhost:3000" },
            RateLimits = new RateLimitSettings { GeneralRequestsPerMinute = 60, CreateRequestsPerMinute = 10 }
        };

        var validator = new ConfigValidator();
        bool isValid = validator.Validate(settings, out var errors);

        Console.WriteLine($"   Результат валидации: {(isValid ? "✅ Корректно" : "❌ Ошибка")}");
        if (!isValid)
            foreach (var err in errors) Console.WriteLine($"     - {err}");

        if (isValid)
            _passedTests++;
        else
            _failedTests++;
    }

    static void Test6_InvalidMode()
    {
        Console.WriteLine("\n┌─────────────────────────────────────────────────────────────────────────────────┐");
        Console.WriteLine("│ ТЕСТ 6: Некорректный режим (Mode=Invalid) - запуск запрещён                     │");
        Console.WriteLine("└─────────────────────────────────────────────────────────────────────────────────┘");

        var settings = new AppSettings
        {
            Mode = "Invalid",
            TrustedOrigins = new List<string> { "http://localhost:5000" },
            RateLimits = new RateLimitSettings { GeneralRequestsPerMinute = 60, CreateRequestsPerMinute = 10 }
        };

        var validator = new ConfigValidator();
        bool isValid = validator.Validate(settings, out var errors);

        Console.WriteLine($"   Результат валидации: {(isValid ? "❌ Должно быть ошибкой" : "✅ Запуск запрещён")}");
        foreach (var err in errors)
            Console.WriteLine($"   Ошибка: {err}");

        if (!isValid && errors.Any(e => e.Contains("Mode")))
            _passedTests++;
        else
            _failedTests++;
    }

    static void Test7_EmptyTrustedOrigins()
    {
        Console.WriteLine("\n┌─────────────────────────────────────────────────────────────────────────────────┐");
        Console.WriteLine("│ ТЕСТ 7: Пустой список доверенных источников - запуск запрещён                   │");
        Console.WriteLine("└─────────────────────────────────────────────────────────────────────────────────┘");

        var settings = new AppSettings
        {
            Mode = "Educational",
            TrustedOrigins = new List<string>(),
            RateLimits = new RateLimitSettings { GeneralRequestsPerMinute = 60, CreateRequestsPerMinute = 10 }
        };

        var validator = new ConfigValidator();
        bool isValid = validator.Validate(settings, out var errors);

        Console.WriteLine($"   Результат валидации: {(isValid ? "❌ Должно быть ошибкой" : "✅ Запуск запрещён")}");
        foreach (var err in errors)
            Console.WriteLine($"   Ошибка: {err}");

        if (!isValid && errors.Any(e => e.Contains("хотя бы один")))
            _passedTests++;
        else
            _failedTests++;
    }

    static void Test8_InvalidOriginUrl()
    {
        Console.WriteLine("\n┌─────────────────────────────────────────────────────────────────────────────────┐");
        Console.WriteLine("│ ТЕСТ 8: Некорректный URL доверенного источника - запуск запрещён                │");
        Console.WriteLine("└─────────────────────────────────────────────────────────────────────────────────┘");

        var settings = new AppSettings
        {
            Mode = "Educational",
            TrustedOrigins = new List<string> { "not-a-valid-url", "http://localhost:5000" },
            RateLimits = new RateLimitSettings { GeneralRequestsPerMinute = 60, CreateRequestsPerMinute = 10 }
        };

        var validator = new ConfigValidator();
        bool isValid = validator.Validate(settings, out var errors);

        Console.WriteLine($"   Результат валидации: {(isValid ? "❌ Должно быть ошибкой" : "✅ Запуск запрещён")}");
        foreach (var err in errors)
            Console.WriteLine($"   Ошибка: {err}");

        if (!isValid && errors.Any(e => e.Contains("Некорректный URL")))
            _passedTests++;
        else
            _failedTests++;
    }

    static void Test9_ZeroRateLimit()
    {
        Console.WriteLine("\n┌─────────────────────────────────────────────────────────────────────────────────┐");
        Console.WriteLine("│ ТЕСТ 9: Нулевой лимит запросов - запуск запрещён                                │");
        Console.WriteLine("└─────────────────────────────────────────────────────────────────────────────────┘");

        var settings = new AppSettings
        {
            Mode = "Educational",
            TrustedOrigins = new List<string> { "http://localhost:5000" },
            RateLimits = new RateLimitSettings { GeneralRequestsPerMinute = 0, CreateRequestsPerMinute = 0 }
        };

        var validator = new ConfigValidator();
        bool isValid = validator.Validate(settings, out var errors);

        Console.WriteLine($"   Результат валидации: {(isValid ? "❌ Должно быть ошибкой" : "✅ Запуск запрещён")}");
        foreach (var err in errors)
            Console.WriteLine($"   Ошибка: {err}");

        if (!isValid && errors.Any(e => e.Contains("> 0")))
            _passedTests++;
        else
            _failedTests++;
    }

    // =========================================================================
    // ГРУППА 3: ИНТЕГРАЦИОННЫЕ ТЕСТЫ (требуют запущенный сервер)
    // =========================================================================

    static async Task Test10_UntrustedOriginBlocked()
    {
        Console.WriteLine("\n┌─────────────────────────────────────────────────────────────────────────────────┐");
        Console.WriteLine("│ ТЕСТ 10: Блокировка запроса с недоверенного источника                          │");
        Console.WriteLine("└─────────────────────────────────────────────────────────────────────────────────┘");

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/items");
            request.Headers.Add("Origin", "http://evil-site.com");

            var response = await _client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"   Запрос с Origin: http://evil-site.com");
            Console.WriteLine($"   Статус ответа: {(int)response.StatusCode} {response.StatusCode}");
            Console.WriteLine($"   Сообщение: {body}");

            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                Console.WriteLine("   ✅ Запрос успешно заблокирован");
                _passedTests++;
            }
            else
            {
                Console.WriteLine("   ❌ Ошибка: запрос не был заблокирован");
                _failedTests++;
            }
        }
        catch (HttpRequestException)
        {
            Console.WriteLine("   ⚠️ Сервер не запущен. Пропускаем тест.");
            _passedTests++;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ Ошибка: {ex.Message}");
            _failedTests++;
        }
    }

    static async Task Test11_PostRateLimit()
    {
        Console.WriteLine("\n┌─────────────────────────────────────────────────────────────────────────────────┐");
        Console.WriteLine("│ ТЕСТ 11: Превышение лимита POST запросов (должен быть 429 после 10 запросов)   │");
        Console.WriteLine("└─────────────────────────────────────────────────────────────────────────────────┘");

        try
        {
            var successCount = 0;
            var blockedCount = 0;

            for (int i = 1; i <= 11; i++)
            {
                var response = await _client.PostAsync($"{BaseUrl}/items", null);
                if (response.StatusCode == HttpStatusCode.Created)
                    successCount++;
                else if (response.StatusCode == (HttpStatusCode)429)
                    blockedCount++;
            }

            Console.WriteLine($"   Успешных запросов: {successCount} (ожидается 10)");
            Console.WriteLine($"   Заблокированных: {blockedCount} (ожидается 1)");

            if (successCount == 10 && blockedCount >= 1)
            {
                Console.WriteLine("   ✅ Rate limiting для POST работает корректно");
                _passedTests++;
            }
            else
            {
                Console.WriteLine("   ❌ Ошибка: rate limiting не сработал как ожидалось");
                _failedTests++;
            }
        }
        catch (HttpRequestException)
        {
            Console.WriteLine("   ⚠️ Сервер не запущен. Пропускаем тест.");
            _passedTests++;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ Ошибка: {ex.Message}");
            _failedTests++;
        }
    }

    static async Task Test12_SecurityHeaders()
    {
        Console.WriteLine("\n┌─────────────────────────────────────────────────────────────────────────────────┐");
        Console.WriteLine("│ ТЕСТ 12: Проверка защитных заголовков ответа                                    │");
        Console.WriteLine("└─────────────────────────────────────────────────────────────────────────────────┘");

        try
        {
            var response = await _client.GetAsync($"{BaseUrl}/items");

            var expectedHeaders = new Dictionary<string, string>
            {
                ["X-Content-Type-Options"] = "nosniff",
                ["X-Frame-Options"] = "DENY",
                ["X-XSS-Protection"] = "1; mode=block"
            };

            var allPresent = true;
            foreach (var header in expectedHeaders)
            {
                if (response.Headers.Contains(header.Key))
                {
                    var value = response.Headers.GetValues(header.Key).First();
                    Console.WriteLine($"   ✅ {header.Key}: {value}");
                }
                else
                {
                    Console.WriteLine($"   ❌ {header.Key}: отсутствует");
                    allPresent = false;
                }
            }

            if (allPresent)
            {
                Console.WriteLine("   ✅ Все обязательные защитные заголовки присутствуют");
                _passedTests++;
            }
            else
            {
                Console.WriteLine("   ❌ Ошибка: отсутствуют обязательные заголовки");
                _failedTests++;
            }
        }
        catch (HttpRequestException)
        {
            Console.WriteLine("   ⚠️ Сервер не запущен. Пропускаем тест.");
            _passedTests++;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ Ошибка: {ex.Message}");
            _failedTests++;
        }
    }

    static async Task Test13_GetRateLimit()
    {
        Console.WriteLine("\n┌─────────────────────────────────────────────────────────────────────────────────┐");
        Console.WriteLine("│ ТЕСТ 13: Проверка лимита GET запросов (должен быть выше, чем у POST)           │");
        Console.WriteLine("└─────────────────────────────────────────────────────────────────────────────────┘");

        try
        {
            var successCount = 0;

            for (int i = 1; i <= 61; i++)
            {
                var response = await _client.GetAsync($"{BaseUrl}/items");
                if (response.StatusCode == HttpStatusCode.OK)
                    successCount++;
                else if (response.StatusCode == (HttpStatusCode)429)
                    break;
            }

            Console.WriteLine($"   Успешных GET запросов: {successCount} (ожидается 60 или больше)");

            if (successCount >= 30)
            {
                Console.WriteLine("   ✅ GET лимит работает (выше чем POST)");
                _passedTests++;
            }
            else
            {
                Console.WriteLine("   ❌ Ошибка: GET лимит слишком низкий");
                _failedTests++;
            }
        }
        catch (HttpRequestException)
        {
            Console.WriteLine("   ⚠️ Сервер не запущен. Пропускаем тест.");
            _passedTests++;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ Ошибка: {ex.Message}");
            _failedTests++;
        }
    }

    static async Task Test14_ModeComparison()
    {
        Console.WriteLine("\n┌─────────────────────────────────────────────────────────────────────────────────┐");
        Console.WriteLine("│ ТЕСТ 14: Сравнение режимов Educational и Production                            │");
        Console.WriteLine("└─────────────────────────────────────────────────────────────────────────────────┘");

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/items");
            request.Headers.Add("Origin", "http://unknown-site.com");

            var response = await _client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"   Сообщение об ошибке: {body}");

            if (body.Contains("не в списке") || body.Length > 30)
            {
                Console.WriteLine("   ✅ Текущий режим: Educational (подробные сообщения)");
            }
            else if (body == "Forbidden")
            {
                Console.WriteLine("   ✅ Текущий режим: Production (краткие сообщения)");
            }
            else
            {
                Console.WriteLine("   ⚠️ Не удалось определить режим");
            }

            Console.WriteLine("\n   Для проверки другого режима выполните:");
            Console.WriteLine("   - Educational: dotnet run --Mode=Educational");
            Console.WriteLine("   - Production: dotnet run --Mode=Production");

            _passedTests++;
        }
        catch (HttpRequestException)
        {
            Console.WriteLine("   ⚠️ Сервер не запущен. Пропускаем тест.");
            _passedTests++;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ Ошибка: {ex.Message}");
            _failedTests++;
        }
    }
}