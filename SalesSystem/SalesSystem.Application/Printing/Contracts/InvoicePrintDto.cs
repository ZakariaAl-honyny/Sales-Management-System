namespace SalesSystem.Application.Printing.Contracts;

public class InvoicePrintDto
{
    // ─── Store Info ───────────────────────────────
    public string StoreName { get; init; } = string.Empty;
    public string StorePhone { get; init; } = string.Empty;
    public string StoreAddress { get; init; } = string.Empty;
    public string StoreTaxNumber { get; init; } = string.Empty;
    public byte[]? LogoBytes { get; init; }

    // ─── Invoice Header ───────────────────────────
    public int InvoiceId { get; init; }
    public string InvoiceNumber { get; init; } = string.Empty;
    public DateTime InvoiceDate { get; init; }
    public InvoiceTypePrint InvoiceType { get; init; }

    // ─── Parties ──────────────────────────────────
    public string CustomerOrSupplierName { get; init; } = string.Empty;
    public string? CustomerPhone { get; init; }
    public string? CustomerAddress { get; init; }

    // ─── Items ────────────────────────────────────
    public List<InvoiceItemPrintDto> Items { get; init; } = new();

    // ─── Financials ───────────────────────────────
    public decimal SubTotal { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal TaxRate { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal GrandTotal { get; init; }
    public bool IsTaxInclusive { get; init; }

    // ─── Payment ──────────────────────────────────
    public string PaymentMethod { get; init; } = string.Empty;
    public decimal AmountPaid { get; init; }
    public decimal ChangeAmount { get; init; }
    public string? Notes { get; init; }
}
