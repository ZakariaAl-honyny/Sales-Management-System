namespace SalesSystem.Domain.Enums;

/// <summary>
/// Lifecycle status for cheques.
/// UnderCollection → Deposited → Cleared (happy path)
/// UnderCollection/Deposited → Bounced (failure path)
/// Any status → Cancelled (terminal path)
/// </summary>
public enum ChequeStatus : byte
{
    UnderCollection = 1,  // تحت التحصيل
    Deposited = 2,        // تم الإيداع
    Cleared = 3,          // مقبوض (تم الصرف)
    Bounced = 4,          // مرتجع (لم يصرف)
    Cancelled = 5         // ملغي
}
