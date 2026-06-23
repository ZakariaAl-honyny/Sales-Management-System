namespace SalesSystem.Domain.Enums;

/// <summary>
/// Lifecycle status for Sales Quotations.
/// Draft(1) → Sent(2) → Accepted(3) → Converted(4)
///                        → Rejected(5)
/// Draft(1) → Rejected(5)
/// Sent(2) → Rejected(5)
/// Accepted(3)/Converted(4)/Rejected(5) are terminal states.
/// </summary>
public enum QuotationStatus : byte
{
    Draft = 1,
    Sent = 2,
    Accepted = 3,
    Converted = 4,
    Rejected = 5
}
