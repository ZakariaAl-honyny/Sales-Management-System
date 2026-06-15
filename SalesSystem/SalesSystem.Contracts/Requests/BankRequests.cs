using System.Text.Json.Serialization;

namespace SalesSystem.Contracts.Requests;

/// <summary>
/// Request to create a new bank.
/// AccountId is nullable — when null or 0, the service auto-creates a linked
/// chart-of-accounts account before constructing the entity.
/// </summary>
public record CreateBankRequest(
    [property: JsonPropertyName("accountId")] int? AccountId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("accountNumber")] string? AccountNumber = null,
    [property: JsonPropertyName("iban")] string? Iban = null
);

/// <summary>
/// Request to update an existing bank.
/// Note: AccountId cannot be changed after creation — it is immutable.
/// </summary>
public record UpdateBankRequest(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("accountNumber")] string? AccountNumber = null,
    [property: JsonPropertyName("iban")] string? Iban = null
);
