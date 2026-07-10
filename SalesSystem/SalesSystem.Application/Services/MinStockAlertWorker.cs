using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Domain.Enums;

namespace SalesSystem.Application.Services;

/// <summary>
/// Background service that periodically checks warehouse stock levels
/// and logs warnings for products whose stock will be exhausted within
/// <c>StockAlertDays</c> (default 5) based on average daily sales over
/// the last 30 days. Products without recent sales fall back to the
/// <c>Product.ReorderLevel</c> threshold. Gated by the "LowStockAlert"
/// setting; polling interval controlled by "StockAlertIntervalMinutes"
/// (default 360 minutes = 6 hours).
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

        return 360;
    }

    private async Task<int> GetStockAlertDaysAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var setting = await uow.SystemSettings.FirstOrDefaultAsync(
                s => s.SettingKey == "StockAlertDays", ct);

            if (setting != null && int.TryParse(setting.SettingValue, out var parsed) && parsed > 0)
                return parsed;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read StockAlertDays setting, using default 5");
        }

        return 5;
    }

    /// <summary>
    /// Computes average daily sales quantity per product from posted sales invoices
    /// over the last 30 days. Returns a dictionary keyed by ProductId.
    /// </summary>
    private async Task<Dictionary<int, decimal>> ComputeDailySalesRatesAsync(
        IUnitOfWork uow, CancellationToken ct)
    {
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

        var salesData = await uow.SalesInvoiceLines.Query()
            .Where(l => l.SalesInvoice != null
                     && l.SalesInvoice.Status == InvoiceStatus.Posted
                     && l.SalesInvoice.InvoiceDate >= thirtyDaysAgo)
            .GroupBy(l => l.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                TotalQty = g.Sum(l => l.Quantity)
            })
            .ToListAsync(ct);

        return salesData.ToDictionary(
            x => x.ProductId,
            x => x.TotalQty / 30m);
    }

    /// <summary>
    /// Performs the low-stock check across all warehouses. Skips if LowStockAlert is disabled.
    /// Alerts when a product's stock will be exhausted within StockAlertDays based on average
    /// daily sales, or when stock is at/below Product.ReorderLevel (fallback for products with
    /// no recent sales history).
    /// </summary>
    private async Task CheckLowStockAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        // Master gate: skip ALL notifications if EnableNotifications is disabled
        var notificationsEnabledSetting = await uow.SystemSettings.FirstOrDefaultAsync(
            s => s.SettingKey == "EnableNotifications", ct);
        if (notificationsEnabledSetting != null &&
            bool.TryParse(notificationsEnabledSetting.SettingValue, out var notificationsEnabled) &&
            !notificationsEnabled)
        {
            _logger.LogDebug("MinStockAlertWorker: EnableNotifications is disabled, skipping check");
            return;
        }

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

        var stockAlertDays = await GetStockAlertDaysAsync(ct);
        var dailySalesRates = await ComputeDailySalesRatesAsync(uow, ct);

        _logger.LogInformation(
            "MinStockAlertWorker: Checking stock levels (alert threshold: {Days} days)...",
            stockAlertDays);

        // Fetch all warehouse stocks with Product and Warehouse navigation properties
        var allStocks = await uow.WarehouseStocks.ToListAsync(
            null,
            queryConfig: q => q.OrderBy(ws => ws.ProductId).ThenBy(ws => ws.WarehouseId),
            ct: ct,
            includePaths: new[] { "Product", "Warehouse" });

        var lowStockItems = new List<(string ProductName, int ProductId, string WarehouseName,
            short WarehouseId, decimal Quantity, string Reason)>();

        foreach (var item in allStocks)
        {
            var productName = item.Product?.Name ?? $"(Id={item.ProductId})";
            var warehouseName = item.Warehouse?.Name ?? $"(Id={item.WarehouseId})";
            var reorderLevel = item.Product?.ReorderLevel ?? 0m;

            if (item.Quantity <= 0)
            {
                lowStockItems.Add((productName, item.ProductId, warehouseName,
                    item.WarehouseId, item.Quantity, "out of stock"));
                continue;
            }

            if (dailySalesRates.TryGetValue(item.ProductId, out var dailyRate) && dailyRate > 0)
            {
                var daysRemaining = item.Quantity / dailyRate;
                if (daysRemaining <= stockAlertDays)
                {
                    lowStockItems.Add((productName, item.ProductId, warehouseName,
                        item.WarehouseId, item.Quantity,
                        $"~{daysRemaining:F1} days remaining (daily avg: {dailyRate:N3})"));
                }
            }
            else if (reorderLevel > 0 && item.Quantity <= reorderLevel)
            {
                lowStockItems.Add((productName, item.ProductId, warehouseName,
                    item.WarehouseId, item.Quantity,
                    $"at/below reorder level ({reorderLevel:N3}), no recent sales data"));
            }
        }

        if (lowStockItems.Count == 0)
        {
            _logger.LogInformation("MinStockAlertWorker: No low stock items found");
            return;
        }

        _logger.LogWarning(
            "MinStockAlertWorker: Found {Count} low stock item(s) (threshold: {Days} days)",
            lowStockItems.Count, stockAlertDays);

        foreach (var item in lowStockItems)
        {
            _logger.LogWarning(
                "Low stock alert: Product \"{ProductName}\" (ID {ProductId}) " +
                "in Warehouse \"{WarehouseName}\" (ID {WarehouseId}) — " +
                "Current Qty: {Quantity:N3} — {Reason}",
                item.ProductName, item.ProductId,
                item.WarehouseName, item.WarehouseId,
                item.Quantity, item.Reason);
        }

        _logger.LogWarning(
            "MinStockAlertWorker: Summary — {Count} products with low/zero stock across all warehouses",
            lowStockItems.Count);
    }
}
