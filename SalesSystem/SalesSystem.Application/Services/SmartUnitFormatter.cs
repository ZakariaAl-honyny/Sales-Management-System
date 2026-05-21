using SalesSystem.Contracts.DTOs;

namespace SalesSystem.Application.Services;

/// <summary>
/// Formats low-stock shortfall quantities as human-readable Arabic text,
/// combining wholesale boxes and remaining retail pieces.
/// Example: 15 pieces shortfall with ConversionFactor=12 → "1 كرتون و 3 حبة"
/// </summary>
public static class SmartUnitFormatter
{
    /// <summary>
    /// Returns a localized Arabic description like "1 كرتون و 3 حبة" instead of "15 حبة".
    /// Uses the pre-computed SuggestedWholesaleBoxes and SuggestedRetailRemainder from the DTO.
    /// </summary>
    public static string FormatShortfall(LowStockReportDto item)
    {
        var boxes    = (int)item.SuggestedWholesaleBoxes;
        var pieces   = (int)item.SuggestedRetailRemainder;
        var deficit  = item.DeficitRetailQty;

        if (deficit <= 0)
            return "لا يوجد عجز";

        if (item.ConversionFactor <= 1)
            return $"{deficit:N0} حبة";

        if (boxes == 0)
            return $"{pieces} حبة";

        if (pieces == 0)
            return $"{boxes} كرتون";

        return $"{boxes} كرتون و {pieces} حبة";
    }

    /// <summary>
    /// Formats any quantity into Arabic text using boxes + pieces.
    /// </summary>
    public static string FormatQuantity(
        decimal quantity,
        decimal conversionFactor,
        string wholesaleUnit = "كرتون",
        string retailUnit = "حبة")
    {
        if (conversionFactor <= 1 || quantity <= 0)
            return $"{quantity:N3} {retailUnit}";

        var boxes     = (int)(quantity / conversionFactor);
        var remainder = quantity % conversionFactor;

        if (boxes == 0)
            return $"{remainder:N0} {retailUnit}";

        if (remainder == 0)
            return $"{boxes} {wholesaleUnit}";

        return $"{boxes} {wholesaleUnit} و {remainder:N0} {retailUnit}";
    }
}
