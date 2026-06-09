namespace SalesSystem.DesktopPWF.ViewModels.Purchases;

/// <summary>
/// ViewModel for an additional fee line in a purchase invoice.
/// Supports named fees with amounts, distribution methods, and optional account linking.
/// </summary>
public class AdditionalFeeLineViewModel : ViewModelBase
{
    private string _feeName = string.Empty;
    private decimal _feeAmount;
    private byte _distributionMethod;
    private int? _accountId;

    public string FeeName
    {
        get => _feeName;
        set
        {
            if (SetProperty(ref _feeName, value))
            {
                ClearErrors(nameof(FeeName));
                if (string.IsNullOrWhiteSpace(value))
                    AddError(nameof(FeeName), "اسم الرسم الإضافي مطلوب");
            }
        }
    }

    public decimal FeeAmount
    {
        get => _feeAmount;
        set
        {
            if (SetProperty(ref _feeAmount, value))
            {
                ClearErrors(nameof(FeeAmount));
                if (value <= 0)
                    AddError(nameof(FeeAmount), "المبلغ يجب أن يكون أكبر من صفر");
            }
        }
    }

    /// <summary>
    /// طريقة توزيع الرسم: 0 = حسب التكلفة، 1 = حسب الكمية
    /// </summary>
    public byte DistributionMethod
    {
        get => _distributionMethod;
        set => SetProperty(ref _distributionMethod, value);
    }

    public int? AccountId
    {
        get => _accountId;
        set => SetProperty(ref _accountId, value);
    }

    public string DistributionMethodDisplay => DistributionMethod switch
    {
        0 => "حسب التكلفة",
        1 => "حسب الكمية",
        _ => "غير معروف"
    };

    public bool IsByCost => DistributionMethod == 0;
    public bool IsByQuantity => DistributionMethod == 1;

    public List<EnumDisplayItem> DistributionMethodOptions { get; } = new()
    {
        new EnumDisplayItem { Value = 0, Display = "حسب التكلفة" },
        new EnumDisplayItem { Value = 1, Display = "حسب الكمية" }
    };
}
