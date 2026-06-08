namespace SalesSystem.Contracts.Requests;

public record UpdateCashBoxRequest(
    string? BoxName,
    int? CategoryId,
    int? BranchId,
    int? AssignedUserId,
    int? CurrencyId,
    string? PhoneNumber,
    string? TaxNumber,
    string? Address,
    string? Notes);
