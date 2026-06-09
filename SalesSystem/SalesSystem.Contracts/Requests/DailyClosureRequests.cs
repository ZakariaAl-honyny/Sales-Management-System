namespace SalesSystem.Contracts.Requests;

/// <summary>
/// Request to create a daily closure for a cash box.
/// </summary>
public record CreateDailyClosureRequest(
    int CashBoxId,
    DateTime ClosureDate
);

/// <summary>
/// Request to reconcile a daily closure by entering the actual cash count.
/// </summary>
public record ReconcileDailyClosureRequest(
    decimal ActualCashCount,
    string? Notes = null
);
