using SalesSystem.Contracts.Enums;

namespace SalesSystem.DesktopPWF.Helpers;

public class PaymentMethodOption
{
    public PaymentMethod Value { get; }
    public string DisplayName { get; }

    public PaymentMethodOption(PaymentMethod value, string displayName)
    {
        Value = value;
        DisplayName = displayName;
    }
}
