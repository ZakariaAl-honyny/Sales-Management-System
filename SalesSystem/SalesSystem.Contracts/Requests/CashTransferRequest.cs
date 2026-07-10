namespace SalesSystem.Contracts.Requests;

/// <summary>
/// Request to transfer cash between two cash boxes.
/// Creates a JournalEntry with two lines (Dr source, Cr destination).
/// </summary>
public record CashTransferRequest(
    int SourceCashBoxId,
    int DestinationCashBoxId,
    decimal Amount,
    string? Notes = null);
