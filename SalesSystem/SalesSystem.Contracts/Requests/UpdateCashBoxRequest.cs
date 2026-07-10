using System.Text.Json.Serialization;

namespace SalesSystem.Contracts.Requests;

/// <summary>
/// Request to update an existing cash box.
/// Note: AccountId cannot be changed after creation — it is immutable.
/// </summary>
public record UpdateCashBoxRequest(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string? Description = null
);
