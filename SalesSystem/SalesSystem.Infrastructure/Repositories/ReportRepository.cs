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
            .Include(i => i.Customer).ThenInclude(c => c!.Party)
            .Where(i => i.Status == InvoiceStatus.Posted &&
                        i.InvoiceDate >= startDate &&
                        i.InvoiceDate <= endDate &&
                        (!warehouseId.HasValue || i.WarehouseId == warehouseId.Value))
            .OrderBy(i => i.InvoiceDate)
            .Select(i => new SalesReportDto(
                i.InvoiceDate,
                i.Id,
                i.Customer != null ? i.Customer.Party.Name : "عميل نقدي",
                i.SubTotal,
                i.DiscountAmount,
                i.TaxAmount,
                i.NetTotal,
                i.PaidAmount,
                i.RemainingAmount
            ))
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<PurchaseReportDto>> GetPurchasesReportAsync(int? warehouseId, DateTime from, DateTime to, CancellationToken ct)
    {
        var startDate = from.Date;
        var endDate = to.Date.AddDays(1).AddTicks(-1);

        var fromDateOnly = DateOnly.FromDateTime(startDate);
        var toDateOnly = DateOnly.FromDateTime(endDate);

        return await _context.PurchaseInvoices
            .Include(i => i.Supplier).ThenInclude(s => s!.Party)
            .Where(i => i.Status == InvoiceStatus.Posted &&
                        i.InvoiceDate >= fromDateOnly &&
                        i.InvoiceDate <= toDateOnly &&
                        (!warehouseId.HasValue || i.WarehouseId == warehouseId.Value))
            .OrderBy(i => i.InvoiceDate)
            .Select(i => new PurchaseReportDto(
                i.InvoiceDate.ToDateTime(TimeOnly.MinValue),
                i.Id,
                i.Supplier != null ? i.Supplier.Party.Name : "غير معروف",
                i.SubTotal,
                i.DiscountAmount,
                i.TaxAmount,
                i.NetTotal,
                i.PaidAmount,
                i.RemainingAmount
            ))
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<StockReportDto>> GetStockReportAsync(int? warehouseId, CancellationToken ct)
    {
        var query = _context.WarehouseStocks
            .Include(s => s.Product)
                .ThenInclude(p => p!.ProductCategory)
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
                s.Product!.ProductCategory != null ? s.Product.ProductCategory.Name : "General",
                "-",
                s.Warehouse!.Name,
                s.Quantity,
                s.Product.ReorderLevel,
                0m,
                s.Quantity * 0m
            ))
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<LowStockReportDto>> GetLowStockReportAsync(int? warehouseId, CancellationToken ct)
    {
        var stocks = await _context.WarehouseStocks
            .Include(s => s.Product)
                .ThenInclude(p => p!.ProductCategory)
            .Include(s => s.Warehouse)
            .Where(s => s.Quantity <= s.Product!.ReorderLevel && s.Product.ReorderLevel > 0 && (!warehouseId.HasValue || s.WarehouseId == warehouseId.Value))
            .ToListAsync(ct);

        return stocks.Select(s =>
        {
            var product = s.Product!;
            var deficit = product.ReorderLevel - s.Quantity;
            return new LowStockReportDto(
                s.ProductId,
                product.Name,
                product.ProductCategory?.Name ?? "General",
                s.Warehouse!.Name,
                s.Quantity,
                product.ReorderLevel,
                deficit,
                0m,
                0m,
                "-",
                "-",
                1m
            );
        });
    }

    public async Task<IEnumerable<CustomerFinancialBalanceDto>> GetCustomerBalancesReportAsync(int? customerId, CancellationToken ct)
    {
        var query = _context.Customers
            .Include(c => c.Account)
            .Include(c => c.Party)
            .AsQueryable();

        if (customerId.HasValue)
            query = query.Where(c => c.Id == customerId.Value);

        return await query
            .OrderBy(c => c.Party.Name)
            .Select(c => new CustomerFinancialBalanceDto(
                c.Id,
                c.Party.Name,
                0m,
                _context.SalesInvoices.Where(i => i.CustomerId == c.Id && i.Status == InvoiceStatus.Posted).Sum(i => i.NetTotal),
                _context.SalesReturns.Where(r => r.CustomerId == c.Id && r.Status == InvoiceStatus.Posted).Sum(r => r.TotalAmount),
                0m,
                0m,
                0m
            ))
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<SupplierBalanceReportDto>> GetSupplierBalancesReportAsync(int? supplierId, CancellationToken ct)
    {
        var query = _context.Suppliers
            .Include(s => s.Account)
            .Include(s => s.Party)
            .AsQueryable();

        if (supplierId.HasValue)
            query = query.Where(s => s.Id == supplierId.Value);

        return await query
            .OrderBy(s => s.Party.Name)
            .Select(s => new SupplierBalanceReportDto(
                s.Id,
                s.Party.Name,
                0m,
                _context.PurchaseInvoices.Where(i => i.SupplierId == s.Id && i.Status == InvoiceStatus.Posted).Sum(i => i.NetTotal),
                _context.PurchaseReturns.Where(r => r.SupplierId == s.Id && r.Status == InvoiceStatus.Posted).Sum(r => r.TotalAmount),
                _context.SupplierPayments.Where(p => p.SupplierId == s.Id).Sum(p => p.Amount),
                0m,
                0m
            ))
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<ProductMovementReportDto>> GetProductMovementsReportAsync(int productId, DateTime? from, DateTime? to, CancellationToken ct)
    {
        var query = _context.InventoryTransactionLines
            .Include(tl => tl.InventoryTransaction)
                .ThenInclude(t => t!.Warehouse)
            .Include(tl => tl.ProductUnit)
            .Where(tl => tl.ProductUnit!.ProductId == productId);

        if (from.HasValue)
            query = query.Where(tl => tl.InventoryTransaction!.CreatedAt >= from.Value.Date);

        if (to.HasValue)
            query = query.Where(tl => tl.InventoryTransaction!.CreatedAt <= to.Value.Date.AddDays(1).AddTicks(-1));

        var rawData = await query
            .OrderBy(tl => tl.InventoryTransaction!.CreatedAt)
            .ThenBy(tl => tl.Id)
            .Select(tl => new
            {
                tl.InventoryTransaction!.CreatedAt,
                WarehouseName = tl.InventoryTransaction.Warehouse!.Name,
                MovementType = tl.InventoryTransaction.MovementType,
                ReferenceType = tl.InventoryTransaction.ReferenceType,
                ReferenceId = tl.InventoryTransaction.ReferenceId,
                tl.Quantity
            })
            .ToListAsync(ct);

        decimal runningBalance = 0;
        var result = new List<ProductMovementReportDto>();

        foreach (var item in rawData)
        {
            var isOutgoing = item.MovementType == InventoryTransactionType.Sale
                          || item.MovementType == InventoryTransactionType.TransferOut
                          || item.MovementType == InventoryTransactionType.PurchaseReturn
                          || item.MovementType == InventoryTransactionType.Damage
                          || item.MovementType == InventoryTransactionType.InternalIssue;

            var quantityChange = isOutgoing ? -item.Quantity : item.Quantity;
            runningBalance += quantityChange;

            result.Add(new ProductMovementReportDto(
                item.CreatedAt,
                item.WarehouseName,
                item.MovementType.ToString(),
                $"{item.ReferenceType} #{item.ReferenceId}",
                quantityChange,
                runningBalance
            ));
        }

        return result.OrderByDescending(r => r.Date).ToList();
    }

    public async Task<List<StockBalanceReportDto>> GetStockBalanceReportAsync(int? warehouseId, CancellationToken ct)
    {
        _logger.LogInformation("Fetching stock balance report for warehouse: {WarehouseId}", warehouseId);

        var query = _context.WarehouseStocks
            .Include(ws => ws.Product)
                .ThenInclude(p => p!.ProductCategory)
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
                ws.Product!.ProductCategory != null ? ws.Product.ProductCategory.Name : null,
                ws.WarehouseId,
                ws.Warehouse!.Name,
                ws.Quantity,
                ws.Product.ReorderLevel,
                0m,
                0m
            ))
            .ToListAsync(ct);
    }

    public async Task<List<WarehouseMovementReportDto>> GetWarehouseMovementsAsync(
        int? warehouseId, DateTime? from, DateTime? to, CancellationToken ct)
    {
        _logger.LogInformation("Fetching warehouse movements — warehouseId: {Id}, from: {From}, to: {To}",
            warehouseId, from, to);

        var query = _context.InventoryTransactionLines
            .Include(tl => tl.InventoryTransaction)
                .ThenInclude(t => t!.Warehouse)
            .Include(tl => tl.ProductUnit)
                .ThenInclude(pu => pu.Product)
            .AsQueryable();

        if (warehouseId.HasValue)
            query = query.Where(tl => tl.InventoryTransaction!.WarehouseId == warehouseId.Value);

        if (from.HasValue)
        {
            var fromDate = from.Value.Date;
            query = query.Where(tl => tl.InventoryTransaction!.CreatedAt >= fromDate);
        }

        if (to.HasValue)
        {
            var toDate = to.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(tl => tl.InventoryTransaction!.CreatedAt <= toDate);
        }

        var items = await query
            .OrderByDescending(tl => tl.InventoryTransaction!.CreatedAt)
            .Select(tl => new
            {
                tl.InventoryTransaction!.CreatedAt,
                ProductId = (int)tl.ProductUnit!.ProductId,
                ProductName = tl.ProductUnit.Product.Name,
                WarehouseName = tl.InventoryTransaction.Warehouse!.Name,
                MovementType = tl.InventoryTransaction.MovementType,
                tl.Quantity,
                ReferenceType = tl.InventoryTransaction.ReferenceType,
                tl.InventoryTransaction.ReferenceId,
                WarehouseId = (short)tl.InventoryTransaction.WarehouseId
            })
            .ToListAsync(ct);

        var balances = new Dictionary<(int ProductId, short WarehouseId), decimal>();
        return items.Select(im =>
        {
            var key = (im.ProductId, im.WarehouseId);
            var before = balances.GetValueOrDefault(key, 0m);

            var isOutgoing = im.MovementType == InventoryTransactionType.Sale
                          || im.MovementType == InventoryTransactionType.TransferOut
                          || im.MovementType == InventoryTransactionType.PurchaseReturn
                          || im.MovementType == InventoryTransactionType.Damage
                          || im.MovementType == InventoryTransactionType.InternalIssue;

            var change = isOutgoing ? -im.Quantity : im.Quantity;
            var after = before + change;
            balances[key] = after;

            return new WarehouseMovementReportDto(
                im.CreatedAt,
                im.ProductId,
                im.ProductName,
                im.WarehouseName,
                GetInventoryTransactionTypeArabic(im.MovementType),
                change,
                before,
                after,
                im.ReferenceType?.ToString(),
                im.ReferenceId
            );
        }).ToList();
    }

    // ─────────────────────────────────────────────────────────────────────
    // Detailed Stock Ledger
    // ─────────────────────────────────────────────────────────────────────
    public async Task<List<DetailedStockLedgerDto>> GetDetailedStockLedgerAsync(
        int? productId, int? warehouseId, DateTime? from, DateTime? to, CancellationToken ct)
    {
        _logger.LogInformation("Fetching detailed stock ledger — productId: {ProductId}, warehouseId: {WhId}, from: {From}, to: {To}",
            productId, warehouseId, from, to);

        var query = _context.InventoryTransactionLines
            .Include(tl => tl.InventoryTransaction)
                .ThenInclude(t => t!.Warehouse)
            .Include(tl => tl.ProductUnit)
                .ThenInclude(pu => pu.Product)
            .AsQueryable();

        if (productId.HasValue)
            query = query.Where(tl => tl.ProductUnit!.ProductId == productId.Value);

        if (warehouseId.HasValue)
            query = query.Where(tl => tl.InventoryTransaction!.WarehouseId == warehouseId.Value);

        if (from.HasValue)
        {
            var fromDate = from.Value.Date;
            query = query.Where(tl => tl.InventoryTransaction!.CreatedAt >= fromDate);
        }

        if (to.HasValue)
        {
            var toDate = to.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(tl => tl.InventoryTransaction!.CreatedAt <= toDate);
        }

        var rawData = await query
            .OrderBy(tl => tl.InventoryTransaction!.CreatedAt)
            .ThenBy(tl => tl.Id)
            .Select(tl => new
            {
                tl.InventoryTransaction!.CreatedAt,
                tl.InventoryTransaction.TransactionNo,
                MovementType = tl.InventoryTransaction.MovementType,
                tl.InventoryTransaction.ReferenceType,
                tl.InventoryTransaction.ReferenceId,
                ProductId = (int)tl.ProductUnit!.ProductId,
                ProductName = tl.ProductUnit.Product.Name,
                WarehouseName = tl.InventoryTransaction.Warehouse!.Name,
                tl.Quantity,
                tl.UnitCost,
                TotalCost = tl.Quantity * tl.UnitCost,
                WarehouseId = (short)tl.InventoryTransaction.WarehouseId,
                tl.InventoryTransaction.CreatedByUserId
            })
            .ToListAsync(ct);

        // Compute running balance per (ProductId, WarehouseId)
        var balances = new Dictionary<(int ProductId, short WarehouseId), decimal>();
        var now = DateTime.UtcNow;
        var users = await _context.Users
            .Where(u => rawData.Select(r => r.CreatedByUserId).Distinct().Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.UserName, ct);

        return rawData.Select(r =>
        {
            var key = (r.ProductId, r.WarehouseId);
            var before = balances.GetValueOrDefault<(int, short), decimal>(key, 0m);

            var isOutgoing = r.MovementType == InventoryTransactionType.Sale
                          || r.MovementType == InventoryTransactionType.TransferOut
                          || r.MovementType == InventoryTransactionType.PurchaseReturn
                          || r.MovementType == InventoryTransactionType.Damage
                          || r.MovementType == InventoryTransactionType.InternalIssue;

            var change = isOutgoing ? -r.Quantity : r.Quantity;
            var after = before + change;
            balances[key] = after;

            var refNo = r.TransactionNo.ToString();
            var refType = r.ReferenceType?.ToString() ?? r.MovementType.ToString();

            var createdByName = r.CreatedByUserId > 0 && users.TryGetValue(r.CreatedByUserId, out var name)
                ? name
                : null;

            return new DetailedStockLedgerDto(
                r.CreatedAt,
                refNo,
                refType,
                GetInventoryTransactionTypeArabic(r.MovementType),
                before,
                change,
                after,
                r.UnitCost,
                r.TotalCost,
                createdByName
            );
        }).ToList();
    }

    // ─────────────────────────────────────────────────────────────────────
    // Returns Report
    // ─────────────────────────────────────────────────────────────────────
    public async Task<List<ReturnsReportDto>> GetReturnsReportAsync(
        string? returnType, DateTime? from, DateTime? to, int? productId, CancellationToken ct)
    {
        _logger.LogInformation("Fetching returns report — returnType: {Type}, from: {From}, to: {To}, productId: {ProductId}",
            returnType, from, to, productId);

        var result = new List<ReturnsReportDto>();

        // Sales returns (unless filtering to Purchases only)
        if (string.IsNullOrEmpty(returnType) || returnType == "Sales")
        {
            var salesQuery = _context.SalesReturnLines
                .Include(srl => srl.SalesReturn)
                    .ThenInclude(sr => sr!.Customer)
                        .ThenInclude(c => c!.Party)
                .Include(srl => srl.SalesInvoiceLine)
                    .ThenInclude(sil => sil.Product)
                .Where(srl => srl.SalesReturn!.Status == InvoiceStatus.Posted)
                .AsQueryable();

            if (from.HasValue)
            {
                var fromDate = from.Value.Date;
                salesQuery = salesQuery.Where(srl => srl.SalesReturn!.ReturnDate >= fromDate);
            }
            if (to.HasValue)
            {
                var toDate = to.Value.Date.AddDays(1).AddTicks(-1);
                salesQuery = salesQuery.Where(srl => srl.SalesReturn!.ReturnDate <= toDate);
            }
            if (productId.HasValue)
                salesQuery = salesQuery.Where(srl => srl.SalesInvoiceLine!.ProductId == productId.Value);

            var salesData = await salesQuery
                .Select(srl => new ReturnsReportDto(
                    srl.SalesReturn!.ReturnNo.ToString(),
                    srl.SalesReturn.ReturnDate,
                    "مبيعات",
                    srl.SalesReturn.Customer != null ? srl.SalesReturn.Customer.Party.Name : null,
                    srl.SalesInvoiceLine!.Product!.Name,
                    srl.Quantity,
                    srl.Amount,
                    null,
                    srl.SalesReturn.Status == InvoiceStatus.Posted ? "مرحل" : "مسودة"
                ))
                .ToListAsync(ct);

            result.AddRange(salesData);
        }

        // Purchase returns (unless filtering to Sales only)
        if (string.IsNullOrEmpty(returnType) || returnType == "Purchases")
        {
            var purchaseQuery = _context.PurchaseReturnLines
                .Include(prl => prl.PurchaseReturn)
                    .ThenInclude(pr => pr!.Supplier)
                        .ThenInclude(s => s!.Party)
                .Include(prl => prl.PurchaseInvoiceLine)
                    .ThenInclude(pil => pil.Product)
                .Where(prl => prl.PurchaseReturn!.Status == InvoiceStatus.Posted)
                .AsQueryable();

            if (from.HasValue)
            {
                var fromDate = DateOnly.FromDateTime(from.Value.Date);
                purchaseQuery = purchaseQuery.Where(prl => prl.PurchaseReturn!.ReturnDate >= fromDate);
            }
            if (to.HasValue)
            {
                var toDate = DateOnly.FromDateTime(to.Value.Date.AddDays(1).AddTicks(-1));
                purchaseQuery = purchaseQuery.Where(prl => prl.PurchaseReturn!.ReturnDate <= toDate);
            }
            if (productId.HasValue)
                purchaseQuery = purchaseQuery.Where(prl => prl.PurchaseInvoiceLine!.ProductId == productId.Value);

            var purchaseData = await purchaseQuery
                .Select(prl => new ReturnsReportDto(
                    prl.PurchaseReturn!.ReturnNo.ToString(),
                    prl.PurchaseReturn.ReturnDate.ToDateTime(TimeOnly.MinValue),
                    "مشتريات",
                    prl.PurchaseReturn.Supplier.Party.Name,
                    prl.PurchaseInvoiceLine!.Product!.Name,
                    prl.Quantity,
                    prl.Amount,
                    null,
                    prl.PurchaseReturn.Status == InvoiceStatus.Posted ? "مرحل" : "مسودة"
                ))
                .ToListAsync(ct);

            result.AddRange(purchaseData);
        }

        return result.OrderByDescending(r => r.Date).ToList();
    }

    // ─────────────────────────────────────────────────────────────────────
    // Aging Report
    // ─────────────────────────────────────────────────────────────────────
    public async Task<List<AgingReportDto>> GetAgingReportAsync(
        string partyType, int? partyId, CancellationToken ct)
    {
        _logger.LogInformation("Fetching aging report — partyType: {Type}, partyId: {Id}", partyType, partyId);

        var today = DateTime.Today;
        var result = new List<AgingReportDto>();

        if (partyType == "Customers" || partyType == "Suppliers")
        {
            if (partyType == "Customers")
            {
                // Query customers with their posted sales invoices for aging computation
                var customersQuery = _context.Customers
                    .Include(c => c.Party)
                    .Where(c => c.IsActive)
                    .AsQueryable();

                if (partyId.HasValue)
                    customersQuery = customersQuery.Where(c => c.Id == partyId.Value);

                var customers = await customersQuery.ToListAsync(ct);

                foreach (var customer in customers)
                {
                    var invoices = await _context.SalesInvoices
                        .Where(i => i.CustomerId == customer.Id && i.Status == InvoiceStatus.Posted)
                        .ToListAsync(ct);

                    var totalDue = invoices.Sum(i => i.RemainingAmount);
                    var totalBalance = totalDue;
                    decimal current = 0, days1To30 = 0, days31To60 = 0, days61To90 = 0, days90Plus = 0;

                    foreach (var inv in invoices)
                    {
                        if (inv.RemainingAmount <= 0) continue;
                        var ageDays = (today - inv.InvoiceDate).Days;

                        if (ageDays <= 0) current += inv.RemainingAmount;
                        else if (ageDays <= 30) days1To30 += inv.RemainingAmount;
                        else if (ageDays <= 60) days31To60 += inv.RemainingAmount;
                        else if (ageDays <= 90) days61To90 += inv.RemainingAmount;
                        else days90Plus += inv.RemainingAmount;
                    }

                    result.Add(new AgingReportDto(
                        customer.Party.Name,
                        totalBalance,
                        current,
                        days1To30,
                        days31To60,
                        days61To90,
                        days90Plus,
                        totalDue
                    ));
                }
            }
            else // Suppliers
            {
                var suppliersQuery = _context.Suppliers
                    .Include(s => s.Party)
                    .Where(s => s.IsActive)
                    .AsQueryable();

                if (partyId.HasValue)
                    suppliersQuery = suppliersQuery.Where(s => s.Id == partyId.Value);

                var suppliers = await suppliersQuery.ToListAsync(ct);

                var todayDateOnly = DateOnly.FromDateTime(today);
                foreach (var supplier in suppliers)
                {
                    var invoices = await _context.PurchaseInvoices
                        .Where(i => i.SupplierId == supplier.Id && i.Status == InvoiceStatus.Posted)
                        .ToListAsync(ct);

                    var totalDue = invoices.Sum(i => i.RemainingAmount);
                    var totalBalance = totalDue;
                    decimal current = 0, days1To30 = 0, days31To60 = 0, days61To90 = 0, days90Plus = 0;

                    foreach (var inv in invoices)
                    {
                        if (inv.RemainingAmount <= 0) continue;
                        var ageDays = todayDateOnly.DayNumber - inv.InvoiceDate.DayNumber;

                        if (ageDays <= 0) current += inv.RemainingAmount;
                        else if (ageDays <= 30) days1To30 += inv.RemainingAmount;
                        else if (ageDays <= 60) days31To60 += inv.RemainingAmount;
                        else if (ageDays <= 90) days61To90 += inv.RemainingAmount;
                        else days90Plus += inv.RemainingAmount;
                    }

                    result.Add(new AgingReportDto(
                        supplier.Party.Name,
                        totalBalance,
                        current,
                        days1To30,
                        days31To60,
                        days61To90,
                        days90Plus,
                        totalDue
                    ));
                }
            }
        }

        return result.OrderByDescending(r => r.TotalDue).ToList();
    }

    private static string GetInventoryTransactionTypeArabic(InventoryTransactionType type) => type switch
    {
        InventoryTransactionType.Purchase => "مشتريات",
        InventoryTransactionType.Sale => "مبيعات",
        InventoryTransactionType.SaleReturn => "مرتجع مبيعات",
        InventoryTransactionType.PurchaseReturn => "مرتجع مشتريات",
        InventoryTransactionType.TransferOut => "تحويل خارج",
        InventoryTransactionType.TransferIn => "تحويل داخل",
        InventoryTransactionType.Adjustment => "تسوية",
        InventoryTransactionType.Damage => "تلف",
        InventoryTransactionType.OpeningBalance => "رصيد افتتاحي",
        InventoryTransactionType.InternalIssue => "صرف داخلي",
        InventoryTransactionType.InternalReceipt => "إيراد داخلي",
        InventoryTransactionType.Count => "جرد",
        _ => type.ToString()
    };

    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken ct)
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var todayDateOnly = DateOnly.FromDateTime(today);
        var tomorrowDateOnly = DateOnly.FromDateTime(tomorrow);
        var monthStartDateOnly = DateOnly.FromDateTime(monthStart);

        var salesToday = await _context.SalesInvoices
            .Where(i => i.Status == InvoiceStatus.Posted && i.InvoiceDate >= today && i.InvoiceDate < tomorrow)
            .ToListAsync(ct);

        var purchasesToday = await _context.PurchaseInvoices
            .Where(i => i.Status == InvoiceStatus.Posted && i.InvoiceDate >= todayDateOnly && i.InvoiceDate < tomorrowDateOnly)
            .SumAsync(i => i.NetTotal, ct);

        var salesMonth = await _context.SalesInvoices
            .Where(i => i.Status == InvoiceStatus.Posted && i.InvoiceDate >= monthStart && i.InvoiceDate < tomorrow)
            .SumAsync(i => i.NetTotal, ct);

        var purchasesMonth = await _context.PurchaseInvoices
            .Where(i => i.Status == InvoiceStatus.Posted && i.InvoiceDate >= monthStartDateOnly && i.InvoiceDate < tomorrowDateOnly)
            .SumAsync(i => i.NetTotal, ct);

        var lowStockCount = await _context.WarehouseStocks
            .CountAsync(s => s.Quantity <= s.Product!.ReorderLevel && s.Product.ReorderLevel > 0, ct);

        var activeCustomersCount = await _context.Customers
            .CountAsync(c => c.IsActive, ct);

        var activeSuppliersCount = await _context.Suppliers
            .CountAsync(s => s.IsActive, ct);

        var totalProductsCount = await _context.Products
            .CountAsync(p => p.IsActive, ct);

        // TODO: Compute from JournalEntryLine balances once accounting engine is fully wired.
        // Party no longer has PartyType or AccountId — receivables/payables are now on Customer.AccountId and Supplier.AccountId.
        var totalReceivables = 0m;
        var totalPayables = 0m;

        return new DashboardSummaryDto(
            salesToday.Sum(i => i.NetTotal),
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
