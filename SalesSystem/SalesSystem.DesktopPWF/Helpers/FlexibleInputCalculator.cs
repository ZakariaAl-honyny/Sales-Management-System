namespace SalesSystem.DesktopPWF.Helpers;

/// <summary>
/// Calculates the third field when ANY TWO of (Quantity, UnitPrice, LineTotal) are provided.
/// Supports flexible data entry: user enters two fields, the third is auto-computed.
/// </summary>
public static class FlexibleInputCalculator
{
    public enum CalculationField
    {
        Quantity,
        Price,
        Total
    }

    /// <summary>
    /// Given two known values (plus the field that was last modified),
    /// computes the third. The 'total' parameter represents the gross line total
    /// (Quantity × Price), before any line-level discount.
    /// </summary>
    /// <param name="quantity">Current quantity value.</param>
    /// <param name="price">Current unit price/cost value.</param>
    /// <param name="total">Current gross total (Quantity × Price), before discount.</param>
    /// <param name="lastModifiedField">Which field the user most recently edited.</param>
    /// <returns>(quantity, price, total) — the computed triple with the third field filled in.</returns>
    public static (decimal quantity, decimal price, decimal total) Calculate(
        decimal quantity, decimal price, decimal total,
        CalculationField lastModifiedField)
    {
        switch (lastModifiedField)
        {
            case CalculationField.Quantity:
                // User just changed Quantity.
                // If a gross Total (> 0) was already entered → recalc Price = Total / Qty
                // Otherwise → recalc Total = Qty × Price
                if (total > 0 && quantity > 0)
                {
                    return (quantity, total / quantity, total);
                }
                // Either Total was 0 (not entered) or Qty is 0 (invalid)
                return (quantity, price, quantity * price);

            case CalculationField.Price:
                // User just changed Price.
                // If a gross Total (> 0) was already entered → recalc Qty = Total / Price
                // Otherwise → recalc Total = Qty × Price
                if (total > 0 && price != 0)
                {
                    return (total / price, price, total);
                }
                return (quantity, price, quantity * price);

            case CalculationField.Total:
                // User just changed the gross Total.
                // If Quantity (> 0) was already entered → recalc Price = Total / Qty
                // Else if Price (≠ 0) was already entered → recalc Qty = Total / Price
                if (quantity > 0)
                {
                    return (quantity, total / quantity, total);
                }
                if (price != 0)
                {
                    return (total / price, price, total);
                }
                // Only Total was entered — nothing to recalc
                return (quantity, price, total);

            default:
                return (quantity, price, total);
        }
    }
}
