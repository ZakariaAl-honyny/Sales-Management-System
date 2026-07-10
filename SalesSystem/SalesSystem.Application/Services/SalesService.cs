using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Repositories;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Application.Printing;
using SalesSystem.Application.Printing.Contracts;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Domain.Accounting.Enums;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Entities;
using DomainDiscountType = SalesSystem.Domain.Enums.DiscountType;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Application.Services;

public class SalesService : ISalesService
{
    private readonly IUnitOfWork _uow;
    private readonly IInventoryService _inventoryService;
    private readonly ICashBoxService _cashBoxService;
    private readonly IPrintDataService _printDataService;
    private readonly IPrintService _printService;
    private readonly IAccountingIntegrationService _accountingService;
    private readonly IDocumentSequenceService _documentSequenceService;
    private readonly ISystemSettingsRepository _systemSettingsRepo;
    private readonly IProductCostService _productCostService;
    private readonly IProductPriceService _productPriceService;
    private readonly IFifoAllocationService _fifoAllocationService;
    private readonly ILogger<SalesService> _logger;

    public SalesService(
        IUnitOfWork uow,
        IInventoryService inventoryService,
        ICashBoxService cashBoxService,
        IPrintDataService printDataService,
        IPrintService printService,
        IAccountingIntegrationService accountingService,
        IDocumentSequenceService documentSequenceService,
        ISystemSettingsRepository systemSettingsRepo,
        IProductCostService productCostService,
        IProductPriceService productPriceService,
        IFifoAllocationService fifoAllocationService,
        ILogger<SalesService> logger)
    {
        _uow = uow;
        _inventoryService = inventoryService;
        _cashBoxService = cashBoxService;
        _printDataService = printDataService;
        _printService = printService;
        _accountingService = accountingService;
        _documentSequenceService = documentSequenceService;
        _systemSettingsRepo = systemSettingsRepo;
        _productCostService = productCostService;
        _productPriceService = productPriceService;
        _fifoAllocationService = fifoAllocationService;
        _logger = logger;
    }

    public async Task<Result<SalesInvoiceDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        var invoice = await _uow.SalesInvoices.FirstOrDefaultAsync(
            i => i.Id == id, ct, "Customer", "Warehouse", "Items.Product");

        if (invoice == null)
            return Result<SalesInvoiceDto>.Failure("فاتورة المبيعات غير موجودة", ErrorCodes.NotFound);

