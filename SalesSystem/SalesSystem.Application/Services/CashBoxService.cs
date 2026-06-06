using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Application.Services;

public class CashBoxService : ICashBoxService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<CashBoxService> _logger;

    public CashBoxService(IUnitOfWork uow, ILogger<CashBoxService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<Result<List<CashBoxDto>>> GetAllAsync(CancellationToken ct)
    {
        try
        {
            var boxes = await _uow.CashBoxes.ToListAsync(ct);
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
            var box = CashBox.Create(
                request.BoxName,
                request.BranchId,
                request.AssignedUserId,
                null,
                0);

            box.SetCreatedBy(userId);

            if (request.OpeningBalance > 0)
            {
                box.Deposit(request.OpeningBalance, CashTransactionType.OpeningBalance, createdBy: userId);
            }

            await _uow.CashBoxes.AddAsync(box, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Cash box created: {BoxName} (ID: {Id}) with opening balance {Balance:N2} by User {UserId}",
                box.BoxName, box.Id, request.OpeningBalance, userId);

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

            var transaction = box.Withdraw(
                request.Amount, CashTransactionType.Expense, createdBy: userId, notes: request.Notes);
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

        await using var dbTransaction = await _uow.BeginTransactionAsync(ct);
        try
        {
            var sourceBox = await _uow.CashBoxes.GetByIdAsync(request.SourceCashBoxId, ct);
            if (sourceBox == null)
            {
                await dbTransaction.RollbackAsync(ct);
                _logger.LogWarning("Source cash box {SourceCashBoxId} not found for transfer", request.SourceCashBoxId);
                return Result.Failure("الصندوق المصدر غير موجود", ErrorCodes.NotFound);
            }

            var destBox = await _uow.CashBoxes.GetByIdAsync(request.DestinationCashBoxId, ct);
            if (destBox == null)
            {
                await dbTransaction.RollbackAsync(ct);
                _logger.LogWarning("Destination cash box {DestCashBoxId} not found for transfer", request.DestinationCashBoxId);
                return Result.Failure("الصندوق الوجهة غير موجود", ErrorCodes.NotFound);
            }

            sourceBox.Withdraw(
                request.Amount, CashTransactionType.TransferOut, "CashTransfer", destBox.Id, userId, request.Notes);
            destBox.Deposit(
                request.Amount, CashTransactionType.TransferIn, "CashTransfer", sourceBox.Id, userId, request.Notes);

            await _uow.SaveChangesAsync(ct);
            await dbTransaction.CommitAsync(ct);

            _logger.LogInformation(
                "Cash transfer of {Amount:N2} from box {SourceBoxId} to box {DestBoxId} by User {UserId}",
                request.Amount, request.SourceCashBoxId, request.DestinationCashBoxId, userId);

            return Result.Success();
        }
        catch (DomainException ex)
        {
            await dbTransaction.RollbackAsync(ct);
            _logger.LogWarning(ex, "Domain rule violation transferring cash from {SourceBoxId} to {DestBoxId}: {Message}",
                request.SourceCashBoxId, request.DestinationCashBoxId, ex.Message);
            return Result.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync(ct);
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

            var openingBalance = beforeTodayTransactions.Count > 0
                ? beforeTodayTransactions.Last().BalanceAfter
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

            CashTransaction transaction;
            if (IsIncomeType(type))
            {
                transaction = box.Deposit(amount, type, referenceType, referenceId, userId);
            }
            else
            {
                transaction = box.Withdraw(amount, type, referenceType, referenceId, userId);
            }

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

    private static CashBoxDto MapToDto(CashBox box) => new(
        box.Id,
        box.BoxName,
        box.OpeningBalance,
        box.CurrentBalance,
        box.BranchId,
        box.CurrencyId,
        box.Currency?.Name,
        box.Currency?.Code,
        box.AssignedUserId,
        box.Notes,
        box.IsActive);

    private static CashTransactionDto MapToTransactionDto(CashTransaction t) => new(
        t.Id,
        t.CashBoxId,
        (byte)t.TransactionType,
        t.Amount,
        t.BalanceBefore,
        t.BalanceAfter,
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
