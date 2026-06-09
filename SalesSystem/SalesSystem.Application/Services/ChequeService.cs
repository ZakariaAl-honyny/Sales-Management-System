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

/// <summary>
/// Full implementation of IChequeService.
/// Handles CRUD, status transitions, and financial integrations.
/// All methods return Result[T] / Result — NEVER throw to controllers (RULE-006).
/// </summary>
public class ChequeService : IChequeService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ChequeService> _logger;
    private readonly IAccountingIntegrationService _accountingService;

    public ChequeService(
        IUnitOfWork uow,
        ILogger<ChequeService> logger,
        IAccountingIntegrationService accountingService)
    {
        _uow = uow;
        _logger = logger;
        _accountingService = accountingService;
    }

    public async Task<Result<List<ChequeDto>>> GetAllAsync(int? paymentId = null, byte? status = null, CancellationToken ct = default)
    {
        try
        {
            var cheques = await _uow.Cheques.ToListAsync(
                c =>
                    (!paymentId.HasValue ||
                     c.CustomerPaymentId == paymentId ||
                     c.SupplierPaymentId == paymentId) &&
                    (!status.HasValue || (byte)c.Status == status.Value),
                q => q.OrderByDescending(c => c.CreatedAt),
                ct);

            var dtos = cheques.Select(MapToDto).ToList();
            return Result<List<ChequeDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cheques");
            return Result<List<ChequeDto>>.Failure("حدث خطأ أثناء استرجاع الشيكات");
        }
    }

    public async Task<Result<ChequeDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        try
        {
            var cheque = await _uow.Cheques.GetByIdAsync(id, ct);
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

    public async Task<Result<ChequeDto>> CreateAsync(CreateChequeRequest request, int userId, CancellationToken ct = default)
    {
        try
        {
            // Validate that maturity date is on or after issue date
            if (request.MaturityDate < request.IssueDate)
                return Result<ChequeDto>.Failure("تاريخ الاستحقاق يجب أن يكون بعد تاريخ الإصدار");

            if (request.Amount <= 0)
                return Result<ChequeDto>.Failure("قيمة الشيك يجب أن تكون أكبر من الصفر");

            // Validate that exactly one payment linkage is provided
            if (!request.CustomerPaymentId.HasValue && !request.SupplierPaymentId.HasValue)
                return Result<ChequeDto>.Failure("يجب ربط الشيك بسداد عميل أو سداد مورد");

            if (request.CustomerPaymentId.HasValue && request.SupplierPaymentId.HasValue)
                return Result<ChequeDto>.Failure("لا يمكن ربط الشيك بسداد عميل وسداد مورد معاً");

            // If linked to a customer payment, verify it exists
            if (request.CustomerPaymentId.HasValue)
            {
                var cpExists = await _uow.CustomerPayments.AnyAsync(p => p.Id == request.CustomerPaymentId.Value, ct);
                if (!cpExists)
                    return Result<ChequeDto>.Failure("سداد العميل غير موجود", ErrorCodes.NotFound);
            }

            // If linked to a supplier payment, verify it exists
            if (request.SupplierPaymentId.HasValue)
            {
                var spExists = await _uow.SupplierPayments.AnyAsync(p => p.Id == request.SupplierPaymentId.Value, ct);
                if (!spExists)
                    return Result<ChequeDto>.Failure("سداد المورد غير موجود", ErrorCodes.NotFound);
            }

            var cheque = Cheque.Create(
                request.ChequeNumber,
                request.BankName,
                request.IssueDate,
                request.MaturityDate,
                request.Amount,
                request.CustomerPaymentId,
                request.SupplierPaymentId,
                request.Notes,
                userId);

            await _uow.Cheques.AddAsync(cheque, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Cheque created: {ChequeNumber}, Amount {Amount}, Bank {BankName} by User {UserId}",
                cheque.ChequeNumber, cheque.Amount, cheque.BankName, userId);

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

    public async Task<Result<ChequeDto>> UpdateStatusAsync(int id, UpdateChequeStatusRequest request, int userId, CancellationToken ct = default)
    {
        try
        {
            var cheque = await _uow.Cheques.GetByIdAsync(id, ct);
            if (cheque == null)
                return Result<ChequeDto>.Failure("الشيك غير موجود", ErrorCodes.NotFound);

            return await _uow.ExecuteTransactionAsync<Result<ChequeDto>>(async () =>
            {
                var previousStatus = cheque.Status;

                // Apply the domain status transition
                switch (request.NewStatus)
                {
                    case ChequeStatus.Cleared:
                        cheque.Clear();
                        break;
                    case ChequeStatus.Bounced:
                        cheque.Bounce();
                        break;
                    case ChequeStatus.Cancelled:
                        cheque.Cancel();
                        break;
                    default:
                        return Result<ChequeDto>.Failure("حالة الشيك غير صالحة");
                }

                // Financial impact based on transition
                // Pending → Cleared: record as cleared (Dr Bank/Cash, Cr AR/AP if applicable)
                if (previousStatus == ChequeStatus.Pending && request.NewStatus == ChequeStatus.Cleared)
                {
                    if (cheque.CustomerPaymentId.HasValue)
                    {
                        var payment = await _uow.CustomerPayments.GetByIdAsync(cheque.CustomerPaymentId.Value, ct);
                        if (payment != null)
                        {
                            var customer = await _uow.Customers.GetByIdAsync(payment.CustomerId, ct);
                            if (customer != null)
                            {
                                var entryResult = await _accountingService.CreateCustomerPaymentEntryAsync(
                                    payment, customer.Name, userId, ct);
                                if (!entryResult.IsSuccess)
                                    _logger.LogWarning("Journal entry not created for cheque clearance {Id}: {Error}",
                                        id, entryResult.Error);
                            }
                        }
                    }
                    else if (cheque.SupplierPaymentId.HasValue)
                    {
                        var payment = await _uow.SupplierPayments.GetByIdAsync(cheque.SupplierPaymentId.Value, ct);
                        if (payment != null)
                        {
                            var supplier = await _uow.Suppliers.GetByIdAsync(payment.SupplierId, ct);
                            if (supplier != null)
                            {
                                var entryResult = await _accountingService.CreateSupplierPaymentEntryAsync(
                                    payment, supplier.Name, userId, ct);
                                if (!entryResult.IsSuccess)
                                    _logger.LogWarning("Journal entry not created for cheque clearance {Id}: {Error}",
                                        id, entryResult.Error);
                            }
                        }
                    }
                }

                // Pending → Bounced: reverse original payment journal entry
                if (previousStatus == ChequeStatus.Pending && request.NewStatus == ChequeStatus.Bounced)
                {
                    await ReversePaymentJournalEntryAsync(cheque, userId, ct);
                }

                // Cleared → Bounced: reverse both payment and clearing entries
                if (previousStatus == ChequeStatus.Cleared && request.NewStatus == ChequeStatus.Bounced)
                {
                    await ReversePaymentJournalEntryAsync(cheque, userId, ct);
                }

                // Pending → Cancelled: no financial impact

                if (!string.IsNullOrWhiteSpace(request.Notes))
                {
                    // Notes are informational; the cheque entity doesn't store them via method
                }

                await _uow.SaveChangesAsync(ct);

                _logger.LogInformation(
                    "Cheque status updated: {ChequeNumber}, {PreviousStatus} → {NewStatus} by User {UserId}",
                    cheque.ChequeNumber, previousStatus, request.NewStatus, userId);

                return Result<ChequeDto>.Success(MapToDto(cheque));
            }, ct);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation updating cheque status {Id}: {Message}", id, ex.Message);
            return Result<ChequeDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating cheque status {Id}", id);
            return Result<ChequeDto>.Failure("حدث خطأ أثناء تحديث حالة الشيك");
        }
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct = default)
    {
        try
        {
            var cheque = await _uow.Cheques.GetByIdAsync(id, ct);
            if (cheque == null)
                return Result.Failure("الشيك غير موجود", ErrorCodes.NotFound);

            await _uow.Cheques.SoftDeleteAsync(cheque.Id, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Cheque deleted (soft): {ChequeNumber}", cheque.ChequeNumber);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting cheque {Id}", id);
            return Result.Failure("حدث خطأ أثناء حذف الشيك");
        }
    }

    // ─── Private Helpers ──────────────────────────────────────

    /// <summary>
    /// Reverses the journal entry associated with the payment linked to this cheque.
    /// </summary>
    private async Task ReversePaymentJournalEntryAsync(Cheque cheque, int userId, CancellationToken ct)
    {
        if (cheque.CustomerPaymentId.HasValue)
        {
            var payment = await _uow.CustomerPayments.GetByIdAsync(cheque.CustomerPaymentId.Value, ct);
            if (payment != null)
            {
                var customer = await _uow.Customers.GetByIdAsync(payment.CustomerId, ct);
                if (customer != null)
                {
                    var reverseResult = await _accountingService.ReverseCustomerPaymentEntryAsync(
                        payment.Id, payment.Amount, customer.Name, userId, ct);
                    if (!reverseResult.IsSuccess)
                        _logger.LogWarning("Failed to reverse journal entry for customer payment {PaymentId}: {Error}",
                            payment.Id, reverseResult.Error);
                }
            }
        }
        else if (cheque.SupplierPaymentId.HasValue)
        {
            var payment = await _uow.SupplierPayments.GetByIdAsync(cheque.SupplierPaymentId.Value, ct);
            if (payment != null)
            {
                var supplier = await _uow.Suppliers.GetByIdAsync(payment.SupplierId, ct);
                if (supplier != null)
                {
                    var reverseResult = await _accountingService.ReverseSupplierPaymentEntryAsync(
                        payment.Id, payment.Amount, supplier.Name, userId, ct);
                    if (!reverseResult.IsSuccess)
                        _logger.LogWarning("Failed to reverse journal entry for supplier payment {PaymentId}: {Error}",
                            payment.Id, reverseResult.Error);
                }
            }
        }
    }

    private static ChequeDto MapToDto(Cheque c) => new()
    {
        Id = c.Id,
        ChequeNumber = c.ChequeNumber,
        BankName = c.BankName,
        IssueDate = c.IssueDate,
        MaturityDate = c.MaturityDate,
        Status = (byte)c.Status,
        Amount = c.Amount,
        CustomerPaymentId = c.CustomerPaymentId,
        SupplierPaymentId = c.SupplierPaymentId,
        Notes = c.Notes,
        CreatedAt = c.CreatedAt
    };
}
