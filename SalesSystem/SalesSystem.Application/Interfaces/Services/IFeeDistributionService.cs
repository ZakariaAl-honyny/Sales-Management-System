using SalesSystem.Contracts.Common;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Application.Interfaces.Services;

/// <summary>
/// خدمة توزيع المصاريف الإضافية على بنود فاتورة الشراء.
/// تدعم طريقتين: التوزيع حسب التكلفة (ByCost) أو حسب الكمية (ByQuantity).
/// </summary>
public interface IFeeDistributionService
{
    /// <summary>
    /// توزيع مصروف إضافي على بنود الفاتورة حسب طريقة التوزيع المختارة.
    /// </summary>
    /// <param name="fee">المصروف الإضافي المراد توزيعه.</param>
    /// <param name="items">بنود الفاتورة المستهدفة.</param>
    /// <returns>قائمة بالتخصيصات (Allocation) لكل بند.</returns>
    Task<List<AdditionalFeeAllocation>> DistributeFeeAsync(
        AdditionalFee fee,
        List<PurchaseInvoiceItem> items,
        CancellationToken ct);

    /// <summary>
    /// حساب إجمالي الرسوم الموزعة على جميع بنود الفاتورة.
    /// </summary>
    Task<decimal> CalculateTotalAllocatedFeesAsync(List<PurchaseInvoiceItem> items);
}
