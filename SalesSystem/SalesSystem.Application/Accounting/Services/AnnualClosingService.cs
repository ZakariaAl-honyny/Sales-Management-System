using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Domain.Accounting.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Application.Accounting.Services;

/// <summary>
/// Handles fiscal year closing: zeros out Revenue/Expense accounts,
/// transfers net income/loss to Retained Earnings (AccountCode 3102).
/// Closures are tracked via JournalEntries with ReferenceType = "FiscalYearClosure".
/// </summary>
public class AnnualClosingService : IAnnualClosingService
{
    private const string RetainedEarningsAccountCode = "3102";
    private const string ClosingReferenceType = "FiscalYearClosure";

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

    public async Task<Result<FiscalYearClosureDto>> CloseFiscalYearAsync(
        int fiscalYear, int closedByUserId, CancellationToken ct = default)
    {
        try
        {
            // ─── Step 1: Validate input ─────────────────────────────
            if (fiscalYear <= 0 || fiscalYear > 9999)
                return Result<FiscalYearClosureDto>.Failure("السنة المالية غير صالحة");

            if (closedByUserId <= 0)
                return Result<FiscalYearClosureDto>.Failure("المستخدم مغلق غير صالح");

            // ─── Step 2: Check year not already closed via FiscalYearClosures ──
            var alreadyClosed = await _uow.FiscalYearClosures.AnyAsync(
                fyc => fyc.FiscalYear == fiscalYear, ct);
            if (alreadyClosed)
                return Result<FiscalYearClosureDto>.Failure(
                    $"السنة المالية {fiscalYear} مغلقة بالفعل", ErrorCodes.InvalidOperation);

            // ─── Step 3: Verify ALL journal entries for the year are posted ──
            var unpostedEntries = await _uow.JournalEntries.CountAsync(
                je => je.TransactionDate.Year == fiscalYear && !je.IsPosted, ct);

            if (unpostedEntries > 0)
                return Result<FiscalYearClosureDto>.Failure(
                    $"لا يمكن إغلاق السنة المالية {fiscalYear} — يوجد {unpostedEntries} قيد محاسبي غير مرحل");

            // ─── Step 4: Get all posted JE lines for this fiscal year ──────
            var allLines = await _uow.JournalEntryLines.ToListAsync(
                jel => jel.JournalEntry != null
                    && jel.JournalEntry.IsPosted
                    && jel.JournalEntry.TransactionDate.Year == fiscalYear,
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
                a => a.AccountType == AccountType.Revenue && a.IsActive, ct: ct);

            var expenseAccounts = await _uow.Accounts.ToListAsync(
                a => a.AccountType == AccountType.Expense && a.IsActive, ct: ct);

            if (revenueAccounts.Count == 0 && expenseAccounts.Count == 0)
                return Result<FiscalYearClosureDto>.Failure(
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
                return Result<FiscalYearClosureDto>.Failure(
                    $"حساب الأرباح المحتجزة (كود {RetainedEarningsAccountCode}) غير موجود أو غير نشط");

            // ─── Step 8: Generate entry number ────────────────────────────
            var numberResult = await _numberGenerator.GenerateAsync(ct);
            if (!numberResult.IsSuccess)
                return Result<FiscalYearClosureDto>.Failure(numberResult.Error!);

            // ─── Step 9: Build closing journal entry ──────────────────────
            var entryDate = new DateTime(fiscalYear, 12, 31, 23, 59, 59, DateTimeKind.Utc);

            var closingEntry = Domain.Accounting.Entities.JournalEntry.Create(
                numberResult.Value!,
                entryDate,
                $"قفل السنة المالية {fiscalYear}",
                JournalEntryType.Manual,
                closedByUserId,
                referenceType: ClosingReferenceType,
                referenceId: null,
                referenceNumber: fiscalYear.ToString());

            // Debit each Revenue account to zero
            foreach (var (accountId, accountCode, nameAr, amount) in revenueClosingLines)
            {
                closingEntry.AddDebitLine(accountId, accountCode, nameAr, amount,
                    $"إقفال حساب الأرباح — {nameAr}");
            }

            // Credit each Expense account to zero
            foreach (var (accountId, accountCode, nameAr, amount) in expenseClosingLines)
            {
                closingEntry.AddCreditLine(accountId, accountCode, nameAr, amount,
                    $"إقفال حساب المصروفات — {nameAr}");
            }

            // Net difference to Retained Earnings
            if (netIncome >= 0)
            {
                // Profit: Credit Retained Earnings
                closingEntry.AddCreditLine(
                    retainedEarningsAccount.Id,
                    retainedEarningsAccount.AccountCode,
                    retainedEarningsAccount.NameAr,
                    netIncome,
                    $"صافي الربح للسنة المالية {fiscalYear}");
            }
            else
            {
                // Loss: Debit Retained Earnings
                closingEntry.AddDebitLine(
                    retainedEarningsAccount.Id,
                    retainedEarningsAccount.AccountCode,
                    retainedEarningsAccount.NameAr,
                    Math.Abs(netIncome),
                    $"صافي الخسارة للسنة المالية {fiscalYear}");
            }

            // ─── Step 10: Validate and Post ──────────────────────────────
            closingEntry.ValidateAndPost(closedByUserId);

            // ─── Step 11: Save atomically via execution strategy ──────────────────────
            // Uses ExecuteTransactionAsync which wraps the operation in an execution strategy
            // (retry for transient failures) + explicit transaction (atomicity across both saves).
            int closureId = 0;

            await _uow.ExecuteTransactionAsync(async () =>
            {
                await _uow.JournalEntries.AddAsync(closingEntry, ct);
                await _uow.SaveChangesAsync(ct);

                var closure = Domain.Accounting.Entities.FiscalYearClosure.Create(
                    fiscalYear,
                    closedByUserId,
                    netIncome,
                    closingEntry.Id,
                    createdByUserId: closedByUserId);

                await _uow.FiscalYearClosures.AddAsync(closure, ct);
                await _uow.SaveChangesAsync(ct);

                closureId = closure.Id;
            }, ct);

            _logger.LogInformation(
                "Fiscal year {FiscalYear} closed. NetIncome={NetIncome:N2}, ClosingEntryId={EntryId}, ClosureId={ClosureId}",
                fiscalYear, netIncome, closingEntry.Id, closureId);

            return Result<FiscalYearClosureDto>.Success(new FiscalYearClosureDto(
                Id: closureId,
                FiscalYear: fiscalYear,
                ClosedAt: DateTime.UtcNow,
                ClosedByUserId: closedByUserId,
                NetIncome: netIncome,
                ClosingEntryId: closingEntry.Id,
                NetIncomeFormatted: netIncome >= 0
                    ? $"{netIncome:N2} ربح"
                    : $"{Math.Abs(netIncome):N2} خسارة"));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain validation failed for fiscal year {FiscalYear} closing", fiscalYear);
            return Result<FiscalYearClosureDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing fiscal year {FiscalYear}", fiscalYear);
            return Result<FiscalYearClosureDto>.Failure("حدث خطأ أثناء إغلاق السنة المالية");
        }
    }

    public async Task<Result<bool>> IsFiscalYearClosedAsync(int fiscalYear, CancellationToken ct = default)
    {
        try
        {
            if (fiscalYear <= 0 || fiscalYear > 9999)
                return Result<bool>.Failure("السنة المالية غير صالحة");

            var isClosed = await _uow.FiscalYearClosures.AnyAsync(
                fyc => fyc.FiscalYear == fiscalYear, ct);

            return Result<bool>.Success(isClosed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if fiscal year {FiscalYear} is closed", fiscalYear);
            return Result<bool>.Failure("حدث خطأ أثناء التحقق من حالة السنة المالية");
        }
    }

    public async Task<Result<List<FiscalYearClosureDto>>> GetAllClosuresAsync(CancellationToken ct = default)
    {
        try
        {
            var closures = await _uow.FiscalYearClosures.ToListAsync(
                fyc => true,
                q => q.OrderByDescending(fyc => fyc.FiscalYear),
                ct, false, "ClosingEntry,ClosedByUser");

            var dtos = closures.Select(closure => new FiscalYearClosureDto(
                Id: closure.Id,
                FiscalYear: closure.FiscalYear,
                ClosedAt: closure.ClosedAt,
                ClosedByUserId: closure.ClosedByUserId,
                NetIncome: closure.NetIncome,
                ClosingEntryId: closure.ClosingEntryId,
                NetIncomeFormatted: closure.NetIncome >= 0
                    ? $"{closure.NetIncome:N2} ربح"
                    : $"{Math.Abs(closure.NetIncome):N2} خسارة")
            ).ToList();

            return Result<List<FiscalYearClosureDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving fiscal year closures");
            return Result<List<FiscalYearClosureDto>>.Failure("حدث خطأ أثناء استرجاع إغلاقات السنوات المالية");
        }
    }
}
