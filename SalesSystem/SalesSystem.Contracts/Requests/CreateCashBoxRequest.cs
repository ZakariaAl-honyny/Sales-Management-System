using System.Text.Json.Serialization;

namespace SalesSystem.Contracts.Requests;

/// <summary>
/// Request to create a new cash box.
/// AccountId is nullable — when null or 0, the service auto-creates a linked
/// chart-of-accounts account before constructing the entity.
/// </summary>
public record CreateCashBoxRequest(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("accountId")] int? AccountId,
    [property: JsonPropertyName("description")] string? Description = null
);
