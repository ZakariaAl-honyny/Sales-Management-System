using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces.Repositories;
using SalesSystem.Domain.Entities;
using SalesSystem.Infrastructure.Data;

namespace SalesSystem.Infrastructure.Repositories;

public class SystemSettingsRepository : ISystemSettingsRepository
{
    private readonly SalesDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SystemSettingsRepository> _logger;

    private static readonly ConcurrentDictionary<string, byte> _cachedKeys = new();
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public SystemSettingsRepository(
        SalesDbContext context,
        IMemoryCache cache,
        ILogger<SystemSettingsRepository> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    public async Task<string?> GetStringAsync(string key, string? defaultValue = null, CancellationToken ct = default)
    {
        var cacheKey = $"sys:{key}";
        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            _cachedKeys.TryAdd(cacheKey, 0);
            entry.SlidingExpiration = CacheDuration;

            var setting = await _context.SystemSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SettingKey == key, ct);

            return setting?.SettingValue ?? defaultValue;
        });
    }

    public async Task SetStringAsync(string key, string value, string? category = null, int? userId = null, CancellationToken ct = default)
    {
        var setting = await _context.SystemSettings
            .FirstOrDefaultAsync(s => s.SettingKey == key, ct);

        if (setting != null)
        {
            setting.UpdateValue(value, userId);
        }
        else
        {
            var newSetting = SystemSetting.Create(key, value, category: category ?? "General");
            _context.SystemSettings.Add(newSetting);
        }

        // Cache invalidation — caller is responsible for SaveChanges via IUnitOfWork
        InvalidateCacheSync(key);
        _logger.LogInformation("SystemSetting {Key} updated to {Value}", key, value);
    }

    public async Task<bool> GetBoolAsync(string key, bool defaultValue = false, CancellationToken ct = default)
    {
        var raw = await GetStringAsync(key, null, ct);
        if (raw == null)
            return defaultValue;

        if (bool.TryParse(raw, out var result))
            return result;

        _logger.LogWarning("Failed to parse SystemSetting '{Key}' value '{Value}' as bool. Using default '{Default}'.",
            key, raw, defaultValue);
        return defaultValue;
    }

    public async Task<int> GetIntAsync(string key, int defaultValue = 0, CancellationToken ct = default)
    {
        var raw = await GetStringAsync(key, null, ct);
        if (raw == null)
            return defaultValue;

        if (int.TryParse(raw, out var result))
            return result;

        _logger.LogWarning("Failed to parse SystemSetting '{Key}' value '{Value}' as int. Using default '{Default}'.",
            key, raw, defaultValue);
        return defaultValue;
    }

    public async Task<decimal> GetDecimalAsync(string key, decimal defaultValue = 0m, CancellationToken ct = default)
    {
        var raw = await GetStringAsync(key, null, ct);
        if (raw == null)
            return defaultValue;

        if (decimal.TryParse(raw, out var result))
            return result;

        _logger.LogWarning("Failed to parse SystemSetting '{Key}' value '{Value}' as decimal. Using default '{Default}'.",
            key, raw, defaultValue);
        return defaultValue;
    }

    public async Task<Dictionary<string, string>> GetAllSystemSettingsAsync(CancellationToken ct = default)
    {
        return await _context.SystemSettings
            .AsNoTracking()
            .Select(s => new { s.SettingKey, s.SettingValue })
            .ToDictionaryAsync(s => s.SettingKey, s => s.SettingValue, ct);
    }

    public async Task SetBatchSystemSettingsAsync(Dictionary<string, string> settings, CancellationToken ct = default)
    {
        foreach (var kvp in settings)
        {
            var setting = await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.SettingKey == kvp.Key, ct);

            if (setting != null)
            {
                setting.UpdateValue(kvp.Value);
            }
            else
            {
                var newSetting = SystemSetting.Create(kvp.Key, kvp.Value);
                _context.SystemSettings.Add(newSetting);
            }
        }

        // NO SaveChangesAsync here — caller is responsible via IUnitOfWork (RULE-024)
        InvalidateCacheSync();
        _logger.LogInformation("Batch prepared {Count} SystemSettings for save", settings.Count);
    }

    /// <summary>
    /// Invalidates the in-memory cache for the given key, or all SystemSettings keys if key is null.
    /// </summary>
    public Task InvalidateCache(string? key = null)
    {
        InvalidateCacheSync(key);
        return Task.CompletedTask;
    }

    private void InvalidateCacheSync(string? key = null)
    {
        if (key != null)
        {
            var cacheKey = $"sys:{key}";
            _cache.Remove(cacheKey);
            _cachedKeys.TryRemove(cacheKey, out _);
        }
        else
        {
            foreach (var k in _cachedKeys.Keys)
                _cache.Remove(k);
            _cachedKeys.Clear();
        }
    }
}