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
    public decimal OtherCharges { get; init; }
    public decimal TaxRate { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal GrandTotal { get; init; }
    public bool IsTaxInclusive { get; init; }

    // ─── Payment ──────────────────────────────────
    public string PaymentMethod { get; init; } = string.Empty;
    public decimal AmountPaid { get; init; }
    public decimal ChangeAmount { get; init; }
    public string? Notes { get; init; }

    // ─── Print Template Settings ─────────────────
    /// <summary>
    /// رسالة تذييل مخصصة من إعدادات الطباعة (بدلاً من الرسالة الثابتة)
    /// </summary>
    public string? FooterNote { get; set; }
    public bool ShowBalanceOnPrint { get; set; } = true;
    public bool PrintSignature { get; set; }
    public bool ShowExpiryInInvoices { get; set; }
    public string PaperSize { get; set; } = "A4";

    /// <summary>
    /// مسار صورة التوقيع من إعدادات المتجر — يُعرض كصورة في الفاتورة A4 بدلاً من خط النص
    /// </summary>
    public string? SignatureImagePath { get; set; }

    // ─── Print Feature Flags ──────────────────────
    public bool PrintBarcode { get; set; }
    public bool PrintQRCode { get; set; }
    public bool PrintCompanyAddress { get; set; } = true;
}
