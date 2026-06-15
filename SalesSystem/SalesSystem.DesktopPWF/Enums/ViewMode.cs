namespace SalesSystem.DesktopPWF.Enums;

/// <summary>
/// UI View Mode — determines which navigation items are visible.
/// Basic = operational screens only; Advanced = operational + accounting.
/// Maps to the requirement for simplified vs full interface based on user role.
/// </summary>
public enum ViewMode
{
    /// <summary>Operational screens only (مبيعات, مشتريات, أصناف, عملاء, موردون, مخزون)</summary>
    Basic = 0,

    /// <summary>All screens including accounting (حسابات, قيود, سندات, تقارير مالية)</summary>
    Advanced = 1
}
