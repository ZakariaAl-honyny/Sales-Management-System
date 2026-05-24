namespace SalesSystem.Contracts.Requests;

public record CreateCashBoxRequest(
    string BoxName,
    decimal OpeningBalance,
    int? BranchId,
    int? AssignedUserId,
    string? Notes);
