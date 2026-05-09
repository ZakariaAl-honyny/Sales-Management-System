using Microsoft.EntityFrameworkCore;
using SalesSystem.Application.Interfaces.Repositories;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Domain.Enums;
using SalesSystem.Infrastructure.Data;

namespace SalesSystem.Infrastructure.Data.Repositories;

public class ReportRepository : IReportRepository
{
    private readonly SalesDbContext _context;

    public ReportRepository(SalesDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<SalesReportDto>> GetSalesReportAsync(DateTime from, DateTime to, CancellationToken ct)
    {
        var startDate = from.Date;
        var endDate = to.Date.AddDays(1).AddTicks(-1);

        return await _context.SalesInvoices
            .Include(i => i.Customer)
            .Where(i => i.Status == InvoiceStatus.Posted &&
                        i.InvoiceDate >= startDate &&
                        i.InvoiceDate <= endDate)
            .OrderBy(i => i.InvoiceDate)
            .Select(i => new SalesReportDto(
                i.InvoiceDate,
                i.InvoiceNo,
                i.Customer != null ? i.Customer.Name : "ط¹ظ…ظٹظ„ ظ†ظ‚ط¯ظٹ",
                i.SubTotal,
                i.DiscountAmount,
                i.TaxAmount,
                i.TotalAmount,
                i.PaidAmount,
                i.DueAmount
            ))
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<PurchaseReportDto>> GetPurchasesReportAsync(DateTime from, DateTime to, CancellationToken ct)
    {
        var startDate = from.Date;
        var endDate = to.Date.AddDays(1).AddTicks(-1);

        return await _context.PurchaseInvoices
            .Include(i => i.Supplier)
            .Where(i => i.Status == InvoiceStatus.Posted &&
                        i.InvoiceDate >= startDate &&
                        i.InvoiceDate <= endDate)
            .OrderBy(i => i.InvoiceDate)
            .Select(i => new PurchaseReportDto(
                i.InvoiceDate,
                i.InvoiceNo,
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
                s.Product!.Code ?? string.Empty,
                s.Product!.Name,
                s.Product!.Category != null ? s.Product!.Category.Name : "General",
                s.Product!.Unit != null ? s.Product!.Unit.Name : "-",
                s.Warehouse!.Name,
                s.Quantity,
                s.ReorderLevel,
                s.Product!.PurchasePrice,
                s.Quantity * s.Product!.PurchasePrice
            ))
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<LowStockReportDto>> GetLowStockReportAsync(CancellationToken ct)
    {
        return await _context.WarehouseStocks
            .Include(s => s.Product)
                .ThenInclude(p => p!.Category)
            .Include(s => s.Product)
                .ThenInclude(p => p!.Unit)
            .Include(s => s.Warehouse)
            .Where(s => s.Quantity <= s.ReorderLevel && s.ReorderLevel > 0)
            .Select(s => new LowStockReportDto(
                s.ProductId,
                s.Product!.Code,
                s.Product!.Name,
                s.Product!.Category != null ? s.Product!.Category.Name : "General",
                s.Product!.Unit != null ? s.Product!.Unit.Name : "-",
                s.Warehouse!.Name,
                s.Quantity,
                s.ReorderLevel
            ))
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<CustomerBalanceReportDto>> GetCustomerBalancesReportAsync(int? customerId, CancellationToken ct)
    {
        var query = _context.Customers.AsQueryable();

        if (customerId.HasValue)
            query = query.Where(c => c.Id == customerId.Value);

        return await query
            .OrderBy(c => c.Name)
            .Select(c => new CustomerBalanceReportDto(
                c.Id,
                c.Code ?? string.Empty,
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
                s.Code ?? string.Empty,
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
}
