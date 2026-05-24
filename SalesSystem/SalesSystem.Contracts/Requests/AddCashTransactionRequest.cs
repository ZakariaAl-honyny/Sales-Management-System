namespace SalesSystem.Contracts.Requests;

public record AddCashTransactionRequest(
    decimal Amount,
    string? Notes);
