namespace SalesSystem.Contracts.Requests;

public record CreateAdditionalFeeRequest(
    string FeeName,
    decimal FeeAmount,
    byte DistributionMethod,
    int? AccountId = null);

public record DistributeFeesRequest(
    int PurchaseInvoiceId,
    List<int> AdditionalFeeIds);
