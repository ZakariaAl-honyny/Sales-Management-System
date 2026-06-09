using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Application.Services;

/// <summary>
/// Full implementation of IPaymentAllocationService.
/// Manages how payments are distributed across multiple invoices atomically.
/// All methods return Result/Result[T] — NEVER throw (RULE-006).
/// </summary>
public class PaymentAllocationService : IPaymentAllocationService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<PaymentAllocationService> _logger;

    public PaymentAllocationService(IUnitOfWork uow, ILogger<PaymentAllocationService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<Result<List<PaymentAllocationDto>>> GetAllocationsForPaymentAsync(
        int paymentId, byte paymentType, CancellationToken ct = default)
    {
        try
        {
            List<PaymentAllocation> allocations;

            if (paymentType == 1) // CustomerPayment
            {
                allocations = await _uow.PaymentAllocations.ToListAsync(
                    a => a.CustomerPaymentId == paymentId,
                    q => q.OrderBy(a => a.Id),
                    ct);
            }
            else if (paymentType == 2) // SupplierPayment
            {
                allocations = await _uow.PaymentAllocations.ToListAsync(
                    a => a.SupplierPaymentId == paymentId,
                    q => q.OrderBy(a => a.Id),
                    ct);
            }
            else
            {
                return Result<List<PaymentAllocationDto>>.Failure("نوع الدفع غير صالح — 1 لسداد العميل، 2 لسداد المورد");
            }

            var dtos = allocations.Select(MapToDto).ToList();
            return Result<List<PaymentAllocationDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving allocations for payment {PaymentId} (type {PaymentType})",
                paymentId, paymentType);
            return Result<List<PaymentAllocationDto>>.Failure("حدث خطأ أثناء استرجاع التوزيعات");
        }
    }

    public async Task<Result> UpdateAllocationsAsync(
        int paymentId, byte paymentType, UpdateAllocationsRequest request, CancellationToken ct = default)
    {
        try
        {
            // Validate request
            if (request.Allocations == null || request.Allocations.Count == 0)
                return Result.Failure("يجب تقديم توزيع واحد على الأقل");

            // Verify the payment exists and get its amount
            decimal paymentAmount = 0;

            if (paymentType == 1)
            {
                var payment = await _uow.CustomerPayments.GetByIdAsync(paymentId, ct);
                if (payment == null)
                    return Result.Failure("سداد العميل غير موجود", ErrorCodes.NotFound);
                paymentAmount = payment.Amount;
            }
            else if (paymentType == 2)
            {
                var payment = await _uow.SupplierPayments.GetByIdAsync(paymentId, ct);
                if (payment == null)
                    return Result.Failure("سداد المورد غير موجود", ErrorCodes.NotFound);
                paymentAmount = payment.Amount;
            }
            else
            {
                return Result.Failure("نوع الدفع غير صالح — 1 لسداد العميل، 2 لسداد المورد");
            }

            // Validate total allocated amount does not exceed payment amount
            var totalAllocated = request.Allocations.Sum(a => a.AllocatedAmount);
            if (totalAllocated > paymentAmount)
                return Result.Failure($"إجمالي المبلغ المخصص ({totalAllocated:N2}) يتجاوز مبلغ الدفع ({paymentAmount:N2})");

            // Use ExecuteTransactionAsync for atomic remove + add (RULE-275/276)
            await _uow.ExecuteTransactionAsync(async () =>
            {
                // Remove existing allocations
                List<PaymentAllocation> existingAllocations;

                if (paymentType == 1)
                {
                    existingAllocations = await _uow.PaymentAllocations.ToListAsync(
                        a => a.CustomerPaymentId == paymentId, null, ct);
                }
                else
                {
                    existingAllocations = await _uow.PaymentAllocations.ToListAsync(
                        a => a.SupplierPaymentId == paymentId, null, ct);
                }

                foreach (var alloc in existingAllocations)
                {
                    await _uow.PaymentAllocations.SoftDeleteAsync(alloc.Id, ct);
                }

                // Add new allocations
                foreach (var item in request.Allocations)
                {
                    var newAllocation = PaymentAllocation.Create(
                        item.AllocatedAmount,
                        item.InvoiceId,
                        item.InvoiceType,
                        customerPaymentId: paymentType == 1 ? paymentId : null,
                        supplierPaymentId: paymentType == 2 ? paymentId : null);

                    await _uow.PaymentAllocations.AddAsync(newAllocation, ct);
                }

                await _uow.SaveChangesAsync(ct);
            }, ct);

            _logger.LogInformation(
                "Allocations updated for payment {PaymentId} (type {PaymentType}): {Count} allocations, total {Total:N2}",
                paymentId, paymentType, request.Allocations.Count, totalAllocated);

            return Result.Success();
        }
        catch (Domain.Exceptions.DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation updating allocations for payment {PaymentId}: {Message}",
                paymentId, ex.Message);
            return Result.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating allocations for payment {PaymentId}", paymentId);
            return Result.Failure("حدث خطأ أثناء تحديث التوزيعات");
        }
    }

    // ─── Private Helpers ──────────────────────────────────────

    private static PaymentAllocationDto MapToDto(PaymentAllocation a) => new()
    {
        Id = a.Id,
        CustomerPaymentId = a.CustomerPaymentId,
        SupplierPaymentId = a.SupplierPaymentId,
        InvoiceId = a.InvoiceId,
        InvoiceType = a.InvoiceType,
        AllocatedAmount = a.AllocatedAmount
    };
}
