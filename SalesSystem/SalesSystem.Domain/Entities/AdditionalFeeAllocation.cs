using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// توزيع الرسم الإضافي — يمثل حصة صنف معين من الرسم الإضافي
/// </summary>
public class AdditionalFeeAllocation : BaseEntity
{
    public int AdditionalFeeId { get; private set; }
    public int PurchaseInvoiceItemId { get; private set; }
    public decimal AllocatedAmount { get; private set; }

    public virtual AdditionalFee? AdditionalFee { get; private set; }
    public virtual PurchaseInvoiceItem? PurchaseInvoiceItem { get; private set; }

    private AdditionalFeeAllocation() { }

    public static AdditionalFeeAllocation Create(int additionalFeeId, int purchaseInvoiceItemId, decimal allocatedAmount)
    {
        if (allocatedAmount < 0)
            throw new DomainException("المبلغ الموزع لا يمكن أن يكون سالباً.");

        return new AdditionalFeeAllocation
        {
            AdditionalFeeId = additionalFeeId,
            PurchaseInvoiceItemId = purchaseInvoiceItemId,
            AllocatedAmount = allocatedAmount,
            IsActive = true
        };
    }
}
