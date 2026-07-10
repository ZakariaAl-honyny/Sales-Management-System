using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Domain.Accounting.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Application.Accounting.Services;

/// <summary>
/// Handles fiscal year closing: zeros out Revenue/Expense accounts,
/// transfers net income/loss to Retained Earnings (AccountCode 3102).
/// The closing entry is tracked via JournalEntry ReferenceType = "FiscalYearClosure".
/// </summary>
public class AnnualClosingService : IAnnualClosingService
{
    private const string RetainedEarningsAccountCode = "3102";

    private readonly IUnitOfWork _uow;
    private readonly IJournalEntryService _journalEntryService;
    private readonly IJournalEntryNumberGenerator _numberGenerator;
    private readonly ILogger<AnnualClosingService> _logger;

    public AnnualClosingService(
        IUnitOfWork uow,
        IJournalEntryService journalEntryService,
        IJournalEntryNumberGenerator numberGenerator,
        ILogger<AnnualClosingService> logger)
    {
        _uow = uow;
        _journalEntryService = journalEntryService;
        _numberGenerator = numberGenerator;
        _logger = logger;
    }

    public async Task<Result<int>> CloseFiscalYearAsync(
        int fiscalYear, int closedByUserId, CancellationToken ct = default)
    {
        try
        {
            // ─── Step 1: Validate input ─────────────────────────────
            if (fiscalYear <= 0 || fiscalYear > 9999)
                return Result<int>.Failure("السنة المالية غير صالحة");

            if (closedByUserId <= 0)
                return Result<int>.Failure("المستخدم مغلق غير صالح");

            // ─── Step 2: Check year not already closed via JournalEntries ──
            var alreadyClosed = await _uow.JournalEntries.AnyAsync(
                je => je.ReferenceType == "FiscalYearClosure" && je.ReferenceNumber == fiscalYear.ToString()
                    && je.Status == JournalEntryStatus.Posted, ct);
            if (alreadyClosed)
                return Result<int>.Failure(
                    $"السنة المالية {fiscalYear} مغلقة بالفعل", ErrorCodes.InvalidOperation);

            // ─── Step 3: Verify ALL journal entries for the year are posted ──
            var unpostedEntries = await _uow.JournalEntries.CountAsync(
                je => je.EntryDate.Year == fiscalYear && je.Status == JournalEntryStatus.Draft, ct);

            if (unpostedEntries > 0)
                return Result<int>.Failure(
                    $"لا يمكن إغلاق السنة المالية {fiscalYear} — يوجد {unpostedEntries} قيد محاسبي غير مرحل");

            // ─── Step 4: Get all posted JE lines for this fiscal year ──────
            var allLines = await _uow.JournalEntryLines.ToListAsync(
                jel => jel.JournalEntry != null
                    && jel.JournalEntry.Status == JournalEntryStatus.Posted
                    && jel.JournalEntry.EntryDate.Year == fiscalYear,
                null, ct, false, "JournalEntry");

            // Group by account for faster lookup
            var accountBalances = allLines
                .GroupBy(l => l.AccountId)
                .ToDictionary(g => g.Key, g => new
                {
                    TotalDebit = g.Sum(l => l.Debit),
                    TotalCredit = g.Sum(l => l.Credit)
                });

            // ─── Step 5: Get Revenue and Expense accounts ─────────────────
            var revenueAccounts = await _uow.Accounts.ToListAsync(
                a => a.GetAccountType() == AccountType.Revenue && a.IsActive, ct: ct);

            var expenseAccounts = await _uow.Accounts.ToListAsync(
                a => a.GetAccountType() == AccountType.Expense && a.IsActive, ct: ct);

            if (revenueAccounts.Count == 0 && expenseAccounts.Count == 0)
                return Result<int>.Failure(
                    "لا توجد حسابات إيرادات أو مصروفات لإغلاقها");

            // ─── Step 6: Calculate Net Income ─────────────────────────────
            // Revenue accounts: credit-normal, balance = totalCredit - totalDebit
            // Expense accounts: debit-normal, balance = totalDebit - totalCredit
            decimal totalRevenueBalance = 0;
            var revenueClosingLines = new List<(int AccountId, string AccountCode, string NameAr, decimal Amount)>();

            foreach (var revAcc in revenueAccounts)
            {
                if (accountBalances.TryGetValue(revAcc.Id, out var bal))
                {
                    var balance = bal.TotalCredit - bal.TotalDebit;
                    if (balance != 0)
                    {
                        totalRevenueBalance += balance;
                        // To close revenue: DEBIT the balance (zero out credit balance)
                        revenueClosingLines.Add((revAcc.Id, revAcc.AccountCode, revAcc.NameAr, balance));
                    }
                }
            }

            decimal totalExpenseBalance = 0;
            var expenseClosingLines = new List<(int AccountId, string AccountCode, string NameAr, decimal Amount)>();

            foreach (var expAcc in expenseAccounts)
            {
                if (accountBalances.TryGetValue(expAcc.Id, out var bal))
                {
                    var balance = bal.TotalDebit - bal.TotalCredit;
                    if (balance != 0)
                    {
                        totalExpenseBalance += balance;
                        // To close expense: CREDIT the balance (zero out debit balance)
                        expenseClosingLines.Add((expAcc.Id, expAcc.AccountCode, expAcc.NameAr, balance));
                    }
                }
            }

            var netIncome = totalRevenueBalance - totalExpenseBalance;

            // ─── Step 7: Find Retained Earnings account ──────────────────
            var retainedEarningsAccount = await _uow.Accounts.FirstOrDefaultAsync(
                a => a.AccountCode == RetainedEarningsAccountCode && a.IsActive, ct: ct);

            if (retainedEarningsAccount == null)
                return Result<int>.Failure(
                    $"حساب الأرباح المحتجزة (كود {RetainedEarningsAccountCode}) غير موجود أو غير نشط");

            // ─── Step 8: Generate entry number ────────────────────────────
            var numberResult = await _numberGenerator.GenerateAsync(ct);
            if (!numberResult.IsSuccess)
                return Result<int>.Failure(numberResult.Error!);

            // ─── Step 9: Build closing journal entry ──────────────────────
            var entryDate = new DateTime(fiscalYear, 12, 31, 23, 59, 59, DateTimeKind.Utc);

            // Look up FiscalYear entity for closing year
            var fiscalYearEntity = await _uow.FiscalYears.FirstOrDefaultAsync(
                fy => fy.Year == fiscalYear && fy.IsActive, ct: ct);
            if (fiscalYearEntity == null)
                return Result<int>.Failure($"السنة المالية {fiscalYear} غير موجودة أو غير نشطة");

            var closingEntry = Domain.Accounting.Entities.JournalEntry.Create(
                numberResult.Value!.EntryNumber,
                numberResult.Value!.EntryNo,
                entryDate,
                $"قفل السنة المالية {fiscalYear}",
                JournalEntryType.Manual,
                fiscalYearEntity.Id,
                createdBy: closedByUserId,
                referenceType: "FiscalYearClosure",
                referenceId: null,
                referenceNumber: fiscalYear.ToString());

            // Debit each Revenue account to zero
            foreach (var (accountId, _, nameAr, amount) in revenueClosingLines)
            {
                closingEntry.AddDebitLine(accountId, amount,
                    $"إقفال حساب الأرباح — {nameAr}");
            }

            // Credit each Expense account to zero
            foreach (var (accountId, _, nameAr, amount) in expenseClosingLines)
            {
                closingEntry.AddCreditLine(accountId, amount,
                    $"إقفال حساب المصروفات — {nameAr}");
            }

            // Net difference to Retained Earnings
            if (netIncome >= 0)
            {
                // Profit: Credit Retained Earnings
                closingEntry.AddCreditLine(
                    retainedEarningsAccount.Id,
                    netIncome,
                    $"صافي الربح للسنة المالية {fiscalYear}");
            }
            else
            {
                // Loss: Debit Retained Earnings
                closingEntry.AddDebitLine(
                    retainedEarningsAccount.Id,
                    Math.Abs(netIncome),
                    $"صافي الخسارة للسنة المالية {fiscalYear}");
            }

            // ─── Step 10: Post the closing entry ──────────────────────────
            closingEntry.Post(closedByUserId);

            // ─── Step 11: Save via execution strategy ──────────────────────
            await _uow.ExecuteTransactionAsync(async () =>
            {
                await _uow.JournalEntries.AddAsync(closingEntry, ct);
                await _uow.SaveChangesAsync(ct);
            }, ct);

            _logger.LogInformation(
                "Fiscal year {FiscalYear} closed. NetIncome={NetIncome:N2}, ClosingEntryId={EntryId}",
                fiscalYear, netIncome, closingEntry.Id);

            return Result<int>.Success(closingEntry.Id);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain validation failed for fiscal year {FiscalYear} closing", fiscalYear);
            return Result<int>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing fiscal year {FiscalYear}", fiscalYear);
            return Result<int>.Failure("حدث خطأ أثناء إغلاق السنة المالية");
        }
    }

    public async Task<Result<bool>> IsFiscalYearClosedAsync(int fiscalYear, CancellationToken ct = default)
    {
        try
        {
            if (fiscalYear <= 0 || fiscalYear > 9999)
                return Result<bool>.Failure("السنة المالية غير صالحة");

            var isClosed = await _uow.JournalEntries.AnyAsync(
                je => je.ReferenceType == "FiscalYearClosure" && je.ReferenceNumber == fiscalYear.ToString()
                    && je.Status == JournalEntryStatus.Posted, ct);

            return Result<bool>.Success(isClosed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if fiscal year {FiscalYear} is closed", fiscalYear);
            return Result<bool>.Failure("حدث خطأ أثناء التحقق من حالة السنة المالية");
        }
    }
}
