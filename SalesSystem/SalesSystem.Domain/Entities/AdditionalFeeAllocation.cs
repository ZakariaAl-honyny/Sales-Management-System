using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

public class AdditionalFeeAllocation : BaseEntity
{
    public int AdditionalFeeId { get; private set; }
    public int PurchaseInvoiceItemId { get; private set; }
    public decimal AllocatedAmount { get; private set; }

    // Navigation properties
    public virtual AdditionalFee? AdditionalFee { get; private set; }
    public virtual PurchaseInvoiceItem? PurchaseInvoiceItem { get; private set; }

    private AdditionalFeeAllocation() { }

    public static AdditionalFeeAllocation Create(
        int additionalFeeId,
        int purchaseInvoiceItemId,
        decimal allocatedAmount,
        int? createdByUserId = null)
    {
        if (additionalFeeId <= 0)
            throw new DomainException("الرسوم الإضافية مطلوبة.");
        if (purchaseInvoiceItemId <= 0)
            throw new DomainException("الصنف في الفاتورة مطلوب.");
        if (allocatedAmount < 0)
            throw new DomainException("المبلغ الموزع لا يمكن أن يكون سالباً.");

        var allocation = new AdditionalFeeAllocation
        {
            AdditionalFeeId = additionalFeeId,
            PurchaseInvoiceItemId = purchaseInvoiceItemId,
            AllocatedAmount = allocatedAmount,
            IsActive = true
        };
        allocation.SetCreatedBy(createdByUserId);
        return allocation;
    }
}
