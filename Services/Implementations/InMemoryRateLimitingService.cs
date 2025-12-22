using System.Collections.Concurrent;
using markly.Configuration;
using markly.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace markly.Services.Implementations;

public class InMemoryRateLimitingService : IRateLimitingService
{
    private readonly RateLimitingSettings _settings;
    private readonly ConcurrentDictionary<string, RateLimitEntry> _entries = new();
    private readonly ILogger<InMemoryRateLimitingService> _logger;

    public InMemoryRateLimitingService(
        IOptions<RateLimitingSettings> settings,
        ILogger<InMemoryRateLimitingService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public Task<bool> TryAcquireAsync(string userId, string endpoint)
    {
        var key = GetKey(userId, endpoint);
        var now = DateTime.UtcNow;
        var windowStart = now.AddSeconds(-_settings.WindowSeconds);

        var entry = _entries.AddOrUpdate(
            key,
            // Add new entry
            _ => new RateLimitEntry
            {
                Requests = new List<DateTime> { now }
            },
            // Update existing entry
            (_, existing) =>
            {
                lock (existing)
                {
                    // Remove old requests outside the window
                    existing.Requests.RemoveAll(r => r < windowStart);
                    
                    if (existing.Requests.Count < _settings.MaxRequestsPerWindow)
                    {
                        existing.Requests.Add(now);
                        return existing;
                    }
                    
                    // Rate limited - don't add the request
                    return existing;
                }
            });

        bool allowed;
        lock (entry)
        {
            // Check if our request was added (last request is now)
            allowed = entry.Requests.Count > 0 && 
                      entry.Requests.Count <= _settings.MaxRequestsPerWindow &&
                      entry.Requests.LastOrDefault() == now;
        }

        if (!allowed)
        {
            _logger.LogWarning("Rate limit exceeded for user {UserId} on endpoint {Endpoint}", userId, endpoint);
        }

        return Task.FromResult(allowed);
    }

    public Task<TimeSpan?> GetTimeUntilNextAllowedAsync(string userId, string endpoint)
    {
        var key = GetKey(userId, endpoint);
        var now = DateTime.UtcNow;
        var windowStart = now.AddSeconds(-_settings.WindowSeconds);

        if (!_entries.TryGetValue(key, out var entry))
        {
            return Task.FromResult<TimeSpan?>(null);
        }

        lock (entry)
        {
            // Remove old requests
            entry.Requests.RemoveAll(r => r < windowStart);

            if (entry.Requests.Count < _settings.MaxRequestsPerWindow)
            {
                return Task.FromResult<TimeSpan?>(null);
            }

            // Find the oldest request in the window - when it expires, we can make another request
            var oldestRequest = entry.Requests.Min();
            var expiresAt = oldestRequest.AddSeconds(_settings.WindowSeconds);
            var timeUntilExpiry = expiresAt - now;

            return Task.FromResult<TimeSpan?>(timeUntilExpiry > TimeSpan.Zero ? timeUntilExpiry : null);
        }
    }

    private static string GetKey(string userId, string endpoint) => $"{userId}:{endpoint}";

    private class RateLimitEntry
    {
        public List<DateTime> Requests { get; set; } = new();
    }
}
