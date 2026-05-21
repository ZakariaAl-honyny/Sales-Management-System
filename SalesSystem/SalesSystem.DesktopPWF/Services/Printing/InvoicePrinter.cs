using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using SalesSystem.DesktopPWF.Models.Printing;
using SalesSystem.DesktopPWF.Helpers;
using SalesSystem.DesktopPWF.Services.App;

namespace SalesSystem.DesktopPWF.Services.Printing;

public class InvoicePrinter : IInvoicePrinter
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
        var margin = e.MarginBounds;
        float y = margin.Top;

        // Fonts
        var titleFont = new Font("Segoe UI", 16, FontStyle.Bold);
        var headerFont = new Font("Segoe UI", 12, FontStyle.Bold);
        var bodyFont = new Font("Segoe UI", 10);
        var boldFont = new Font("Segoe UI", 10, FontStyle.Bold);

        // 1. Header (Store Info)
        var logo = PrintHelper.LoadLogo(_storeInfo.LogoPath);
        if (logo != null)
        {
            g.DrawImage(logo, margin.Right - 100, y, 100, 100);
        }

        g.DrawString(_storeInfo.StoreName, titleFont, Brushes.Black, new RectangleF(margin.Left, y, margin.Width - 110, 35), PrintHelper.RTLFormatRight);
        y += 35;
        g.DrawString(_storeInfo.Address, bodyFont, Brushes.Black, new RectangleF(margin.Left, y, margin.Width - 110, 20), PrintHelper.RTLFormatRight);
        y += 20;
        g.DrawString($"هاتف: {_storeInfo.Phone}", bodyFont, Brushes.Black, new RectangleF(margin.Left, y, margin.Width - 110, 20), PrintHelper.RTLFormatRight);
        y += 20;
        if (!string.IsNullOrEmpty(_storeInfo.TaxNumber))
        {
            g.DrawString($"الرقم الضريبي: {_storeInfo.TaxNumber}", bodyFont, Brushes.Black, new RectangleF(margin.Left, y, margin.Width - 110, 20), PrintHelper.RTLFormatRight);
            y += 20;
        }
        y += 5;

        if (y < margin.Top + 100) y = margin.Top + 110;

        PrintHelper.DrawLine(g, Pens.Black, margin.Left, y, margin.Right);
        y += 10;

        // 2. Invoice Meta Info
        g.DrawString($"{_invoice.TypeName}", headerFont, Brushes.Black, new RectangleF(margin.Left, y, margin.Width, 30), PrintHelper.RTLFormatCenter);
        y += 35;

        g.DrawString($"رقم الفاتورة: {_invoice.InvoiceNumber}", boldFont, Brushes.Black, new RectangleF(margin.Left, y, margin.Width, 20), PrintHelper.RTLFormatRight);
        y += 20;
        g.DrawString($"التاريخ: {_invoice.InvoiceDate:yyyy-MM-dd HH:mm}", bodyFont, Brushes.Black, new RectangleF(margin.Left, y, margin.Width, 20), PrintHelper.RTLFormatRight);
        y += 20;
        g.DrawString($"العميل/المورد: {_invoice.CustomerOrSupplierName}", bodyFont, Brushes.Black, new RectangleF(margin.Left, y, margin.Width, 20), PrintHelper.RTLFormatRight);
        y += 20;
        g.DrawString($"المستودع: {_invoice.WarehouseName}", bodyFont, Brushes.Black, new RectangleF(margin.Left, y, margin.Width, 20), PrintHelper.RTLFormatRight);
        y += 20;
        g.DrawString($"نوع الدفع: {_invoice.PaymentType.ToArabicString()}", bodyFont, Brushes.Black, new RectangleF(margin.Left, y, margin.Width, 20), PrintHelper.RTLFormatRight);
        y += 30;

        // 3. Items Table Header
        PrintHelper.DrawLine(g, Pens.Black, margin.Left, y, margin.Right);
        y += 5;
        
        g.DrawString("المنتج", boldFont, Brushes.Black, new RectangleF(margin.Right - 300, y, 300, 20), PrintHelper.RTLFormatRight);
        g.DrawString("الكمية", boldFont, Brushes.Black, new RectangleF(margin.Right - 400, y, 100, 20), PrintHelper.RTLFormatRight);
        g.DrawString("السعر", boldFont, Brushes.Black, new RectangleF(margin.Right - 500, y, 100, 20), PrintHelper.RTLFormatRight);
        g.DrawString("الخصم", boldFont, Brushes.Black, new RectangleF(margin.Right - 600, y, 100, 20), PrintHelper.RTLFormatRight);
        g.DrawString("الإجمالي", boldFont, Brushes.Black, new RectangleF(margin.Left, y, 100, 20), PrintHelper.RTLFormatLeft);

        y += 25;
        PrintHelper.DrawLine(g, Pens.Gray, margin.Left, y, margin.Right);
        y += 5;

        // 4. Items List
        foreach (var item in _items)
        {
            g.DrawString(item.ProductName, bodyFont, Brushes.Black, new RectangleF(margin.Right - 300, y, 300, 20), PrintHelper.RTLFormatRight);
            string quantityStr = PrintHelper.FormatQuantity(item.Quantity);
            if (item.Mode == 2) quantityStr += " (جملة)";
            g.DrawString(quantityStr, bodyFont, Brushes.Black, new RectangleF(margin.Right - 400, y, 100, 20), PrintHelper.RTLFormatRight);
            g.DrawString(PrintHelper.FormatCurrency(item.UnitPrice), bodyFont, Brushes.Black, new RectangleF(margin.Right - 500, y, 100, 20), PrintHelper.RTLFormatRight);
            g.DrawString(PrintHelper.FormatCurrency(item.Discount), bodyFont, Brushes.Black, new RectangleF(margin.Right - 600, y, 100, 20), PrintHelper.RTLFormatRight);
            g.DrawString(PrintHelper.FormatCurrency(item.LineTotal), bodyFont, Brushes.Black, new RectangleF(margin.Left, y, 100, 20), PrintHelper.RTLFormatLeft);
            
            y += 20;
            if (y > margin.Bottom - 150) break;

        }

        y += 10;
        PrintHelper.DrawLine(g, Pens.Black, margin.Left, y, margin.Right);
        y += 10;

        // 5. Totals
        g.DrawString("إجمالي فرعي:", boldFont, Brushes.Black, new RectangleF(margin.Left + 150, y, 150, 20), PrintHelper.RTLFormatRight);
        g.DrawString(PrintHelper.FormatCurrency(_totals.SubTotal), bodyFont, Brushes.Black, new RectangleF(margin.Left, y, 150, 20), PrintHelper.RTLFormatLeft);
        y += 20;
        g.DrawString("الخصم:", boldFont, Brushes.Black, new RectangleF(margin.Left + 150, y, 150, 20), PrintHelper.RTLFormatRight);
        g.DrawString(PrintHelper.FormatCurrency(_totals.Discount), bodyFont, Brushes.Black, new RectangleF(margin.Left, y, 150, 20), PrintHelper.RTLFormatLeft);
        y += 20;
        g.DrawString("الضريبة:", boldFont, Brushes.Black, new RectangleF(margin.Left + 150, y, 150, 20), PrintHelper.RTLFormatRight);
        g.DrawString(PrintHelper.FormatCurrency(_totals.TaxAmount), bodyFont, Brushes.Black, new RectangleF(margin.Left, y, 150, 20), PrintHelper.RTLFormatLeft);
        y += 25;
        PrintHelper.DrawLine(g, Pens.Black, margin.Left, y, margin.Left + 300);
        y += 5;
        g.DrawString("الإجمالي النهائي:", headerFont, Brushes.Black, new RectangleF(margin.Left + 150, y, 150, 30), PrintHelper.RTLFormatRight);
        g.DrawString(PrintHelper.FormatCurrency(_totals.TotalAmount), boldFont, Brushes.Black, new RectangleF(margin.Left, y, 150, 30), PrintHelper.RTLFormatLeft);
        
        if (!string.IsNullOrEmpty(_invoice.Notes))
        {
            y += 40;
            g.DrawString("ملاحظات:", boldFont, Brushes.Black, new RectangleF(margin.Left, y, margin.Width, 20), PrintHelper.RTLFormatRight);
            y += 20;
            g.DrawString(_invoice.Notes, bodyFont, Brushes.Black, new RectangleF(margin.Left, y, margin.Width, 40), PrintHelper.RTLFormatRight);
        }

        y += 50;
        g.DrawString("شكراً لتعاملكم معنا", headerFont, Brushes.Black, new RectangleF(margin.Left, y, margin.Width, 30), PrintHelper.RTLFormatCenter);
    }
}



