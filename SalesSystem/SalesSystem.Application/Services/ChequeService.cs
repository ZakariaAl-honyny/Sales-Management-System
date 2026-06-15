using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Application.Services;

public class ChequeService : IChequeService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ChequeService> _logger;

    public ChequeService(
        IUnitOfWork uow,
        ILogger<ChequeService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<Result<ChequeDto>> CreateAsync(CreateChequeRequest request, int userId, CancellationToken ct)
    {
        try
        {
            var cheque = Cheque.Create(
                chequeNumber: request.ChequeNumber,
                bankName: request.BankName,
                bankBranch: request.BankBranch,
                issueDate: request.IssueDate,
                maturityDate: request.MaturityDate,
                amount: request.Amount,
                notes: request.Notes,
                paymentId: request.PaymentId,
                customerReceiptId: request.CustomerReceiptId,
                receiptVoucherId: request.ReceiptVoucherId,
                paymentVoucherId: request.PaymentVoucherId,
                createdByUserId: userId);

            await _uow.Cheques.AddAsync(cheque, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Cheque created (No: {ChequeNumber}, ID: {Id}) by User {UserId}",
                cheque.ChequeNumber, cheque.Id, userId);

            return Result<ChequeDto>.Success(MapToDto(cheque));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation creating cheque: {Message}", ex.Message);
            return Result<ChequeDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating cheque");
            return Result<ChequeDto>.Failure("حدث خطأ أثناء إنشاء الشيك");
        }
    }

    public async Task<Result<ChequeDto>> UpdateAsync(int id, UpdateChequeRequest request, int userId, CancellationToken ct)
    {
        try
        {
            var cheque = await _uow.Cheques.FirstOrDefaultAsync(c => c.Id == id, ct);
            if (cheque == null)
                return Result<ChequeDto>.Failure("الشيك غير موجود", ErrorCodes.NotFound);

            cheque.Update(
                chequeNumber: request.ChequeNumber,
                bankName: request.BankName,
                bankBranch: request.BankBranch,
                issueDate: request.IssueDate,
                maturityDate: request.MaturityDate,
                amount: request.Amount,
                notes: request.Notes,
                paymentId: request.PaymentId,
                customerReceiptId: request.CustomerReceiptId,
                receiptVoucherId: request.ReceiptVoucherId,
                paymentVoucherId: request.PaymentVoucherId,
                updatedByUserId: userId);

            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Cheque {Id} updated by User {UserId}", id, userId);
            return Result<ChequeDto>.Success(MapToDto(cheque));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation updating cheque {Id}: {Message}", id, ex.Message);
            return Result<ChequeDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating cheque {Id}", id);
            return Result<ChequeDto>.Failure("حدث خطأ أثناء تحديث الشيك");
        }
    }

    public async Task<Result> DeactivateAsync(int id, int userId, CancellationToken ct)
    {
        try
        {
            var cheque = await _uow.Cheques.FirstOrDefaultAsync(c => c.Id == id, ct);
            if (cheque == null)
                return Result.Failure("الشيك غير موجود", ErrorCodes.NotFound);

            if (cheque.Status == ChequeStatus.Cleared)
                return Result.Failure("لا يمكن إلغاء تنشيط شيك تم صرفه.", ErrorCodes.InvalidOperation);

            cheque.MarkAsDeleted();
            cheque.SetUpdatedBy(userId);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Cheque {Id} deactivated by User {UserId}", id, userId);
            return Result.Success();
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation deactivating cheque {Id}: {Message}", id, ex.Message);
            return Result.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating cheque {Id}", id);
            return Result.Failure("حدث خطأ أثناء إلغاء تنشيط الشيك");
        }
    }

    public async Task<Result<ChequeDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        try
        {
            var cheque = await _uow.Cheques.FirstOrDefaultAsync(c => c.Id == id, ct);
            if (cheque == null)
                return Result<ChequeDto>.Failure("الشيك غير موجود", ErrorCodes.NotFound);

            return Result<ChequeDto>.Success(MapToDto(cheque));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cheque {Id}", id);
            return Result<ChequeDto>.Failure("حدث خطأ أثناء استرجاع بيانات الشيك");
        }
    }

    public async Task<Result<List<ChequeDto>>> GetAllAsync(CancellationToken ct)
    {
        try
        {
            var cheques = await _uow.Cheques.ToListAsync(
                predicate: null,
                queryConfig: q => q.OrderByDescending(c => c.Id),
                ct: ct);

            var dtos = cheques.Select(MapToDto).ToList();
            return Result<List<ChequeDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cheques");
            return Result<List<ChequeDto>>.Failure("حدث خطأ أثناء استرجاع قائمة الشيكات");
        }
    }

    public async Task<Result> MarkAsDepositedAsync(int id, int userId, CancellationToken ct)
    {
        try
        {
            var cheque = await _uow.Cheques.FirstOrDefaultAsync(c => c.Id == id, ct);
            if (cheque == null)
                return Result.Failure("الشيك غير موجود", ErrorCodes.NotFound);

            cheque.MarkAsDeposited();
            cheque.SetUpdatedBy(userId);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Cheque {Id} marked as deposited by User {UserId}", id, userId);
            return Result.Success();
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation marking cheque {Id} as deposited: {Message}", id, ex.Message);
            return Result.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking cheque {Id} as deposited", id);
            return Result.Failure("حدث خطأ أثناء إيداع الشيك");
        }
    }

    public async Task<Result> MarkAsClearedAsync(int id, int userId, CancellationToken ct)
    {
        try
        {
            var cheque = await _uow.Cheques.FirstOrDefaultAsync(c => c.Id == id, ct);
            if (cheque == null)
                return Result.Failure("الشيك غير موجود", ErrorCodes.NotFound);

            cheque.MarkAsCleared();
            cheque.SetUpdatedBy(userId);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Cheque {Id} marked as cleared by User {UserId}", id, userId);
            return Result.Success();
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation marking cheque {Id} as cleared: {Message}", id, ex.Message);
            return Result.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking cheque {Id} as cleared", id);
            return Result.Failure("حدث خطأ أثناء قبض الشيك");
        }
    }

    public async Task<Result> MarkAsBouncedAsync(int id, int userId, CancellationToken ct)
    {
        try
        {
            var cheque = await _uow.Cheques.FirstOrDefaultAsync(c => c.Id == id, ct);
            if (cheque == null)
                return Result.Failure("الشيك غير موجود", ErrorCodes.NotFound);

            cheque.MarkAsBounced();
            cheque.SetUpdatedBy(userId);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Cheque {Id} marked as bounced by User {UserId}", id, userId);
            return Result.Success();
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation marking cheque {Id} as bounced: {Message}", id, ex.Message);
            return Result.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking cheque {Id} as bounced", id);
            return Result.Failure("حدث خطأ أثناء إرجاع الشيك");
        }
    }

    public async Task<Result> MarkAsCancelledAsync(int id, int userId, CancellationToken ct)
    {
        try
        {
            var cheque = await _uow.Cheques.FirstOrDefaultAsync(c => c.Id == id, ct);
            if (cheque == null)
                return Result.Failure("الشيك غير موجود", ErrorCodes.NotFound);

            cheque.MarkAsCancelled();
            cheque.SetUpdatedBy(userId);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Cheque {Id} cancelled by User {UserId}", id, userId);
            return Result.Success();
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation cancelling cheque {Id}: {Message}", id, ex.Message);
            return Result.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling cheque {Id}", id);
            return Result.Failure("حدث خطأ أثناء إلغاء الشيك");
        }
    }

    // ─── Private Helpers ─────────────────────────────────

    private static ChequeDto MapToDto(Cheque cheque)
    {
        return new ChequeDto
        {
            Id = cheque.Id,
            ChequeNumber = cheque.ChequeNumber,
            BankName = cheque.BankName,
            BankBranch = cheque.BankBranch,
            PaymentId = cheque.PaymentId,
            CustomerReceiptId = cheque.CustomerReceiptId,
            ReceiptVoucherId = cheque.ReceiptVoucherId,
            PaymentVoucherId = cheque.PaymentVoucherId,
            IssueDate = cheque.IssueDate,
            MaturityDate = cheque.MaturityDate,
            Amount = cheque.Amount,
            Status = cheque.Status,
            StatusDisplay = GetStatusDisplay(cheque.Status),
            Notes = cheque.Notes,
            IsActive = cheque.IsActive
        };
    }

    private static string GetStatusDisplay(ChequeStatus status)
    {
        return status switch
        {
            ChequeStatus.UnderCollection => "تحت التحصيل",
            ChequeStatus.Deposited => "تم الإيداع",
            ChequeStatus.Cleared => "مقبوض",
            ChequeStatus.Bounced => "مرتجع",
            ChequeStatus.Cancelled => "ملغي",
            _ => "غير معروف"
        };
    }
}
