namespace SalesSystem.Contracts.Requests;
public record CreateBankRequest(int AccountId, string Name, int CurrencyId);

public record UpdateBankRequest(string Name, int CurrencyId);
