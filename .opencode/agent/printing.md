

# Print Engine Specification
# Sales Management System — v1.0
# Platform: WPF Desktop (SalesSystem.DesktopPWF)

## Arabic Encoding Requirement

All Arabic string literals in C# source files MUST be valid UTF-8 encoded Arabic text. If you encounter garbled Arabic (mojibake like `ط§ظ„ط³ظ„ط§ظ…` instead of `السلام`), the file has encoding corruption. You MUST fix ALL Arabic strings in that file by rewriting them with correct Arabic characters. Always verify your output files are saved with UTF-8 encoding.
# Note: Printing uses System.Drawing.Printing (compatible with WPF via interop)

---

## 1. Invoice Printer (A4)

```csharp
// SalesSystem.DesktopPWF/Services/Printing/InvoicePrinter.cs
public class InvoicePrinter
{
    private readonly StoreSettings _settings;

    public void Print(SalesInvoiceDto invoice)
    {
        var pd = new PrintDocument();
        pd.PrintPage += (sender, e) => DrawInvoice(e, invoice);
        pd.Print();
    }

    public void ShowPreview(SalesInvoiceDto invoice)
    {
        var pd = new PrintDocument();
        pd.PrintPage += (sender, e) => DrawInvoice(e, invoice);

        var preview = new PrintPreviewDialog
        {
            Document = pd,
            WindowState = FormWindowState.Maximized
        };
        preview.ShowDialog();
    }

    private void DrawInvoice(PrintPageEventArgs e, SalesInvoiceDto invoice)
    {
        var g = e.Graphics!;
        var y = 20f;

        // ── Header: Store Logo ──
        if (!string.IsNullOrEmpty(_settings.LogoPath)
            && File.Exists(_settings.LogoPath))
        {
            using var logo = Image.FromFile(_settings.LogoPath);
            g.DrawImage(logo, new RectangleF(20, y, 80, 80));
        }

        // ── Header: Store Info ──
        var titleFont = new Font("Arial", 16, FontStyle.Bold);
        var normalFont = new Font("Arial", 10);
        var boldFont = new Font("Arial", 10, FontStyle.Bold);

        g.DrawString(_settings.StoreName, titleFont,
            Brushes.Black, 120, y);
        y += 25;
        g.DrawString(_settings.Phone ?? "", normalFont,
            Brushes.Black, 120, y);
        y += 20;
        g.DrawString(_settings.Address ?? "", normalFont,
            Brushes.Black, 120, y);
        y += 40;

        // ── Invoice Info ──
        g.DrawString($"Invoice No: {invoice.InvoiceNo}", boldFont,
            Brushes.Black, 20, y);
        g.DrawString($"Date: {invoice.InvoiceDate:dd/MM/yyyy HH:mm}",
            normalFont, Brushes.Black, 300, y);
        y += 20;
        g.DrawString($"Customer: {invoice.CustomerName}", normalFont,
            Brushes.Black, 20, y);
        y += 30;

        // ── Table Header ──
        DrawTableRow(g, y, boldFont,
            "Product", "Qty", "Price", "Discount", "Total");
        y += 20;
        g.DrawLine(Pens.Black, 20, y, 570, y);
        y += 5;

        // ── Table Rows ──
        foreach (var item in invoice.Items)
        {
            DrawTableRow(g, y, normalFont,
                item.ProductName,
                item.Quantity.ToString("N3"),
                item.UnitPrice.ToString("N2"),
                item.DiscountAmount.ToString("N2"),
                item.LineTotal.ToString("N2"));
            y += 18;
        }

        // ── Totals ──
        y += 10;
        g.DrawLine(Pens.Black, 20, y, 570, y);
        y += 10;
        DrawTotalLine(g, y, normalFont, "Subtotal:",
            invoice.SubTotal.ToString("N2"));
        y += 18;

        if (invoice.DiscountAmount > 0)
        {
            DrawTotalLine(g, y, normalFont, "Discount:",
                invoice.DiscountAmount.ToString("N2"));
            y += 18;
        }

        if (invoice.TaxAmount > 0)
        {
            DrawTotalLine(g, y, normalFont, "Tax:",
                invoice.TaxAmount.ToString("N2"));
            y += 18;
        }

        DrawTotalLine(g, y, boldFont, "TOTAL:",
            invoice.TotalAmount.ToString("N2"));
        y += 18;
        DrawTotalLine(g, y, normalFont, "Paid:",
            invoice.PaidAmount.ToString("N2"));
        y += 18;
        DrawTotalLine(g, y, boldFont, "Balance Due:",
            invoice.DueAmount.ToString("N2"));
    }

    private void DrawTableRow(Graphics g, float y, Font font,
        string col1, string col2, string col3, string col4, string col5)
    {
        g.DrawString(col1, font, Brushes.Black, 20, y);
        g.DrawString(col2, font, Brushes.Black, 250, y);
        g.DrawString(col3, font, Brushes.Black, 320, y);
        g.DrawString(col4, font, Brushes.Black, 410, y);
        g.DrawString(col5, font, Brushes.Black, 500, y);
    }

    private void DrawTotalLine(Graphics g, float y, Font font,
        string label, string value)
    {
        g.DrawString(label, font, Brushes.Black, 380, y);
        g.DrawString(value, font, Brushes.Black, 500, y);
    }
}
```

