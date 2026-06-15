namespace SalesSystem.Contracts.Responses;

/// <summary>
/// Result of the effective price lookup with fallback chain information.
/// Indicates which currency was used and whether conversion was applied.
/// </summary>
public record EffectivePriceDto(
    int ProductUnitId,
    int CurrencyId,
    string? CurrencyCode,
    string? CurrencyName,
    decimal Price,
    decimal? OriginalPrice,
    int? OriginalCurrencyId,
    string? OriginalCurrencyCode,
    string? OriginalCurrencyName,
    string? FallbackDescription)
{
    /// <summary>
    /// True if the price was converted from another currency via exchange rate.
    /// </summary>
    public bool IsConverted => OriginalCurrencyId.HasValue && OriginalCurrencyId.Value != CurrencyId;
}
