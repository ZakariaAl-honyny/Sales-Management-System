namespace SalesSystem.Contracts.Requests;

public record CreatePartyRequest(
    string Name,
    byte PartyType,
    string? NameAr,
    string? Phone,
    string? Mobile,
    string? Email,
    string? Address,
    string? TaxNumber
);

public record UpdatePartyRequest(
    string Name,
    string? NameAr,
    string? Phone,
    string? Mobile,
    string? Email,
    string? Address,
    string? TaxNumber
);
