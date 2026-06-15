using System.Text.Json.Serialization;

namespace SalesSystem.Contracts.Responses;

/// <summary>
/// DTO for a bank record.
/// Schema: Banks — Id, AccountId (int), Name, AccountNumber (nullable), IBAN (nullable), IsActive.
/// </summary>
public record BankDto(
    int Id,
    [property: JsonPropertyName("accountId")] int AccountId,
    [property: JsonPropertyName("accountName")] string? AccountName,
    [property: JsonPropertyName("accountCode")] string? AccountCode,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("accountNumber")] string? AccountNumber,
    [property: JsonPropertyName("iban")] string? Iban,
    [property: JsonPropertyName("isActive")] bool IsActive
);
