namespace SalesSystem.Contracts.Requests;

public record CashTransferRequest(
    int SourceCashBoxId,
    int DestinationCashBoxId,
    decimal Amount,
    string? Notes);
