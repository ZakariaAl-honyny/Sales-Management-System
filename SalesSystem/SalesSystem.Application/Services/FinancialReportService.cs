using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;

namespace SalesSystem.Application.Services;

public class FinancialReportService : IFinancialReportService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<FinancialReportService> _logger;

    public FinancialReportService(IUnitOfWork uow, ILogger<FinancialReportService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<Result<List<IncomeStatementDto>>> GetIncomeStatementAsync(DateTime from, DateTime to, CancellationToken ct)
    {
        try
        {
            if (from > to)
                return Result<List<IncomeStatementDto>>.Failure("تاريخ البداية يجب أن يكون قبل تاريخ النهاية");

            _logger.LogInformation("Generating income statement from {From} to {To}", from, to);

            // Get posted sales invoices in date range
            var salesInvoices = await _uow.SalesInvoices.ToListAsync(
                si => si.Status == InvoiceStatus.Posted
                   && si.InvoiceDate >= from
                   && si.InvoiceDate <= to,
                ct: ct);

            var totalSales = salesInvoices.Sum(si => si.TotalAmount);

            // Get posted purchase invoices in date range
            var purchaseInvoices = await _uow.PurchaseInvoices.ToListAsync(
                pi => pi.Status == InvoiceStatus.Posted
                   && pi.InvoiceDate >= from
                   && pi.InvoiceDate <= to,
                ct: ct);

            var totalPurchases = purchaseInvoices.Sum(pi => pi.TotalAmount);

            var netProfit = totalSales - totalPurchases;

            var items = new List<IncomeStatementDto>
            {
                new("إيرادات", "إيرادات المبيعات", totalSales),
                new("تكاليف", "تكلفة المشتريات", totalPurchases),
                new(netProfit >= 0 ? "أرباح" : "خسائر",
                    netProfit >= 0 ? "صافي الربح" : "صافي الخسارة",
                    Math.Abs(netProfit))
            };

            _logger.LogInformation("Income statement generated: Sales={TotalSales}, Purchases={TotalPurchases}, Net={NetProfit}",
                totalSales, totalPurchases, netProfit);

            return Result<List<IncomeStatementDto>>.Success(items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating income statement");
            return Result<List<IncomeStatementDto>>.Failure("حدث خطأ أثناء إنشاء قائمة الدخل");
        }
    }

    public async Task<Result<CashFlowReportDto>> GetCashFlowReportAsync(DateTime from, DateTime to, int? cashBoxId, CancellationToken ct)
    {
        try
        {
            if (from > to)
                return Result<CashFlowReportDto>.Failure("تاريخ البداية يجب أن يكون قبل تاريخ النهاية");

            _logger.LogInformation("Generating cash flow report from {From} to {To} for cash box {CashBoxId}", from, to, cashBoxId);

            // Get transactions in date range, optionally filtered by cash box
            var transactions = await _uow.CashTransactions.ToListAsync(
                tx => tx.CreatedAt >= from
                   && tx.CreatedAt <= to
                   && (!cashBoxId.HasValue || tx.CashBoxId == cashBoxId.Value),
                q => q.OrderBy(tx => tx.CreatedAt),
                ct: ct);

            // Determine opening balance:
            // 1. Find the last transaction BEFORE the from date for the same cash box(es) and use its RunningBalance
            // 2. If none, derive from the net transaction amount in the period
            // 3. For all-boxes mode, compute from the first transaction's RunningBalance minus its Amount
            decimal openingBalance = 0;

            if (cashBoxId.HasValue)
            {
                // Get the last transaction before the from date for this box
                var lastTxList = await _uow.CashTransactions.ToListAsync(
                    tx => tx.CashBoxId == cashBoxId.Value && tx.CreatedAt < from,
                    q => q.OrderByDescending(tx => tx.CreatedAt),
                    ct: ct);
                var lastTxBefore = lastTxList.FirstOrDefault();

                if (lastTxBefore != null)
                {
                    openingBalance = lastTxBefore.RunningBalance;
                }
                else
                {
                    // No transactions before the period — derive from transactions in period
                    var netInPeriod = transactions
                        .Where(tx => tx.CashBoxId == cashBoxId.Value)
                        .Sum(tx => tx.Amount);
                    // Opening = total income - total expense for the period with sign reversed
                    // If all transactions are captured correctly, the running balance starts at 0
                    openingBalance = -netInPeriod;
                    if (openingBalance < 0)
                        openingBalance = 0m;
                }
            }
            else
            {
                // For all cash boxes, estimate opening balance from earliest transaction balance
                var firstTx = transactions.FirstOrDefault();
                if (firstTx != null)
                {
                    openingBalance = firstTx.RunningBalance - firstTx.Amount;
                }
            }

            // Categorize transactions
            var incomeItems = new List<CashFlowItemDto>();
            var expenseItems = new List<CashFlowItemDto>();

            decimal totalIncome = 0;
            decimal totalExpense = 0;

            // Income types: SalesIncome(2), CustomerPayment(8), TransferIn(5)
            var incomeTransactions = transactions
                .Where(tx => tx.TransactionType == CashTransactionType.SalesIncome
                          || tx.TransactionType == CashTransactionType.CustomerPayment
                          || tx.TransactionType == CashTransactionType.TransferIn)
                .ToList();

            var salesIncome = incomeTransactions
                .Where(tx => tx.TransactionType == CashTransactionType.SalesIncome)
                .Sum(tx => Math.Abs(tx.Amount));
            if (salesIncome > 0)
            {
                incomeItems.Add(new CashFlowItemDto("إيرادات مبيعات", salesIncome));
                totalIncome += salesIncome;
            }

            var customerPayments = incomeTransactions
                .Where(tx => tx.TransactionType == CashTransactionType.CustomerPayment)
                .Sum(tx => Math.Abs(tx.Amount));
            if (customerPayments > 0)
            {
                incomeItems.Add(new CashFlowItemDto("مدفوعات عملاء", customerPayments));
                totalIncome += customerPayments;
            }

            var transfersIn = incomeTransactions
                .Where(tx => tx.TransactionType == CashTransactionType.TransferIn)
                .Sum(tx => Math.Abs(tx.Amount));
            if (transfersIn > 0)
            {
                incomeItems.Add(new CashFlowItemDto("تحويلات واردة", transfersIn));
                totalIncome += transfersIn;
            }

            // Expense types: Expense(3), SupplierPayment(7), RefundOut(6), TransferOut(4)
            var expenseTransactions = transactions
                .Where(tx => tx.TransactionType == CashTransactionType.Expense
                          || tx.TransactionType == CashTransactionType.SupplierPayment
                          || tx.TransactionType == CashTransactionType.RefundOut
                          || tx.TransactionType == CashTransactionType.TransferOut)
                .ToList();

            var expenses = expenseTransactions
                .Where(tx => tx.TransactionType == CashTransactionType.Expense)
                .Sum(tx => Math.Abs(tx.Amount));
            if (expenses > 0)
            {
                expenseItems.Add(new CashFlowItemDto("مصروفات", expenses));
                totalExpense += expenses;
            }

            var supplierPayments = expenseTransactions
                .Where(tx => tx.TransactionType == CashTransactionType.SupplierPayment)
                .Sum(tx => Math.Abs(tx.Amount));
            if (supplierPayments > 0)
            {
                expenseItems.Add(new CashFlowItemDto("مدفوعات موردين", supplierPayments));
                totalExpense += supplierPayments;
            }

            var refundsOut = expenseTransactions
                .Where(tx => tx.TransactionType == CashTransactionType.RefundOut)
                .Sum(tx => Math.Abs(tx.Amount));
            if (refundsOut > 0)
            {
                expenseItems.Add(new CashFlowItemDto("مرتجعات مبيعات", refundsOut));
                totalExpense += refundsOut;
            }

            var transfersOut = expenseTransactions
                .Where(tx => tx.TransactionType == CashTransactionType.TransferOut)
                .Sum(tx => Math.Abs(tx.Amount));
            if (transfersOut > 0)
            {
                expenseItems.Add(new CashFlowItemDto("تحويلات صادرة", transfersOut));
                totalExpense += transfersOut;
            }

            var netCashFlow = totalIncome - totalExpense;
            var closingBalance = openingBalance + netCashFlow;

            var report = new CashFlowReportDto(
                openingBalance,
                totalIncome,
                totalExpense,
                netCashFlow,
                closingBalance,
                incomeItems,
                expenseItems);

            _logger.LogInformation(
                "Cash flow report generated: Opening={Opening}, Income={Income}, Expense={Expense}, Closing={Closing}",
                openingBalance, totalIncome, totalExpense, closingBalance);

            return Result<CashFlowReportDto>.Success(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating cash flow report");
            return Result<CashFlowReportDto>.Failure("حدث خطأ أثناء إنشاء تقرير التدفق النقدي");
        }
    }

    public async Task<Result<List<VatReportDto>>> GetVatReportAsync(DateTime from, DateTime to, CancellationToken ct)
    {
        try
        {
            if (from > to)
                return Result<List<VatReportDto>>.Failure("تاريخ البداية يجب أن يكون قبل تاريخ النهاية");

            _logger.LogInformation("Generating VAT report from {From} to {To}", from, to);

            var vatItems = new List<VatReportDto>();

            // Get posted sales invoices with tax
            var salesInvoices = await _uow.SalesInvoices.ToListAsync(
                si => si.Status == InvoiceStatus.Posted
                   && si.TaxAmount > 0
                   && si.InvoiceDate >= from
                   && si.InvoiceDate <= to,
                q => q.OrderBy(si => si.InvoiceDate),
                ct: ct,
                includePaths: "Customer");

            foreach (var invoice in salesInvoices)
            {
                // Calculate taxable amount (SubTotal - DiscountAmount)
                var taxableAmount = invoice.SubTotal - invoice.DiscountAmount;
                var taxRate = taxableAmount > 0
                    ? Math.Round(invoice.TaxAmount / taxableAmount * 100, 2)
                    : 0;

                vatItems.Add(new VatReportDto(
                    invoice.Id.ToString(),
                    invoice.InvoiceDate,
                    invoice.Customer?.Name,
                    taxableAmount,
                    taxRate,
                    invoice.TaxAmount));
            }

            // Get posted purchase invoices with tax
            var purchaseInvoices = await _uow.PurchaseInvoices.ToListAsync(
                pi => pi.Status == InvoiceStatus.Posted
                   && pi.TaxAmount > 0
                   && pi.InvoiceDate >= from
                   && pi.InvoiceDate <= to,
                q => q.OrderBy(pi => pi.InvoiceDate),
                ct: ct,
                includePaths: "Supplier");

            foreach (var invoice in purchaseInvoices)
            {
                var taxableAmount = invoice.SubTotal - invoice.DiscountAmount;
                var taxRate = taxableAmount > 0
                    ? Math.Round(invoice.TaxAmount / taxableAmount * 100, 2)
                    : 0;

                vatItems.Add(new VatReportDto(
                    invoice.Id.ToString(),
                    invoice.InvoiceDate,
                    invoice.Supplier?.Name,
                    taxableAmount,
                    taxRate,
                    invoice.TaxAmount));
            }

            // Sort combined list by date
            vatItems = vatItems.OrderBy(v => v.InvoiceDate).ToList();

            _logger.LogInformation("VAT report generated with {Count} entries", vatItems.Count);

            return Result<List<VatReportDto>>.Success(vatItems);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating VAT report");
            return Result<List<VatReportDto>>.Failure("حدث خطأ أثناء إنشاء تقرير الضريبة");
        }
    }

    public async Task<Result<List<AccountStatementDto>>> GetAccountStatementAsync(int customerId, DateTime from, DateTime to, CancellationToken ct)
    {
        try
        {
            if (from > to)
                return Result<List<AccountStatementDto>>.Failure("تاريخ البداية يجب أن يكون قبل تاريخ النهاية");

            if (customerId <= 0)
                return Result<List<AccountStatementDto>>.Failure("معرف العميل غير صالح");

            _logger.LogInformation("Generating account statement for customer {CustomerId} from {From} to {To}", customerId, from, to);

            var entries = new List<AccountStatementDto>();

            // Get posted sales invoices for this customer in date range
            var invoices = await _uow.SalesInvoices.ToListAsync(
                si => si.CustomerId == customerId
                   && si.Status == InvoiceStatus.Posted
                   && si.InvoiceDate >= from
                   && si.InvoiceDate <= to,
                q => q.OrderBy(si => si.InvoiceDate),
                ct: ct);

            foreach (var invoice in invoices)
            {
                entries.Add(new AccountStatementDto(
                    invoice.InvoiceDate,
                    "فاتورة بيع",
                    invoice.Id.ToString(),
                    invoice.TotalAmount,   // Debit — customer owes
                    0,                      // Credit
                    0));                    // Balance — will compute later
            }

            // Get customer payments in date range
            var payments = await _uow.CustomerPayments.ToListAsync(
                cp => cp.CustomerId == customerId
                   && cp.PaymentDate >= from
                   && cp.PaymentDate <= to,
                q => q.OrderBy(cp => cp.PaymentDate),
                ct: ct);

            foreach (var payment in payments)
            {
                entries.Add(new AccountStatementDto(
                    payment.PaymentDate,
                    "دفعة عميل",
                    payment.PaymentNo,
                    0,                      // Debit
                    payment.Amount,          // Credit — payment reduces debt
                    0));                    // Balance — will compute later
            }

            // Sort all entries by date, then by type (invoices before payments on same day)
            entries = entries
                .OrderBy(e => e.Date)
                .ThenByDescending(e => e.Debit > 0 ? 0 : 1) // Invoices first
                .ToList();

            // Compute running balance
            decimal runningBalance = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                runningBalance += entries[i].Debit - entries[i].Credit;
                entries[i] = entries[i] with { Balance = runningBalance };
            }

            _logger.LogInformation("Account statement for customer {CustomerId}: {Count} entries, final balance {Balance}",
                customerId, entries.Count, runningBalance);

            return Result<List<AccountStatementDto>>.Success(entries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating account statement for customer {CustomerId}", customerId);
            return Result<List<AccountStatementDto>>.Failure("حدث خطأ أثناء إنشاء كشف حساب العميل");
        }
    }

    public async Task<Result<List<AccountStatementDto>>> GetSupplierStatementAsync(int supplierId, DateTime from, DateTime to, CancellationToken ct)
    {
        try
        {
            if (from > to)
                return Result<List<AccountStatementDto>>.Failure("تاريخ البداية يجب أن يكون قبل تاريخ النهاية");

            if (supplierId <= 0)
                return Result<List<AccountStatementDto>>.Failure("معرف المورد غير صالح");

            _logger.LogInformation("Generating account statement for supplier {SupplierId} from {From} to {To}", supplierId, from, to);

            var entries = new List<AccountStatementDto>();

            // Get posted purchase invoices for this supplier in date range
            var invoices = await _uow.PurchaseInvoices.ToListAsync(
                pi => pi.SupplierId == supplierId
                   && pi.Status == InvoiceStatus.Posted
                   && pi.InvoiceDate >= from
                   && pi.InvoiceDate <= to,
                q => q.OrderBy(pi => pi.InvoiceDate),
                ct: ct);

            foreach (var invoice in invoices)
            {
                entries.Add(new AccountStatementDto(
                    invoice.InvoiceDate,
                    "فاتورة شراء",
                    invoice.Id.ToString(),
                    0,                          // Debit
                    invoice.TotalAmount,         // Credit — we owe the supplier
                    0));                         // Balance — will compute later
            }

            // Get supplier payments in date range
            var payments = await _uow.SupplierPayments.ToListAsync(
                sp => sp.SupplierId == supplierId
                   && sp.PaymentDate >= from
                   && sp.PaymentDate <= to,
                q => q.OrderBy(sp => sp.PaymentDate),
                ct: ct);

            foreach (var payment in payments)
            {
                entries.Add(new AccountStatementDto(
                    payment.PaymentDate,
                    "دفعة مورد",
                    payment.PaymentNo,
                    payment.Amount,              // Debit — payment reduces our debt
                    0,                           // Credit
                    0));                         // Balance — will compute later
            }

            // Sort all entries by date, then by type
            entries = entries
                .OrderBy(e => e.Date)
                .ThenByDescending(e => e.Credit > 0 ? 0 : 1) // Invoices first
                .ToList();

            // Compute running balance (positive = we owe the supplier)
            decimal runningBalance = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                runningBalance += entries[i].Credit - entries[i].Debit;
                entries[i] = entries[i] with { Balance = runningBalance };
            }

            _logger.LogInformation("Account statement for supplier {SupplierId}: {Count} entries, final balance {Balance}",
                supplierId, entries.Count, runningBalance);

            return Result<List<AccountStatementDto>>.Success(entries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating account statement for supplier {SupplierId}", supplierId);
            return Result<List<AccountStatementDto>>.Failure("حدث خطأ أثناء إنشاء كشف حساب المورد");
        }
    }
}
