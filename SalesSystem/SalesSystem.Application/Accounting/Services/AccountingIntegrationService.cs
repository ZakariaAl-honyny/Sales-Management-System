using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Domain.Accounting.Entities;
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

    // ─── Helper: Fetch multiple system accounts in parallel ─────────
    private async Task<Result<Dictionary<SystemAccountKey, int>>> GetAccountIdDictionaryAsync(
        int? branchId, IEnumerable<SystemAccountKey> keys, CancellationToken ct)
    {
        var tasks = keys.Select(async key =>
        {
            var result = await _systemAccountService.GetMappingAsync(key, branchId, ct);
            return (Key: key, Result: result);
        });

        var results = await Task.WhenAll(tasks);
        var dict = new Dictionary<SystemAccountKey, int>();
        foreach (var (key, result) in results)
        {
            if (!result.IsSuccess || result.Value == null)
                return Result<Dictionary<SystemAccountKey, int>>.Failure(
                    $"الحساب النظامي غير مهيأ: {key}");
            if (result.Value.AccountId <= 0)
                return Result<Dictionary<SystemAccountKey, int>>.Failure(
                    $"الحساب النظامي غير مهيأ: {key}");
            dict[key] = result.Value.AccountId;
        }
        return Result<Dictionary<SystemAccountKey, int>>.Success(dict);
    }

    private static string GetAccountName(SystemAccountKey key) => key switch
    {
        SystemAccountKey.DefaultCash => "حساب الصندوق النقدي",
        SystemAccountKey.DefaultBank => "الحساب البنكي",
        SystemAccountKey.AccountsReceivable => "حساب الذمم المدينة",
        SystemAccountKey.AccountsPayable => "حساب الذمم الدائنة",
        SystemAccountKey.Inventory => "حساب أصل المخزون",
        SystemAccountKey.CostOfGoodsSold => "حساب تكلفة البضاعة المباعة",
        SystemAccountKey.SalesRevenue => "حساب إيرادات المبيعات",
        SystemAccountKey.SalesReturns => "حساب مردودات المبيعات",
        SystemAccountKey.PurchaseReturns => "حساب مردودات المشتريات",
        SystemAccountKey.VatOutput => "حساب ضريبة المخرجات",
        SystemAccountKey.VatInput => "حساب ضريبة المدخلات",
        SystemAccountKey.Capital => "حساب رأس المال",
        SystemAccountKey.OpeningBalanceEquity => "حساب حقوق الملكية للرصيد الافتتاحي",
        SystemAccountKey.RetainedEarnings => "حساب الأرباح المحتجزة",
        SystemAccountKey.UndistributedProfits => "حساب الأرباح وفاق",
        SystemAccountKey.InventoryShortage => "حساب عجز المخزون",
        SystemAccountKey.InventorySurplus => "حساب زيادة المخزون",
        _ => key.ToString()
    };

    // ─── Helper: Fetch a single system account ──────────────────────
    private async Task<Result<int>> GetAccountIdAsync(SystemAccountKey key,
        int? branchId, CancellationToken ct)
    {
        var result = await _systemAccountService.GetMappingAsync(key, branchId, ct);
        if (!result.IsSuccess || result.Value == null)
            return Result<int>.Failure($"الحساب النظامي غير مهيأ: {GetAccountName(key)}");
        if (result.Value.AccountId <= 0)
            return Result<int>.Failure($"الحساب النظامي غير مهيأ: {GetAccountName(key)}");
        return Result<int>.Success(result.Value.AccountId);
    }

    // ─── Helpers: Get per-entity account IDs ────────────────────────
    private static int GetCustomerAccountId(SalesInvoice invoice, Dictionary<SystemAccountKey, int> m)
    {
        return invoice.Customer?.Party?.AccountId > 0
            ? invoice.Customer.Party.AccountId
            : m.GetValueOrDefault(SystemAccountKey.AccountsReceivable, 0);
    }

    private static int GetSupplierAccountId(PurchaseInvoice invoice, Dictionary<SystemAccountKey, int> m)
    {
        return invoice.Supplier?.Party?.AccountId > 0
            ? invoice.Supplier.Party.AccountId
            : m.GetValueOrDefault(SystemAccountKey.AccountsPayable, 0);
    }

    // ────────────────────────────────────────────────────────────────
    //  A. Customer Opening Entry
    // ────────────────────────────────────────────────────────────────
    public async Task<Result<int>> CreateCustomerOpeningEntryAsync(
        int customerId,
        string customerName,
        int customerAccountId,
        decimal openingBalance,
        int createdByUserId,
        DateTime transactionDate,
        CancellationToken ct = default)
    {
        if (openingBalance <= 0)
            return Result<int>.Success(0); // No entry needed

        try
        {
            var openingEquityResult = await GetAccountIdAsync(
                SystemAccountKey.OpeningBalanceEquity, null, ct);
            if (!openingEquityResult.IsSuccess)
                return Result<int>.Failure(openingEquityResult.Error!);

            var openingBalanceEquityAccountId = openingEquityResult.Value;

            var request = new CreateJournalEntryRequest(
                TransactionDate: transactionDate,
                Description: $"قيد افتتاحي — رصيد افتتاحي للعميل: {customerName}",
                EntryType: JournalEntryType.OpeningBalance,
                ReferenceType: "Customer",
                ReferenceId: customerId,
                ReferenceNumber: customerId.ToString(),
                Lines: new List<JournalEntryLineRequest>
                {
                    new(customerAccountId, openingBalance, 0, "رصيد افتتاحي للعميل"),
                    new(openingBalanceEquityAccountId, 0, openingBalance, "رصيد افتتاحي للعميل")
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
        int supplierAccountId,
        decimal openingBalance,
        int createdByUserId,
        DateTime transactionDate,
        CancellationToken ct = default)
    {
        if (openingBalance <= 0)
            return Result<int>.Success(0); // No entry needed

        try
        {
            var openingEquityResult = await GetAccountIdAsync(
                SystemAccountKey.OpeningBalanceEquity, null, ct);
            if (!openingEquityResult.IsSuccess)
                return Result<int>.Failure(openingEquityResult.Error!);

            var openingBalanceEquityAccountId = openingEquityResult.Value;

            var request = new CreateJournalEntryRequest(
                TransactionDate: transactionDate,
                Description: $"قيد افتتاحي — رصيد افتتاحي للمورد: {supplierName}",
                EntryType: JournalEntryType.OpeningBalance,
                ReferenceType: "Supplier",
                ReferenceId: supplierId,
                ReferenceNumber: supplierId.ToString(),
                Lines: new List<JournalEntryLineRequest>
                {
                    new(openingBalanceEquityAccountId, openingBalance, 0, "رصيد افتتاحي للمورد"),
                    new(supplierAccountId, 0, openingBalance, "رصيد افتتاحي للمورد")
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
    //  B5. Product Opening Entry (Opening Stock)
    // ────────────────────────────────────────────────────────────────
    public async Task<Result<int>> CreateProductOpeningEntryAsync(
        int productId,
        string productName,
        decimal totalOpeningValue,
        int createdByUserId,
        DateTime transactionDate,
        CancellationToken ct = default)
    {
        if (totalOpeningValue <= 0)
            return Result<int>.Success(0); // No entry needed for zero-value stock

        try
        {
            var requiredKeys = new[]
            {
                SystemAccountKey.Inventory,
                SystemAccountKey.OpeningBalanceEquity
            };

            var dictResult = await GetAccountIdDictionaryAsync(null, requiredKeys, ct);
            if (!dictResult.IsSuccess)
                return Result<int>.Failure(dictResult.Error!);

            var m = dictResult.Value!;

            var request = new CreateJournalEntryRequest(
                TransactionDate: transactionDate,
                Description: $"قيد افتتاحي — رصيد افتتاحي للمنتج: {productName}",
                EntryType: JournalEntryType.OpeningBalance,
                ReferenceType: "Product",
                ReferenceId: productId,
                ReferenceNumber: productId.ToString(),
                Lines: new List<JournalEntryLineRequest>
                {
                    new(m[SystemAccountKey.Inventory], totalOpeningValue, 0,
                        "رصيد افتتاحي للمخزون"),
                    new(m[SystemAccountKey.OpeningBalanceEquity], 0, totalOpeningValue,
                        "رصيد افتتاحي للمخزون")
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
            _logger.LogError(ex, "Error creating product opening entry for product {ProductId}", productId);
            return Result<int>.Failure("حدث خطأ أثناء إنشاء القيد الافتتاحي للمنتج");
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
            var requiredKeys = new[]
            {
                SystemAccountKey.DefaultCash,
                SystemAccountKey.AccountsReceivable,
                SystemAccountKey.SalesRevenue,
                SystemAccountKey.VatOutput,
                SystemAccountKey.CostOfGoodsSold,
                SystemAccountKey.Inventory
            };

            var dictResult = await GetAccountIdDictionaryAsync(null, requiredKeys, ct);
            if (!dictResult.IsSuccess)
                return Result<int>.Failure(dictResult.Error!);

            var m = dictResult.Value!;

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
                    m[SystemAccountKey.DefaultCash],
                    invoice.TotalAmount,
                    0,
                    "الجزء النقدي من فاتورة البيع"));
            }
            else if (invoice.PaymentType == PaymentType.Credit)
            {
                var customerAccountId = GetCustomerAccountId(invoice, m);
                lines.Add(new JournalEntryLineRequest(
                    customerAccountId,
                    invoice.TotalAmount,
                    0,
                    "الجزء الآجل من فاتورة البيع"));
            }
            else if (invoice.PaymentType == PaymentType.Mixed)
            {
                lines.Add(new JournalEntryLineRequest(
                    m[SystemAccountKey.DefaultCash],
                    invoice.PaidAmount,
                    0,
                    "الجزء النقدي من فاتورة البيع (مختلط)"));
                var customerAccountId = GetCustomerAccountId(invoice, m);
                lines.Add(new JournalEntryLineRequest(
                    customerAccountId,
                    invoice.DueAmount,
                    0,
                    "الجزء الآجل من فاتورة البيع (مختلط)"));
            }

            // Credit side - Sales Revenue (net after discount)
            lines.Add(new JournalEntryLineRequest(
                m[SystemAccountKey.SalesRevenue],
                0,
                netRevenue,
                "إيراد المبيعات (صافي بعد الخصم)"));

            // Credit side - VAT Output
            if (invoice.TaxAmount > 0)
            {
                lines.Add(new JournalEntryLineRequest(
                    m[SystemAccountKey.VatOutput],
                    0,
                    invoice.TaxAmount,
                    "ضريبة المخرجات"));
            }

            // ── COGS Side ─────────────────────────────────────────
            if (totalCost > 0)
            {
                lines.Add(new JournalEntryLineRequest(
                    m[SystemAccountKey.CostOfGoodsSold],
                    totalCost,
                    0,
                    "تكلفة البضاعة المباعة"));
                lines.Add(new JournalEntryLineRequest(
                    m[SystemAccountKey.Inventory],
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
            var requiredKeys = new[]
            {
                SystemAccountKey.DefaultCash,
                SystemAccountKey.AccountsReceivable,
                SystemAccountKey.SalesRevenue,
                SystemAccountKey.VatOutput,
                SystemAccountKey.CostOfGoodsSold,
                SystemAccountKey.Inventory
            };

            var dictResult = await GetAccountIdDictionaryAsync(null, requiredKeys, ct);
            if (!dictResult.IsSuccess)
                return Result<int>.Failure(dictResult.Error!);

            var m = dictResult.Value!;

            var netRevenue = invoice.SubTotal - invoice.DiscountAmount;
            if (netRevenue < 0)
                return Result<int>.Failure("لا يمكن أن يكون الخصم أكبر من إجمالي الفاتورة");

            var lines = new List<JournalEntryLineRequest>();

            // ── Reverse Revenue Side (mirror: swap Dr ↔ Cr) ──────
            // Original: Cr SalesRevenue, Cr VatOutput
            // Reverse:  Dr SalesRevenue, Dr VatOutput
            lines.Add(new JournalEntryLineRequest(
                m[SystemAccountKey.SalesRevenue],
                netRevenue,
                0,
                "عكس إيراد المبيعات"));

            if (invoice.TaxAmount > 0)
            {
                lines.Add(new JournalEntryLineRequest(
                    m[SystemAccountKey.VatOutput],
                    invoice.TaxAmount,
                    0,
                    "عكس ضريبة المخرجات"));
            }

            // Original: Dr Cash/AR
            // Reverse:  Cr Cash/AR
            if (invoice.PaymentType == PaymentType.Cash)
            {
                lines.Add(new JournalEntryLineRequest(
                    m[SystemAccountKey.DefaultCash],
                    0,
                    invoice.TotalAmount,
                    "عكس الجزء النقدي من فاتورة البيع"));
            }
            else if (invoice.PaymentType == PaymentType.Credit)
            {
                var customerAccountId = GetCustomerAccountId(invoice, m);
                lines.Add(new JournalEntryLineRequest(
                    customerAccountId,
                    0,
                    invoice.TotalAmount,
                    "عكس الجزء الآجل من فاتورة البيع"));
            }
            else if (invoice.PaymentType == PaymentType.Mixed)
            {
                lines.Add(new JournalEntryLineRequest(
                    m[SystemAccountKey.DefaultCash],
                    0,
                    invoice.PaidAmount,
                    "عكس الجزء النقدي من فاتورة البيع (مختلط)"));
                var customerAccountId = GetCustomerAccountId(invoice, m);
                lines.Add(new JournalEntryLineRequest(
                    customerAccountId,
                    0,
                    invoice.DueAmount,
                    "عكس الجزء الآجل من فاتورة البيع (مختلط)"));
            }

            // ── Reverse COGS Side (mirror: swap Dr ↔ Cr) ─────────
            // Original: Dr COGS, Cr Inventory
            // Reverse:  Cr COGS, Dr Inventory
            // We don't have totalCost here, so we need to query the original entry.
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
                        && jel.AccountId == m[SystemAccountKey.CostOfGoodsSold]
                        && jel.Debit > 0,
                    ct: ct);

                foreach (var cogsLine in cogsLines)
                {
                    lines.Add(new JournalEntryLineRequest(
                        m[SystemAccountKey.Inventory],
                        cogsLine.Debit,
                        0,
                        "عكس تكلفة البضاعة المباعة — إعادة المخزون"));
                    lines.Add(new JournalEntryLineRequest(
                        m[SystemAccountKey.CostOfGoodsSold],
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
                    return item.Quantity * (item.CostInBaseCurrency ?? 0m);
                });
                if (computedCost > 0)
                {
                    lines.Add(new JournalEntryLineRequest(
                        m[SystemAccountKey.Inventory],
                        computedCost,
                        0,
                        "عكس تكلفة البضاعة المباعة — إعادة المخزون (تقديري)"));
                    lines.Add(new JournalEntryLineRequest(
                        m[SystemAccountKey.CostOfGoodsSold],
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
            var requiredKeys = new[]
            {
                SystemAccountKey.Inventory,
                SystemAccountKey.VatInput,
                SystemAccountKey.DefaultCash,
                SystemAccountKey.AccountsPayable
            };

            var dictResult = await GetAccountIdDictionaryAsync(null, requiredKeys, ct);
            if (!dictResult.IsSuccess)
                return Result<int>.Failure(dictResult.Error!);

            var m = dictResult.Value!;

            // Validate: discount cannot exceed subtotal
            if (invoice.DiscountAmount > invoice.SubTotal)
                return Result<int>.Failure("لا يمكن أن يكون الخصم أكبر من إجمالي فاتورة الشراء");

            // Net inventory cost = SubTotal - DiscountAmount
            var netInventoryCost = invoice.SubTotal - invoice.DiscountAmount;

            var lines = new List<JournalEntryLineRequest>();

            // Dr Inventory Asset (net cost after discount)
            lines.Add(new JournalEntryLineRequest(
                m[SystemAccountKey.Inventory],
                netInventoryCost,
                0,
                "تكلفة المشتريات (صافي بعد الخصم)"));

            // Dr VAT Input
            if (invoice.TaxAmount > 0)
            {
                lines.Add(new JournalEntryLineRequest(
                    m[SystemAccountKey.VatInput],
                    invoice.TaxAmount,
                    0,
                    "ضريبة المدخلات"));
            }

            // Credit side (Cash / AP) depends on PaymentType
            if (invoice.PaymentType == PaymentType.Cash)
            {
                lines.Add(new JournalEntryLineRequest(
                    m[SystemAccountKey.DefaultCash],
                    0,
                    invoice.NetTotal,
                    "الجزء النقدي من فاتورة الشراء"));
            }
            else if (invoice.PaymentType == PaymentType.Credit)
            {
                var supplierAccountId = GetSupplierAccountId(invoice, m);
                lines.Add(new JournalEntryLineRequest(
                    supplierAccountId,
                    0,
                    invoice.NetTotal,
                    "الجزء الآجل من فاتورة الشراء"));
            }
            else if (invoice.PaymentType == PaymentType.Mixed)
            {
                lines.Add(new JournalEntryLineRequest(
                    m[SystemAccountKey.DefaultCash],
                    0,
                    invoice.PaidAmount,
                    "الجزء النقدي من فاتورة الشراء (مختلط)"));
                var supplierAccountId = GetSupplierAccountId(invoice, m);
                lines.Add(new JournalEntryLineRequest(
                    supplierAccountId,
                    0,
                    invoice.RemainingAmount,
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
            var requiredKeys = new[]
            {
                SystemAccountKey.Inventory,
                SystemAccountKey.VatInput,
                SystemAccountKey.DefaultCash,
                SystemAccountKey.AccountsPayable,
                SystemAccountKey.PurchaseReturns
            };

            var dictResult = await GetAccountIdDictionaryAsync(null, requiredKeys, ct);
            if (!dictResult.IsSuccess)
                return Result<int>.Failure(dictResult.Error!);

            var m = dictResult.Value!;

            var netInventoryCost = invoice.SubTotal - invoice.DiscountAmount;
            if (netInventoryCost < 0)
                return Result<int>.Failure("لا يمكن أن يكون الخصم أكبر من إجمالي الفاتورة");

            var lines = new List<JournalEntryLineRequest>();

            // ── Reverse: Cr PurchaseReturn, Cr VatInput (swap Dr ↔ Cr) ─
            // Original: Dr Inventory, Dr VatInput
            // Reverse:  Cr PurchaseReturn, Cr VatInput
            lines.Add(new JournalEntryLineRequest(
                m[SystemAccountKey.PurchaseReturns],
                0,
                netInventoryCost,
                "عكس تكلفة المشتريات - مردودات مشتريات"));

            if (invoice.TaxAmount > 0)
            {
                lines.Add(new JournalEntryLineRequest(
                    m[SystemAccountKey.VatInput],
                    0,
                    invoice.TaxAmount,
                    "عكس ضريبة المدخلات"));
            }

            // Original: Cr Cash/AP
            // Reverse:  Dr Cash/AP
            if (invoice.PaymentType == PaymentType.Cash)
            {
                lines.Add(new JournalEntryLineRequest(
                    m[SystemAccountKey.DefaultCash],
                    invoice.NetTotal,
                    0,
                    "عكس الجزء النقدي من فاتورة الشراء"));
            }
            else if (invoice.PaymentType == PaymentType.Credit)
            {
                var supplierAccountId = GetSupplierAccountId(invoice, m);
                lines.Add(new JournalEntryLineRequest(
                    supplierAccountId,
                    invoice.NetTotal,
                    0,
                    "عكس الجزء الآجل من فاتورة الشراء"));
            }
            else if (invoice.PaymentType == PaymentType.Mixed)
            {
                lines.Add(new JournalEntryLineRequest(
                    m[SystemAccountKey.DefaultCash],
                    invoice.PaidAmount,
                    0,
                    "عكس الجزء النقدي من فاتورة الشراء (مختلط)"));
                var supplierAccountId = GetSupplierAccountId(invoice, m);
                lines.Add(new JournalEntryLineRequest(
                    supplierAccountId,
                    invoice.RemainingAmount,
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
    //  G. Supplier Payment Entry
    // ────────────────────────────────────────────────────────────────
    public async Task<Result<int>> CreateSupplierPaymentEntryAsync(
        SupplierPayment payment,
        string supplierName,
        int createdByUserId,
        CancellationToken ct = default)
    {
        try
        {
            var requiredKeys = new[]
            {
                SystemAccountKey.DefaultCash,
                SystemAccountKey.AccountsPayable
            };

            var dictResult = await GetAccountIdDictionaryAsync(null, requiredKeys, ct);
            if (!dictResult.IsSuccess)
                return Result<int>.Failure(dictResult.Error!);

            var m = dictResult.Value!;

            var request = new CreateJournalEntryRequest(
                TransactionDate: payment.PaymentDate,
                Description: $"قيد سند دفع للمورد: {supplierName}",
                EntryType: JournalEntryType.SupplierPayment,
                ReferenceType: "SupplierPayment",
                ReferenceId: payment.Id,
                ReferenceNumber: payment.Id.ToString(),
                Lines: new List<JournalEntryLineRequest>
                {
                    new(payment.Supplier?.Party?.AccountId ?? m[SystemAccountKey.AccountsPayable], payment.Amount, 0, "سند دفع للمورد"),
                    new(m[SystemAccountKey.DefaultCash], 0, payment.Amount, "سند دفع للمورد")
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
    //  H. Reverse Supplier Payment Entry
    // ────────────────────────────────────────────────────────────────
    public async Task<Result<int>> ReverseSupplierPaymentEntryAsync(
        int paymentId,
        decimal amount,
        string supplierName,
        int supplierAccountId,
        int reversedByUserId,
        CancellationToken ct = default)
    {
        try
        {
            var cashResult = await GetAccountIdAsync(SystemAccountKey.DefaultCash, null, ct);
            if (!cashResult.IsSuccess)
                return Result<int>.Failure(cashResult.Error!);

            var defaultCashAccountId = cashResult.Value;

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
                    new(defaultCashAccountId, amount, 0, "عكس سند دفع للمورد"),
                    new(supplierAccountId, 0, amount, "عكس سند دفع للمورد")
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
