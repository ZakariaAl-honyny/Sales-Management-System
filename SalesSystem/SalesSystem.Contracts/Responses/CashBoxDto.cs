using System.Text.Json.Serialization;

namespace SalesSystem.Contracts.Responses;

/// <summary>
/// DTO for a cash box.
/// Schema: CashBoxes — Id, AccountId (int), Name, Description (nullable).
/// Balance is tracked on the linked Account, never stored here.
/// </summary>
public record CashBoxDto(
    int Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("accountId")] int AccountId,
    [property: JsonPropertyName("accountName")] string? AccountName,
    [property: JsonPropertyName("accountCode")] string? AccountCode,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("isActive")] bool IsActive
);
