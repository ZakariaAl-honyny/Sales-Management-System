namespace SalesSystem.Contracts.Responses;

public record PartyDto(
    int Id,
    byte PartyType,
    string PartyTypeName,
    string Name,
    string? NameAr,
    string? Phone,
    string? Mobile,
    string? Email,
    string? Address,
    string? TaxNumber,
    int AccountId,
    string? AccountName,
    bool IsActive
);
