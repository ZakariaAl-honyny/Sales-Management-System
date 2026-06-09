namespace SalesSystem.Contracts.Requests;

/// <summary>
/// A single allocation entry — maps a payment to an invoice.
/// </summary>
public record CreateAllocationRequest(
    int InvoiceId,
    byte InvoiceType,
    decimal AllocatedAmount
);

/// <summary>
/// Request to update all allocations for a payment (replaces existing allocations).
/// </summary>
public record UpdateAllocationsRequest(
    List<CreateAllocationRequest> Allocations
);
