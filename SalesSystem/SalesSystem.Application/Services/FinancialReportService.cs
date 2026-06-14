using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Domain.Accounting.Enums;
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

    // ─── Income Statement (existing) ──────────────────────────────────────────

    public async Task<Result<List<IncomeStatementDto>>> GetIncomeStatementAsync(DateTime from, DateTime to, CancellationToken ct)
    {
        try
        {
            if (from > to)
                return Result<List<IncomeStatementDto>>.Failure("تاريخ البداية يجب أن يكون قبل تاريخ النهاية");

            _logger.LogInformation("Generating income statement from {From} to {To}", from, to);

            var salesInvoices = await _uow.SalesInvoices.ToListAsync(
                si => si.Status == InvoiceStatus.Posted && si.InvoiceDate >= from && si.InvoiceDate <= to,
                ct: ct);

            var totalSales = salesInvoices.Sum(si => si.TotalAmount);

            var purchaseInvoices = await _uow.PurchaseInvoices.ToListAsync(
                pi => pi.Status == InvoiceStatus.Posted && pi.InvoiceDate >= from && pi.InvoiceDate <= to,
                ct: ct);

            var totalPurchases = purchaseInvoices.Sum(pi => pi.NetTotal);

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

    // ─── Hierarchical Income Statement (Phase 31 — RULE-422) ──────────────────

    public async Task<Result<IncomeStatementHierarchyDto>> GetIncomeStatementHierarchyAsync(DateTime from, DateTime to, CancellationToken ct)
    {
        try
        {
            if (from > to)
                return Result<IncomeStatementHierarchyDto>.Failure("تاريخ البداية يجب أن يكون قبل تاريخ النهاية");

            _logger.LogInformation("Generating hierarchical income statement from {From} to {To}", from, to);

            var entries = await _uow.JournalEntries.ToListAsync(
                je => je.Status == JournalEntryStatus.Posted
                   && je.TransactionDate >= from
                   && je.TransactionDate <= to,
                q => q.Include(je => je.Lines),
                ct);

            var accountBalances = new Dictionary<int, (decimal TotalDebit, decimal TotalCredit)>();
            foreach (var entry in entries)
            {
                foreach (var line in entry.Lines)
                {
                    if (!accountBalances.ContainsKey(line.AccountId))
                        accountBalances[line.AccountId] = (0, 0);
                    var current = accountBalances[line.AccountId];
                    accountBalances[line.AccountId] = (current.TotalDebit + line.Debit, current.TotalCredit + line.Credit);
                }
            }

            if (accountBalances.Count == 0)
            {
                var emptyResult = new IncomeStatementHierarchyDto("قائمة الدخل", "Header", 0, "0.00", 0, new List<IncomeStatementHierarchyDto>
                {
                    new("لا توجد بيانات", "Total", 0, "0.00", 1)
                });
                return Result<IncomeStatementHierarchyDto>.Success(emptyResult);
            }

            var accountIds = accountBalances.Keys.ToList();
            // Lookup COGS account from key-value system mappings
            var cogsMapping = await _uow.SystemAccountMappings.FirstOrDefaultAsync(
                m => m.MappingKey == SystemAccountKey.CostOfGoodsSold, ct);
            int? cogsAccountId = cogsMapping?.AccountId;

            var accounts = await _uow.Accounts.ToListAsync(a => accountIds.Contains(a.Id), ct: ct);
            var revenueAccounts = accounts.Where(a => a.AccountType == AccountType.Revenue).ToList();
            var expenseAccounts = accounts.Where(a => a.AccountType == AccountType.Expense).ToList();

            decimal totalRevenue = 0;
            decimal totalCogs = 0;
            decimal totalOperatingExpenses = 0;

            var revenueChildren = new List<IncomeStatementHierarchyDto>();
            var cogsChildren = new List<IncomeStatementHierarchyDto>();
            var expenseChildren = new List<IncomeStatementHierarchyDto>();

            foreach (var acc in revenueAccounts)
            {
                if (!accountBalances.TryGetValue(acc.Id, out var bal)) continue;
                var amount = bal.TotalCredit - bal.TotalDebit;
                if (amount <= 0) continue;

                if (cogsAccountId.HasValue && acc.Id == cogsAccountId.Value)
                {
                    totalCogs += amount;
                    cogsChildren.Add(new IncomeStatementHierarchyDto(acc.NameAr, "SubTotal", amount, amount.ToString("N2"), revenueChildren.Count + 1));
                }
                else
                {
                    totalRevenue += amount;
                    revenueChildren.Add(new IncomeStatementHierarchyDto(acc.NameAr, "SubTotal", amount, amount.ToString("N2"), revenueChildren.Count + 1));
                }
            }

            foreach (var acc in expenseAccounts)
            {
                if (!accountBalances.TryGetValue(acc.Id, out var bal)) continue;
                var amount = bal.TotalDebit - bal.TotalCredit;
                if (amount <= 0) continue;

                if (cogsAccountId.HasValue && acc.Id == cogsAccountId.Value)
                {
                    totalCogs += amount;
                    cogsChildren.Add(new IncomeStatementHierarchyDto(acc.NameAr, "SubTotal", amount, amount.ToString("N2"), cogsChildren.Count + 1));
                }
                else
                {
                    totalOperatingExpenses += amount;
                    expenseChildren.Add(new IncomeStatementHierarchyDto(acc.NameAr, "SubTotal", amount, amount.ToString("N2"), expenseChildren.Count + 1));
                }
            }

            var grossProfit = totalRevenue - totalCogs;
            var netIncome = grossProfit - totalOperatingExpenses;

            var hierarchy = new IncomeStatementHierarchyDto("قائمة الدخل", "Header", 0, null, 0, new List<IncomeStatementHierarchyDto>
            {
                new("الإيرادات", "Header", totalRevenue, totalRevenue.ToString("N2"), 1,
                    revenueChildren.Count > 0 ? revenueChildren : new List<IncomeStatementHierarchyDto>
                    {
                        new("إيرادات المبيعات", "Total", totalRevenue, totalRevenue.ToString("N2"), 1)
                    }),
                new("تكلفة المبيعات", "Header", totalCogs, totalCogs.ToString("N2"), 2,
                    cogsChildren.Count > 0 ? cogsChildren : new List<IncomeStatementHierarchyDto>
                    {
                        new("تكلفة المبيعات", "Total", totalCogs, totalCogs.ToString("N2"), 1)
                    }),
                new("مجمل الربح", "SubTotal", grossProfit, grossProfit.ToString("N2"), 3),
                new("المصروفات التشغيلية", "Header", totalOperatingExpenses, totalOperatingExpenses.ToString("N2"), 4,
                    expenseChildren.Count > 0 ? expenseChildren : new List<IncomeStatementHierarchyDto>
                    {
                        new("مصروفات تشغيلية", "Total", totalOperatingExpenses, totalOperatingExpenses.ToString("N2"), 1)
                    }),
                new("صافي الربح / الخسارة", "Total", netIncome, netIncome.ToString("N2"), 5)
            });

            return Result<IncomeStatementHierarchyDto>.Success(hierarchy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating hierarchical income statement");
            return Result<IncomeStatementHierarchyDto>.Failure("حدث خطأ أثناء إنشاء قائمة الدخل الهرمية");
        }
    }

    // ─── Balance Sheet (Phase 31 — RULE-423) ──────────────────────────────────

    public async Task<Result<BalanceSheetDto>> GetBalanceSheetAsync(DateTime asOfDate, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Generating balance sheet as of {AsOfDate}", asOfDate);

            var entries = await _uow.JournalEntries.ToListAsync(
                je => je.Status == JournalEntryStatus.Posted && je.TransactionDate <= asOfDate,
                q => q.Include(je => je.Lines),
                ct);

            var accountBalances = new Dictionary<int, (decimal TotalDebit, decimal TotalCredit)>();
            foreach (var entry in entries)
            {
                foreach (var line in entry.Lines)
                {
                    if (!accountBalances.ContainsKey(line.AccountId))
                        accountBalances[line.AccountId] = (0, 0);
                    var current = accountBalances[line.AccountId];
                    accountBalances[line.AccountId] = (current.TotalDebit + line.Debit, current.TotalCredit + line.Credit);
                }
            }

            var allAccounts = await _uow.Accounts.ToListAsync(a => a.IsActive, ct: ct);

            var assetLines = new List<BalanceSheetLineDto>();
            var liabilityLines = new List<BalanceSheetLineDto>();
            var equityLines = new List<BalanceSheetLineDto>();

            decimal totalAssets = 0;
            decimal totalLiabilities = 0;
            decimal totalEquity = 0;

            foreach (var acc in allAccounts.Where(a => a.AllowTransactions))
            {
                var bal = accountBalances.GetValueOrDefault(acc.Id, (0, 0));
                decimal balance;

                switch (acc.AccountType)
                {
                    case AccountType.Asset:
                        balance = bal.TotalDebit - bal.TotalCredit + (acc.OpeningBalance ?? 0);
                        if (balance != 0)
                        {
                            assetLines.Add(new BalanceSheetLineDto(acc.NameAr, acc.AccountCode, balance, balance.ToString("N2")));
                            totalAssets += balance;
                        }
                        break;

                    case AccountType.Liability:
                        balance = bal.TotalCredit - bal.TotalDebit + (acc.OpeningBalance ?? 0);
                        if (balance != 0)
                        {
                            liabilityLines.Add(new BalanceSheetLineDto(acc.NameAr, acc.AccountCode, balance, balance.ToString("N2")));
                            totalLiabilities += balance;
                        }
                        break;

                    case AccountType.Equity:
                        balance = bal.TotalCredit - bal.TotalDebit + (acc.OpeningBalance ?? 0);
                        if (balance != 0)
                        {
                            equityLines.Add(new BalanceSheetLineDto(acc.NameAr, acc.AccountCode, balance, balance.ToString("N2")));
                            totalEquity += balance;
                        }
                        break;
                }
            }

            // Fallback: use OpeningBalance from accounts if no journal entries
            if (assetLines.Count == 0 && liabilityLines.Count == 0 && equityLines.Count == 0)
            {
                foreach (var acc in allAccounts.Where(a => a.OpeningBalance.HasValue && a.OpeningBalance.Value != 0))
                {
                    switch (acc.AccountType)
                    {
                        case AccountType.Asset:
                            assetLines.Add(new BalanceSheetLineDto(acc.NameAr, acc.AccountCode, acc.OpeningBalance!.Value, acc.OpeningBalance!.Value.ToString("N2")));
                            totalAssets += acc.OpeningBalance!.Value;
                            break;
                        case AccountType.Liability:
                            liabilityLines.Add(new BalanceSheetLineDto(acc.NameAr, acc.AccountCode, acc.OpeningBalance!.Value, acc.OpeningBalance!.Value.ToString("N2")));
                            totalLiabilities += acc.OpeningBalance!.Value;
                            break;
                        case AccountType.Equity:
                            equityLines.Add(new BalanceSheetLineDto(acc.NameAr, acc.AccountCode, acc.OpeningBalance!.Value, acc.OpeningBalance!.Value.ToString("N2")));
                            totalEquity += acc.OpeningBalance!.Value;
                            break;
                    }
                }
            }

            var totalLiabilitiesAndEquity = totalLiabilities + totalEquity;
            var isBalanced = Math.Abs(totalAssets - totalLiabilitiesAndEquity) < 0.01m;

            var sections = new List<BalanceSheetSectionDto>
            {
                new("الأصول", totalAssets, totalAssets.ToString("N2"), assetLines),
                new("الخصوم", totalLiabilities, totalLiabilities.ToString("N2"), liabilityLines),
                new("حقوق الملكية", totalEquity, totalEquity.ToString("N2"), equityLines)
            };

            var dto = new BalanceSheetDto(totalAssets, totalLiabilities, totalEquity,
                totalLiabilitiesAndEquity, isBalanced, sections);

            _logger.LogInformation("Balance sheet as of {AsOfDate}: Assets={TotalAssets}, Liabilities={TotalLiabilities}, Equity={TotalEquity}, Balanced={IsBalanced}",
                asOfDate, totalAssets, totalLiabilities, totalEquity, isBalanced);

            return Result<BalanceSheetDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating balance sheet");
            return Result<BalanceSheetDto>.Failure("حدث خطأ أثناء إنشاء الميزانية العمومية");
        }
    }

    // ─── Trial Balance (Phase 31) ─────────────────────────────────────────────

    public async Task<Result<List<TrialBalanceDto>>> GetTrialBalanceAsync(DateTime asOfDate, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Generating trial balance as of {AsOfDate}", asOfDate);

            var entries = await _uow.JournalEntries.ToListAsync(
                je => je.Status == JournalEntryStatus.Posted && je.TransactionDate <= asOfDate,
                q => q.Include(je => je.Lines),
                ct);

            var accountBalances = new Dictionary<int, (decimal TotalDebit, decimal TotalCredit)>();
            foreach (var entry in entries)
            {
                foreach (var line in entry.Lines)
                {
                    if (!accountBalances.ContainsKey(line.AccountId))
                        accountBalances[line.AccountId] = (0, 0);
                    var current = accountBalances[line.AccountId];
                    accountBalances[line.AccountId] = (current.TotalDebit + line.Debit, current.TotalCredit + line.Credit);
                }
            }

            var accounts = await _uow.Accounts.ToListAsync(
                a => a.IsActive && a.AllowTransactions,
                q => q.OrderBy(a => a.AccountCode),
                ct);

            var result = new List<TrialBalanceDto>();
            foreach (var acc in accounts)
            {
                var bal = accountBalances.GetValueOrDefault(acc.Id, (0, 0));
                var openingBal = acc.OpeningBalance ?? 0;

                decimal openingDebit = acc.IsDebitNormal() ? openingBal : 0;
                decimal openingCredit = acc.IsDebitNormal() ? 0 : openingBal;

                decimal closingDebit, closingCredit;
                if (acc.IsDebitNormal())
                {
                    var netBalance = openingBal + bal.TotalDebit - bal.TotalCredit;
                    closingDebit = netBalance >= 0 ? netBalance : 0;
                    closingCredit = netBalance < 0 ? Math.Abs(netBalance) : 0;
                }
                else
                {
                    var netBalance = openingBal + bal.TotalCredit - bal.TotalDebit;
                    closingCredit = netBalance >= 0 ? netBalance : 0;
                    closingDebit = netBalance < 0 ? Math.Abs(netBalance) : 0;
                }

                if (bal.TotalDebit == 0 && bal.TotalCredit == 0 && openingBal == 0)
                    continue;

                result.Add(new TrialBalanceDto(
                    acc.AccountCode, acc.NameAr,
                    openingDebit, openingCredit,
                    bal.TotalDebit, bal.TotalCredit,
                    closingDebit, closingCredit,
                    acc.AccountType switch
                    {
                        AccountType.Asset => "أصل",
                        AccountType.Liability => "خصم",
                        AccountType.Equity => "حق ملكية",
                        AccountType.Revenue => "إيراد",
                        AccountType.Expense => "مصروف",
                        _ => null
                    }
                ));
            }

            _logger.LogInformation("Trial balance generated with {Count} accounts", result.Count);
            return Result<List<TrialBalanceDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating trial balance");
            return Result<List<TrialBalanceDto>>.Failure("حدث خطأ أثناء إنشاء ميزان المراجعة");
        }
    }

    // ─── General Ledger (Phase 31) ────────────────────────────────────────────

    public async Task<Result<GeneralLedgerDto>> GetGeneralLedgerAsync(int accountId, DateTime from, DateTime to, CancellationToken ct)
    {
        try
        {
            if (accountId <= 0)
                return Result<GeneralLedgerDto>.Failure("معرف الحساب غير صالح");
            if (from > to)
                return Result<GeneralLedgerDto>.Failure("تاريخ البداية يجب أن يكون قبل تاريخ النهاية");

            _logger.LogInformation("Generating general ledger for account {AccountId} from {From} to {To}", accountId, from, to);

            var account = await _uow.Accounts.GetByIdAsync(accountId, ct);
            if (account == null)
                return Result<GeneralLedgerDto>.Failure("الحساب غير موجود");

            var priorLines = await _uow.JournalEntryLines.ToListAsync(
                jel => jel.AccountId == accountId
                    && jel.JournalEntry!.Status == JournalEntryStatus.Posted
                    && jel.JournalEntry!.TransactionDate < from,
                q => q.Include(jel => jel.JournalEntry!),
                ct);

            var priorDebit = priorLines.Sum(jel => jel.Debit);
            var priorCredit = priorLines.Sum(jel => jel.Credit);

            decimal openingBalance;
            if (account.IsDebitNormal())
                openingBalance = (account.OpeningBalance ?? 0) + priorDebit - priorCredit;
            else
                openingBalance = (account.OpeningBalance ?? 0) + priorCredit - priorDebit;

            var periodLines = await _uow.JournalEntryLines.ToListAsync(
                jel => jel.AccountId == accountId
                    && jel.JournalEntry!.Status == JournalEntryStatus.Posted
                    && jel.JournalEntry!.TransactionDate >= from
                    && jel.JournalEntry!.TransactionDate <= to,
                q => q.OrderBy(jel => jel.JournalEntry!.TransactionDate)
                      .ThenBy(jel => jel.Id)
                      .Include(jel => jel.JournalEntry!),
                ct);

            var lines = new List<GeneralLedgerLineDto>();
            var runningBalance = openingBalance;

            foreach (var line in periodLines)
            {
                if (account.IsDebitNormal())
                    runningBalance += line.Debit - line.Credit;
                else
                    runningBalance += line.Credit - line.Debit;

                lines.Add(new GeneralLedgerLineDto(
                    line.JournalEntry!.TransactionDate,
                    line.JournalEntry!.EntryNumber,
                    line.JournalEntry!.Description,
                    line.Debit, line.Credit,
                    runningBalance
                ));
            }

            var totalDebit = periodLines.Sum(l => l.Debit);
            var totalCredit = periodLines.Sum(l => l.Credit);
            var closingBalance = lines.Count > 0 ? lines.Last().RunningBalance : openingBalance;

            var dto = new GeneralLedgerDto(
                account.AccountCode, account.NameAr,
                from, to, openingBalance,
                lines, totalDebit, totalCredit, closingBalance
            );

            _logger.LogInformation("General ledger for account {AccountId}: {Count} lines, closing={ClosingBalance}",
                accountId, lines.Count, closingBalance);

            return Result<GeneralLedgerDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating general ledger for account {AccountId}", accountId);
            return Result<GeneralLedgerDto>.Failure("حدث خطأ أثناء إنشاء دفتر الأستاذ العام");
        }
    }

    // ─── Cash Flow (existing) ─────────────────────────────────────────────────

    public async Task<Result<CashFlowReportDto>> GetCashFlowReportAsync(DateTime from, DateTime to, int? cashBoxId, CancellationToken ct)
    {
        try
        {
            _logger.LogWarning("Cash flow report not available - CashTransaction entity removed. Rewrite using ReceiptVoucher/PaymentVoucher.");

            return Result<CashFlowReportDto>.Failure("تقرير التدفق النقدي قيد إعادة البناء باستخدام سندات القبض والصرف الجديدة");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating cash flow report");
            return Result<CashFlowReportDto>.Failure("حدث خطأ أثناء إنشاء تقرير التدفق النقدي");
        }
    }

    // ─── VAT Report (existing) ────────────────────────────────────────────────

    public async Task<Result<List<VatReportDto>>> GetVatReportAsync(DateTime from, DateTime to, CancellationToken ct)
    {
        try
        {
            if (from > to)
                return Result<List<VatReportDto>>.Failure("تاريخ البداية يجب أن يكون قبل تاريخ النهاية");

            _logger.LogInformation("Generating VAT report from {From} to {To}", from, to);

            var vatItems = new List<VatReportDto>();

            var salesInvoices = await _uow.SalesInvoices.ToListAsync(
                si => si.Status == InvoiceStatus.Posted
                   && si.TaxAmount > 0
                   && si.InvoiceDate >= from
                   && si.InvoiceDate <= to,
                q => q.OrderBy(si => si.InvoiceDate),
                ct: ct,
                includePaths: "Customer.Party");

            foreach (var invoice in salesInvoices)
            {
                var taxableAmount = invoice.SubTotal - invoice.DiscountAmount;
                var taxRate = taxableAmount > 0
                    ? Math.Round(invoice.TaxAmount / taxableAmount * 100, 2)
                    : 0;

                vatItems.Add(new VatReportDto(
                    invoice.Id.ToString(), invoice.InvoiceDate,
                    invoice.Customer?.Party?.Name, taxableAmount, taxRate, invoice.TaxAmount));
            }

            var purchaseInvoices = await _uow.PurchaseInvoices.ToListAsync(
                pi => pi.Status == InvoiceStatus.Posted
                   && pi.TaxAmount > 0
                   && pi.InvoiceDate >= from
                   && pi.InvoiceDate <= to,
                q => q.OrderBy(pi => pi.InvoiceDate),
                ct: ct,
                includePaths: "Supplier.Party");

            foreach (var invoice in purchaseInvoices)
            {
                var taxableAmount = invoice.SubTotal - invoice.DiscountAmount;
                var taxRate = taxableAmount > 0
                    ? Math.Round(invoice.TaxAmount / taxableAmount * 100, 2)
                    : 0;

                vatItems.Add(new VatReportDto(
                    invoice.Id.ToString(), invoice.InvoiceDate,
                    invoice.Supplier?.Party?.Name, taxableAmount, taxRate, invoice.TaxAmount));
            }

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

    // ─── Account Statements (existing) ────────────────────────────────────────

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

            var invoices = await _uow.SalesInvoices.ToListAsync(
                si => si.CustomerId == customerId && si.Status == InvoiceStatus.Posted
                   && si.InvoiceDate >= from && si.InvoiceDate <= to,
                q => q.OrderBy(si => si.InvoiceDate), ct: ct);

            foreach (var invoice in invoices)
            {
                entries.Add(new AccountStatementDto(invoice.InvoiceDate, "فاتورة بيع",
                    invoice.Id.ToString(), invoice.TotalAmount, 0, 0));
            }

            entries = entries.OrderBy(e => e.Date).ThenByDescending(e => e.Debit > 0 ? 0 : 1).ToList();

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

            var invoices = await _uow.PurchaseInvoices.ToListAsync(
                pi => pi.SupplierId == supplierId && pi.Status == InvoiceStatus.Posted
                   && pi.InvoiceDate >= from && pi.InvoiceDate <= to,
                q => q.OrderBy(pi => pi.InvoiceDate), ct: ct);

            foreach (var invoice in invoices)
            {
                entries.Add(new AccountStatementDto(invoice.InvoiceDate, "فاتورة شراء",
                    invoice.Id.ToString(), 0, invoice.NetTotal, 0));
            }

            var payments = await _uow.SupplierPayments.ToListAsync(
                sp => sp.SupplierId == supplierId && sp.PaymentDate >= from && sp.PaymentDate <= to,
                q => q.OrderBy(sp => sp.PaymentDate), ct: ct);

            foreach (var payment in payments)
            {
                entries.Add(new AccountStatementDto(payment.PaymentDate, "دفعة مورد",
                    payment.PaymentNo, payment.Amount, 0, 0));
            }

            entries = entries.OrderBy(e => e.Date).ThenByDescending(e => e.Credit > 0 ? 0 : 1).ToList();

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
