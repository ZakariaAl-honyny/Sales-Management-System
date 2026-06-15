namespace SalesSystem.Contracts.Requests;

public record CreatePartyRequest(
    string Name,
    string? Phone,
    string? Email,
    string? Address,
    string? TaxNumber,
    string? Notes
);

public record UpdatePartyRequest(
    string Name,
    string? Phone,
    string? Email,
    string? Address,
    string? TaxNumber,
    string? Notes
);
