using SalesSystem.Domain.Enums;

namespace SalesSystem.Application.Interfaces.Repositories;

public interface ISystemSettingsRepository
{
    Task<CostingMethod> GetCostingMethodAsync(CancellationToken ct = default);
    Task SetCostingMethodAsync(CostingMethod method, CancellationToken ct = default);
    Task<string?> GetStringAsync(string key, string? defaultValue = null, CancellationToken ct = default);
    Task SetStringAsync(string key, string value, string? category = null, int? userId = null, CancellationToken ct = default);

    /// <summary>
    /// Reads a boolean setting with safe parsing. Logs a warning on parse failure and returns defaultValue.
    /// </summary>
    Task<bool> GetBoolAsync(string key, bool defaultValue = false, CancellationToken ct = default);

    /// <summary>
    /// Reads an integer setting with safe parsing. Logs a warning on parse failure and returns defaultValue.
    /// </summary>
    Task<int> GetIntAsync(string key, int defaultValue = 0, CancellationToken ct = default);

    /// <summary>
    /// Reads a decimal setting with safe parsing. Logs a warning on parse failure and returns defaultValue.
    /// </summary>
    Task<decimal> GetDecimalAsync(string key, decimal defaultValue = 0m, CancellationToken ct = default);

    /// <summary>
    /// Gets ALL system settings as a key-value dictionary.
    /// </summary>
    Task<Dictionary<string, string>> GetAllSystemSettingsAsync(CancellationToken ct = default);

    /// <summary>
    /// Batch updates multiple system settings and persists changes.
    /// </summary>
    Task SetBatchSystemSettingsAsync(Dictionary<string, string> settings, CancellationToken ct = default);

    /// <summary>
    /// Invalidates the in-memory cache for the given key, or all SystemSettings keys if key is null.
    /// </summary>
    Task InvalidateCache(string? key = null);
}