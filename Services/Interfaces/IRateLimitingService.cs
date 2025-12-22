namespace markly.Services.Interfaces;

public interface IRateLimitingService
{
    /// <summary>
    /// Checks if the user can make an AI suggestion request.
    /// Returns true if allowed, false if rate limited.
    /// </summary>
    Task<bool> TryAcquireAsync(string userId, string endpoint);
    
    /// <summary>
    /// Gets the remaining time until the user can make another request.
    /// </summary>
    Task<TimeSpan?> GetTimeUntilNextAllowedAsync(string userId, string endpoint);
}
