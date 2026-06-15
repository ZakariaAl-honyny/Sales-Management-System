namespace SalesSystem.Application.Helpers;

/// <summary>
/// تقوم بتوزيع المصاريف الإضافية (نقل، جمارك، إلخ) بشكل تناسبي على بنود الفاتورة.
/// </summary>
public static class AdditionalChargeAllocator
{
    /// <summary>
    /// يوزع المصاريف الإضافية على بنود الفاتورة بناءً على نسبة (LineTotal / SubTotal).
    /// </summary>
    /// <param name="lines">قائمة بنود الفاتورة مع LineTotal و UnitCost و Quantity</param>
    /// <param name="otherCharges">إجمالي المصاريف الإضافية المراد توزيعها</param>
    /// <param name="subTotal">إجمالي الفاتورة قبل الخصم والضريبة (مجموع LineTotals)</param>
    /// <returns>قاموس رقم البند ← التكلفة الوحدوية بعد توزيع المصاريف (Landed Unit Cost)</returns>
    /// <remarks>
    /// المعادلة:
    /// <code>
    /// lineShare = (LineTotal / SubTotal) * OtherCharges
    /// landedUnitCost = UnitCost + (lineShare / Quantity)
    /// </code>
    /// إذا كانت <paramref name="otherCharges"/> صفراً أو <paramref name="subTotal"/> صفراً،
    /// تُعاد التكلفة الوحدوية الأصلية دون تغيير.
    /// </remarks>
    public static Dictionary<int, decimal> Allocate(
        IReadOnlyList<AllocationLine> lines,
        decimal otherCharges,
        decimal subTotal)
    {
        var result = new Dictionary<int, decimal>(lines.Count);

        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];

            if (subTotal > 0 && otherCharges > 0)
            {
                // حصة هذا البند من المصاريف الإضافية
                var lineShare = (line.LineTotal / subTotal) * otherCharges;

                // التكلفة الوحدوية بعد إضافة حصة المصاريف
                var landedUnitCost = line.UnitCost +
                    (line.Quantity > 0 ? lineShare / line.Quantity : 0);

                result[i] = landedUnitCost;
            }
            else
            {
                // لا توجد مصاريف إضافية — التكلفة كما هي
                result[i] = line.UnitCost;
            }
        }

        return result;
    }
}

/// <summary>
/// يمثل بنداً واحداً في الفاتورة لحساب توزيع المصاريف الإضافية.
/// </summary>
public class AllocationLine
{
    /// <summary>رقم البند في قائمة البنود (0-based).</summary>
    public int Index { get; init; }

    /// <summary>إجمالي البند = (الكمية × السعر) - الخصم.</summary>
    public decimal LineTotal { get; init; }

    /// <summary>الكمية لهذا البند.</summary>
    public decimal Quantity { get; init; }

    /// <summary>التكلفة الوحدوية الأصلية قبل توزيع المصاريف.</summary>
    public decimal UnitCost { get; init; }
}
