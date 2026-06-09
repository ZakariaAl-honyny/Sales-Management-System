namespace SalesSystem.Contracts.DTOs;

/// <summary>
/// Cheque DTO for displaying cheque information in lists and details.
/// </summary>
public class ChequeDto
{
    public int Id { get; set; }
    public string ChequeNumber { get; set; } = "";
    public string BankName { get; set; } = "";
    public DateTime IssueDate { get; set; }
    public DateTime MaturityDate { get; set; }
    public byte Status { get; set; }
    public string StatusDisplay => Status switch
    {
        1 => "معلق",
        2 => "مقبوض",
        3 => "مرتجع",
        4 => "ملغي",
        _ => "غير معروف"
    };
    public decimal Amount { get; set; }
    public int? CustomerPaymentId { get; set; }
    public int? SupplierPaymentId { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Payment allocation DTO for displaying how a payment is distributed across invoices.
/// </summary>
public class PaymentAllocationDto
{
    public int Id { get; set; }
    public int? CustomerPaymentId { get; set; }
    public int? SupplierPaymentId { get; set; }
    public int InvoiceId { get; set; }
    public byte InvoiceType { get; set; }
    public string? InvoiceNo { get; set; }
    public decimal AllocatedAmount { get; set; }
}
