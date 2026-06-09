using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Application.Interfaces.Services;

/// <summary>
/// Service for managing payment allocations — how a payment is distributed across multiple invoices.
/// One payment can settle multiple invoices; one invoice can be settled by multiple payments.
/// </summary>
public interface IPaymentAllocationService
{
    /// <summary>
    /// Gets all allocations for a specific payment (customer or supplier).
    /// </summary>
    /// <param name="paymentId">The payment ID.</param>
    /// <param name="paymentType">1 = CustomerPayment, 2 = SupplierPayment.</param>
    Task<Result<List<PaymentAllocationDto>>> GetAllocationsForPaymentAsync(int paymentId, byte paymentType, CancellationToken ct = default);

    /// <summary>
    /// Replaces all allocations for a payment with the new set.
    /// Uses ExecuteTransactionAsync to atomically remove old and add new allocations.
    /// Validates that total allocated amount does not exceed the payment amount.
    /// </summary>
    Task<Result> UpdateAllocationsAsync(int paymentId, byte paymentType, UpdateAllocationsRequest request, CancellationToken ct = default);
}
