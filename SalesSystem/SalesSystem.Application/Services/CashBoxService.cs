using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;
using SalesSystem.Domain.Accounting.Entities;
using SalesSystem.Domain.Accounting.Enums;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Application.Services;

public class CashBoxService : ICashBoxService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<CashBoxService> _logger;

    private const string CashParentAccountCode = "1110";

    public CashBoxService(IUnitOfWork uow, ILogger<CashBoxService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<Result<List<CashBoxDto>>> GetAllAsync(CancellationToken ct)
    {
        try
        {
            var boxes = await _uow.CashBoxes.ToListAsync(ct, "Account", "Category");
            var dtos = boxes.Select(MapToDto).ToList();
            return Result<List<CashBoxDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all cash boxes");
            return Result<List<CashBoxDto>>.Failure("حدث خطأ أثناء استرجاع قائمة الصناديق");
        }
    }

    public async Task<Result<CashBoxDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        try
        {
            var box = await _uow.CashBoxes.GetByIdAsync(id, ct);
            if (box == null)
                return Result<CashBoxDto>.Failure("الصندوق غير موجود", ErrorCodes.NotFound);

            return Result<CashBoxDto>.Success(MapToDto(box));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cash box {Id}", id);
            return Result<CashBoxDto>.Failure("حدث خطأ أثناء استرجاع بيانات الصندوق");
        }
    }

    public async Task<Result<CashBoxDto>> CreateAsync(CreateCashBoxRequest request, int userId, CancellationToken ct)
    {
        try
        {
            // Step 1: Resolve AccountId (auto-create if not provided)
            int accountId;
            if (request.AccountId.HasValue && request.AccountId.Value > 0)
            {
                accountId = request.AccountId.Value;
            }
            else
            {
                // Create the account and persist it FIRST so EF Core populates account.Id with the real DB-generated value
                var account = await CreateCashBoxAccountAsync(request.BoxName, ct);
                await _uow.Accounts.AddAsync(account, ct);
                await _uow.SaveChangesAsync(ct);
                accountId = account.Id;
            }

            // Step 2: Create the CashBox entity
            var box = CashBox.Create(
                request.BoxName,
                accountId,
                categoryId: request.CategoryId,
                branchId: request.BranchId,
                assignedUserId: request.AssignedUserId,
                currencyId: request.CurrencyId,
                phoneNumber: request.PhoneNumber,
                taxNumber: request.TaxNumber,
                address: request.Address,
                notes: request.Notes);

            box.SetCreatedBy(userId);
            await _uow.CashBoxes.AddAsync(box, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Cash box created: {BoxName} (ID: {Id}, AccountId: {AccountId}) by User {UserId}",
                box.BoxName, box.Id, accountId, userId);

            return Result<CashBoxDto>.Success(MapToDto(box));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation creating cash box: {Message}", ex.Message);
            return Result<CashBoxDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating cash box");
            return Result<CashBoxDto>.Failure("حدث خطأ أثناء إنشاء الصندوق");
        }
    }

    /// <summary>
    /// Creates a Level-4 detail Account entity under "1110 — النقدية" (Cash &amp; Cash Equivalents).
    /// Generates the next available account code (e.g., 1113, 1114, ...).
    /// NOTE: Does NOT save to DB — caller must persist and use the returned Account.Id.
    /// </summary>
    private async Task<Account> CreateCashBoxAccountAsync(string boxName, CancellationToken ct)
    {
        // Find the parent account "1110 — النقدية"
        var parentAccount = await _uow.Accounts.FirstOrDefaultAsync(
            a => a.AccountCode == CashParentAccountCode, ct);

        if (parentAccount == null)
            throw new DomainException(
                "الحساب المحاسبي للخزنة (1110) غير موجود. يرجى التأكد من ترحيل دليل الحسابات.");

        // Generate next account code for Level 4 under 1110
        // Fetch existing children and find max code
        var children = await _uow.Accounts.ToListAsync(
            a => a.ParentAccountId == parentAccount.Id, q => q.OrderByDescending(a => a.AccountCode), ct);

        string nextCode;
        if (children.Count == 0)
        {
            nextCode = "1111";
        }
        else
        {
            var maxChildCode = children.First().AccountCode;
            if (int.TryParse(maxChildCode, out var maxCode))
                nextCode = (maxCode + 1).ToString();
            else
                nextCode = "1111";
        }

        return Account.Create(
            accountCode: nextCode,
            nameAr: boxName,
            nameEn: boxName,
            accountType: AccountType.Asset,
            level: 4,
            parentAccountId: parentAccount.Id,
            isSystemAccount: false,
            colorCode: "#2196F3",
            allowTransactions: true,
            description: $"خزنة: {boxName}",
            explanation: $"حساب الخزنة النقدية: {boxName}",
            openingBalance: 0,
            notes: null,
            createdByUserId: 0);
    }

    public async Task<Result<CashBoxDto>> UpdateAsync(int id, UpdateCashBoxRequest request, int userId, CancellationToken ct)
    {
        try
        {
            var box = await _uow.CashBoxes.GetByIdAsync(id, ct);
            if (box == null)
                return Result<CashBoxDto>.Failure("الصندوق غير موجود", ErrorCodes.NotFound);

            box.Update(
                boxName: request.BoxName,
                phoneNumber: request.PhoneNumber,
                taxNumber: request.TaxNumber,
                address: request.Address,
                notes: request.Notes,
                categoryId: request.CategoryId,
                branchId: request.BranchId,
                assignedUserId: request.AssignedUserId,
                currencyId: request.CurrencyId);

            box.SetUpdatedBy(userId);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Cash box updated: {BoxName} (ID: {Id}) by User {UserId}",
                box.BoxName, id, userId);

            return Result<CashBoxDto>.Success(MapToDto(box));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation updating cash box {Id}: {Message}", id, ex.Message);
            return Result<CashBoxDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating cash box {Id}", id);
            return Result<CashBoxDto>.Failure("حدث خطأ أثناء تحديث بيانات الصندوق");
        }
    }

    public async Task<Result> DeactivateAsync(int id, CancellationToken ct)
    {
        try
        {
            var box = await _uow.CashBoxes.GetByIdAsync(id, ct);
            if (box == null)
                return Result.Failure("الصندوق غير موجود", ErrorCodes.NotFound);

            box.MarkAsDeleted();
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Cash box deactivated: {BoxName} (ID: {Id})", box.BoxName, id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating cash box {Id}", id);
            return Result.Failure("حدث خطأ أثناء إلغاء تنشيط الصندوق");
        }
    }

    public async Task<Result<List<CashTransactionDto>>> GetTransactionsAsync(
        int cashBoxId, DateOnly? from, DateOnly? to, CancellationToken ct)
    {
        try
        {
            var boxExists = await _uow.CashBoxes.AnyAsync(b => b.Id == cashBoxId, ct);
            if (!boxExists)
                return Result<List<CashTransactionDto>>.Failure("الصندوق غير موجود", ErrorCodes.NotFound);

            var fromDate = from?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var toDate = to?.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

            var transactions = await _uow.CashTransactions.ToListAsync(
                t => t.CashBoxId == cashBoxId &&
                     (!fromDate.HasValue || t.CreatedAt >= fromDate.Value) &&
                     (!toDate.HasValue || t.CreatedAt <= toDate.Value),
                q => q.OrderByDescending(t => t.CreatedAt),
                ct);

            var dtos = transactions.Select(MapToTransactionDto).ToList();
            return Result<List<CashTransactionDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving transactions for cash box {CashBoxId}", cashBoxId);
            return Result<List<CashTransactionDto>>.Failure("حدث خطأ أثناء استرجاع المعاملات");
        }
    }

    public async Task<Result<CashTransactionDto>> RecordExpenseAsync(
        int cashBoxId, AddCashTransactionRequest request, int userId, CancellationToken ct)
    {
        try
        {
            var box = await _uow.CashBoxes.GetByIdAsync(cashBoxId, ct);
            if (box == null)
                return Result<CashTransactionDto>.Failure("الصندوق غير موجود", ErrorCodes.NotFound);

            // Compute running balance from all existing transactions
            var runningBalance = await ComputeRunningBalanceAsync(cashBoxId, ct);
            var amount = -Math.Abs(request.Amount); // expense is always negative
            var newRunningBalance = runningBalance + amount;

            var transaction = CashTransaction.Create(
                cashBoxId, CashTransactionType.Expense, amount, newRunningBalance,
                referenceType: null, referenceId: null, createdBy: userId,
                notes: request.Notes, currencyId: null);

            await _uow.CashTransactions.AddAsync(transaction, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Expense of {Amount:N2} recorded in cash box {CashBoxId} by User {UserId}",
                request.Amount, cashBoxId, userId);

            return Result<CashTransactionDto>.Success(MapToTransactionDto(transaction));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation recording expense in cash box {CashBoxId}: {Message}", cashBoxId, ex.Message);
            return Result<CashTransactionDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording expense in cash box {CashBoxId}", cashBoxId);
            return Result<CashTransactionDto>.Failure("حدث خطأ أثناء تسجيل المصروف");
        }
    }

    public async Task<Result> TransferAsync(CashTransferRequest request, int userId, CancellationToken ct)
    {
        if (request.SourceCashBoxId == request.DestinationCashBoxId)
            return Result.Failure("لا يمكن تحويل الأموال إلى نفس الصندوق");

        // Validate source and destination exist BEFORE entering the transaction (per RULE-027)
        var sourceBox = await _uow.CashBoxes.GetByIdAsync(request.SourceCashBoxId, ct);
        if (sourceBox == null)
        {
            _logger.LogWarning("Source cash box {SourceCashBoxId} not found for transfer", request.SourceCashBoxId);
            return Result.Failure("الصندوق المصدر غير موجود", ErrorCodes.NotFound);
        }

        var destBox = await _uow.CashBoxes.GetByIdAsync(request.DestinationCashBoxId, ct);
        if (destBox == null)
        {
            _logger.LogWarning("Destination cash box {DestCashBoxId} not found for transfer", request.DestinationCashBoxId);
            return Result.Failure("الصندوق الوجهة غير موجود", ErrorCodes.NotFound);
        }

        try
        {
            // Use ExecuteTransactionAsync instead of BeginTransactionAsync (per RULE-275)
            await _uow.ExecuteTransactionAsync(async () =>
            {
                var sourceRunningBalance = await ComputeRunningBalanceAsync(request.SourceCashBoxId, ct);
                var destRunningBalance = await ComputeRunningBalanceAsync(request.DestinationCashBoxId, ct);

                // Create withdrawal from source
                var sourceNewBalance = sourceRunningBalance - request.Amount;
                var withdrawalTx = CashTransaction.Create(
                    request.SourceCashBoxId, CashTransactionType.TransferOut,
                    -request.Amount, sourceNewBalance,
                    "CashTransfer", request.DestinationCashBoxId,
                    userId, request.Notes, null);
                await _uow.CashTransactions.AddAsync(withdrawalTx, ct);

                // Create deposit to destination
                var destNewBalance = destRunningBalance + request.Amount;
                var depositTx = CashTransaction.Create(
                    request.DestinationCashBoxId, CashTransactionType.TransferIn,
                    request.Amount, destNewBalance,
                    "CashTransfer", request.SourceCashBoxId,
                    userId, request.Notes, null);
                await _uow.CashTransactions.AddAsync(depositTx, ct);

                await _uow.SaveChangesAsync(ct);
            }, ct);

            _logger.LogInformation(
                "Cash transfer of {Amount:N2} from box {SourceBoxId} to box {DestBoxId} by User {UserId}",
                request.Amount, request.SourceCashBoxId, request.DestinationCashBoxId, userId);

            return Result.Success();
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation transferring cash from {SourceBoxId} to {DestBoxId}: {Message}",
                request.SourceCashBoxId, request.DestinationCashBoxId, ex.Message);
            return Result.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transferring cash from {SourceBoxId} to {DestBoxId}",
                request.SourceCashBoxId, request.DestinationCashBoxId);
            return Result.Failure("حدث خطأ أثناء تحويل الأموال");
        }
    }

    public async Task<Result<DailyClosureDto>> PerformDailyClosureAsync(int cashBoxId, int userId, CancellationToken ct)
    {
        try
        {
            var box = await _uow.CashBoxes.GetByIdAsync(cashBoxId, ct);
            if (box == null)
                return Result<DailyClosureDto>.Failure("الصندوق غير موجود", ErrorCodes.NotFound);

            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var hasExistingClosure = await _uow.DailyClosures.AnyAsync(
                dc => dc.CashBoxId == cashBoxId && dc.ClosureDate == today, ct);
            if (hasExistingClosure)
                return Result<DailyClosureDto>.Failure("تم إغلاق الصندوق بالفعل اليوم");

            var todayStart = today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

            var allTransactions = await _uow.CashTransactions.ToListAsync(
                t => t.CashBoxId == cashBoxId,
                q => q.OrderBy(t => t.CreatedAt),
                ct);

            if (allTransactions.Count == 0)
            {
                var emptyClosure = DailyClosure.Create(
                    cashBoxId, today, 0, 0, 0, 0, userId);
                await _uow.DailyClosures.AddAsync(emptyClosure, ct);
                await _uow.SaveChangesAsync(ct);

                _logger.LogInformation("Daily closure for empty cash box {CashBoxId} by User {UserId}", cashBoxId, userId);
                return Result<DailyClosureDto>.Success(MapToClosureDto(emptyClosure));
            }

            var beforeTodayTransactions = allTransactions.Where(t => t.CreatedAt < todayStart).ToList();
            var todayTransactions = allTransactions.Where(t => t.CreatedAt >= todayStart).ToList();

            // Opening balance = running balance of last transaction before today
            var openingBalance = beforeTodayTransactions.Count > 0
                ? beforeTodayTransactions.Last().RunningBalance
                : 0m;

            var totalIncome = todayTransactions.Where(t => t.Amount > 0).Sum(t => t.Amount);
            var totalExpense = todayTransactions.Where(t => t.Amount < 0).Sum(t => Math.Abs(t.Amount));
            var closingBalance = openingBalance + totalIncome - totalExpense;

            var dailyClosure = DailyClosure.Create(
                cashBoxId, today, openingBalance, totalIncome, totalExpense, closingBalance, userId);

            await _uow.DailyClosures.AddAsync(dailyClosure, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Daily closure for cash box {CashBoxId}: Opening={Opening:N2}, Income={Income:N2}, Expense={Expense:N2}, Closing={Closing:N2}",
                cashBoxId, openingBalance, totalIncome, totalExpense, closingBalance);

            return Result<DailyClosureDto>.Success(MapToClosureDto(dailyClosure));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation performing daily closure for cash box {CashBoxId}: {Message}", cashBoxId, ex.Message);
            return Result<DailyClosureDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing daily closure for cash box {CashBoxId}", cashBoxId);
            return Result<DailyClosureDto>.Failure("حدث خطأ أثناء تنفيذ الإغلاق اليومي");
        }
    }

    public async Task<Result<List<DailyClosureDto>>> GetDailyClosuresAsync(int cashBoxId, CancellationToken ct)
    {
        try
        {
            var boxExists = await _uow.CashBoxes.AnyAsync(b => b.Id == cashBoxId, ct);
            if (!boxExists)
                return Result<List<DailyClosureDto>>.Failure("الصندوق غير موجود", ErrorCodes.NotFound);

            var closures = await _uow.DailyClosures.ToListAsync(
                dc => dc.CashBoxId == cashBoxId,
                q => q.OrderByDescending(dc => dc.ClosureDate),
                ct);

            var dtos = closures.Select(MapToClosureDto).ToList();
            return Result<List<DailyClosureDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving daily closures for cash box {CashBoxId}", cashBoxId);
            return Result<List<DailyClosureDto>>.Failure("حدث خطأ أثناء استرجاع الإغلاقات اليومية");
        }
    }

    public async Task<Result<CashTransactionDto>> RecordInvoicePaymentAsync(
        int cashBoxId, decimal amount, CashTransactionType type,
        string referenceType, int referenceId, int userId, CancellationToken ct)
    {
        try
        {
            var box = await _uow.CashBoxes.GetByIdAsync(cashBoxId, ct);
            if (box == null)
            {
                _logger.LogWarning("Cash box {CashBoxId} not found for invoice payment", cashBoxId);
                return Result<CashTransactionDto>.Failure("الصندوق غير موجود", ErrorCodes.NotFound);
            }

            var runningBalance = await ComputeRunningBalanceAsync(cashBoxId, ct);
            var signedAmount = IsIncomeType(type) ? amount : -amount;
            var newRunningBalance = runningBalance + signedAmount;

            var transaction = CashTransaction.Create(
                cashBoxId, type, signedAmount, newRunningBalance,
                referenceType, referenceId, userId, null, null);

            await _uow.CashTransactions.AddAsync(transaction, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Invoice payment recorded: {Amount:N2} in cash box {CashBoxId} for {ReferenceType} #{ReferenceId} by User {UserId}",
                amount, cashBoxId, referenceType, referenceId, userId);

            return Result<CashTransactionDto>.Success(MapToTransactionDto(transaction));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation recording invoice payment in cash box {CashBoxId}: {Message}", cashBoxId, ex.Message);
            return Result<CashTransactionDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording invoice payment in cash box {CashBoxId}", cashBoxId);
            return Result<CashTransactionDto>.Failure("حدث خطأ أثناء تسجيل الدفعة");
        }
    }

    // ─── Helper Methods ─────────────────────────────

    private static bool IsIncomeType(CashTransactionType type) => type switch
    {
        CashTransactionType.OpeningBalance => true,
        CashTransactionType.SalesIncome => true,
        CashTransactionType.TransferIn => true,
        CashTransactionType.CustomerPayment => true,
        _ => false
    };

    /// <summary>
    /// Computes the running balance of a cash box by summing all transaction amounts.
    /// </summary>
    private async Task<decimal> ComputeRunningBalanceAsync(int cashBoxId, CancellationToken ct)
    {
        var allTransactions = await _uow.CashTransactions.ToListAsync(
            t => t.CashBoxId == cashBoxId, null, ct);
        return allTransactions.Sum(t => t.Amount);
    }

    private static CashBoxDto MapToDto(CashBox box) => new(
        box.Id,
        box.BoxName,
        box.AccountId,
        box.Account?.NameAr ?? box.Account?.NameEn,
        box.CategoryId,
        box.Category?.Name,
        box.BranchId,
        box.CurrencyId,
        box.Currency?.Name,
        box.Currency?.Code,
        box.AssignedUserId,
        box.PhoneNumber,
        box.TaxNumber,
        box.Address,
        box.Notes,
        box.IsActive);

    private static CashTransactionDto MapToTransactionDto(CashTransaction t) => new(
        t.Id,
        t.CashBoxId,
        (byte)t.TransactionType,
        t.Amount,
        t.RunningBalance,
        t.ReferenceType,
        t.ReferenceId,
        t.CurrencyId,
        t.Notes,
        t.CreatedByUserId ?? 0,
        t.CreatedAt);

    private static DailyClosureDto MapToClosureDto(DailyClosure c) => new(
        c.Id,
        c.CashBoxId,
        c.ClosureDate,
        c.OpeningBalance,
        c.TotalIncome,
        c.TotalExpense,
        c.ClosingBalance,
        c.ClosedByUserId,
        c.CreatedAt);
}
