using task3.Config;

namespace task3.Services;

public class ConfigValidator
{
    public bool Validate(AppSettings settings, out List<string> errors)
    {
        errors = new List<string>();

        if (settings.Mode != "Educational" && settings.Mode != "Production")
            errors.Add("Mode должен быть Educational или Production");

        if (settings.TrustedOrigins == null || settings.TrustedOrigins.Count == 0)
            errors.Add("Должен быть хотя бы один доверенный источник");
        else
        {
            foreach (var origin in settings.TrustedOrigins)
            {
                if (!Uri.IsWellFormedUriString(origin, UriKind.Absolute))
                    errors.Add($"Некорректный URL доверенного источника: {origin}");
            }
        }

        if (settings.RateLimits.GeneralRequestsPerMinute <= 0)
            errors.Add("GeneralRequestsPerMinute должен быть > 0");

        if (settings.RateLimits.CreateRequestsPerMinute <= 0)
            errors.Add("CreateRequestsPerMinute должен быть > 0");

        return errors.Count == 0;
    }
}