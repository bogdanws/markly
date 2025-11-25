namespace markly.Configuration;

public class RateLimitingSettings
{
    public const string SectionName = "RateLimiting";
    
    /// <summary>
    /// Maximum number of AI suggestion requests per time window.
    /// </summary>
    public int MaxRequestsPerWindow { get; set; } = 10;
    
    /// <summary>
    /// Time window in seconds for rate limiting.
    /// </summary>
    public int WindowSeconds { get; set; } = 60;
}
