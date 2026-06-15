namespace SalesSystem.Contracts.DTOs;

/// <summary>
/// DTO for additional fees on purchase invoices (landed cost distribution).
/// </summary>
public record AdditionalFeeDto(
    int Id,
    int PurchaseInvoiceId,
    string FeeName,
    decimal FeeAmount,
    byte DistributionMethod,
    int? AccountId = null);
