namespace SalesSystem.Contracts.Responses;

/// <summary>
/// Result of the effective price lookup with fallback chain information.
/// Indicates which currency was used and whether conversion was applied.
/// </summary>
public record EffectivePriceDto(
    int ProductUnitId,
    decimal Price,
    decimal? OriginalPrice,
    string? FallbackDescription);
