using SalesSystem.Domain.Accounting.Entities;
using SalesSystem.Domain.Common;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// رسم إضافي — يمثل تكلفة إضافية تضاف إلى فاتورة الشراء (نقل، جمارك، تخليص، تحميل)
/// </summary>
public class AdditionalFee : BaseEntity
{
    public int PurchaseInvoiceId { get; private set; }
    public string FeeName { get; private set; } = string.Empty;
    public decimal FeeAmount { get; private set; }
    public DistributionMethod DistributionMethod { get; private set; }
    public int? AccountId { get; private set; }

    public virtual PurchaseInvoice? PurchaseInvoice { get; private set; }
    public virtual Account? Account { get; private set; }

    public virtual List<AdditionalFeeAllocation> Allocations { get; private set; } = new();

    private AdditionalFee() { }

    public static AdditionalFee Create(
        int purchaseInvoiceId,
        string feeName,
        decimal feeAmount,
        DistributionMethod distributionMethod = DistributionMethod.ByCost,
        int? accountId = null)
    {
        if (string.IsNullOrWhiteSpace(feeName))
            throw new DomainException("اسم الرسم الإضافي مطلوب.");
        if (feeName.Trim().Length > 100)
            throw new DomainException("اسم الرسم الإضافي لا يتجاوز 100 حرف.");
        if (feeAmount <= 0)
            throw new DomainException("قيمة الرسم الإضافي يجب أن تكون أكبر من الصفر.");

        return new AdditionalFee
        {
            PurchaseInvoiceId = purchaseInvoiceId,
            FeeName = feeName.Trim(),
            FeeAmount = feeAmount,
            DistributionMethod = distributionMethod,
            AccountId = accountId,
            IsActive = true
        };
    }
}
