using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Application.Services;

/// <summary>
/// تنفيذ خدمة توزيع المصاريف الإضافية على بنود فاتورة الشراء.
/// تدعم طريقتين: التوزيع حسب التكلفة (ByCost) أو حسب الكمية (ByQuantity).
/// </summary>
public class FeeDistributionService : IFeeDistributionService
{
    private readonly ILogger<FeeDistributionService> _logger;

    public FeeDistributionService(ILogger<FeeDistributionService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// توزيع مصروف إضافي على بنود الفاتورة حسب طريقة التوزيع المختارة.
    /// </summary>
    /// <param name="fee">المصروف الإضافي المراد توزيعه.</param>
    /// <param name="items">بنود الفاتورة المستهدفة.</param>
    /// <returns>قائمة بالتخصيصات (Allocation) لكل بند.</returns>
    public Task<List<AdditionalFeeAllocation>> DistributeFeeAsync(
        AdditionalFee fee,
        List<PurchaseInvoiceItem> items,
        CancellationToken ct)
    {
        var allocations = new List<AdditionalFeeAllocation>();

        if (!items.Any() || fee.FeeAmount <= 0)
            return Task.FromResult(allocations);

        try
        {
            decimal totalWeight;

            if (fee.DistributionMethod == DistributionMethod.ByCost)
            {
                // التوزيع حسب التكلفة: الوزن = LineTotal للصنف / مجموع LineTotal لكل البنود
                totalWeight = items.Sum(i => i.LineTotal);
            }
            else
            {
                // التوزيع حسب الكمية: الوزن = Quantity للصنف / مجموع Quantity لكل البنود
                totalWeight = items.Sum(i => i.Quantity);
            }

            if (totalWeight <= 0)
            {
                _logger.LogWarning("لا يمكن توزيع المصروف الإضافي {FeeName}: مجموع الأوزان يساوي صفر",
                    fee.FeeName);
                return Task.FromResult(allocations);
            }

            foreach (var item in items)
            {
                decimal weight;

                if (fee.DistributionMethod == DistributionMethod.ByCost)
                {
                    weight = item.LineTotal / totalWeight;
                }
                else
                {
                    weight = item.Quantity / totalWeight;
                }

                var allocatedAmount = weight * fee.FeeAmount;

                // تخصيص المبلغ للصنف
                item.SetAdditionalFeesAllocation(item.AdditionalFeesAmount + allocatedAmount);

                var allocation = AdditionalFeeAllocation.Create(
                    fee.Id,
                    item.Id > 0 ? item.Id : 0, // If item not yet persisted, ID will be 0
                    allocatedAmount);

                allocations.Add(allocation);
            }

            _logger.LogInformation("تم توزيع المصروف الإضافي {FeeName} بقيمة {FeeAmount} على {ItemCount} بنود",
                fee.FeeName, fee.FeeAmount, items.Count);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "خطأ في المجال أثناء توزيع المصروف الإضافي: {Message}", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ غير متوقع أثناء توزيع المصروف الإضافي {FeeName}", fee.FeeName);
        }

        return Task.FromResult(allocations);
    }

    /// <summary>
    /// حساب إجمالي الرسوم الموزعة على جميع بنود الفاتورة.
    /// </summary>
    public Task<decimal> CalculateTotalAllocatedFeesAsync(List<PurchaseInvoiceItem> items)
    {
        var total = items.Sum(i => i.AdditionalFeesAmount);
        return Task.FromResult(total);
    }
}
