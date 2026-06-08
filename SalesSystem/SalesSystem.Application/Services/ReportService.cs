using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Repositories;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;

namespace SalesSystem.Application.Services;

public class ReportService : IReportService
{
    private readonly IUnitOfWork _uow;
    private readonly IReportRepository _reportRepository;
    private readonly ILogger<ReportService> _logger;

    public ReportService(IUnitOfWork uow, IReportRepository reportRepository, ILogger<ReportService> logger)
    {
        _uow = uow;
        _reportRepository = reportRepository;
        _logger = logger;
    }

    public async Task<Result<IEnumerable<SalesReportDto>>> GetSalesReportAsync(int? warehouseId, DateTime from, DateTime to, CancellationToken ct)
    {
        try
        {
            if (from > to)
            {
                _logger.LogWarning("Sales report failed: Start date {From} is after end date {To}", from, to);
                return Result<IEnumerable<SalesReportDto>>.Failure("تاريخ البداية يجب أن يكون قبل تاريخ النهاية");
            }

            _logger.LogInformation("Generating sales report from {From} to {To} for warehouse {WarehouseId}", from, to, warehouseId);
            var report = await _reportRepository.GetSalesReportAsync(warehouseId, from, to, ct);
            return Result<IEnumerable<SalesReportDto>>.Success(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating sales report");
            return Result<IEnumerable<SalesReportDto>>.Failure("حدث خطأ أثناء إنشاء تقرير المبيعات");
        }
    }

    public async Task<Result<IEnumerable<PurchaseReportDto>>> GetPurchasesReportAsync(int? warehouseId, DateTime from, DateTime to, CancellationToken ct)
    {
        try
        {
            if (from > to)
                return Result<IEnumerable<PurchaseReportDto>>.Failure("تاريخ البداية يجب أن يكون قبل تاريخ النهاية");

            _logger.LogInformation("Generating purchases report from {From} to {To} for warehouse {WarehouseId}", from, to, warehouseId);
            var report = await _reportRepository.GetPurchasesReportAsync(warehouseId, from, to, ct);
            return Result<IEnumerable<PurchaseReportDto>>.Success(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating purchases report");
            return Result<IEnumerable<PurchaseReportDto>>.Failure("حدث خطأ أثناء إنشاء تقرير المشتريات");
        }
    }

    public async Task<Result<IEnumerable<StockReportDto>>> GetStockReportAsync(int? warehouseId, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Generating stock report for warehouse: {WarehouseId}", warehouseId ?? 0);
            var report = await _reportRepository.GetStockReportAsync(warehouseId, ct);
            return Result<IEnumerable<StockReportDto>>.Success(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating stock report");
            return Result<IEnumerable<StockReportDto>>.Failure("حدث خطأ أثناء إنشاء تقرير المخزون");
        }
    }

    public async Task<Result<IEnumerable<CustomerFinancialBalanceDto>>> GetCustomerBalancesReportAsync(int? customerId, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Generating customer balances report for customer: {CustomerId}", customerId ?? 0);
            var report = await _reportRepository.GetCustomerBalancesReportAsync(customerId, ct);
            return Result<IEnumerable<CustomerFinancialBalanceDto>>.Success(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating customer balances report");
            return Result<IEnumerable<CustomerFinancialBalanceDto>>.Failure("حدث خطأ أثناء إنشاء تقرير أرصدة العملاء");
        }
    }

    public async Task<Result<IEnumerable<SupplierBalanceReportDto>>> GetSupplierBalancesReportAsync(int? supplierId, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Generating supplier balances report for supplier: {SupplierId}", supplierId ?? 0);
            var report = await _reportRepository.GetSupplierBalancesReportAsync(supplierId, ct);
            return Result<IEnumerable<SupplierBalanceReportDto>>.Success(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating supplier balances report");
            return Result<IEnumerable<SupplierBalanceReportDto>>.Failure("حدث خطأ أثناء إنشاء تقرير أرصدة الموردين");
        }
    }

    public async Task<Result<IEnumerable<ProductMovementReportDto>>> GetProductMovementsReportAsync(int productId, DateTime? from, DateTime? to, CancellationToken ct)
    {
        try
        {
            if (productId <= 0)
                return Result<IEnumerable<ProductMovementReportDto>>.Failure("معرف المنتج غير صالح");

            _logger.LogInformation("Generating product movements report for product: {ProductId}", productId);
            var report = await _reportRepository.GetProductMovementsReportAsync(productId, from, to, ct);
            return Result<IEnumerable<ProductMovementReportDto>>.Success(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating product movements report");
            return Result<IEnumerable<ProductMovementReportDto>>.Failure("حدث خطأ أثناء إنشاء تقرير حركات المنتج");
        }
    }

    public async Task<Result<IEnumerable<LowStockReportDto>>> GetLowStockReportAsync(int? warehouseId, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Generating low stock report for warehouse: {WarehouseId}", warehouseId);
            var report = await _reportRepository.GetLowStockReportAsync(warehouseId, ct);
            return Result<IEnumerable<LowStockReportDto>>.Success(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating low stock report");
            return Result<IEnumerable<LowStockReportDto>>.Failure("حدث خطأ أثناء إنشاء تقرير المخزون المنخفض");
        }
    }

    public async Task<Result<DashboardSummaryDto>> GetDashboardSummaryAsync(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Generating dashboard summary");
            var summary = await _reportRepository.GetDashboardSummaryAsync(ct);
            return Result<DashboardSummaryDto>.Success(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating dashboard summary");
            return Result<DashboardSummaryDto>.Failure("حدث خطأ أثناء إنشاء ملخص لوحة التحكم");
        }
    }

    public async Task<Result<IEnumerable<ExpiredProductDto>>> GetExpiredProductsReportAsync(int thresholdDays, CancellationToken ct)
    {
        try
        {
            if (thresholdDays < 0)
            {
                _logger.LogWarning("Expired products report called with negative threshold: {ThresholdDays}", thresholdDays);
                return Result<IEnumerable<ExpiredProductDto>>.Failure("عدد الأيام يجب أن يكون صفراً أو أكثر");
            }

            var cutoffDate = DateTime.Today.AddDays(thresholdDays);

            _logger.LogInformation("Generating expired products report with threshold {ThresholdDays} days (cutoff: {CutoffDate})",
                thresholdDays, cutoffDate);

            // Phase 25: ExpirationDate moved from Product to InventoryBatch.ExpiryDate
            var productIds = await _uow.InventoryBatches.ToListAsync(
                b => b.ExpiryDate.HasValue && b.ExpiryDate.Value <= cutoffDate,
                null, ct, false);

            var distinctProductIds = productIds.Select(b => b.ProductId).Distinct().ToList();

            var products = await _uow.Products.ToListAsync(
                p => distinctProductIds.Contains(p.Id),
                null, ct, false,
                includePaths: new[] { "Category" });

            // Get earliest expiry per product
            var productEarliestExpiry = productIds
                .GroupBy(b => b.ProductId)
                .ToDictionary(g => g.Key, g => g.Min(b => b.ExpiryDate!.Value));

            // Fetch real stock quantities for expired products
            var stocks = await _uow.WarehouseStocks.ToListAsync(
                ws => distinctProductIds.Contains(ws.ProductId),
                null, ct, false, "Warehouse");
            var stockByProduct = stocks
                .GroupBy(ws => ws.ProductId)
                .ToDictionary(g => g.Key, g => g.Sum(ws => ws.Quantity));

            var dtos = products.Select(p =>
            {
                stockByProduct.TryGetValue(p.Id, out var totalStock);
                var expiryDate = productEarliestExpiry.GetValueOrDefault(p.Id, DateTime.Today);
                return new ExpiredProductDto(
                    p.Id,
                    p.Name,
                    p.Category?.Name,
                    null,
                    totalStock,
                    expiryDate,
                    (DateTime.Today - expiryDate).Days
                );
            }).ToList();

            _logger.LogInformation("Found {Count} expired/expiring products within {ThresholdDays} days", dtos.Count, thresholdDays);

            return Result<IEnumerable<ExpiredProductDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating expired products report");
            return Result<IEnumerable<ExpiredProductDto>>.Failure("حدث خطأ أثناء إنشاء تقرير المنتجات المنتهية");
        }
    }
}
