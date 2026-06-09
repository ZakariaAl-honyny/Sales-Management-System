using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Domain.Accounting.Enums;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;

namespace SalesSystem.Application.Accounting.Services;

/// <summary>
/// Auto-creates balanced double-entry journal entries for all business operations
/// (sales, purchases, payments, opening balances).
/// Callers are responsible for wrapping calls inside <see cref="IUnitOfWork.ExecuteTransactionAsync"/>.
/// This service does NOT start its own transaction.
/// </summary>
public class AccountingIntegrationService : IAccountingIntegrationService
{
    private readonly IJournalEntryService _journalEntryService;
    private readonly ISystemAccountService _systemAccountService;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<AccountingIntegrationService> _logger;

    public AccountingIntegrationService(
        IJournalEntryService journalEntryService,
        ISystemAccountService systemAccountService,
        IUnitOfWork uow,
        ILogger<AccountingIntegrationService> logger)
    {
        _journalEntryService = journalEntryService;
        _systemAccountService = systemAccountService;
        _uow = uow;
        _logger = logger;
    }

    // ────────────────────────────────────────────────────────────────
    //  A. Customer Opening Entry
    // ────────────────────────────────────────────────────────────────
    public async Task<Result<int>> CreateCustomerOpeningEntryAsync(
        int customerId,
        string customerName,
        decimal openingBalance,
        int createdByUserId,
        DateTime transactionDate,
        CancellationToken ct = default)
    {
        if (openingBalance <= 0)
            return Result<int>.Success(0); // No entry needed

        try
        {
            var mappings = await _systemAccountService.GetMappingsAsync(null, ct);
            if (!mappings.IsSuccess)
                return Result<int>.Failure(mappings.Error!);

            var m = mappings.Value!;

            // Validate required accounts
            if (m.AccountsReceivableAccountId <= 0)
                return Result<int>.Failure("الحساب النظامي غير مهيأ: حساب الذمم المدينة");
            if (!m.OpeningBalanceEquityAccountId.HasValue || m.OpeningBalanceEquityAccountId.Value <= 0)
                return Result<int>.Failure("الحساب النظامي غير مهيأ: حساب حقوق الملكية للرصيد الافتتاحي");

            var request = new CreateJournalEntryRequest(
                TransactionDate: transactionDate,
                Description: $"قيد افتتاحي — رصيد افتتاحي للعميل: {customerName}",
                EntryType: JournalEntryType.OpeningBalance,
                ReferenceType: "Customer",
                ReferenceId: customerId,
                ReferenceNumber: customerId.ToString(),
                Lines: new List<JournalEntryLineRequest>
                {
                    new(m.AccountsReceivableAccountId, openingBalance, 0, "رصيد افتتاحي للعميل"),
                    new(m.OpeningBalanceEquityAccountId.Value, 0, openingBalance, "رصيد افتتاحي للعميل")
                }
            );

            var createResult = await _journalEntryService.CreateJournalEntryAsync(request, createdByUserId, ct);
            if (!createResult.IsSuccess) return createResult;

            // Post the entry (transition from Draft to Posted)
            var postResult = await _journalEntryService.PostJournalEntryAsync(createResult.Value, createdByUserId, ct);
            if (!postResult.IsSuccess) return Result<int>.Failure(postResult.Error!);

            return Result<int>.Success(createResult.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating customer opening entry for customer {CustomerId}", customerId);
            return Result<int>.Failure("حدث خطأ أثناء إنشاء القيد الافتتاحي للعميل");
        }
    }

    // ────────────────────────────────────────────────────────────────
    //  B. Supplier Opening Entry
    // ────────────────────────────────────────────────────────────────
    public async Task<Result<int>> CreateSupplierOpeningEntryAsync(
        int supplierId,
        string supplierName,
        decimal openingBalance,
        int createdByUserId,
        DateTime transactionDate,
        CancellationToken ct = default)
    {
        if (openingBalance <= 0)
            return Result<int>.Success(0); // No entry needed

        try
        {
            var mappings = await _systemAccountService.GetMappingsAsync(null, ct);
            if (!mappings.IsSuccess)
                return Result<int>.Failure(mappings.Error!);

            var m = mappings.Value!;

            // Validate required accounts
            if (m.AccountsPayableAccountId <= 0)
                return Result<int>.Failure("الحساب النظامي غير مهيأ: حساب الذمم الدائنة");
            if (!m.OpeningBalanceEquityAccountId.HasValue || m.OpeningBalanceEquityAccountId.Value <= 0)
                return Result<int>.Failure("الحساب النظامي غير مهيأ: حساب حقوق الملكية للرصيد الافتتاحي");

            var request = new CreateJournalEntryRequest(
                TransactionDate: transactionDate,
                Description: $"قيد افتتاحي — رصيد افتتاحي للمورد: {supplierName}",
                EntryType: JournalEntryType.OpeningBalance,
                ReferenceType: "Supplier",
                ReferenceId: supplierId,
                ReferenceNumber: supplierId.ToString(),
                Lines: new List<JournalEntryLineRequest>
                {
                    new(m.OpeningBalanceEquityAccountId.Value, openingBalance, 0, "رصيد افتتاحي للمورد"),
                    new(m.AccountsPayableAccountId, 0, openingBalance, "رصيد افتتاحي للمورد")
                }
            );

            var createResult = await _journalEntryService.CreateJournalEntryAsync(request, createdByUserId, ct);
            if (!createResult.IsSuccess) return createResult;

            var postResult = await _journalEntryService.PostJournalEntryAsync(createResult.Value, createdByUserId, ct);
            if (!postResult.IsSuccess) return Result<int>.Failure(postResult.Error!);

            return Result<int>.Success(createResult.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating supplier opening entry for supplier {SupplierId}", supplierId);
            return Result<int>.Failure("حدث خطأ أثناء إنشاء القيد الافتتاحي للمورد");
        }
    }

    // ────────────────────────────────────────────────────────────────
    //  C. Sales Invoice Post Entry
    // ────────────────────────────────────────────────────────────────
    public async Task<Result<int>> CreateSalesPostEntryAsync(
        SalesInvoice invoice,
        int createdByUserId,
        decimal totalCost,
        CancellationToken ct = default)
    {
        try
        {
            var mappings = await _systemAccountService.GetMappingsAsync(null, ct);
            if (!mappings.IsSuccess)
                return Result<int>.Failure(mappings.Error!);

            var m = mappings.Value!;

            // Validate required accounts
            if (m.DefaultCashAccountId <= 0)
                return Result<int>.Failure("الحساب النظامي غير مهيأ: حساب الصندوق النقدي");
            if (m.AccountsReceivableAccountId <= 0)
                return Result<int>.Failure("الحساب النظامي غير مهيأ: حساب الذمم المدينة");
            if (m.SalesRevenueAccountId <= 0)
                return Result<int>.Failure("الحساب النظامي غير مهيأ: حساب إيرادات المبيعات");
            if (m.VatOutputAccountId <= 0)
                return Result<int>.Failure("الحساب النظامي غير مهيأ: حساب ضريبة المخرجات");
            if (m.CogsAccountId <= 0)
                return Result<int>.Failure("الحساب النظامي غير مهيأ: حساب تكلفة البضاعة المباعة");
            if (m.InventoryAssetAccountId <= 0)
                return Result<int>.Failure("الحساب النظامي غير مهيأ: حساب أصل المخزون");

            // Validate: discount cannot exceed subtotal
            if (invoice.DiscountAmount > invoice.SubTotal)
                return Result<int>.Failure("لا يمكن أن يكون الخصم أكبر من إجمالي الفاتورة");

            // Net revenue after header discount
            var netRevenue = invoice.SubTotal - invoice.DiscountAmount;

            var lines = new List<JournalEntryLineRequest>();

            // ── Revenue Side ──────────────────────────────────────
            // Debit side (Cash / AR) depends on PaymentType
            if (invoice.PaymentType == PaymentType.Cash)
            {
                lines.Add(new JournalEntryLineRequest(
                    m.DefaultCashAccountId,
                    invoice.TotalAmount,
                    0,
                    "الجزء النقدي من فاتورة البيع"));
            }
            else if (invoice.PaymentType == PaymentType.Credit)
            {
                lines.Add(new JournalEntryLineRequest(
                    m.AccountsReceivableAccountId,
                    invoice.TotalAmount,
                    0,
                    "الجزء الآجل من فاتورة البيع"));
            }
            else if (invoice.PaymentType == PaymentType.Mixed)
            {
                lines.Add(new JournalEntryLineRequest(
                    m.DefaultCashAccountId,
                    invoice.PaidAmount,
                    0,
                    "الجزء النقدي من فاتورة البيع (مختلط)"));
                lines.Add(new JournalEntryLineRequest(
                    m.AccountsReceivableAccountId,
                    invoice.DueAmount,
                    0,
                    "الجزء الآجل من فاتورة البيع (مختلط)"));
            }

            // Credit side - Sales Revenue (net after discount)
            lines.Add(new JournalEntryLineRequest(
                m.SalesRevenueAccountId,
                0,
                netRevenue,
                "إيراد المبيعات (صافي بعد الخصم)"));

            // Credit side - VAT Output
            if (invoice.TaxAmount > 0)
            {
                lines.Add(new JournalEntryLineRequest(
                    m.VatOutputAccountId,
                    0,
                    invoice.TaxAmount,
                    "ضريبة المخرجات"));
            }

            // ── COGS Side ─────────────────────────────────────────
            if (totalCost > 0)
            {
                lines.Add(new JournalEntryLineRequest(
                    m.CogsAccountId,
                    totalCost,
                    0,
                    "تكلفة البضاعة المباعة"));
                lines.Add(new JournalEntryLineRequest(
                    m.InventoryAssetAccountId,
                    0,
                    totalCost,
                    "تخفيض المخزون"));
            }

            var request = new CreateJournalEntryRequest(
                TransactionDate: invoice.InvoiceDate,
                Description: $"قيد ترحيل فاتورة بيع رقم {invoice.InvoiceNo}",
                EntryType: JournalEntryType.Sales,
                ReferenceType: "SalesInvoice",
                ReferenceId: invoice.Id,
                ReferenceNumber: invoice.InvoiceNo.ToString(),
                Lines: lines
            );

            var createResult = await _journalEntryService.CreateJournalEntryAsync(request, createdByUserId, ct);
            if (!createResult.IsSuccess) return createResult;

            var postResult = await _journalEntryService.PostJournalEntryAsync(createResult.Value, createdByUserId, ct);
            if (!postResult.IsSuccess) return Result<int>.Failure(postResult.Error!);

            return Result<int>.Success(createResult.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating sales post entry for invoice #{InvoiceNo}", invoice.InvoiceNo);
            return Result<int>.Failure("حدث خطأ أثناء إنشاء قيد ترحيل فاتورة البيع");
        }
    }

    // ────────────────────────────────────────────────────────────────
    //  D. Reverse Sales Post Entry (cancellation)
    // ────────────────────────────────────────────────────────────────
    public async Task<Result<int>> ReverseSalesPostEntryAsync(
        SalesInvoice invoice,
        int reversedByUserId,
        CancellationToken ct = default)
    {
        try
        {
            var mappings = await _systemAccountService.GetMappingsAsync(null, ct);
            if (!mappings.IsSuccess)
                return Result<int>.Failure(mappings.Error!);

            var m = mappings.Value!;

            // Validate required accounts (same as create)
            if (m.DefaultCashAccountId <= 0)
                return Result<int>.Failure("الحساب النظامي غير مهيأ: حساب الصندوق النقدي");
            if (m.AccountsReceivableAccountId <= 0)
                return Result<int>.Failure("الحساب النظامي غير مهيأ: حساب الذمم المدينة");
            if (m.SalesRevenueAccountId <= 0)
                return Result<int>.Failure("الحساب النظامي غير مهيأ: حساب إيرادات المبيعات");
            if (m.VatOutputAccountId <= 0)
                return Result<int>.Failure("الحساب النظامي غير مهيأ: حساب ضريبة المخرجات");
            if (m.CogsAccountId <= 0)
                return Result<int>.Failure("الحساب النظامي غير مهيأ: حساب تكلفة البضاعة المباعة");
            if (m.InventoryAssetAccountId <= 0)
                return Result<int>.Failure("الحساب النظامي غير مهيأ: حساب أصل المخزون");

            var netRevenue = invoice.SubTotal - invoice.DiscountAmount;
            if (netRevenue < 0)
                return Result<int>.Failure("لا يمكن أن يكون الخصم أكبر من إجمالي الفاتورة");

            var lines = new List<JournalEntryLineRequest>();

            // ── Reverse Revenue Side (mirror: swap Dr ↔ Cr) ──────
            // Original: Cr SalesRevenue, Cr VatOutput
            // Reverse:  Dr SalesRevenue, Dr VatOutput
            lines.Add(new JournalEntryLineRequest(
                m.SalesRevenueAccountId,
                netRevenue,
                0,
                "عكس إيراد المبيعات"));

            if (invoice.TaxAmount > 0)
            {
                lines.Add(new JournalEntryLineRequest(
                    m.VatOutputAccountId,
                    invoice.TaxAmount,
                    0,
                    "عكس ضريبة المخرجات"));
            }

            // Original: Dr Cash/AR
            // Reverse:  Cr Cash/AR
            if (invoice.PaymentType == PaymentType.Cash)
            {
                lines.Add(new JournalEntryLineRequest(
                    m.DefaultCashAccountId,
                    0,
                    invoice.TotalAmount,
                    "عكس الجزء النقدي من فاتورة البيع"));
            }
            else if (invoice.PaymentType == PaymentType.Credit)
            {
                lines.Add(new JournalEntryLineRequest(
                    m.AccountsReceivableAccountId,
                    0,
                    invoice.TotalAmount,
                    "عكس الجزء الآجل من فاتورة البيع"));
            }
            else if (invoice.PaymentType == PaymentType.Mixed)
            {
                lines.Add(new JournalEntryLineRequest(
                    m.DefaultCashAccountId,
                    0,
                    invoice.PaidAmount,
                    "عكس الجزء النقدي من فاتورة البيع (مختلط)"));
                lines.Add(new JournalEntryLineRequest(
                    m.AccountsReceivableAccountId,
                    0,
                    invoice.DueAmount,
                    "عكس الجزء الآجل من فاتورة البيع (مختلط)"));
            }

            // ── Reverse COGS Side (mirror: swap Dr ↔ Cr) ─────────
            // Original: Dr COGS, Cr Inventory
            // Reverse:  Cr COGS, Dr Inventory
            // We don't have totalCost here, so we need to query the original entry.
            // Primary lookup by ReferenceId; fallback by ReferenceNumber (defensive).
            var originalEntry = await _uow.JournalEntries.FirstOrDefaultAsync(
                je => je.ReferenceType == "SalesInvoice"
                    && je.ReferenceId == invoice.Id
                    && je.EntryType == JournalEntryType.Sales,
                ct: ct)
                ?? await _uow.JournalEntries.FirstOrDefaultAsync(
                    je => je.ReferenceType == "SalesInvoice"
                        && je.ReferenceNumber == invoice.InvoiceNo.ToString()
                        && je.EntryType == JournalEntryType.Sales,
                    ct: ct);

            if (originalEntry != null)
            {
                var cogsLines = await _uow.JournalEntryLines.ToListAsync(
                    jel => jel.JournalEntryId == originalEntry.Id
                        && jel.AccountId == m.CogsAccountId
                        && jel.Debit > 0,
                    ct: ct);

                foreach (var cogsLine in cogsLines)
                {
                    lines.Add(new JournalEntryLineRequest(
                        m.InventoryAssetAccountId,
                        cogsLine.Debit,
                        0,
                        "عكس تكلفة البضاعة المباعة — إعادة المخزون"));
                    lines.Add(new JournalEntryLineRequest(
                        m.CogsAccountId,
                        0,
                        cogsLine.Debit,
                        "عكس تكلفة البضاعة المباعة"));
                }
            }
            else
            {
                // Fallback: compute COGS estimate from invoice items
                _logger.LogWarning(
                    "Original journal entry not found for SalesInvoice {Id} ({InvoiceNo}), computing COGS from items",
                    invoice.Id, invoice.InvoiceNo);
                var computedCost = invoice.Items.Sum(item =>
                {
                    if (item.Product == null) return 0m;
                    var retailQty = item.Product.GetRetailQuantityEquivalent(item.Quantity, item.Mode);
                    var baseUnit = item.Product.Units?.FirstOrDefault(u => u.IsBaseUnit);
                    var cost = baseUnit?.AverageCost ?? baseUnit?.PurchaseCost ?? 0;
                    return retailQty * cost;
                });
                if (computedCost > 0)
                {
                    lines.Add(new JournalEntryLineRequest(
                        m.InventoryAssetAccountId,
                        computedCost,
                        0,
                        "عكس تكلفة البضاعة المباعة — إعادة المخزون (تقديري)"));
                    lines.Add(new JournalEntryLineRequest(
                        m.CogsAccountId,
                        0,
                        computedCost,
                        "عكس تكلفة البضاعة المباعة (تقديري)"));
                }
            }

            var request = new CreateJournalEntryRequest(
                TransactionDate: DateTime.UtcNow,
                Description: $"قيد عكس ترحيل فاتورة بيع رقم {invoice.InvoiceNo}",
                EntryType: JournalEntryType.SalesReturn,
                ReferenceType: "SalesInvoice",
                ReferenceId: invoice.Id,
                ReferenceNumber: $"{invoice.InvoiceNo}-REV",
                Lines: lines
            );

            var createResult = await _journalEntryService.CreateJournalEntryAsync(request, reversedByUserId, ct);
            if (!createResult.IsSuccess) return createResult;

            var postResult = await _journalEntryService.PostJournalEntryAsync(createResult.Value, reversedByUserId, ct);
            if (!postResult.IsSuccess) return Result<int>.Failure(postResult.Error!);

            return Result<int>.Success(createResult.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reversing sales post entry for invoice #{InvoiceNo}", invoice.InvoiceNo);
            return Result<int>.Failure("حدث خطأ أثناء إنشاء قيد عكس فاتورة البيع");
        }
    }

    // ────────────────────────────────────────────────────────────────
    //  E. Purchase Invoice Post Entry
    // ────────────────────────────────────────────────────────────────
    public async Task<Result<int>> CreatePurchasePostEntryAsync(
        PurchaseInvoice invoice,
        int createdByUserId,
        CancellationToken ct = default)
    {
        try
        {
            var mappings = await _systemAccountService.GetMappingsAsync(null, ct);
            if (!mappings.IsSuccess)
                return Result<int>.Failure(mappings.Error!);

            var m = mappings.Value!;

            // Validate required accounts
            if (m.InventoryAssetAccountId <= 0)
                return Result<int>.Failure("الحساب النظامي غير مهيأ: حساب أصل المخزون");
            if (m.VatInputAccountId <= 0)
                return Result<int>.Failure("الحساب النظامي غير مهيأ: حساب ضريبة المدخلات");
            if (m.DefaultCashAccountId <= 0)
                return Result<int>.Failure("الحساب النظامي غير مهيأ: حساب الصندوق النقدي");
            if (m.AccountsPayableAccountId <= 0)
                return Result<int>.Failure("الحساب النظامي غير مهيأ: حساب الذمم الدائنة");

            // Validate: discount cannot exceed subtotal
            if (invoice.DiscountAmount > invoice.SubTotal)
                return Result<int>.Failure("لا يمكن أن يكون الخصم أكبر من إجمالي فاتورة الشراء");

            // Net inventory cost = SubTotal - DiscountAmount
            var netInventoryCost = invoice.SubTotal - invoice.DiscountAmount;

            var lines = new List<JournalEntryLineRequest>();

            // Dr Inventory Asset (net cost after discount)
            lines.Add(new JournalEntryLineRequest(
                m.InventoryAssetAccountId,
                netInventoryCost,
                0,
                "تكلفة المشتريات (صافي بعد الخصم)"));

            // Dr VAT Input
            if (invoice.TaxAmount > 0)
            {
                lines.Add(new JournalEntryLineRequest(
                    m.VatInputAccountId,
                    invoice.TaxAmount,
                    0,
                    "ضريبة المدخلات"));
            }

            // Credit side (Cash / AP) depends on PaymentType
            if (invoice.PaymentType == PaymentType.Cash)
            {
                lines.Add(new JournalEntryLineRequest(
                    m.DefaultCashAccountId,
                    0,
                    invoice.TotalAmount,
                    "الجزء النقدي من فاتورة الشراء"));
            }
            else if (invoice.PaymentType == PaymentType.Credit)
            {
                lines.Add(new JournalEntryLineRequest(
                    m.AccountsPayableAccountId,
                    0,
                    invoice.TotalAmount,
                    "الجزء الآجل من فاتورة الشراء"));
            }
            else if (invoice.PaymentType == PaymentType.Mixed)
            {
                lines.Add(new JournalEntryLineRequest(
                    m.DefaultCashAccountId,
                    0,
                    invoice.PaidAmount,
                    "الجزء النقدي من فاتورة الشراء (مختلط)"));
                lines.Add(new JournalEntryLineRequest(
                    m.AccountsPayableAccountId,
                    0,
                    invoice.DueAmount,
                    "الجزء الآجل من فاتورة الشراء (مختلط)"));
            }

            var request = new CreateJournalEntryRequest(
                TransactionDate: invoice.InvoiceDate,
                Description: $"قيد ترحيل فاتورة شراء رقم {invoice.InvoiceNo}",
                EntryType: JournalEntryType.Purchase,
                ReferenceType: "PurchaseInvoice",
                ReferenceId: invoice.Id,
                ReferenceNumber: invoice.InvoiceNo.ToString(),
                Lines: lines
            );

            var createResult = await _journalEntryService.CreateJournalEntryAsync(request, createdByUserId, ct);
            if (!createResult.IsSuccess) return createResult;

            var postResult = await _journalEntryService.PostJournalEntryAsync(createResult.Value, createdByUserId, ct);
            if (!postResult.IsSuccess) return Result<int>.Failure(postResult.Error!);

            return Result<int>.Success(createResult.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating purchase post entry for invoice #{InvoiceNo}", invoice.InvoiceNo);
            return Result<int>.Failure("حدث خطأ أثناء إنشاء قيد ترحيل فاتورة الشراء");
        }
    }

    // ────────────────────────────────────────────────────────────────
    //  F. Reverse Purchase Post Entry (cancellation)
    // ────────────────────────────────────────────────────────────────
    public async Task<Result<int>> ReversePurchasePostEntryAsync(
        PurchaseInvoice invoice,
        int reversedByUserId,
        CancellationToken ct = default)
    {
        try
        {
            var mappings = await _systemAccountService.GetMappingsAsync(null, ct);
            if (!mappings.IsSuccess)
                return Result<int>.Failure(mappings.Error!);

            var m = mappings.Value!;

            // Validate required accounts
            if (m.InventoryAssetAccountId <= 0)
                return Result<int>.Failure("الحساب النظامي غير مهيأ: حساب أصل المخزون");
            if (m.VatInputAccountId <= 0)
                return Result<int>.Failure("الحساب النظامي غير مهيأ: حساب ضريبة المدخلات");
            if (m.DefaultCashAccountId <= 0)
                return Result<int>.Failure("الحساب النظامي غير مهيأ: حساب الصندوق النقدي");
            if (m.AccountsPayableAccountId <= 0)
                return Result<int>.Failure("الحساب النظامي غير مهيأ: حساب الذمم الدائنة");

            var netInventoryCost = invoice.SubTotal - invoice.DiscountAmount;
            if (netInventoryCost < 0)
                return Result<int>.Failure("لا يمكن أن يكون الخصم أكبر من إجمالي الفاتورة");

            var lines = new List<JournalEntryLineRequest>();

            // ── Reverse: Cr Inventory, Cr VatInput (swap Dr ↔ Cr) ─
            // Original: Dr Inventory, Dr VatInput
            // Reverse:  Cr Inventory, Cr VatInput
            lines.Add(new JournalEntryLineRequest(
                m.InventoryAssetAccountId,
                0,
                netInventoryCost,
                "عكس تكلفة المشتريات"));

            if (invoice.TaxAmount > 0)
            {
                lines.Add(new JournalEntryLineRequest(
                    m.VatInputAccountId,
                    0,
                    invoice.TaxAmount,
                    "عكس ضريبة المدخلات"));
            }

            // Original: Cr Cash/AP
            // Reverse:  Dr Cash/AP
            if (invoice.PaymentType == PaymentType.Cash)
            {
                lines.Add(new JournalEntryLineRequest(
                    m.DefaultCashAccountId,
                    invoice.TotalAmount,
                    0,
                    "عكس الجزء النقدي من فاتورة الشراء"));
            }
            else if (invoice.PaymentType == PaymentType.Credit)
            {
                lines.Add(new JournalEntryLineRequest(
                    m.AccountsPayableAccountId,
                    invoice.TotalAmount,
                    0,
                    "عكس الجزء الآجل من فاتورة الشراء"));
            }
            else if (invoice.PaymentType == PaymentType.Mixed)
            {
                lines.Add(new JournalEntryLineRequest(
                    m.DefaultCashAccountId,
                    invoice.PaidAmount,
                    0,
                    "عكس الجزء النقدي من فاتورة الشراء (مختلط)"));
                lines.Add(new JournalEntryLineRequest(
                    m.AccountsPayableAccountId,
                    invoice.DueAmount,
                    0,
                    "عكس الجزء الآجل من فاتورة الشراء (مختلط)"));
            }

            var request = new CreateJournalEntryRequest(
                TransactionDate: DateTime.UtcNow,
                Description: $"قيد عكس ترحيل فاتورة شراء رقم {invoice.InvoiceNo}",
                EntryType: JournalEntryType.PurchaseReturn,
                ReferenceType: "PurchaseInvoice",
                ReferenceId: invoice.Id,
                ReferenceNumber: $"{invoice.InvoiceNo}-REV",
                Lines: lines
            );

            var createResult = await _journalEntryService.CreateJournalEntryAsync(request, reversedByUserId, ct);
            if (!createResult.IsSuccess) return createResult;

            var postResult = await _journalEntryService.PostJournalEntryAsync(createResult.Value, reversedByUserId, ct);
            if (!postResult.IsSuccess) return Result<int>.Failure(postResult.Error!);

            return Result<int>.Success(createResult.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reversing purchase post entry for invoice #{InvoiceNo}", invoice.InvoiceNo);
            return Result<int>.Failure("حدث خطأ أثناء إنشاء قيد عكس فاتورة الشراء");
        }
    }

    // ────────────────────────────────────────────────────────────────
    //  G. Customer Payment Entry (Receipt)
    // ────────────────────────────────────────────────────────────────
    public async Task<Result<int>> CreateCustomerPaymentEntryAsync(
        CustomerPayment payment,
        string customerName,
        int createdByUserId,
        CancellationToken ct = default)
    {
        try
        {
            var mappings = await _systemAccountService.GetMappingsAsync(null, ct);
            if (!mappings.IsSuccess)
                return Result<int>.Failure(mappings.Error!);

            var m = mappings.Value!;

            // Validate required accounts
            if (m.DefaultCashAccountId <= 0)
                return Result<int>.Failure("الحساب النظامي غير مهيأ: حساب الصندوق النقدي");
            if (m.AccountsReceivableAccountId <= 0)
                return Result<int>.Failure("الحساب النظامي غير مهيأ: حساب الذمم المدينة");

            var request = new CreateJournalEntryRequest(
                TransactionDate: payment.PaymentDate,
                Description: $"قيد سند قبض من العميل: {customerName}",
                EntryType: JournalEntryType.CustomerReceipt,
                ReferenceType: "CustomerPayment",
                ReferenceId: payment.Id,
                ReferenceNumber: payment.Id.ToString(),
                Lines: new List<JournalEntryLineRequest>
                {
                    new(m.DefaultCashAccountId, payment.Amount, 0, "سند قبض من العميل"),
                    new(m.AccountsReceivableAccountId, 0, payment.Amount, "سند قبض من العميل")
                }
            );

            var createResult = await _journalEntryService.CreateJournalEntryAsync(request, createdByUserId, ct);
            if (!createResult.IsSuccess) return createResult;

            var postResult = await _journalEntryService.PostJournalEntryAsync(createResult.Value, createdByUserId, ct);
            if (!postResult.IsSuccess) return Result<int>.Failure(postResult.Error!);

            return Result<int>.Success(createResult.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating customer payment entry for payment {PaymentId}", payment.Id);
            return Result<int>.Failure("حدث خطأ أثناء إنشاء قيد سند القبض");
        }
    }

    // ────────────────────────────────────────────────────────────────
    //  H. Supplier Payment Entry
    // ────────────────────────────────────────────────────────────────
    public async Task<Result<int>> CreateSupplierPaymentEntryAsync(
        SupplierPayment payment,
        string supplierName,
        int createdByUserId,
        CancellationToken ct = default)
    {
        try
        {
            var mappings = await _systemAccountService.GetMappingsAsync(null, ct);
            if (!mappings.IsSuccess)
                return Result<int>.Failure(mappings.Error!);

            var m = mappings.Value!;

            // Validate required accounts
            if (m.DefaultCashAccountId <= 0)
                return Result<int>.Failure("الحساب النظامي غير مهيأ: حساب الصندوق النقدي");
            if (m.AccountsPayableAccountId <= 0)
                return Result<int>.Failure("الحساب النظامي غير مهيأ: حساب الذمم الدائنة");

            var request = new CreateJournalEntryRequest(
                TransactionDate: payment.PaymentDate,
                Description: $"قيد سند دفع للمورد: {supplierName}",
                EntryType: JournalEntryType.SupplierPayment,
                ReferenceType: "SupplierPayment",
                ReferenceId: payment.Id,
                ReferenceNumber: payment.Id.ToString(),
                Lines: new List<JournalEntryLineRequest>
                {
                    new(m.AccountsPayableAccountId, payment.Amount, 0, "سند دفع للمورد"),
                    new(m.DefaultCashAccountId, 0, payment.Amount, "سند دفع للمورد")
                }
            );

            var createResult = await _journalEntryService.CreateJournalEntryAsync(request, createdByUserId, ct);
            if (!createResult.IsSuccess) return createResult;

            var postResult = await _journalEntryService.PostJournalEntryAsync(createResult.Value, createdByUserId, ct);
            if (!postResult.IsSuccess) return Result<int>.Failure(postResult.Error!);

            return Result<int>.Success(createResult.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating supplier payment entry for payment {PaymentId}", payment.Id);
            return Result<int>.Failure("حدث خطأ أثناء إنشاء قيد سند الدفع");
        }
    }

    // ────────────────────────────────────────────────────────────────
    //  I. Reverse Customer Payment Entry
    // ────────────────────────────────────────────────────────────────
    public async Task<Result<int>> ReverseCustomerPaymentEntryAsync(
        int paymentId,
        decimal amount,
        string customerName,
        int reversedByUserId,
        CancellationToken ct = default)
    {
        try
        {
            var mappings = await _systemAccountService.GetMappingsAsync(null, ct);
            if (!mappings.IsSuccess)
                return Result<int>.Failure(mappings.Error!);

            var m = mappings.Value!;

            // Validate required accounts
            if (m.DefaultCashAccountId <= 0)
                return Result<int>.Failure("الحساب النظامي غير مهيأ: حساب الصندوق النقدي");
            if (m.AccountsReceivableAccountId <= 0)
                return Result<int>.Failure("الحساب النظامي غير مهيأ: حساب الذمم المدينة");

            // Reverse: Dr AR / Cr Cash (mirror of original Dr Cash / Cr AR)
            var request = new CreateJournalEntryRequest(
                TransactionDate: DateTime.UtcNow,
                Description: $"قيد عكس سند قبض من العميل: {customerName}",
                EntryType: JournalEntryType.Manual,
                ReferenceType: "CustomerPayment",
                ReferenceId: paymentId,
                ReferenceNumber: $"{paymentId}-REV",
                Lines: new List<JournalEntryLineRequest>
                {
                    new(m.AccountsReceivableAccountId, amount, 0, "عكس سند قبض من العميل"),
                    new(m.DefaultCashAccountId, 0, amount, "عكس سند قبض من العميل")
                }
            );

            var createResult = await _journalEntryService.CreateJournalEntryAsync(request, reversedByUserId, ct);
            if (!createResult.IsSuccess) return createResult;

            var postResult = await _journalEntryService.PostJournalEntryAsync(createResult.Value, reversedByUserId, ct);
            if (!postResult.IsSuccess) return Result<int>.Failure(postResult.Error!);

            return Result<int>.Success(createResult.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reversing customer payment entry for payment {PaymentId}", paymentId);
            return Result<int>.Failure("حدث خطأ أثناء إنشاء قيد عكس سند القبض");
        }
    }

    // ────────────────────────────────────────────────────────────────
    //  J. Reverse Supplier Payment Entry
    // ────────────────────────────────────────────────────────────────
    public async Task<Result<int>> ReverseSupplierPaymentEntryAsync(
        int paymentId,
        decimal amount,
        string supplierName,
        int reversedByUserId,
        CancellationToken ct = default)
    {
        try
        {
            var mappings = await _systemAccountService.GetMappingsAsync(null, ct);
            if (!mappings.IsSuccess)
                return Result<int>.Failure(mappings.Error!);

            var m = mappings.Value!;

            // Validate required accounts
            if (m.DefaultCashAccountId <= 0)
                return Result<int>.Failure("الحساب النظامي غير مهيأ: حساب الصندوق النقدي");
            if (m.AccountsPayableAccountId <= 0)
                return Result<int>.Failure("الحساب النظامي غير مهيأ: حساب الذمم الدائنة");

            // Reverse: Dr Cash / Cr AP (mirror of original Dr AP / Cr Cash)
            var request = new CreateJournalEntryRequest(
                TransactionDate: DateTime.UtcNow,
                Description: $"قيد عكس سند دفع للمورد: {supplierName}",
                EntryType: JournalEntryType.Manual,
                ReferenceType: "SupplierPayment",
                ReferenceId: paymentId,
                ReferenceNumber: $"{paymentId}-REV",
                Lines: new List<JournalEntryLineRequest>
                {
                    new(m.DefaultCashAccountId, amount, 0, "عكس سند دفع للمورد"),
                    new(m.AccountsPayableAccountId, 0, amount, "عكس سند دفع للمورد")
                }
            );

            var createResult = await _journalEntryService.CreateJournalEntryAsync(request, reversedByUserId, ct);
            if (!createResult.IsSuccess) return createResult;

            var postResult = await _journalEntryService.PostJournalEntryAsync(createResult.Value, reversedByUserId, ct);
            if (!postResult.IsSuccess) return Result<int>.Failure(postResult.Error!);

            return Result<int>.Success(createResult.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reversing supplier payment entry for payment {PaymentId}", paymentId);
            return Result<int>.Failure("حدث خطأ أثناء إنشاء قيد عكس سند الدفع");
        }
    }
}