## 2. Receipt Printer (80mm Thermal)

```csharp
// SalesSystem.DesktopPWF/Services/Printing/ReceiptPrinter.cs
public class ReceiptPrinter
{
    private const float PageWidth = 227f; // 80mm in points
    private readonly StoreSettings _settings;

    public void Print(SalesInvoiceDto invoice)
    {
        var pd = new PrintDocument();
        pd.DefaultPageSettings.PaperSize =
            new PaperSize("Receipt", 315, 1000);
        pd.PrintPage += (sender, e) => DrawReceipt(e, invoice);
        pd.Print();
    }

    private void DrawReceipt(PrintPageEventArgs e, SalesInvoiceDto invoice)
    {
        var g = e.Graphics!;
        var y = 5f;
        var font = new Font("Courier New", 8);
        var boldFont = new Font("Courier New", 9, FontStyle.Bold);
        var center = new StringFormat
            { Alignment = StringAlignment.Center };
        var right = new StringFormat
            { Alignment = StringAlignment.Far };

        // Logo
        if (!string.IsNullOrEmpty(_settings.LogoPath)
            && File.Exists(_settings.LogoPath))
        {
            using var logo = Image.FromFile(_settings.LogoPath);
            g.DrawImage(logo, new RectangleF(
                PageWidth / 2 - 30, y, 60, 60));
            y += 65;
        }

        // Store name centered
        g.DrawString(_settings.StoreName, boldFont,
            Brushes.Black, new RectangleF(0, y, PageWidth, 20), center);
        y += 15;

        if (!string.IsNullOrEmpty(_settings.Phone))
        {
            g.DrawString(_settings.Phone, font,
                Brushes.Black,
                new RectangleF(0, y, PageWidth, 15), center);
            y += 12;
        }

        // Separator
        g.DrawString(new string('-', 32), font,
            Brushes.Black, 0, y);
        y += 12;

        // Invoice info
        g.DrawString($"No: {invoice.InvoiceNo}", font,
            Brushes.Black, 0, y);
        y += 10;
        g.DrawString(
            $"Date: {invoice.InvoiceDate:dd/MM/yy HH:mm}",
            font, Brushes.Black, 0, y);
        y += 10;
        g.DrawString($"Customer: {invoice.CustomerName}",
            font, Brushes.Black, 0, y);
        y += 10;

        g.DrawString(new string('-', 32), font,
            Brushes.Black, 0, y);
        y += 12;

        // Items
        foreach (var item in invoice.Items)
        {
            g.DrawString(item.ProductName, font, Brushes.Black, 0, y);
            y += 10;
            var detail = $"  {item.Quantity:N2} x {item.UnitPrice:N2}";
            g.DrawString(detail, font, Brushes.Black, 0, y);
            g.DrawString(item.LineTotal.ToString("N2"), font,
                Brushes.Black,
                new RectangleF(0, y, PageWidth, 12), right);
            y += 10;
        }

        g.DrawString(new string('=', 32), font,
            Brushes.Black, 0, y);
        y += 12;

        // Totals
        DrawReceiptTotal(g, y, boldFont, right, "TOTAL:",
            invoice.TotalAmount.ToString("N2"));
        y += 14;
        DrawReceiptTotal(g, y, font, right, "Paid:",
            invoice.PaidAmount.ToString("N2"));
        y += 12;
        DrawReceiptTotal(g, y, font, right, "Balance:",
            invoice.DueAmount.ToString("N2"));
        y += 20;

        g.DrawString("Thank you!", boldFont,
            Brushes.Black,
            new RectangleF(0, y, PageWidth, 15), center);
    }
 
    private void DrawReceiptTotal(Graphics g, float y, Font font,
        StringFormat rightAlign, string label, string value)
    {
        g.DrawString(label, font, Brushes.Black, 0, y);
        g.DrawString(value, font, Brushes.Black,
            new RectangleF(0, y, PageWidth, 12), rightAlign);
    }
}

## Phase 21: Users & Permissions Module — COMPLETE (v4.6.9)

Phase 21 (PRD alignment) — Users & Permissions is now complete. No direct changes to printing. Permission-based UI controls may affect print button visibility for different roles. Verify that PrintController endpoints respect [Authorize] attributes.

---

## 📋 Phase Awareness (Phases 23-31)

The system is currently at **v4.6.9+ with Phases 18-24 completed and Phases 25-31 planned**:

| Phase | Status | Description |
|-------|--------|-------------|
| 23 — Customers Module | ✅ Completed | Customer groups, Account linking, CheckCreditLimit, CustomerType removed |
| 24 — Accounting Integration | ✅ Completed | Auto journal entries for all money ops, COGS (AverageCost), Payment reversals |
| 25 — Products Module | 📝 Planned | Multi-currency pricing (ProductPrices), FIFO batches (InventoryBatches), PriceLevel enum (4 levels), BOM, product images, opening stock |
| 26 — Warehouses Module | 📝 Planned | Warehouse types, manager, AccountId FK, stock adjustments, issue reasons, physical count V2 |
| 27 — Purchases Module | 📝 Planned | Multi-currency, landed cost (AdditionalCharge), Purchase Orders, standalone returns, attachments |
| 28 — Sales Module | 📝 Planned | Multi-currency, profit display, Sales Quotations, barcode POS, credit limit enforcement |
| 29 — Receipts & Payments | 📝 Planned | Multi-invoice distribution, Cheques, PaymentAllocation, CashBox.AccountId, DailyClosure |
| 30 — Journal Entries | 📝 Planned | 3-state lifecycle, multi-currency, attachments, FiscalYear, Annual Closing |
| 31 — Reports | 📝 Planned | 35+ DTOs, Hierarchical Income Statement + Balance Sheet, Excel export |

### Key Architecture Rules for Subagents

When implementing or reviewing code, ALWAYS enforce these rules:

1. **Multi-Currency First**: All pricing MUST support multi-currency via ProductPrices table — NEVER store single-currency prices on Product entity
2. **FIFO/FEFO Batches**: Inventory MUST use InventoryBatches for cost allocation — NEVER use weighted-average only
3. **Landed Cost**: Purchase costs MUST include AdditionalCharge distribution — NEVER record purchase cost without transport/customs allocation
4. **Auto Journal Entries**: Every money-affecting operation MUST create journal entries via AccountingIntegrationService — NEVER leave the general ledger out of sync
5. **Chart of Accounts Links**: CashBox, Warehouse, Customer, Supplier MUST link to Account via AccountId FK — NEVER operate without COA integration
6. **Payment Allocation**: Payments MUST use PaymentAllocation for multi-invoice settlement — NEVER leave partial payments untracked
7. **Report Excellence**: ALL reports MUST support Excel export via ClosedXML — NEVER limit to on-screen display only
8. **Passwordless Users**: User.Create() NEVER accepts a password — MustChangePassword=true is the default
9. **ReferenceId over ReferenceNumber**: Journal entry lookups use int FK (ReferenceId), not string matching
10. **AvgCost for COGS**: COGS uses ProductUnit.AverageCost (weighted average), never PurchaseCost

### 💡 Bug Prevention Checklist

When writing or reviewing code in ANY layer, check these:
- [ ] Does the code handle multi-currency correctly? (CurrencyId + ExchangeRate on all financial entities)
- [ ] Are all prices stored per ProductUnit (not per Product)?
- [ ] Does costing use the configured CostingMethod from SystemSettings?
- [ ] Are all FK relationships `DeleteBehavior.Restrict`?
- [ ] Does the service return `Result<T>` (not throw exceptions)?
- [ ] Is the controller free of business logic (delegates to service)?
- [ ] Do all ViewModels use `ExecuteAsync()` wrapper (no manual try/catch)?
- [ ] Are all buttons ALWAYS enabled (no CanExecute predicates)?
- [ ] Does the validation use `INotifyDataErrorInfo` (not `HasXxxError` booleans)?
- [ ] Does every editor call `ValidateAllAsync()` on save?
- [ ] Is the connection string DPAPI-encrypted or from env var?
- [ ] Are Arabic messages properly UTF-8 encoded?
- [ ] Does the list display newest-first (OrderByDescending)?
- [ ] Are EventBus subscriptions disposed in `Cleanup()`?

### Features to Fix By Default

When you encounter any code related to these areas, apply fixes automatically:

1. Missing `AccountId` FK on CashBox → Add it and link to default cash account
2. Missing `AccountId` FK on Warehouse → Add it and link to inventory account
3. Missing `CustomerGroupId` on Customer → Make optional with "عام" as default
4. Missing `CurrencyId` on financial entities → Add multi-currency support
5. Missing `PriceLevel` support → Extend pricing to use PriceLevel enum
6. Missing `InventoryBatch` creation on purchase → Add FIFO batch tracking
7. Missing `AdditionalCharge` support on purchase → Add landed cost allocation
8. Missing journal entry on cash operations → Call AccountingIntegrationService
9. Missing Excel export on report → Add ClosedXML worksheet generation
10. COGS using PurchaseCost → Change to AverageCost from ProductUnit
11. Payment without allocation → Add PaymentAllocation tracking
12. Missing reversal entries on payment update/delete → Add reversal journal entries