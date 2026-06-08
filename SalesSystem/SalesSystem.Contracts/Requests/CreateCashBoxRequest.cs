namespace SalesSystem.Contracts.Requests;

public record CreateCashBoxRequest(
    string BoxName,
    int? AccountId,          // null = auto-create under Cash & Cash Equivalents (1110)
    int? CategoryId,
    int? BranchId,
    int? AssignedUserId,
    int? CurrencyId,
    string? PhoneNumber,
    string? TaxNumber,
    string? Address,
    string? Notes);
