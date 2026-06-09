using SalesSystem.Domain.Accounting.Entities;
using SalesSystem.Domain.Common;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

public class AdditionalFee : BaseEntity
{
    public int PurchaseInvoiceId { get; private set; }
    public string FeeName { get; private set; } = string.Empty;
    public decimal FeeAmount { get; private set; }
    public DistributionMethod DistributionMethod { get; private set; }
    public int? AccountId { get; private set; }

    // Navigation properties
    public virtual PurchaseInvoice? PurchaseInvoice { get; private set; }
    public virtual Account? Account { get; private set; }

    private AdditionalFee() { }

    public static AdditionalFee Create(
        int purchaseInvoiceId,
        string feeName,
        decimal feeAmount,
        DistributionMethod distributionMethod = DistributionMethod.ByCost,
        int? accountId = null,
        int? createdByUserId = null)
    {
        if (purchaseInvoiceId <= 0)
            throw new DomainException("الفاتورة مطلوبة.");
        if (string.IsNullOrWhiteSpace(feeName))
            throw new DomainException("اسم الرسوم الإضافية مطلوب.");
        if (feeName.Length > 200)
            throw new DomainException("اسم الرسوم الإضافية لا يمكن أن يتجاوز 200 حرف.");
        if (feeAmount <= 0)
            throw new DomainException("قيمة الرسوم الإضافية يجب أن تكون أكبر من الصفر.");
        if (!Enum.IsDefined(typeof(DistributionMethod), distributionMethod))
            throw new DomainException("طريقة التوزيع غير صالحة.");

        var fee = new AdditionalFee
        {
            PurchaseInvoiceId = purchaseInvoiceId,
            FeeName = feeName.Trim(),
            FeeAmount = feeAmount,
            DistributionMethod = distributionMethod,
            AccountId = accountId,
            IsActive = true
        };
        fee.SetCreatedBy(createdByUserId);
        return fee;
    }

    public void Update(decimal feeAmount, DistributionMethod distributionMethod, int? accountId = null)
    {
        if (feeAmount <= 0)
            throw new DomainException("قيمة الرسوم الإضافية يجب أن تكون أكبر من الصفر.");
        if (!Enum.IsDefined(typeof(DistributionMethod), distributionMethod))
            throw new DomainException("طريقة التوزيع غير صالحة.");

        FeeAmount = feeAmount;
        DistributionMethod = distributionMethod;
        AccountId = accountId;
        UpdateTimestamp();
    }
}
