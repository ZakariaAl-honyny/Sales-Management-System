using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using SalesSystem.Desktop.Printing.Models;
using SalesSystem.Desktop.Printing.Extensions;

namespace SalesSystem.Desktop.Printing.Core;

public class ReceiptPrinter : IReceiptPrinter
{
    private InvoicePrintDto? _invoice;
    private IEnumerable<InvoiceItemPrintDto>? _items;
    private InvoiceTotalsPrintDto? _totals;
    private StoreInfoPrintDto? _storeInfo;

    public void PrintPreview(InvoicePrintDto invoice, IEnumerable<InvoiceItemPrintDto> items, InvoiceTotalsPrintDto totals, StoreInfoPrintDto storeInfo)
    {
        _invoice = invoice;
        _items = items;
        _totals = totals;
        _storeInfo = storeInfo;

        using var pd = new PrintDocument();
        // 80mm width = ~3.15 inches = 315 units
        pd.DefaultPageSettings.PaperSize = new PaperSize("80mm", 315, 1000); 
        pd.PrintPage += Pd_PrintPage;
        
        using var preview = new PrintPreviewDialog();
        preview.Document = pd;
        preview.WindowState = FormWindowState.Maximized;
        preview.ShowDialog();
    }

    public void Print(InvoicePrintDto invoice, IEnumerable<InvoiceItemPrintDto> items, InvoiceTotalsPrintDto totals, StoreInfoPrintDto storeInfo, string? printerName = null)
    {
        _invoice = invoice;
        _items = items;
        _totals = totals;
        _storeInfo = storeInfo;

        using var pd = new PrintDocument();
        pd.DefaultPageSettings.PaperSize = new PaperSize("80mm", 315, 1000);
        pd.PrintPage += Pd_PrintPage;
        if (!string.IsNullOrEmpty(printerName))
        {
            pd.PrinterSettings.PrinterName = printerName;
        }
        pd.Print();
    }

    private void Pd_PrintPage(object sender, PrintPageEventArgs e)
    {
        if (_invoice == null || _items == null || _totals == null || _storeInfo == null) return;

        var g = e.Graphics!;
        float width = 280; 
        float x = 10;
        float y = 10;

        // Fonts
        var titleFont = new Font("Segoe UI", 12, FontStyle.Bold);
        var headerFont = new Font("Segoe UI", 10, FontStyle.Bold);
        var bodyFont = new Font("Segoe UI", 9);
        var smallFont = new Font("Segoe UI", 8);

        // 1. Header
        g.DrawString(_storeInfo.StoreName, titleFont, Brushes.Black, new RectangleF(x, y, width, 25), PrintHelper.RTLFormatCenter);
        y += 25;
        g.DrawString(_storeInfo.Address, smallFont, Brushes.Black, new RectangleF(x, y, width, 15), PrintHelper.RTLFormatCenter);
        y += 15;
        g.DrawString($"هاتف: {_storeInfo.Phone}", smallFont, Brushes.Black, new RectangleF(x, y, width, 15), PrintHelper.RTLFormatCenter);
        y += 20;

        PrintHelper.DrawLine(g, Pens.Black, x, y, width + x);
        y += 5;

        // 2. Meta
        g.DrawString(_invoice.TypeName, headerFont, Brushes.Black, new RectangleF(x, y, width, 20), PrintHelper.RTLFormatCenter);
        y += 20;
        g.DrawString($"رقم: {_invoice.InvoiceNumber}", bodyFont, Brushes.Black, new RectangleF(x, y, width, 15), PrintHelper.RTLFormatRight);
        y += 15;
        g.DrawString($"التاريخ: {_invoice.InvoiceDate:yyyy-MM-dd HH:mm}", bodyFont, Brushes.Black, new RectangleF(x, y, width, 15), PrintHelper.RTLFormatRight);
        y += 15;
        g.DrawString($"العميل: {_invoice.CustomerOrSupplierName}", bodyFont, Brushes.Black, new RectangleF(x, y, width, 15), PrintHelper.RTLFormatRight);
        y += 15;
        g.DrawString($"نوع الدفع: {_invoice.PaymentType.ToArabicString()}", bodyFont, Brushes.Black, new RectangleF(x, y, width, 15), PrintHelper.RTLFormatRight);
        y += 20;

        PrintHelper.DrawLine(g, Pens.Black, x, y, width + x);
        y += 5;

        // 3. Items Header
        g.DrawString("الصنف", smallFont, Brushes.Black, new RectangleF(x + width - 120, y, 120, 15), PrintHelper.RTLFormatRight);
        g.DrawString("كمية", smallFont, Brushes.Black, new RectangleF(x + width - 170, y, 50, 15), PrintHelper.RTLFormatRight);
        g.DrawString("إجمالي", smallFont, Brushes.Black, new RectangleF(x, y, 80, 15), PrintHelper.RTLFormatLeft);
        y += 15;
        PrintHelper.DrawLine(g, Pens.Gray, x, y, width + x);
        y += 5;

        // 4. Items
        foreach (var item in _items)
        {
            g.DrawString(item.ProductName, smallFont, Brushes.Black, new RectangleF(x + width - 120, y, 120, 15), PrintHelper.RTLFormatRight);
            g.DrawString(PrintHelper.FormatQuantity(item.Quantity), smallFont, Brushes.Black, new RectangleF(x + width - 170, y, 50, 15), PrintHelper.RTLFormatRight);
            g.DrawString(PrintHelper.FormatCurrency(item.LineTotal), smallFont, Brushes.Black, new RectangleF(x, y, 80, 15), PrintHelper.RTLFormatLeft);
            y += 15;
        }

        y += 5;
        PrintHelper.DrawLine(g, Pens.Black, x, y, width + x);
        y += 5;

        // 5. Totals
        g.DrawString("الإجمالي النهائي:", headerFont, Brushes.Black, new RectangleF(x + 100, y, 150, 20), PrintHelper.RTLFormatRight);
        g.DrawString(PrintHelper.FormatCurrency(_totals.TotalAmount), headerFont, Brushes.Black, new RectangleF(x, y, 100, 20), PrintHelper.RTLFormatLeft);
        y += 25;

        g.DrawString("شكراً لزيارتكم", bodyFont, Brushes.Black, new RectangleF(x, y, width, 20), PrintHelper.RTLFormatCenter);
        
        e.HasMorePages = false;
    }
}
