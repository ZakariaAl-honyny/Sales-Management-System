namespace SalesSystem.Contracts.Responses;

public record PartyDto(
    int Id,
    string Name,
    string? Phone,
    string? Email,
    string? Address,
    string? TaxNumber,
    string? Notes,
    bool IsActive
);
