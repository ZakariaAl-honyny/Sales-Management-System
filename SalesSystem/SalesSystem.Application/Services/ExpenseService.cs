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

public class ExpenseService : IExpenseService
{
    private readonly IUnitOfWork _uow;
    private readonly IAccountingIntegrationService _accountingService;
    private readonly IDocumentSequenceService _sequenceService;
    private readonly ILogger<ExpenseService> _logger;

    public ExpenseService(
        IUnitOfWork uow,
        IAccountingIntegrationService accountingService,
        IDocumentSequenceService sequenceService,
        ILogger<ExpenseService> logger)
    {
        _uow = uow;
        _accountingService = accountingService;
        _sequenceService = sequenceService;
        _logger = logger;
    }

    public async Task<Result<ExpenseDto>> CreateAsync(CreateExpenseRequest request, int userId, CancellationToken ct)
    {
        return await _uow.ExecuteTransactionAsync<Result<ExpenseDto>>(async () =>
        {
            try
            {
                var seqResult = await _sequenceService.GetNextIntAsync("Expense", ct);
                if (!seqResult.IsSuccess)
                    return Result<ExpenseDto>.Failure("فشل في توليد رقم المصروف");
                var expenseNo = seqResult.Value;

                var expense = Expense.Create(
                    expenseNo,
                    request.ExpenseDate,
                    request.ExpenseAccountId,
                    request.CashBoxId,
                    (short)request.CurrencyId,
                    request.Amount,
                    request.Notes,
                    userId);

                await _uow.Expenses.AddAsync(expense, ct);
                await _uow.SaveChangesAsync(ct);

                _logger.LogInformation("Expense created (No: {ExpenseNo}, ID: {Id}) by User {UserId}",
                    expense.ExpenseNo, expense.Id, userId);

                var created = await _uow.Expenses.FirstOrDefaultAsync(
                    e => e.Id == expense.Id, ct, "ExpenseAccount", "CashBox", "Currency");
                return Result<ExpenseDto>.Success(MapToDto(created!));
            }
            catch (DomainException ex)
            {
                _logger.LogWarning(ex, "Domain rule violation creating expense: {Message}", ex.Message);
                return Result<ExpenseDto>.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating expense");
                return Result<ExpenseDto>.Failure("حدث خطأ أثناء إنشاء المصروف");
            }
        }, ct);
    }

