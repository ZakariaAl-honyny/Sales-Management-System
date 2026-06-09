using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;

namespace SalesSystem.Application.Services;

/// <summary>
/// Background service that periodically checks warehouse stock levels
/// and logs warnings for products that have fallen below their reorder level.
/// The check interval is configurable via the SystemSetting "StockAlertIntervalMinutes"
/// (default: 360 minutes = 6 hours). The alert is gated by the "LowStockAlert" setting.
/// </summary>
public sealed class MinStockAlertWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MinStockAlertWorker> _logger;

    public MinStockAlertWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<MinStockAlertWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MinStockAlertWorker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Read the configured interval (default 6 hours) at the start of each cycle
                var intervalMinutes = await GetIntervalMinutesAsync(stoppingToken);
                _logger.LogDebug("MinStockAlertWorker will check again in {Interval} minutes", intervalMinutes);

                await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("MinStockAlertWorker cancelled during delay");
                break;
            }

            if (stoppingToken.IsCancellationRequested) break;

            try
            {
                await CheckLowStockAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("MinStockAlertWorker cancelled during stock check");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in MinStockAlertWorker during stock check");
            }
        }

        _logger.LogInformation("MinStockAlertWorker stopped");
    }

    /// <summary>
    /// Reads the "LowStockAlert" enable flag and "StockAlertIntervalMinutes" from SystemSettings.
    /// Default interval is 360 minutes (6 hours) if not configured.
    /// </summary>
    private async Task<int> GetIntervalMinutesAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var setting = await uow.SystemSettings.FirstOrDefaultAsync(
                s => s.SettingKey == "StockAlertIntervalMinutes", ct);

            if (setting != null && int.TryParse(setting.SettingValue, out var parsed) && parsed > 0)
                return parsed;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read StockAlertIntervalMinutes setting, using default 360");
        }

        return 360; // default 6 hours
    }

    /// <summary>
    /// Performs the low-stock check across all warehouses. Skips if LowStockAlert is disabled.
    /// Logs warnings for every product whose warehouse stock quantity is at or below its reorder level.
    /// </summary>
    private async Task CheckLowStockAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        // Gate: skip if LowStockAlert is disabled
        var alertEnabledSetting = await uow.SystemSettings.FirstOrDefaultAsync(
            s => s.SettingKey == "LowStockAlert", ct);

        if (alertEnabledSetting != null &&
            bool.TryParse(alertEnabledSetting.SettingValue, out var enabled) &&
            !enabled)
        {
            _logger.LogDebug("MinStockAlertWorker: LowStockAlert is disabled, skipping check");
            return;
        }

        _logger.LogInformation("MinStockAlertWorker: Checking low stock levels across all warehouses...");

        // Query all WarehouseStocks where quantity is at or below reorder level
        // Include Product and Warehouse navigations for detailed logging
        var lowStockItems = await uow.WarehouseStocks.ToListAsync(
            ws => ws.Quantity <= ws.ReorderLevel && ws.IsActive,
            queryConfig: q => q.OrderBy(ws => ws.ProductId).ThenBy(ws => ws.WarehouseId),
            ct: ct,
            includePaths: new[] { "Product", "Warehouse" });

        if (lowStockItems.Count == 0)
        {
            _logger.LogInformation("MinStockAlertWorker: No low stock items found");
            return;
        }

        _logger.LogWarning(
            "MinStockAlertWorker: Found {Count} low stock item(s)",
            lowStockItems.Count);

        foreach (var item in lowStockItems)
        {
            var productName = item.Product?.Name ?? $"(Id={item.ProductId})";
            var warehouseName = item.Warehouse?.Name ?? $"(Id={item.WarehouseId})";
            var reorderLevel = item.ReorderLevel;

            _logger.LogWarning(
                "Low stock alert: Product \"{ProductName}\" (ID {ProductId}) " +
                "in Warehouse \"{WarehouseName}\" (ID {WarehouseId}) — " +
                "Current Qty: {Quantity:N3}, Reorder Level: {ReorderLevel:N3}",
                productName, item.ProductId,
                warehouseName, item.WarehouseId,
                item.Quantity, reorderLevel);
        }

        // Also log a summary with total counts
        _logger.LogWarning(
            "MinStockAlertWorker: Summary — {Count} products below reorder level across all warehouses",
            lowStockItems.Count);
    }
}
