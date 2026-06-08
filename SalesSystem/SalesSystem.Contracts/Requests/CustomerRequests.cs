namespace SalesSystem.Contracts.Requests;

public record CreateCustomerRequest(
    string Name,
    string? Phone,
    string? Email,
    string? Address,
    string? TaxNumber,
    decimal OpeningBalance,
    decimal CreditLimit = 0,
    int? AccountId = null,
    int? CustomerGroupId = null
);

public record UpdateCustomerRequest(
    string Name,
    string? Phone,
    string? Email,
    string? Address,
    string? TaxNumber,
    decimal CreditLimit,
    bool IsActive,
    int? AccountId = null,
    int? CustomerGroupId = null
);

public record CreateCustomerGroupRequest(
    string Name,
    string? Description = null
);

public record UpdateCustomerGroupRequest(
    string Name,
    string? Description = null,
    bool IsActive = true
);
