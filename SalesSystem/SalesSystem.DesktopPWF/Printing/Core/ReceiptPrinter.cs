using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using SalesSystem.DesktopPWF.Printing.Models;
using SalesSystem.DesktopPWF.Printing.Core;

namespace SalesSystem.DesktopPWF.Printing.Core;

/// <summary>
/// 80mm Thermal Receipt Printer implementation
/// </summary>
public class ReceiptPrinter : IPrinterService
{
    private InvoicePrintDto? _invoice;
    private StoreInfoPrintDto? _store;

    public Task PrintAsync(InvoicePrintDto invoice, StoreInfoPrintDto storeInfo)
    {
        _invoice = invoice;
        _store = storeInfo;

        using var pd = new PrintDocument();
        // 80mm width, height is dynamic (set to 0 for continuous roll)
        pd.DefaultPageSettings.PaperSize = new PaperSize("80mm", 280, 0);
        pd.DefaultPageSettings.Margins = new Margins(10, 10, 10, 10);
        pd.PrintPage += OnPrintPage;
        pd.Print();

        return Task.CompletedTask;
    }

    public Task PreviewAsync(InvoicePrintDto invoice, StoreInfoPrintDto storeInfo)
    {
        _invoice = invoice;
        _store = storeInfo;

        using var pd = new PrintDocument();
        pd.DefaultPageSettings.PaperSize = new PaperSize("80mm", 280, 1000); // 1000 height for preview
        pd.DefaultPageSettings.Margins = new Margins(10, 10, 10, 10);
        pd.PrintPage += OnPrintPage;

        using var ppd = new PrintPreviewDialog
        {
            Document = pd,
            Width = 400,
            Height = 800,
            ShowIcon = false,
            Text = $"معاينة الإيصال - #{invoice.Id}"
        };
        ppd.ShowDialog();

        return Task.CompletedTask;
    }

    private void OnPrintPage(object sender, PrintPageEventArgs e)
    {
        if (_invoice == null || _store == null) return;

        var g = e.Graphics!;
        float y = 10;
        float left = 10;
        float width = 260; // Approx width for 80mm with margins
        float right = left + width;

        // Fonts
        using var titleFont = new Font("Arial", 12, FontStyle.Bold);
        using var bodyFont = new Font("Arial", 9, FontStyle.Regular);
        using var bodyBoldFont = new Font("Arial", 9, FontStyle.Bold);
        using var smallFont = new Font("Arial", 8, FontStyle.Regular);

        // 1. Store Header
        g.DrawString(_store.Name, titleFont, Brushes.Black, new RectangleF(left, y, width, 25), PrintHelper.RTLCenterFormat);
        y += 25;
        if (!string.IsNullOrEmpty(_store.Address))
        {
            g.DrawString(_store.Address, smallFont, Brushes.Black, new RectangleF(left, y, width, 20), PrintHelper.RTLCenterFormat);
            y += 15;
        }
        if (!string.IsNullOrEmpty(_store.Phone))
        {
            g.DrawString($"ت: {_store.Phone}", smallFont, Brushes.Black, new RectangleF(left, y, width, 20), PrintHelper.RTLCenterFormat);
            y += 15;
        }
        if (!string.IsNullOrEmpty(_store.TaxNumber))
        {
            g.DrawString($"الضريبي: {_store.TaxNumber}", smallFont, Brushes.Black, new RectangleF(left, y, width, 20), PrintHelper.RTLCenterFormat);
            y += 15;
        }

        y += 10;
        PrintHelper.DrawLine(g, y, left, right);
        y += 5;

        // 2. Invoice Meta
        g.DrawString(_invoice.Title, bodyBoldFont, Brushes.Black, new RectangleF(left, y, width, 20), PrintHelper.RTLCenterFormat);
        y += 20;
        g.DrawString($"رقم: {_invoice.Id}", bodyFont, Brushes.Black, new RectangleF(left, y, width, 20), PrintHelper.RTLFormatRight);
        y += 15;
        g.DrawString($"التاريخ: {_invoice.Date:yyyy/MM/dd HH:mm}", bodyFont, Brushes.Black, new RectangleF(left, y, width, 20), PrintHelper.RTLFormatRight);
        y += 15;
        g.DrawString($"العميل: {_invoice.CustomerName}", bodyFont, Brushes.Black, new RectangleF(left, y, width, 20), PrintHelper.RTLFormatRight);
        y += 20;

        // 3. Items Header
        PrintHelper.DrawLine(g, y, left, right);
        y += 5;
        
        float colQty = 40;
        float colTotal = 60;
        float colName = width - (colQty + colTotal);

        float curX = right;
        g.DrawString("البيان", smallFont, Brushes.Black, new RectangleF(curX - colName, y, colName, 20), PrintHelper.RTLFormatRight); curX -= colName;
        g.DrawString("الكمية", smallFont, Brushes.Black, new RectangleF(curX - colQty, y, colQty, 20), PrintHelper.RTLCenterFormat); curX -= colQty;
        g.DrawString("الإجمالي", smallFont, Brushes.Black, new RectangleF(curX - colTotal, y, colTotal, 20), PrintHelper.RTLLeftFormat);

        y += 15;
        PrintHelper.DrawLine(g, y, left, right);
        y += 5;

        // 4. Items
        foreach (var item in _invoice.Items)
        {
            // Name might wrap
            float nameHeight = g.MeasureString(item.ProductName, smallFont, (int)colName).Height;
            g.DrawString(item.ProductName, smallFont, Brushes.Black, new RectangleF(right - colName, y, colName, nameHeight), PrintHelper.RTLFormatRight);
            
            g.DrawString(item.Quantity.ToString("N0"), smallFont, Brushes.Black, new RectangleF(right - colName - colQty, y, colQty, 20), PrintHelper.RTLCenterFormat);
            g.DrawString(item.Total.ToString("N2"), smallFont, Brushes.Black, new RectangleF(left, y, colTotal, 20), PrintHelper.RTLLeftFormat);

            y += Math.Max(20, nameHeight);
        }

        y += 5;
        PrintHelper.DrawLine(g, y, left, right);
        y += 5;

        // 5. Totals
        void DrawTotalRow(string label, decimal value, Font font)
        {
            g.DrawString(label, font, Brushes.Black, new RectangleF(left + 60, y, width - 60, 20), PrintHelper.RTLFormatRight);
            g.DrawString(value.ToString("N2"), font, Brushes.Black, new RectangleF(left, y, 60, 20), PrintHelper.RTLLeftFormat);
            y += 18;
        }

        DrawTotalRow("الإجمالي:", _invoice.Totals.SubTotal, bodyFont);
        if (_invoice.Totals.Discount > 0)
            DrawTotalRow("الخصم:", _invoice.Totals.Discount, bodyFont);
        
        y += 2;
        DrawTotalRow("الإجمالي النهائي:", _invoice.Totals.Total, bodyBoldFont);
        y += 5;
        DrawTotalRow("المدفوع:", _invoice.Totals.Paid, bodyFont);
        DrawTotalRow("المتبقي:", _invoice.Totals.Due, bodyFont);

        y += 15;
        g.DrawString("شكراً لزيارتكم", bodyFont, Brushes.Black, new RectangleF(left, y, width, 20), PrintHelper.RTLCenterFormat);
        y += 30; // Extra space for tear-off
    }
}
