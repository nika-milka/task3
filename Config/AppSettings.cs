namespace task3.Config;

public class AppSettings
{
    public string Mode { get; set; } = "Educational"; // Educational или Production
    public List<string> TrustedOrigins { get; set; } = new();
    public RateLimitSettings RateLimits { get; set; } = new();
    public bool EnableDetailedErrors { get; set; } = true;
}

public class RateLimitSettings
{
    public int GeneralRequestsPerMinute { get; set; } = 60;
    public int CreateRequestsPerMinute { get; set; } = 10;
}