    public async Task<Result<ExpenseDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        try
        {
            var expense = await _uow.Expenses.FirstOrDefaultAsync(
                e => e.Id == id, ct, "ExpenseAccount", "CashBox", "Currency");
            if (expense == null)
                return Result<ExpenseDto>.Failure("المصروف غير موجود", ErrorCodes.NotFound);

            return Result<ExpenseDto>.Success(MapToDto(expense));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving expense {Id}", id);
            return Result<ExpenseDto>.Failure("حدث خطأ أثناء استرجاع بيانات المصروف");
        }
    }

    public async Task<Result<PagedResult<ExpenseDto>>> GetAllAsync(
        string? search, DateTime? from, DateTime? to, int page, int pageSize, CancellationToken ct)
    {
        try
        {
            System.Linq.Expressions.Expression<Func<Expense, bool>>? predicate = null;

            if (from.HasValue || to.HasValue || !string.IsNullOrWhiteSpace(search))
            {
                predicate = e =>
                    (!from.HasValue || e.ExpenseDate >= from.Value) &&
                    (!to.HasValue || e.ExpenseDate <= to.Value) &&
                    (string.IsNullOrWhiteSpace(search) ||
                     e.ExpenseNo.ToString().Contains(search) ||
                     (e.Notes != null && e.Notes.Contains(search)));
            }

            var (items, totalCount) = await _uow.Expenses.GetPagedAsync(
                predicate,
                orderConfig: q => q.OrderByDescending(e => e.ExpenseDate).ThenByDescending(e => e.Id),
                page,
                pageSize,
                ct,
                includePaths: new[] { "ExpenseAccount", "CashBox", "Currency" });

            var dtos = items.Select(MapToDto).ToList();
            var result = PagedResult<ExpenseDto>.Create(dtos, totalCount, page, pageSize);

            return Result<PagedResult<ExpenseDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving expenses list");
            return Result<PagedResult<ExpenseDto>>.Failure("حدث خطأ أثناء استرجاع قائمة المصروفات");
        }
    }

    public async Task<Result<ExpenseDto>> UpdateAsync(int id, UpdateExpenseRequest request, int userId, CancellationToken ct)
    {
        try
        {
            var expense = await _uow.Expenses.FirstOrDefaultAsync(
                e => e.Id == id, ct, "ExpenseAccount", "CashBox", "Currency");
            if (expense == null)
                return Result<ExpenseDto>.Failure("المصروف غير موجود", ErrorCodes.NotFound);

            expense.Update(
                request.ExpenseDate,
                request.ExpenseAccountId,
                request.CashBoxId,
                (short)request.CurrencyId,
                request.Amount,
                request.Notes);

            expense.SetUpdatedBy(userId);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Expense {Id} updated by User {UserId}", id, userId);

            var updated = await _uow.Expenses.FirstOrDefaultAsync(
                e => e.Id == id, ct, "ExpenseAccount", "CashBox", "Currency");
            return Result<ExpenseDto>.Success(MapToDto(updated!));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation updating expense {Id}: {Message}", id, ex.Message);
            return Result<ExpenseDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating expense {Id}", id);
            return Result<ExpenseDto>.Failure("حدث خطأ أثناء تحديث المصروف");
        }
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct)
    {
        try
        {
            var expense = await _uow.Expenses.GetByIdAsync(id, ct);
            if (expense == null)
                return Result.Failure("المصروف غير موجود", ErrorCodes.NotFound);

            if (expense.Status == InvoiceStatus.Posted)
                return Result.Failure("لا يمكن حذف مصروف مرحّل. قم بإلغائه أولاً.");

            expense.Cancel();
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Expense {Id} cancelled (deleted)", id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting expense {Id}", id);
            return Result.Failure("حدث خطأ أثناء حذف المصروف");
        }
    }

    public async Task<Result<ExpenseDto>> PostAsync(int id, int userId, CancellationToken ct)
    {
        return await _uow.ExecuteTransactionAsync<Result<ExpenseDto>>(async () =>
        {
            try
            {
                var expense = await _uow.Expenses.FirstOrDefaultAsync(
                    e => e.Id == id, ct, "ExpenseAccount", "CashBox", "Currency");
                if (expense == null)
                    return Result<ExpenseDto>.Failure("المصروف غير موجود", ErrorCodes.NotFound);

                // Post the expense entity
                expense.Post();
                expense.SetUpdatedBy(userId);
                await _uow.SaveChangesAsync(ct);

                // Create journal entry: Dr ExpenseAccount / Cr CashBox.Account
                var jeResult = await _accountingService.CreateExpenseEntryAsync(expense, userId, ct);
                if (!jeResult.IsSuccess)
                    return Result<ExpenseDto>.Failure(jeResult.Error!);

                _logger.LogInformation("Expense {Id} posted by User {UserId}", id, userId);

                var posted = await _uow.Expenses.FirstOrDefaultAsync(
                    e => e.Id == id, ct, "ExpenseAccount", "CashBox", "Currency");
                return Result<ExpenseDto>.Success(MapToDto(posted!));
            }
            catch (DomainException ex)
            {
                _logger.LogWarning(ex, "Domain rule violation posting expense {Id}: {Message}", id, ex.Message);
                return Result<ExpenseDto>.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error posting expense {Id}", id);
                return Result<ExpenseDto>.Failure("حدث خطأ أثناء ترحيل المصروف");
            }
        }, ct);
    }

    public async Task<Result<ExpenseDto>> CancelAsync(int id, CancellationToken ct)
    {
        return await _uow.ExecuteTransactionAsync<Result<ExpenseDto>>(async () =>
        {
            try
            {
                var expense = await _uow.Expenses.FirstOrDefaultAsync(
                    e => e.Id == id, ct, "ExpenseAccount", "CashBox", "Currency");
                if (expense == null)
                    return Result<ExpenseDto>.Failure("المصروف غير موجود", ErrorCodes.NotFound);

                bool wasPosted = expense.Status == InvoiceStatus.Posted;

                // Cancel the expense entity
                expense.Cancel();
                await _uow.SaveChangesAsync(ct);

                // If was posted, create reversal journal entry: Dr CashBox.Account / Cr ExpenseAccount
                if (wasPosted)
                {
                    var jeResult = await _accountingService.ReverseExpenseEntryAsync(expense, 0, ct);
                    if (!jeResult.IsSuccess)
                        return Result<ExpenseDto>.Failure(jeResult.Error!);
                }

                _logger.LogInformation("Expense {Id} cancelled", id);

                var cancelled = await _uow.Expenses.FirstOrDefaultAsync(
                    e => e.Id == id, ct, "ExpenseAccount", "CashBox", "Currency");
                return Result<ExpenseDto>.Success(MapToDto(cancelled!));
            }
            catch (DomainException ex)
            {
                _logger.LogWarning(ex, "Domain rule violation cancelling expense {Id}: {Message}", id, ex.Message);
                return Result<ExpenseDto>.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling expense {Id}", id);
                return Result<ExpenseDto>.Failure("حدث خطأ أثناء إلغاء المصروف");
            }
        }, ct);
    }

    // ─── Private Helpers ─────────────────────────────────

    private static ExpenseDto MapToDto(Expense expense)
    {
        return new ExpenseDto(
            expense.Id,
            expense.ExpenseNo,
            expense.ExpenseDate,
            expense.ExpenseAccountId,
            expense.ExpenseAccount?.NameAr,
            expense.CashBoxId,
            expense.CashBox?.Name,
            expense.CurrencyId,
            expense.Currency?.Name,
            expense.Amount,
            expense.Notes,
            (byte)expense.Status,
            GetStatusName(expense.Status),
            expense.Status != InvoiceStatus.Cancelled
        );
    }

    private static string? GetStatusName(InvoiceStatus status) => status switch
    {
        InvoiceStatus.Draft => "مسودة",
        InvoiceStatus.Posted => "مرحّل",
        InvoiceStatus.Cancelled => "ملغي",
        _ => null
    };
}
