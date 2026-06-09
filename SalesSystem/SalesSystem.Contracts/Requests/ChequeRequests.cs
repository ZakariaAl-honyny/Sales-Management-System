using SalesSystem.Domain.Enums;

namespace SalesSystem.Contracts.Requests;

/// <summary>
/// Request to create a new cheque linked to a customer or supplier payment.
/// </summary>
public record CreateChequeRequest(
    string ChequeNumber,
    string BankName,
    DateTime IssueDate,
    DateTime MaturityDate,
    decimal Amount,
    int? CustomerPaymentId = null,
    int? SupplierPaymentId = null,
    string? Notes = null
);

/// <summary>
/// Request to update a cheque's status (Clear, Bounce, Cancel).
/// </summary>
public record UpdateChequeStatusRequest(
    ChequeStatus NewStatus,
    string? Notes = null
);
