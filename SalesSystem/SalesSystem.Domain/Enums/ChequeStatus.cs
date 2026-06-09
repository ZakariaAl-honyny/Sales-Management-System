namespace SalesSystem.Domain.Enums;

/// <summary>
/// Status lifecycle for cheques.
/// Pending → Cleared | Bounced | Cancelled
/// </summary>
public enum ChequeStatus : byte
{
    Pending = 1,
    Cleared = 2,
    Bounced = 3,
    Cancelled = 4
}
