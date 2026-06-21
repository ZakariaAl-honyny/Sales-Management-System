using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Domain.Accounting.Enums;
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

    public async Task<Result<int>> CreateJournalEntryAsync(CreateJournalEntryRequest request, int userId, CancellationToken ct = default)
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

            var entryNo = numberResult.Value!.EntryNo;

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

            // 6. Create the journal entry via domain factory (Draft status)
            // Resolve fiscal year from entry date year
            short fiscalYearId = (short)request.EntryDate.Year;
            short currencyId = 1; // Default to base currency (resolved at service level)
            var entry = Domain.Accounting.Entities.JournalEntry.Create(
                numberResult.Value.EntryNumber,
                entryNo,
                request.EntryDate,
                request.Description ?? string.Empty,
                request.EntryType,
                fiscalYearId,
                currencyId,
                userId,
                exchangeRate: 1m,
                referenceType: request.ReferenceType,
                referenceId: request.ReferenceId,
                referenceNumber: request.ReferenceNumber);

            // 7. Add debit/credit lines via domain methods
            foreach (var line in request.Lines)
            {
                var account = accountMap[line.AccountId];
                if (line.Debit > 0)
                    entry.AddDebitLine(line.AccountId, line.Debit, line.Description);
                if (line.Credit > 0)
                    entry.AddCreditLine(line.AccountId, line.Credit, line.Description);
            }

            // 8. Save to database (Draft)
            await _uow.JournalEntries.AddAsync(entry, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Journal entry {EntryNumber} (ID={Id}) created as Draft for {EntryType} by User {UserId}",
                entry.EntryNumber, entry.Id, entry.EntryType, userId);

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

    public async Task<Result<List<JournalEntryListDto>>> GetAllAsync(int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        try
        {
            var (items, totalCount) = await _uow.JournalEntries.GetPagedAsync(
                predicate: null,
                orderConfig: q => q.OrderByDescending(e => e.EntryDate).ThenByDescending(e => e.Id),
                page: page,
                pageSize: pageSize,
                ct: ct,
                includePaths: "Lines");

            var dtos = items.Select(MapToListDto).ToList();

            _logger.LogInformation(
                "Retrieved {Count} journal entries (page {Page}, pageSize {PageSize}, total {TotalCount})",
                dtos.Count, page, pageSize, totalCount);

            return Result<List<JournalEntryListDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving journal entry list");
            return Result<List<JournalEntryListDto>>.Failure("حدث خطأ أثناء استرجاع قائمة القيود المحاسبية");
        }
    }

    public async Task<Result<JournalEntryDetailDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        try
        {
            var entry = await _uow.JournalEntries.FirstOrDefaultAsync(
                e => e.Id == id,
                ct,
                "Lines");

            if (entry == null)
                return Result<JournalEntryDetailDto>.Failure("القيد المحاسبي غير موجود", ErrorCodes.NotFound);

            var dto = MapToDetailDto(entry);

            return Result<JournalEntryDetailDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving journal entry {Id}", id);
            return Result<JournalEntryDetailDto>.Failure("حدث خطأ أثناء استرجاع القيد المحاسبي");
        }
    }

    // ─── Lifecycle Methods ────────────────────────────

    public async Task<Result<JournalEntryDetailDto>> PostJournalEntryAsync(int id, int userId, CancellationToken ct = default)
    {
        try
        {
            var entry = await _uow.JournalEntries.FirstOrDefaultAsync(
                e => e.Id == id, ct, "Lines");

            if (entry == null)
                return Result<JournalEntryDetailDto>.Failure("القيد المحاسبي غير موجود", ErrorCodes.NotFound);

            entry.Post(userId);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Journal entry {EntryNumber} (ID={Id}) posted by User {UserId}",
                entry.EntryNumber, entry.Id, userId);

            return Result<JournalEntryDetailDto>.Success(MapToDetailDto(entry));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain validation failed for posting journal entry {Id}", id);
            return Result<JournalEntryDetailDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error posting journal entry {Id}", id);
            return Result<JournalEntryDetailDto>.Failure("حدث خطأ أثناء ترحيل القيد المحاسبي");
        }
    }

    public async Task<Result<JournalEntryDetailDto>> CancelJournalEntryAsync(int id, int userId, CancellationToken ct = default)
    {
        try
        {
            var entry = await _uow.JournalEntries.FirstOrDefaultAsync(
                e => e.Id == id, ct, "Lines");

            if (entry == null)
                return Result<JournalEntryDetailDto>.Failure("القيد المحاسبي غير موجود", ErrorCodes.NotFound);

            entry.Cancel(userId);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Journal entry {EntryNumber} (ID={Id}) cancelled by User {UserId}",
                entry.EntryNumber, entry.Id, userId);

            return Result<JournalEntryDetailDto>.Success(MapToDetailDto(entry));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain validation failed for cancelling journal entry {Id}", id);
            return Result<JournalEntryDetailDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling journal entry {Id}", id);
            return Result<JournalEntryDetailDto>.Failure("حدث خطأ أثناء إلغاء القيد المحاسبي");
        }
    }

    public async Task<Result> DeleteDraftAsync(int id, int userId, CancellationToken ct = default)
    {
        try
        {
            var entry = await _uow.JournalEntries.FirstOrDefaultAsync(
                e => e.Id == id, ct);

            if (entry == null)
                return Result.Failure("القيد المحاسبي غير موجود", ErrorCodes.NotFound);

            if (entry.Status != JournalEntryStatus.Draft)
                return Result.Failure("لا يمكن حذف إلا القيود المحاسبية في حالة مسودة", ErrorCodes.InvalidOperation);

            await _uow.JournalEntries.HardDeleteAsync(entry.Id, ct);

            _logger.LogInformation(
                "Journal entry {EntryNumber} (ID={Id}) deleted (draft) by User {UserId}",
                entry.EntryNumber, entry.Id, userId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting draft journal entry {Id}", id);
            return Result.Failure("حدث خطأ أثناء حذف القيد المحاسبي");
        }
    }

    // ─── Private Mapping Helpers ──────────────────────

    private static string GetEntryTypeDisplay(JournalEntryType entryType) => entryType switch
    {
        JournalEntryType.Sales => "مبيعات",
        JournalEntryType.SalesReturn => "مرتجع مبيعات",
        JournalEntryType.Purchase => "مشتريات",
        JournalEntryType.PurchaseReturn => "مرتجع مشتريات",
        JournalEntryType.Expense => "مصروف",
        JournalEntryType.StockWriteOff => "إعدام مخزون",
        JournalEntryType.Transfer => "تحويل",
        JournalEntryType.Manual => "يدوي",
        JournalEntryType.OpeningBalance => "رصيد افتتاحي",
        JournalEntryType.CustomerReceipt => "مقبوضات عميل",
        JournalEntryType.SupplierPayment => "مدفوعات مورد",
        _ => "غير معروف"
    };

    private static string GetStatusDisplay(JournalEntryStatus status) => status switch
    {
        JournalEntryStatus.Draft => "مسودة",
        JournalEntryStatus.Posted => "مرحل",
        JournalEntryStatus.Cancelled => "ملغي",
        _ => "غير معروف"
    };

    private static JournalEntryListDto MapToListDto(Domain.Accounting.Entities.JournalEntry entry)
    {
        return new JournalEntryListDto(
            entry.Id,
            entry.EntryNumber,
            entry.EntryDate,
            entry.Description,
            GetEntryTypeDisplay(entry.EntryType),
            entry.ReferenceType,
            entry.ReferenceId,
            entry.ReferenceNumber,
            entry.TotalDebit,
            entry.TotalCredit,
            (int)entry.Status,
            GetStatusDisplay(entry.Status),
            entry.CreatedAt,
            entry.CreatedByUserId
        );
    }

    private static JournalEntryDetailDto MapToDetailDto(Domain.Accounting.Entities.JournalEntry entry)
    {
        var lineDtos = entry.Lines.Select(line => new JournalEntryLineDetailDto(
            line.Id,
            line.AccountId,
            line.Debit,
            line.Credit,
            line.Description
        )).ToList();

        return new JournalEntryDetailDto(
            entry.Id,
            entry.EntryNumber,
            entry.EntryDate,
            entry.Description,
            GetEntryTypeDisplay(entry.EntryType),
            entry.ReferenceType,
            entry.ReferenceId,
            entry.ReferenceNumber,
            (int)entry.Status,
            GetStatusDisplay(entry.Status),
            entry.ReversedByEntryId,
            entry.CreatedAt,
            entry.CreatedByUserId,
            lineDtos
        );
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
                    && jel.JournalEntry.Status == JournalEntryStatus.Posted
                    && jel.JournalEntry.EntryDate <= queryDate,
                null,
                ct,
                false,
                "JournalEntry");

            var totalDebit = lines.Sum(l => l.Debit);
            var totalCredit = lines.Sum(l => l.Credit);
            var balance = account.IsDebitNormal() ? totalDebit - totalCredit : totalCredit - totalDebit;

            return Result<AccountBalanceDto>.Success(new AccountBalanceDto(
                accountId,
                account.AccountCode,
                account.NameAr,
                (byte)account.GetAccountType(),
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
                    && jel.JournalEntry.Status == JournalEntryStatus.Posted
                    && jel.JournalEntry.EntryDate < startDate,
                null,
                ct,
                false,
                "JournalEntry");

            var openingDebit = openingLines.Sum(l => l.Debit);
            var openingCredit = openingLines.Sum(l => l.Credit);
            var openingBalance = account.IsDebitNormal() ? openingDebit - openingCredit : openingCredit - openingDebit;

            // Lines in date range
            var periodLines = await _uow.JournalEntryLines.ToListAsync(
                jel => jel.AccountId == accountId
                    && jel.JournalEntry != null
                    && jel.JournalEntry.Status == JournalEntryStatus.Posted
                    && jel.JournalEntry.EntryDate >= startDate
                    && jel.JournalEntry.EntryDate <= endDate,
                null,
                ct,
                false,
                "JournalEntry");

            var runningBalance = openingBalance;
            var statementLines = periodLines
                .OrderBy(l => l.JournalEntry!.EntryDate)
                .ThenBy(l => l.Id)
                .Select(l =>
                {
                    runningBalance += account.IsDebitNormal()
                        ? l.Debit - l.Credit
                        : l.Credit - l.Debit;
                    return new AccountLedgerLineDto(
                        l.JournalEntry!.EntryDate,
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
