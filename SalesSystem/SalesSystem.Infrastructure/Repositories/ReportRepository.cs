using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces.Repositories;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Domain.Enums;
using SalesSystem.Infrastructure.Data;

namespace SalesSystem.Infrastructure.Repositories;

public class ReportRepository : IReportRepository
{
    private readonly SalesDbContext _context;
    private readonly ILogger<ReportRepository> _logger;

    public ReportRepository(SalesDbContext context, ILogger<ReportRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<SalesReportDto>> GetSalesReportAsync(int? warehouseId, DateTime from, DateTime to, CancellationToken ct)
    {
        var startDate = from.Date;
        var endDate = to.Date.AddDays(1).AddTicks(-1);

        return await _context.SalesInvoices
            .Include(i => i.Customer)
            .Where(i => i.Status == InvoiceStatus.Posted &&
                        i.InvoiceDate >= startDate &&
                        i.InvoiceDate <= endDate &&
                        (!warehouseId.HasValue || i.WarehouseId == warehouseId.Value))
            .OrderBy(i => i.InvoiceDate)
            .Select(i => new SalesReportDto(
                i.InvoiceDate,
                i.Id,
                i.Customer != null ? i.Customer.Name : "عميل نقدي",
                i.SubTotal,
                i.DiscountAmount,
                i.TaxAmount,
                i.TotalAmount,
                i.PaidAmount,
                i.DueAmount
            ))
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<PurchaseReportDto>> GetPurchasesReportAsync(int? warehouseId, DateTime from, DateTime to, CancellationToken ct)
    {
        var startDate = from.Date;
        var endDate = to.Date.AddDays(1).AddTicks(-1);

        return await _context.PurchaseInvoices
            .Include(i => i.Supplier)
            .Where(i => i.Status == InvoiceStatus.Posted &&
                        i.InvoiceDate >= startDate &&
                        i.InvoiceDate <= endDate &&
                        (!warehouseId.HasValue || i.WarehouseId == warehouseId.Value))
            .OrderBy(i => i.InvoiceDate)
            .Select(i => new PurchaseReportDto(
                i.InvoiceDate,
                i.Id,
                i.Supplier != null ? i.Supplier.Name : "Unknown",
                i.SubTotal,
                i.DiscountAmount,
                i.TaxAmount,
                i.TotalAmount,
                i.PaidAmount,
                i.DueAmount
            ))
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<StockReportDto>> GetStockReportAsync(int? warehouseId, CancellationToken ct)
    {
        var query = _context.WarehouseStocks
            .Include(s => s.Product)
                .ThenInclude(p => p!.Category)
            .Include(s => s.Product)
                .ThenInclude(p => p!.Unit)
            .Include(s => s.Warehouse)
            .AsQueryable();

        if (warehouseId.HasValue)
        {
            query = query.Where(s => s.WarehouseId == warehouseId.Value);
        }

        return await query
            .OrderBy(s => s.Warehouse!.Name)
            .ThenBy(s => s.Product!.Name)
            .Select(s => new StockReportDto(
                s.ProductId,
                s.Product!.Name,
                s.Product!.Category != null ? s.Product!.Category.Name : "General",
                s.Product!.Unit != null ? s.Product!.Unit.Name : "-",
                s.Warehouse!.Name,
                s.Quantity,
                s.ReorderLevel,
                0m, // TODO: Phase 25 — PurchasePrice moved to ProductPrices table
                s.Quantity * 0m // TODO: Phase 25 — total cost needs ProductUnit lookup
            ))
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<LowStockReportDto>> GetLowStockReportAsync(int? warehouseId, CancellationToken ct)
    {
        var stocks = await _context.WarehouseStocks
            .Include(s => s.Product)
                .ThenInclude(p => p!.WholesaleUnit)
            .Include(s => s.Product)
                .ThenInclude(p => p!.RetailUnit)
            .Include(s => s.Warehouse)
            .Where(s => s.Quantity <= s.ReorderLevel && s.ReorderLevel > 0 && (!warehouseId.HasValue || s.WarehouseId == warehouseId.Value))
            .ToListAsync(ct);

        return stocks.Select(s =>
        {
            var product = s.Product!;
            var deficit = s.ReorderLevel - s.Quantity;
            return new LowStockReportDto(
                s.ProductId,
                product.Name,
                product.Category?.Name ?? "General",
                s.Warehouse!.Name,
                s.Quantity,
                s.ReorderLevel,
                deficit,
                product.ConvertRetailToWholesaleBoxes(deficit),
                product.GetRemainingRetailAfterWholesale(deficit),
                product.WholesaleUnit?.Name ?? "-",
                product.RetailUnit?.Name ?? "-",
                product.ConversionFactor
            );
        });
    }

    public async Task<IEnumerable<CustomerFinancialBalanceDto>> GetCustomerBalancesReportAsync(int? customerId, CancellationToken ct)
    {
        var query = _context.Customers.AsQueryable();

        if (customerId.HasValue)
            query = query.Where(c => c.Id == customerId.Value);

        return await query
            .OrderBy(c => c.Name)
            .Select(c => new CustomerFinancialBalanceDto(
                c.Id,
                c.Name,
                c.OpeningBalance,
                _context.SalesInvoices.Where(i => i.CustomerId == c.Id && i.Status == InvoiceStatus.Posted).Sum(i => i.TotalAmount),
                _context.SalesReturns.Where(r => r.CustomerId == c.Id).Sum(r => r.TotalAmount),
                _context.CustomerPayments.Where(p => p.CustomerId == c.Id).Sum(p => p.Amount),
                0m, // TotalCredit (PlaceHolder)
                c.CurrentBalance
            ))
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<SupplierBalanceReportDto>> GetSupplierBalancesReportAsync(int? supplierId, CancellationToken ct)
    {
        var query = _context.Suppliers.AsQueryable();

        if (supplierId.HasValue)
            query = query.Where(s => s.Id == supplierId.Value);

        return await query
            .OrderBy(s => s.Name)
            .Select(s => new SupplierBalanceReportDto(
                s.Id,
                s.Name,
                s.OpeningBalance,
                _context.PurchaseInvoices.Where(i => i.SupplierId == s.Id && i.Status == InvoiceStatus.Posted).Sum(i => i.TotalAmount),
                _context.PurchaseReturns.Where(r => r.SupplierId == s.Id).Sum(r => r.TotalAmount),
                _context.SupplierPayments.Where(p => p.SupplierId == s.Id).Sum(p => p.Amount),
                0m, // TotalDebit (PlaceHolder)
                s.CurrentBalance
            ))
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<ProductMovementReportDto>> GetProductMovementsReportAsync(int productId, DateTime? from, DateTime? to, CancellationToken ct)
    {
        var query = _context.InventoryMovements
            .Include(m => m.Warehouse)
            .Where(m => m.ProductId == productId);

        if (from.HasValue)
            query = query.Where(m => m.MovementDate >= from.Value.Date);

        if (to.HasValue)
            query = query.Where(m => m.MovementDate <= to.Value.Date.AddDays(1).AddTicks(-1));

        return await query
            .OrderByDescending(m => m.MovementDate)
            .Select(m => new ProductMovementReportDto(
                m.MovementDate,
                m.Warehouse!.Name,
                m.MovementType.ToString(),
                m.ReferenceType + " #" + m.ReferenceId,
                m.QuantityChange,
                m.QuantityAfter
            ))
            .ToListAsync(ct);
    }

    public async Task<List<StockBalanceReportDto>> GetStockBalanceReportAsync(int? warehouseId, CancellationToken ct)
    {
        _logger.LogInformation("Fetching stock balance report for warehouse: {WarehouseId}", warehouseId);

        var query = _context.WarehouseStocks
            .Include(ws => ws.Product)
                .ThenInclude(p => p!.Category)
            .Include(ws => ws.Product)
                .ThenInclude(p => p!.Units)
            .Include(ws => ws.Warehouse)
            .AsQueryable();

        if (warehouseId.HasValue)
            query = query.Where(ws => ws.WarehouseId == warehouseId.Value);

        return await query
            .OrderBy(ws => ws.Warehouse!.Name)
            .ThenBy(ws => ws.Product!.Name)
            .Select(ws => new StockBalanceReportDto(
                ws.ProductId,
                ws.Product!.Name,
                ws.Product!.Category != null ? ws.Product!.Category.Name : null,
                ws.WarehouseId,
                ws.Warehouse!.Name,
                ws.Quantity,
                ws.ReorderLevel,
                ws.Product.Units.Where(pu => pu.IsBaseUnit).Select(pu => pu.AverageCost).FirstOrDefault(),
                ws.Quantity * ws.Product.Units.Where(pu => pu.IsBaseUnit).Select(pu => pu.AverageCost).FirstOrDefault()
            ))
            .ToListAsync(ct);
    }

    public async Task<List<WarehouseMovementReportDto>> GetWarehouseMovementsAsync(
        int? warehouseId, DateTime? from, DateTime? to, CancellationToken ct)
    {
        _logger.LogInformation("Fetching warehouse movements — warehouseId: {Id}, from: {From}, to: {To}",
            warehouseId, from, to);

        var query = _context.InventoryMovements
            .Include(im => im.Product)
            .Include(im => im.Warehouse)
            .AsQueryable();

        if (warehouseId.HasValue)
            query = query.Where(im => im.WarehouseId == warehouseId.Value);

        if (from.HasValue)
        {
            var fromDate = from.Value.Date;
            query = query.Where(im => im.MovementDate >= fromDate);
        }

        if (to.HasValue)
        {
            var toDate = to.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(im => im.MovementDate <= toDate);
        }

        var items = await query
            .OrderByDescending(im => im.MovementDate)
            .Select(im => new
            {
                im.MovementDate,
                im.ProductId,
                ProductName = im.Product!.Name,
                WarehouseName = im.Warehouse!.Name,
                im.MovementType,
                im.QuantityChange,
                im.QuantityBefore,
                im.QuantityAfter,
                im.ReferenceType,
                im.ReferenceId
            })
            .ToListAsync(ct);

        return items.Select(im => new WarehouseMovementReportDto(
            im.MovementDate,
            im.ProductId,
            im.ProductName,
            im.WarehouseName,
            GetMovementTypeArabic(im.MovementType),
            im.QuantityChange,
            im.QuantityBefore,
            im.QuantityAfter,
            im.ReferenceType,
            im.ReferenceId
        )).ToList();
    }

    private static string GetMovementTypeArabic(MovementType type) => type switch
    {
        MovementType.PurchaseIn => "مشتريات",
        MovementType.SaleOut => "مبيعات",
        MovementType.SaleReturnIn => "مرتجع مبيعات",
        MovementType.PurchaseReturnOut => "مرتجع مشتريات",
        MovementType.TransferOut => "تحويل خارج",
        MovementType.TransferIn => "تحويل داخل",
        MovementType.Adjustment => "تسوية",
        _ => type.ToString()
    };

    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken ct)
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var salesToday = await _context.SalesInvoices
            .Where(i => i.Status == InvoiceStatus.Posted && i.InvoiceDate >= today && i.InvoiceDate < tomorrow)
            .ToListAsync(ct);

        var purchasesToday = await _context.PurchaseInvoices
            .Where(i => i.Status == InvoiceStatus.Posted && i.InvoiceDate >= today && i.InvoiceDate < tomorrow)
            .SumAsync(i => i.TotalAmount, ct);

        var salesMonth = await _context.SalesInvoices
            .Where(i => i.Status == InvoiceStatus.Posted && i.InvoiceDate >= monthStart && i.InvoiceDate < tomorrow)
            .SumAsync(i => i.TotalAmount, ct);

        var purchasesMonth = await _context.PurchaseInvoices
            .Where(i => i.Status == InvoiceStatus.Posted && i.InvoiceDate >= monthStart && i.InvoiceDate < tomorrow)
            .SumAsync(i => i.TotalAmount, ct);

        var lowStockCount = await _context.WarehouseStocks
            .CountAsync(s => s.Quantity <= s.ReorderLevel && s.ReorderLevel > 0, ct);

        var activeCustomersCount = await _context.Customers
            .CountAsync(c => c.IsActive, ct);

        var activeSuppliersCount = await _context.Suppliers
            .CountAsync(s => s.IsActive, ct);

        var totalProductsCount = await _context.Products
            .CountAsync(p => p.IsActive, ct);

        var totalReceivables = await _context.Customers
            .SumAsync(c => c.CurrentBalance, ct);

        var totalPayables = await _context.Suppliers
            .SumAsync(s => s.CurrentBalance, ct);

        return new DashboardSummaryDto(
            salesToday.Sum(i => i.TotalAmount),
            salesToday.Count,
            purchasesToday,
            lowStockCount,
            activeCustomersCount,
            activeSuppliersCount,
            totalProductsCount,
            totalReceivables,
            totalPayables,
            salesMonth,
            purchasesMonth
        );
    }
}
