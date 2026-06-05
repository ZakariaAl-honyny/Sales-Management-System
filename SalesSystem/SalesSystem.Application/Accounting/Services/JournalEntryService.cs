using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Application.Accounting.Services;

public class JournalEntryService : IJournalEntryService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<JournalEntryService> _logger;
    private readonly IJournalEntryNumberGenerator _numberGenerator;

    public JournalEntryService(
        IUnitOfWork uow,
        ILogger<JournalEntryService> logger,
        IJournalEntryNumberGenerator numberGenerator)
    {
        _uow = uow;
        _logger = logger;
        _numberGenerator = numberGenerator;
    }

    public async Task<Result<int>> CreateJournalEntryAsync(CreateJournalEntryRequest request, CancellationToken ct = default)
    {
        try
        {
            // 1. Validate lines exist
            if (request.Lines == null || request.Lines.Count == 0)
                return Result<int>.Failure("يجب إضافة بند واحد على الأقل");

            if (request.Lines.Count < 2)
                return Result<int>.Failure("يجب إضافة بندين على الأقل لقيد محاسبي مزدوج");

            // 2. Check balance (sum debit ≈ sum credit within 0.001m tolerance)
            var totalDebit = request.Lines.Sum(l => l.Debit);
            var totalCredit = request.Lines.Sum(l => l.Credit);
            if (Math.Abs(totalDebit - totalCredit) > 0.001m)
                return Result<int>.Failure(
                    $"القيد غير متوازن — مجموع الخصوم ({totalDebit:N2}) لا يساوي مجموع الإيداعات ({totalCredit:N2})");

            // 3. Validate individual line amounts
            foreach (var line in request.Lines)
            {
                if (line.Debit < 0 || line.Credit < 0)
                    return Result<int>.Failure("لا يمكن أن تكون قيم الخصم أو الإيداع سالبة");

                if (line.Debit == 0 && line.Credit == 0)
                    return Result<int>.Failure("يجب أن يكون للبند قيمة خصم أو إيداع");
            }

            // 4. Generate entry number
            var numberResult = await _numberGenerator.GenerateAsync(ct);
            if (!numberResult.IsSuccess)
                return Result<int>.Failure(numberResult.Error!);

            // 5. Verify all accounts exist and are active
            var accountIds = request.Lines.Select(l => l.AccountId).Distinct().ToList();
            var accounts = await _uow.Accounts.ToListAsync(
                a => accountIds.Contains(a.Id), ct: ct);
            var accountMap = accounts.ToDictionary(a => a.Id);

            foreach (var line in request.Lines)
            {
                if (!accountMap.TryGetValue(line.AccountId, out var account))
                    return Result<int>.Failure($"الحساب برقم {line.AccountId} غير موجود");

                if (!account.IsActive)
                    return Result<int>.Failure($"الحساب \"{account.NameAr}\" غير نشط");
            }

            // 6. Create the journal entry via domain factory
            var entry = Domain.Accounting.Entities.JournalEntry.Create(
                numberResult.Value!,
                request.TransactionDate,
                request.EntryType,
                request.CreatedBy,
                request.Description,
                request.ReferenceType,
                request.ReferenceId,
                request.ReferenceNumber);

            // 7. Add debit/credit lines via domain methods
            foreach (var line in request.Lines)
            {
                var account = accountMap[line.AccountId];
                if (line.Debit > 0)
                    entry.AddDebitLine(line.AccountId, account.AccountCode, account.NameAr, line.Debit, line.Description);
                if (line.Credit > 0)
                    entry.AddCreditLine(line.AccountId, account.AccountCode, account.NameAr, line.Credit, line.Description);
            }

            // 8. Validate and post the entry
            entry.ValidateAndPost(request.CreatedBy);

            // 9. Save to database
            await _uow.JournalEntries.AddAsync(entry, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Journal entry {EntryNumber} (ID={Id}) created and posted for {EntryType} by User {UserId}",
                entry.EntryNumber, entry.Id, entry.EntryType, request.CreatedBy);

            return Result<int>.Success(entry.Id);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain validation failed for journal entry creation");
            return Result<int>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating journal entry");
            return Result<int>.Failure("حدث خطأ أثناء إنشاء القيد المحاسبي");
        }
    }

    public async Task<Result<AccountBalanceDto>> GetAccountBalanceAsync(int accountId, DateTime? asOfDate = null, CancellationToken ct = default)
    {
        try
        {
            var account = await _uow.Accounts.GetByIdAsync(accountId, ct);
            if (account == null)
                return Result<AccountBalanceDto>.Failure("الحساب غير موجود", ErrorCodes.NotFound);

            var queryDate = asOfDate ?? DateTime.MaxValue;

            // Get all posted journal entry lines for this account with JournalEntry data eagerly loaded
            var lines = await _uow.JournalEntryLines.ToListAsync(
                jel => jel.AccountId == accountId
                    && jel.JournalEntry != null
                    && jel.JournalEntry.IsPosted
                    && jel.JournalEntry.TransactionDate <= queryDate,
                null, // queryConfig
                ct,   // ct
                false, // ignoreQueryFilters
                "JournalEntry");

            var totalDebit = lines.Sum(l => l.Debit);
            var totalCredit = lines.Sum(l => l.Credit);
            var balance = account.IsDebitNormal() ? totalDebit - totalCredit : totalCredit - totalDebit;

            return Result<AccountBalanceDto>.Success(new AccountBalanceDto(
                accountId,
                account.AccountCode,
                account.NameAr,
                (byte)account.AccountType,
                totalDebit,
                totalCredit,
                balance,
                account.IsDebitNormal()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting account balance for account {AccountId}", accountId);
            return Result<AccountBalanceDto>.Failure("حدث خطأ أثناء استرجاع رصيد الحساب");
        }
    }

    public async Task<Result<AccountLedgerDto>> GetAccountLedgerAsync(int accountId, DateTime startDate, DateTime endDate, CancellationToken ct = default)
    {
        try
        {
            var account = await _uow.Accounts.GetByIdAsync(accountId, ct);
            if (account == null)
                return Result<AccountLedgerDto>.Failure("الحساب غير موجود", ErrorCodes.NotFound);

            if (startDate > endDate)
                return Result<AccountLedgerDto>.Failure("تاريخ البداية يجب أن يكون قبل تاريخ النهاية");

            // Opening balance: sum all posted entries BEFORE startDate
            var openingLines = await _uow.JournalEntryLines.ToListAsync(
                jel => jel.AccountId == accountId
                    && jel.JournalEntry != null
                    && jel.JournalEntry.IsPosted
                    && jel.JournalEntry.TransactionDate < startDate,
                null, // queryConfig
                ct,   // ct
                false, // ignoreQueryFilters
                "JournalEntry");

            var openingDebit = openingLines.Sum(l => l.Debit);
            var openingCredit = openingLines.Sum(l => l.Credit);
            var openingBalance = account.IsDebitNormal() ? openingDebit - openingCredit : openingCredit - openingDebit;

            // Lines in date range
            var periodLines = await _uow.JournalEntryLines.ToListAsync(
                jel => jel.AccountId == accountId
                    && jel.JournalEntry != null
                    && jel.JournalEntry.IsPosted
                    && jel.JournalEntry.TransactionDate >= startDate
                    && jel.JournalEntry.TransactionDate <= endDate,
                null, // queryConfig
                ct,   // ct
                false, // ignoreQueryFilters
                "JournalEntry");

            var runningBalance = openingBalance;
            var statementLines = periodLines
                .OrderBy(l => l.JournalEntry!.TransactionDate)
                .ThenBy(l => l.Id)
                .Select(l =>
                {
                    runningBalance += account.IsDebitNormal()
                        ? l.Debit - l.Credit
                        : l.Credit - l.Debit;
                    return new AccountLedgerLineDto(
                        l.JournalEntry!.TransactionDate,
                        l.JournalEntry.EntryNumber,
                        l.Description ?? "",
                        l.JournalEntry.ReferenceNumber,
                        l.Debit,
                        l.Credit,
                        runningBalance);
                }).ToList();

            var totalDebit = periodLines.Sum(l => l.Debit);
            var totalCredit = periodLines.Sum(l => l.Credit);
            var closingBalance = openingBalance + (account.IsDebitNormal()
                ? totalDebit - totalCredit
                : totalCredit - totalDebit);

            return Result<AccountLedgerDto>.Success(new AccountLedgerDto(
                account.AccountCode,
                account.NameAr,
                openingBalance,
                statementLines,
                totalDebit,
                totalCredit,
                closingBalance));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating account ledger for account {AccountId}", accountId);
            return Result<AccountLedgerDto>.Failure("حدث خطأ أثناء إنشاء كشف الحساب");
        }
    }
}