        return Result<SalesInvoiceDto>.Success(MapToDto(invoice));
    }

    public async Task<Result<PagedResult<SalesInvoiceDto>>> GetAllAsync(
        int? customerId, 
        int? status, 
        string? search = null, 
        DateTime? from = null, 
        DateTime? to = null, 
        int page = 1, 
        int pageSize = 10, 
        bool includeInactive = false, 
        CancellationToken ct = default)
    {
        // Build predicate dynamically for search conditions
        var searchLower = string.IsNullOrWhiteSpace(search) ? null : search.Trim().ToLower();

        // Parse search text as Id outside the expression tree (EF Core can't translate int.TryParse)
        int? searchId = int.TryParse(searchLower, out var parsedId) ? parsedId : null;

        System.Linq.Expressions.Expression<System.Func<SalesInvoice, bool>> predicate = i =>
            (includeInactive || i.Status != InvoiceStatus.Cancelled) &&
            (!customerId.HasValue || i.CustomerId == customerId.Value) &&
            (!status.HasValue || (int)i.Status == status.Value) &&
            (!from.HasValue || i.InvoiceDate >= from.Value) &&
            (!to.HasValue || i.InvoiceDate <= to.Value) &&
            (searchLower == null ||
             (searchId.HasValue && i.Id == searchId.Value) ||
             (i.Customer != null && i.Customer.Name.ToLower().Contains(searchLower)) ||
             (i.Notes != null && i.Notes.ToLower().Contains(searchLower)) ||
             i.Items.Any(item =>
                 item.Product != null &&
                 item.Product.Name.ToLower().Contains(searchLower)));

        var includes = new[] { "Customer", "Warehouse", "Items.Product" };

        var (items, total) = await _uow.SalesInvoices.GetPagedAsync(
            predicate, q => q.OrderByDescending(i => i.InvoiceDate), page, pageSize, ct, includeInactive, includes);

        var dtos = items.Select(MapToDto).ToList();

        return Result<PagedResult<SalesInvoiceDto>>.Success(PagedResult<SalesInvoiceDto>.Create(dtos, total, page, pageSize));
    }

    public async Task<Result<SalesInvoiceDto>> CreateAsync(CreateSalesInvoiceRequest request, int userId, CancellationToken ct)
    {
        return await _uow.ExecuteTransactionAsync<Result<SalesInvoiceDto>>(async () =>
        {
            try
            {
                // ── AllowDrafts setting gate ─────────────────────
                var allowDrafts = await _systemSettingsRepo.GetBoolAsync("AllowDrafts", true, ct);
                if (!allowDrafts)
                {
                    _logger.LogInformation("Draft invoices disabled via AllowDrafts setting");
                    return Result<SalesInvoiceDto>.Failure("السماح بالمسودات معطّل — يرجى الترحيل مباشرة");
                }

                // Resolve InvoiceNo INSIDE transaction — prevents TOCTOU race (RULE-384 fix)
                int invoiceNo;
                if (request.InvoiceNo.HasValue && request.InvoiceNo.Value > 0)
                {
                    var existing = await _uow.SalesInvoices.AnyAsync(i => i.InvoiceNo == request.InvoiceNo.Value, ct);
                    if (existing)
                        return Result<SalesInvoiceDto>.Failure("رقم الفاتورة موجود بالفعل");
                    invoiceNo = request.InvoiceNo.Value;
                }
                else
                {
                    var seqResult = await _documentSequenceService.GetNextIntAsync("SalesInvoice", ct);
                    if (!seqResult.IsSuccess)
                        return Result<SalesInvoiceDto>.Failure("فشل في توليد رقم الفاتورة");
                    invoiceNo = seqResult.Value;
                }

                // Auto-assign default cash customer if none specified
                var customerId = request.CustomerId;
                if (customerId <= 0 && (PaymentType)request.PaymentType == PaymentType.Cash)
                {
                    customerId = await _systemSettingsRepo.GetIntAsync("DefaultCashCustomerId", 1, ct);
                    _logger.LogInformation("Auto-assigned DefaultCashCustomerId={CustomerId} for cash sale", customerId);
                }

                // Apply DefaultWarehouse fallback when WarehouseId is not provided
                var warehouseId = request.WarehouseId;
                if (warehouseId <= 0)
                {
                    var defaultWarehouse = await _systemSettingsRepo.GetStringAsync("DefaultWarehouse", "1", ct) ?? "1";
                    warehouseId = short.Parse(defaultWarehouse);
                    _logger.LogInformation("Applied DefaultWarehouse fallback: WarehouseId={WarehouseId}", warehouseId);
                }

                var invoice = SalesInvoice.Create(
                    (short)warehouseId,
                    invoiceNo,
                    customerId,
                    request.InvoiceDate,
                    (PaymentType)request.PaymentType,
                    request.DiscountAmount,
                    otherCharges: request.OtherCharges,
                    notes: request.Notes,
                    cashBoxId: request.CashBoxId,
                    taxId: (short?)request.TaxId,
                    discountType: (Domain.Enums.DiscountType)request.DiscountType,
                    discountRate: request.DiscountRate,
                    createdByUserId: userId
                );
                foreach (var item in request.Items)
                {
                    var invoiceItem = SalesInvoiceLine.Create(
                        item.ProductId,
                        item.ProductUnitId,
                        item.Quantity,
                        item.UnitPrice,
                        (Domain.Enums.DiscountType)item.DiscountType,
                        item.DiscountRate
                    );
                    invoice.AddItem(invoiceItem);
                }

                invoice.SetTaxAmount(request.TaxAmount);
                invoice.SetPaidAmount(request.PaidAmount);

                // ── Price Enforcement (server-side) ─────────────────────
                var preventBelowRetailPrice = await _systemSettingsRepo.GetBoolAsync("PreventBelowRetailPrice", false, ct);
                if (preventBelowRetailPrice)
                {
                    foreach (var item in request.Items)
                    {
                        if (item.ProductUnitId <= 0) continue;
                        var priceResult = await _productPriceService.GetEffectivePriceForInvoiceAsync(
                            item.ProductUnitId, ct);
                        if (priceResult.IsSuccess && priceResult.Value != null && item.UnitPrice < priceResult.Value.Price)
                        {
                            _logger.LogWarning(
                                "Price enforcement: Item ProductId={ProductId}, unit price {UnitPrice} < registered price {RegisteredPrice}",
                                item.ProductId, item.UnitPrice, priceResult.Value.Price);
                            return Result<SalesInvoiceDto>.Failure(
                                $"سعر البيع أقل من السعر الرسمي للمنتج (معرف المنتج: {item.ProductId})");
                        }
                    }
                }

                await _uow.SalesInvoices.AddAsync(invoice, ct);
                await _uow.SaveChangesAsync(ct);

                _logger.LogInformation("Sales Invoice created as Draft: ID {Id} by User {UserId}", invoice.Id, userId);

                // Auto-post if setting is enabled
                var autoPost = await _systemSettingsRepo.GetBoolAsync("AutoPostInvoices", true, ct);
                if (autoPost)
                {
                    _logger.LogInformation("Auto-posting sales invoice {Id} via AutoPostInvoices setting", invoice.Id);
                    var postResult = await PostAsync(invoice.Id, userId, ct);
                    if (postResult.IsSuccess)
                        return postResult;
                    _logger.LogWarning("Auto-post failed for invoice {Id}: {Error} — returning draft", invoice.Id, postResult.Error);
                }

                return await GetByIdAsync(invoice.Id, ct);
            }
            catch (DomainException ex)
            {
                _logger.LogWarning(ex, "Validation error creating sales invoice draft");
                return Result<SalesInvoiceDto>.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating sales invoice draft");
                return Result<SalesInvoiceDto>.Failure("حدث خطأ أثناء حفظ مسودة الفاتورة");
            }
        }, ct);
    }

    public async Task<Result<SalesInvoiceDto>> CreateAndPostAsync(CreateSalesInvoiceRequest request, int userId, CancellationToken ct)
    {
        try
        {
            // ─── Step 1: Create as Draft (inline, single SaveChanges) ─────
            int invoiceNo;
            if (request.InvoiceNo.HasValue && request.InvoiceNo.Value > 0)
            {
                var existing = await _uow.SalesInvoices.AnyAsync(i => i.InvoiceNo == request.InvoiceNo.Value, ct);
                if (existing)
                    return Result<SalesInvoiceDto>.Failure("رقم الفاتورة موجود بالفعل");
                invoiceNo = request.InvoiceNo.Value;
            }
            else
            {
                var seqResult = await _documentSequenceService.GetNextIntAsync("SalesInvoice", ct);
                if (!seqResult.IsSuccess)
                    return Result<SalesInvoiceDto>.Failure("فشل في توليد رقم الفاتورة");
                invoiceNo = seqResult.Value;
            }

            var customerId = request.CustomerId;
            if (customerId <= 0 && (PaymentType)request.PaymentType == PaymentType.Cash)
                customerId = await _systemSettingsRepo.GetIntAsync("DefaultCashCustomerId", 1, ct);

            // Apply DefaultWarehouse fallback when WarehouseId is not provided
            var warehouseId = request.WarehouseId;
            if (warehouseId <= 0)
            {
                var defaultWarehouse = await _systemSettingsRepo.GetStringAsync("DefaultWarehouse", "1", ct) ?? "1";
                warehouseId = short.Parse(defaultWarehouse);
                _logger.LogInformation("Applied DefaultWarehouse fallback: WarehouseId={WarehouseId}", warehouseId);
            }

            var invoice = SalesInvoice.Create(
                (short)warehouseId, invoiceNo, customerId,
                request.InvoiceDate, (PaymentType)request.PaymentType,
                request.DiscountAmount, otherCharges: request.OtherCharges,
                notes: request.Notes, cashBoxId: request.CashBoxId,
                taxId: (short?)request.TaxId,
                discountType: (Domain.Enums.DiscountType)request.DiscountType, discountRate: request.DiscountRate,
                createdByUserId: userId);

            foreach (var item in request.Items)
            {
                var invoiceItem = SalesInvoiceLine.Create(
                    item.ProductId, item.ProductUnitId, item.Quantity, item.UnitPrice,
                    (Domain.Enums.DiscountType)item.DiscountType, item.DiscountRate);
                invoice.AddItem(invoiceItem);
            }

            invoice.SetTaxAmount(request.TaxAmount);
            invoice.SetPaidAmount(request.PaidAmount);

            // Price enforcement
            var preventBelowRetail = await _systemSettingsRepo.GetBoolAsync("PreventBelowRetailPrice", false, ct);
            if (preventBelowRetail)
            {
                foreach (var item in request.Items)
                {
                    if (item.ProductUnitId <= 0) continue;
                    var priceResult = await _productPriceService.GetEffectivePriceForInvoiceAsync(item.ProductUnitId, ct);
                    if (priceResult.IsSuccess && priceResult.Value != null && item.UnitPrice < priceResult.Value.Price)
                        return Result<SalesInvoiceDto>.Failure($"سعر البيع أقل من السعر الرسمي للمنتج (معرف المنتج: {item.ProductId})");
                }
            }

            await _uow.SalesInvoices.AddAsync(invoice, ct);
            await _uow.SaveChangesAsync(ct);

            // ─── Step 2: Post immediately (has its own ExecuteTransactionAsync) ─────
            var postResult = await PostAsync(invoice.Id, userId, ct);
            return postResult;
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Validation error creating and posting sales invoice");
            return Result<SalesInvoiceDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating and posting sales invoice");
            return Result<SalesInvoiceDto>.Failure("حدث خطأ أثناء إنشاء وترحيل الفاتورة");
        }
    }

    public async Task<Result<SalesInvoiceDto>> UpdateAsync(int id, UpdateSalesInvoiceRequest request, int userId, CancellationToken ct)
    {
        var invoice = await _uow.SalesInvoices.FirstOrDefaultAsync(
            i => i.Id == id, ct, "Items");

        if (invoice == null)
            return Result<SalesInvoiceDto>.Failure("فاتورة المبيعات غير موجودة", ErrorCodes.NotFound);

        if (invoice.Status != InvoiceStatus.Draft)
            return Result<SalesInvoiceDto>.Failure("يمكن تعديل الفواتير المسودة فقط");

        try
        {
            if (request.CustomerId > 0)
            {
                var customer = await _uow.Customers.GetByIdAsync(request.CustomerId, ct);
                if (customer == null)
                    return Result<SalesInvoiceDto>.Failure("العميل غير موجود");
            }

            invoice.UpdateTotals(request.DiscountAmount, request.TaxAmount, request.OtherCharges,
                (Domain.Enums.DiscountType)request.DiscountType, request.DiscountRate);
            invoice.SetPaidAmount(request.PaidAmount);

            // Re-create items (simplest way for draft)
            _uow.SalesInvoiceLines.DeleteRange(invoice.Items);
            invoice.Items.Clear();
            foreach (var item in request.Items)
            {
                var invoiceItem = SalesInvoiceLine.Create(
                    item.ProductId,
                    item.ProductUnitId,
                    item.Quantity,
                    item.UnitPrice,
                    (Domain.Enums.DiscountType)item.DiscountType,
                    item.DiscountRate
                );
                invoice.AddItem(invoiceItem);
            }

            await _uow.SalesInvoices.UpdateAsync(invoice, ct);
            await _uow.SaveChangesAsync(ct);

            return await GetByIdAsync(invoice.Id, ct);
        }
        catch (DomainException ex)
        {
            return Result<SalesInvoiceDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating sales invoice {Id}", id);
            return Result<SalesInvoiceDto>.Failure("حدث خطأ أثناء تحديث الفاتورة");
        }
    }

    public async Task<Result<SalesInvoiceDto>> PostAsync(int id, PostSalesInvoiceRequest request, int userId, CancellationToken ct)
    {
        // Delegate to the existing PostAsync which uses the invoice's own CashBoxId
        // The request can provide an override for CashBoxId if not already set
        if (request.CashBoxId.HasValue)
        {
            var invoice = await _uow.SalesInvoices.GetByIdAsync(id, ct);
            if (invoice != null && !invoice.CashBoxId.HasValue)
            {
                // CashBoxId can be set directly in the service if needed
                // For now, the existing post logic handles CashBoxId from the invoice entity
            }
        }

        return await PostAsync(id, userId, ct);
    }

    public async Task<Result<SalesInvoiceDto>> PostAsync(int id, int userId, CancellationToken ct)
    {
        var invoice = await _uow.SalesInvoices.FirstOrDefaultAsync(
            i => i.Id == id, ct, "Items.Product", "Items.Product.Units");

        if (invoice == null)
            return Result<SalesInvoiceDto>.Failure("الفاتورة غير موجودة", ErrorCodes.NotFound);

        if (invoice.Status != InvoiceStatus.Draft)
        {
            _logger.LogWarning("Cannot post invoice {Id} because status is {Status}", invoice.Id, invoice.Status);
            return Result<SalesInvoiceDto>.Failure("يمكن فقط ترحيل الفواتير المسودة");
        }

        var allowNegativeStock = await _systemSettingsRepo.GetBoolAsync("AllowNegativeStock", false, ct);

        // ── 0. Price Enforcement (BEFORE Transaction) ──────────────
        var preventBelowRetailPrice = await _systemSettingsRepo.GetBoolAsync("PreventBelowRetailPrice", false, ct);
        var allowBelowCostSale = await _systemSettingsRepo.GetBoolAsync("AllowBelowCostSale", true, ct);

                if (preventBelowRetailPrice)
                {
                    foreach (var item in invoice.Items)
                    {
                        var priceResult = await _productPriceService.GetEffectivePriceForInvoiceAsync(
                            item.ProductUnitId, ct);
                if (priceResult.IsSuccess && priceResult.Value != null && item.UnitPrice < priceResult.Value.Price)
                {
                    var productName = item.Product?.Name ?? $"معرف {item.ProductId}";
                    _logger.LogWarning(
                        "Price enforcement on post: Product '{Product}' unit price {UnitPrice} < registered price {RegisteredPrice}",
                        productName, item.UnitPrice, priceResult.Value.Price);
                    return Result<SalesInvoiceDto>.Failure(
                        $"سعر البيع أقل من السعر الرسمي للمنتج: {productName}");
                }
            }
        }

        if (!allowBelowCostSale)
        {
            // Fetch costs per distinct product for the warning check
            var productCosts = new Dictionary<int, decimal>();
            foreach (var productId in invoice.Items.Select(i => i.ProductId).Distinct())
            {
                var costResult = await _productCostService.GetAverageCostAsync(productId, ct);
                if (costResult.IsSuccess && costResult.Value > 0)
                    productCosts[productId] = costResult.Value;
            }

            foreach (var item in invoice.Items)
            {
                if (productCosts.TryGetValue(item.ProductId, out var avgCost) && item.UnitPrice < avgCost)
                {
                    var productName = item.Product?.Name ?? $"معرف {item.ProductId}";
                    _logger.LogWarning(
                        "Sale below cost: Product '{Product}' (unit price {UnitPrice} < cost {Cost}) for invoice #{InvoiceNo} — warning only, sale allowed",
                        productName, item.UnitPrice, avgCost, invoice.InvoiceNo);
                    // Per analysis: warning only, do not block — user can proceed
                }
            }
        }

        // 1. Validate Stock BEFORE Transaction
        foreach (var item in invoice.Items)
        {
            // Phase 25: GetRetailQuantityEquivalent removed. Quantity is in base units.
            var stockValidation = await _inventoryService.ValidateStockAsync(item.ProductId, invoice.WarehouseId, item.Quantity, allowNegativeStock, ct);
            if (!stockValidation.IsSuccess)
                return Result<SalesInvoiceDto>.Failure(stockValidation.Error!);
        }

        // 2. Open Transaction via ExecuteTransactionAsync
        return await _uow.ExecuteTransactionAsync<Result<SalesInvoiceDto>>(async () =>
        {
            try
            {
                invoice.Post();
                await _uow.SaveChangesAsync(ct);

                // 3. Deduct Stock
                foreach (var item in invoice.Items)
                {
                    var stockResult = await _inventoryService.DecreaseStockAsync(
                        item.ProductId,
                        invoice.WarehouseId,
                        item.Quantity,
                        unitCost: item.UnitPrice,
                        userId: userId,
                        ct: ct);

                    if (!stockResult.IsSuccess)
                    {
                        _logger.LogWarning("Stock decrease failed for sales invoice post: {Error}", stockResult.Error);
                        return Result<SalesInvoiceDto>.Failure(stockResult.Error!);
                    }
                }

                // 3a. FIFO/FEFO Batch Allocation — deduct from specific batches (BEFORE InventoryTransaction)
                var fifoAllocationsPerItem = new Dictionary<int, List<InventoryBatchAllocation>>();
                foreach (var item in invoice.Items)
                {
                    var fifoResult = await _fifoAllocationService.DeductFromBatchesAsync(
                        item.ProductId,
                        invoice.WarehouseId,
                        item.Quantity,
                        item.Id,
                        userId,
                        ct);

                    if (!fifoResult.IsSuccess)
                    {
                        _logger.LogWarning("FIFO allocation failed for sales invoice {Id}, Product {ProductId}: {Error}",
                            invoice.Id, item.ProductId, fifoResult.Error);
                        return Result<SalesInvoiceDto>.Failure(fifoResult.Error!);
                    }

                    fifoAllocationsPerItem[item.Id] = fifoResult.Value!;
                }

                // 3b. Create InventoryTransaction audit trail for stock decrease (with batch allocation info in BatchNo)
                var invTxSeq = await _documentSequenceService.GetNextIntAsync("InventoryTransaction", ct);
                if (invTxSeq.IsSuccess)
                {
                    var invTx = InventoryTransaction.Create(
                        invTxSeq.Value.ToString("D6"),
                        InventoryTransactionType.Sale,
                        (short)invoice.WarehouseId,
                        InventoryReferenceType.SalesInvoice,
                        invoice.Id,
                        $"بيع - فاتورة رقم {invoice.InvoiceNo}",
                        userId);
                    foreach (var item in invoice.Items)
                    {
                        // Encode FIFO batch allocations as "batchId:qty" pairs in BatchNo — enables restoration on cancel
                        string? batchNo = null;
                        if (fifoAllocationsPerItem.TryGetValue(item.Id, out var allocations) && allocations.Count > 0)
                        {
                            batchNo = string.Join(",", allocations.Select(a => $"{a.BatchId}:{a.Quantity}"));
                        }

                        invTx.AddLine(InventoryTransactionLine.Create(
                            invTx.Id, item.ProductUnitId, item.Quantity, item.UnitPrice, batchNo, null, (short)invoice.WarehouseId));
                    }
                    await _uow.InventoryTransactions.AddAsync(invTx, ct);
                }

                // 5. Update Customer Balance
                if (invoice.RemainingAmount > 0)
                {
                    var customer = await _uow.Customers.GetByIdAsync(invoice.CustomerId, ct);
                    if (customer == null)
                    {
                        _logger.LogWarning("Customer {CustomerId} not found for credit sales invoice {Id} post", invoice.CustomerId, invoice.Id);
                        return Result<SalesInvoiceDto>.Failure("العميل غير موجود");
                    }

                    // ── Credit Limit Alert setting gate ──────────────────
                    var creditLimitAlert = await _systemSettingsRepo.GetBoolAsync("CreditLimitAlert", true, ct);
                    if (creditLimitAlert)
                    {
                        // Credit limit enforcement — soft check only (balance tracked on linked Account via journal entries)
                        if (customer.CreditLimit > 0 && invoice.RemainingAmount > 0)
                        {
                            if (!customer.CheckCreditLimit(invoice.RemainingAmount))
                            {
                                _logger.LogWarning(
                                    "Customer {CustomerId} credit limit exceeded. Limit: {Limit}, Remaining: {Remaining}",
                                    customer.Id, customer.CreditLimit, invoice.RemainingAmount);
                                return Result<SalesInvoiceDto>.Failure("تجاوز الحد الائتماني للعميل");
                            }
                        }
                    }

                    // Balance is tracked on the linked Account via journal entries (AccountingIntegrationService).
                    // Direct In-memory balance tracking on Customer entity is removed — use Account balance instead.
                }

                // 6. Record payment voucher (سند صرف) if payment is linked to a cash box
                if (invoice.CashBoxId.HasValue && invoice.PaidAmount > 0)
                {
                    var paymentAccountId = await GetCashAccountIdAsync(ct);
                    var cashResult = await _cashBoxService.RecordInvoicePaymentAsync(
                        invoice.CashBoxId.Value,
                        invoice.PaidAmount,
                        paymentAccountId,
                        notes: null,
                        userId: userId,
                        ct: ct);

                    if (!cashResult.IsSuccess)
                    {
                        _logger.LogError("Payment voucher recording failed for invoice {Id}: {Error}",
                            invoice.Id, cashResult.Error);
                        return Result<SalesInvoiceDto>.Failure(cashResult.Error!);
                    }
                }

                await _uow.SaveChangesAsync(ct);

                // 7. Auto-print intent logging (actual print triggered from Desktop via API)
                var autoPrint = await _systemSettingsRepo.GetBoolAsync("AutoPrintAfterPosting", false, ct);
                if (autoPrint)
                {
                    _logger.LogInformation("AutoPrintAfterPosting enabled for sales invoice {InvoiceId}", invoice.Id);
                }

                // 8. Create journal entry for sales posting (revenue + COGS)
                var totalCost = 0m;
                var distinctProductIds = invoice.Items
                    .Select(item => item.ProductId)
                    .Distinct()
                    .ToList();
                foreach (var productId in distinctProductIds)
                {
                    var costResult = await _productCostService.GetAverageCostAsync(productId, ct);
                    var avgCost = costResult.IsSuccess ? costResult.Value : 0m;
                    var productQty = invoice.Items
                        .Where(item => item.ProductId == productId)
                        .Sum(item => item.Quantity);
                    totalCost += productQty * avgCost;
                }
                var entryResult = await _accountingService.CreateSalesPostEntryAsync(invoice, userId, totalCost, ct);
                if (!entryResult.IsSuccess)
                {
                    _logger.LogWarning("Journal entry creation failed for sales invoice post {Id}: {Error}", invoice.Id, entryResult.Error);
                    return Result<SalesInvoiceDto>.Failure(entryResult.Error!);
                }

                _logger.LogInformation("Sales Invoice posted: ID {Id} by User {UserId}", invoice.Id, userId);

                var postedResult = await GetByIdAsync(invoice.Id, ct);

                // Fire-and-forget auto-print if enabled — failure MUST NOT roll back the post
                if (postedResult.IsSuccess && postedResult.Value != null)
                {
                    var capturedInvoiceId = invoice.Id;
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var settingsResult = await _printDataService.GetPrintSettingsAsync(CancellationToken.None);
                            if (settingsResult.IsSuccess && settingsResult.Value != null && settingsResult.Value.AutoPrintOnPost)
                            {
                                var printDataResult = await _printDataService.GetSalesInvoicePrintDataAsync(capturedInvoiceId, CancellationToken.None);
                                if (printDataResult.IsSuccess && printDataResult.Value != null)
                                {
                                    var printResult = await _printService.PrintThermalAsync(printDataResult.Value);
                                    if (!printResult.IsSuccess)
                                    {
                                        _logger.LogWarning("Auto-print failed for invoice {InvoiceId}: {Error}", capturedInvoiceId, printResult.ErrorMessage);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Auto-print error for invoice {InvoiceId}", capturedInvoiceId);
                        }
                    }, CancellationToken.None);
                }

                return postedResult;
            }
            catch (DomainException ex)
            {
                _logger.LogWarning(ex, "Domain exception posting sales invoice {Id}: {Message}", id, ex.Message);
                return Result<SalesInvoiceDto>.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error posting sales invoice {Id}", id);
                return Result<SalesInvoiceDto>.Failure("حدث خطأ أثناء ترحيل الفاتورة");
            }
        }, ct);
    }

    public async Task<Result<SalesInvoiceDto>> CancelAsync(int id, int userId, CancellationToken ct)
    {
        var invoice = await _uow.SalesInvoices.FirstOrDefaultAsync(
            i => i.Id == id, ct, "Items.Product");

        if (invoice == null)
            return Result<SalesInvoiceDto>.Failure("الفاتورة غير موجودة", ErrorCodes.NotFound);

        if (invoice.Status == InvoiceStatus.Cancelled)
            return Result<SalesInvoiceDto>.Failure("الفاتورة ملغاة بالفعل", ErrorCodes.InvalidOperation);

        // GUARD: Check for posted returns referencing this invoice
        var hasReturns = await _uow.SalesReturns.AnyAsync(
            r => r.SalesInvoiceId == id && r.Status == InvoiceStatus.Posted, ct);
        if (hasReturns)
            return Result<SalesInvoiceDto>.Failure("لا يمكن إلغاء فاتورة عليها مرتجعات مسجلة. استخدم إلغاء المرتجع أولاً.",
                ErrorCodes.InvalidOperation);

        // GUARD: Check for customer receipts applied to this invoice
        var hasPayments = await _uow.CustomerReceiptApplications.AnyAsync(
            a => a.SalesInvoiceId == id, ct);
        if (hasPayments)
            return Result<SalesInvoiceDto>.Failure("لا يمكن إلغاء فاتورة تم تحصيل جزء من قيمتها. استخدم مرتجع بدلاً من الإلغاء.",
                ErrorCodes.InvalidOperation);

        return await _uow.ExecuteTransactionAsync<Result<SalesInvoiceDto>>(async () =>
        {
            try
            {
                if (invoice.Status == InvoiceStatus.Posted)
                {
                    // Reverse Stock
                    foreach (var item in invoice.Items)
                    {
                        // Phase 25: GetRetailQuantityEquivalent removed. Quantity is in base units.
                        var stockResult = await _inventoryService.IncreaseStockAsync(
                            item.ProductId,
                            invoice.WarehouseId,
                            item.Quantity,
                            unitCost: item.UnitPrice,
                            userId: userId,
                            ct: ct);

                        if (!stockResult.IsSuccess)
                        {
                            _logger.LogWarning("Stock increase reversal failed for sales invoice cancel: {Error}", stockResult.Error);
                            return Result<SalesInvoiceDto>.Failure(stockResult.Error!);
                        }
                    }

                    // 3a. Restore FIFO batch quantities — read original InventoryTransaction to get batch allocations
                    var originalInvTx = await _uow.InventoryTransactions.FirstOrDefaultAsync(
                        t => t.ReferenceType == InventoryReferenceType.SalesInvoice
                             && t.ReferenceId == id,
                        ct);

                    if (originalInvTx != null)
                    {
                        foreach (var line in originalInvTx.Lines)
                        {
                            if (string.IsNullOrWhiteSpace(line.BatchNo))
                                continue;

                            // Parse "batchId:qty" pairs stored during PostAsync
                            var pairs = line.BatchNo.Split(',', StringSplitOptions.RemoveEmptyEntries);
                            foreach (var pair in pairs)
                            {
                                var parts = pair.Split(':');
                                if (parts.Length == 2
                                    && int.TryParse(parts[0], out var batchId)
                                    && decimal.TryParse(parts[1], out var batchQty)
                                    && batchQty > 0)
                                {
                                    var batch = await _uow.InventoryBatches.GetByIdAsync(batchId, ct);
                                    if (batch != null)
                                    {
                                        batch.IncreaseRemaining(batchQty);
                                        _logger.LogInformation(
                                            "FIFO batch restored: Batch {BatchId} +{Quantity} for cancelled sales invoice {InvoiceId}",
                                            batchId, batchQty, id);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Original InventoryTransaction not found for sales invoice {Id} — batch restoration skipped", id);
                    }

                    // 3b. Create InventoryTransaction audit trail for stock reversal (cancellation)
                    var cancelInvTxSeq = await _documentSequenceService.GetNextIntAsync("InventoryTransaction", ct);
                    if (cancelInvTxSeq.IsSuccess)
                    {
                        var invTx = InventoryTransaction.Create(
                            cancelInvTxSeq.Value.ToString("D6"),
                            InventoryTransactionType.SaleReturn,
                            (short)invoice.WarehouseId,
                            InventoryReferenceType.SalesInvoice,
                            invoice.Id,
                            $"إلغاء بيع - فاتورة رقم {invoice.InvoiceNo}",
                            userId);
                        foreach (var item in invoice.Items)
                        {
                            invTx.AddLine(InventoryTransactionLine.Create(
                                invTx.Id, item.ProductUnitId, item.Quantity, item.UnitPrice, null, null, (short)invoice.WarehouseId));
                        }
                        await _uow.InventoryTransactions.AddAsync(invTx, ct);
                    }

                    // Create offsetting payment voucher if invoice had cash box
                    if (invoice.CashBoxId.HasValue && invoice.PaidAmount > 0)
                    {
                        var reversalAccountId = await GetCashAccountIdAsync(ct);
                        var cashResult = await _cashBoxService.RecordInvoicePaymentAsync(
                            invoice.CashBoxId.Value,
                            invoice.PaidAmount,
                            reversalAccountId,
                            notes: "إلغاء فاتورة - مردود مدفوعات",
                            userId: userId,
                            ct: ct);

                        if (!cashResult.IsSuccess)
                        {
                            _logger.LogError("Payment reversal failed during sales invoice cancel {Id}: {Error}",
                                invoice.Id, cashResult.Error);
                            return Result<SalesInvoiceDto>.Failure(cashResult.Error!);
                        }
                    }

                    // Create reversal journal entry for accounting
                    var reversalResult = await _accountingService.ReverseSalesPostEntryAsync(invoice, userId, ct);
                    if (!reversalResult.IsSuccess)
                    {
                        _logger.LogWarning("Journal entry reversal failed for sales invoice cancel {Id}: {Error}", invoice.Id, reversalResult.Error);
                        return Result<SalesInvoiceDto>.Failure(reversalResult.Error!);
                    }
                }

                // Zero out PaidAmount before Cancel() — financial entries have already been reversed
                if (invoice.PaidAmount > 0)
                    invoice.SetPaidAmount(0);

                invoice.Cancel();
                await _uow.SaveChangesAsync(ct);

                _logger.LogInformation("Sales Invoice cancelled: ID {Id} by User {UserId}", invoice.Id, userId);

                return await GetByIdAsync(invoice.Id, ct);
            }
            catch (DomainException ex)
            {
                _logger.LogWarning(ex, "Domain exception cancelling sales invoice {Id}: {Message}", id, ex.Message);
                return Result<SalesInvoiceDto>.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling sales invoice {Id}", id);
                return Result<SalesInvoiceDto>.Failure("حدث خطأ أثناء إلغاء الفاتورة");
            }
        }, ct);
    }

    /// <summary>
    /// Gets the default cash account ID from SystemAccountMappings for payment voucher recording.
    /// Falls back to 1 if no mapping is configured.
    /// </summary>
    private async Task<int> GetCashAccountIdAsync(CancellationToken ct)
    {
        var mapping = await _uow.SystemAccountMappings.FirstOrDefaultAsync(
            m => m.MappingKey == nameof(SystemAccountKey.DefaultCash), ct);
        return mapping?.AccountId ?? 1;
    }

    private static SalesInvoiceDto MapToDto(SalesInvoice i)
    {
        return new SalesInvoiceDto(
            i.Id,
            i.InvoiceNo,
            i.CustomerId,
            i.Customer?.Name ?? "عميل نقدي",
            i.WarehouseId,
            i.Warehouse?.Name ?? "غير معروف",
            i.InvoiceDate,
            (byte)i.PaymentType,
            i.SubTotal,
            i.DiscountAmount,
            (byte)i.DiscountType,
            i.DiscountRate,
            i.TaxAmount,
            i.OtherCharges,
            i.NetTotal,
            i.PaidAmount,
            i.RemainingAmount,
            i.CostInBaseCurrency,
            i.Notes,
            (byte)i.Status,
            i.TaxId,
            i.Tax?.Name,
            (decimal?)i.Tax?.Rate,
            i.CashBoxId,
            i.CashBox?.Name,
            i.Items.Select(it => new SalesInvoiceLineDto(
                it.Id,
                it.ProductId,
                it.Product?.Name ?? "غير معروف",
                it.Quantity,
                it.UnitPrice,
                it.LineTotal,
                it.ProductUnitId,
                (byte)it.DiscountType,
                it.DiscountRate,
                it.DiscountAmount,
                it.CostInBaseCurrency,
                it.UnitCost,
                it.ProfitAmount
            )).ToList()
        );
    }
}
