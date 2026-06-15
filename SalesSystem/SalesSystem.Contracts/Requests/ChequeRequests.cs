namespace SalesSystem.Contracts.Requests;

public class CreateChequeRequest
{
    public string ChequeNumber { get; set; } = string.Empty;
    public string? BankName { get; set; }
    public string? BankBranch { get; set; }
    public int? PaymentId { get; set; }
    public int? CustomerReceiptId { get; set; }
    public int? ReceiptVoucherId { get; set; }
    public int? PaymentVoucherId { get; set; }
    public DateTime IssueDate { get; set; }
    public DateTime? MaturityDate { get; set; }
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
}

public class UpdateChequeRequest
{
    public string ChequeNumber { get; set; } = string.Empty;
    public string? BankName { get; set; }
    public string? BankBranch { get; set; }
    public int? PaymentId { get; set; }
    public int? CustomerReceiptId { get; set; }
    public int? ReceiptVoucherId { get; set; }
    public int? PaymentVoucherId { get; set; }
    public DateTime IssueDate { get; set; }
    public DateTime? MaturityDate { get; set; }
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
}
