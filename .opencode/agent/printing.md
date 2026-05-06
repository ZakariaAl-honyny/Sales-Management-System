# Print Engine Specification
# Sales Management System — v1.0

---

## 1. Invoice Printer (A4)

```csharp
// Desktop/Services/Printing/InvoicePrinter.cs
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
// Desktop/Services/Printing/ReceiptPrinter.cs
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
```