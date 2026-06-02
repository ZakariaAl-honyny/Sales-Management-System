using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using SalesSystem.DesktopPWF.Printing.Models;
using SalesSystem.DesktopPWF.Printing.Core;

namespace SalesSystem.DesktopPWF.Printing.Core;

/// <summary>
/// A4 Invoice Printer implementation
/// </summary>
public class InvoicePrinter : IPrinterService
{
    private InvoicePrintDto? _invoice;
    private StoreInfoPrintDto? _store;

    public Task PrintAsync(InvoicePrintDto invoice, StoreInfoPrintDto storeInfo)
    {
        _invoice = invoice;
        _store = storeInfo;

        using var pd = new PrintDocument();
        pd.DefaultPageSettings.Margins = new Margins(40, 40, 40, 40);
        pd.PrintPage += OnPrintPage;
        pd.Print();

        return Task.CompletedTask;
    }

    public Task PreviewAsync(InvoicePrintDto invoice, StoreInfoPrintDto storeInfo)
    {
        _invoice = invoice;
        _store = storeInfo;

        using var pd = new PrintDocument();
        pd.DefaultPageSettings.Margins = new Margins(40, 40, 40, 40);
        pd.PrintPage += OnPrintPage;

        using var ppd = new PrintPreviewDialog
        {
            Document = pd,
            Width = 1000,
            Height = 800,
            ShowIcon = false,
            Text = $"معاينة الفاتورة - #{invoice.Id}"
        };
        ppd.ShowDialog();

        return Task.CompletedTask;
    }

    private void OnPrintPage(object sender, PrintPageEventArgs e)
    {
        if (_invoice == null || _store == null) return;

        var g = e.Graphics!;
        var bounds = e.MarginBounds;
        float y = bounds.Top;
        float left = bounds.Left;
        float right = bounds.Right;
        float width = bounds.Width;

        // Fonts
        using var titleFont = new Font("Arial", 18, FontStyle.Bold);
        using var headerFont = new Font("Arial", 14, FontStyle.Bold);
        using var subHeaderFont = new Font("Arial", 11, FontStyle.Bold);
        using var bodyFont = new Font("Arial", 10, FontStyle.Regular);
        using var bodyBoldFont = new Font("Arial", 10, FontStyle.Bold);
        using var smallFont = new Font("Arial", 9, FontStyle.Regular);

        // 1. Logo & Store Info
        var logo = PrintHelper.LoadImage(_store.LogoPath);
        if (logo != null)
        {
            g.DrawImage(logo, right - 80, y, 80, 80);
        }

        // Store Details (Right Aligned)
        float infoX = logo != null ? right - 90 : right;
        g.DrawString(_store.Name, headerFont, Brushes.Black, new RectangleF(left, y, infoX - left, 30), PrintHelper.RTLFormatRight);
        y += 25;
        if (!string.IsNullOrEmpty(_store.Address))
        {
            g.DrawString(_store.Address, bodyFont, Brushes.Black, new RectangleF(left, y, infoX - left, 20), PrintHelper.RTLFormatRight);
            y += 20;
        }
        if (!string.IsNullOrEmpty(_store.Phone))
        {
            g.DrawString($"هاتف: {_store.Phone}", bodyFont, Brushes.Black, new RectangleF(left, y, infoX - left, 20), PrintHelper.RTLFormatRight);
            y += 20;
        }
        if (!string.IsNullOrEmpty(_store.TaxNumber))
        {
            g.DrawString($"الرقم الضريبي: {_store.TaxNumber}", bodyFont, Brushes.Black, new RectangleF(left, y, infoX - left, 20), PrintHelper.RTLFormatRight);
            y += 20;
        }

        // 2. Invoice Meta (Top Left)
        float metaY = bounds.Top;
        g.DrawString(_invoice.Title, titleFont, Brushes.Black, new RectangleF(left, metaY, width / 2, 40), PrintHelper.LTRFormat);
        metaY += 35;
        g.DrawString($"رقم الفاتورة: {_invoice.Id}", subHeaderFont, Brushes.Black, new RectangleF(left, metaY, width / 2, 25), PrintHelper.LTRFormat);
        metaY += 25;
        g.DrawString($"التاريخ: {_invoice.Date:yyyy/MM/dd}", bodyFont, Brushes.Black, new RectangleF(left, metaY, width / 2, 20), PrintHelper.LTRFormat);
        
        y = Math.Max(y, metaY) + 20;

        // 3. Customer Info
        PrintHelper.DrawLine(g, y, left, right + 40);
        y += 10;
        g.DrawString("إلى السيد / السادة:", subHeaderFont, Brushes.Black, new RectangleF(left, y, width, 25), PrintHelper.RTLFormatRight);
        y += 25;
        g.DrawString(_invoice.CustomerName, headerFont, Brushes.Black, new RectangleF(left, y, width, 30), PrintHelper.RTLFormatRight);
        y += 40;

        // 4. Items Table Header
        PrintHelper.DrawLine(g, y, left, right + 40);
        y += 5;
        float colIndex = 40;
        float colTotal = 100;
        float colDisc = 80;
        float colPrice = 100;
        float colQty = 80;
        float colUnit = 60;
        float colName = width - (colIndex + colTotal + colDisc + colPrice + colQty + colUnit);

        float curX = right;
        g.DrawString("#", bodyBoldFont, Brushes.Black, new RectangleF(curX - colIndex, y, colIndex, 25), PrintHelper.RTLCenterFormat); curX -= colIndex;
        g.DrawString("اسم المنتج", bodyBoldFont, Brushes.Black, new RectangleF(curX - colName, y, colName, 25), PrintHelper.RTLCenterFormat); curX -= colName;
        g.DrawString("الوحدة", bodyBoldFont, Brushes.Black, new RectangleF(curX - colUnit, y, colUnit, 25), PrintHelper.RTLCenterFormat); curX -= colUnit;
        g.DrawString("الكمية", bodyBoldFont, Brushes.Black, new RectangleF(curX - colQty, y, colQty, 25), PrintHelper.RTLCenterFormat); curX -= colQty;
        g.DrawString("السعر", bodyBoldFont, Brushes.Black, new RectangleF(curX - colPrice, y, colPrice, 25), PrintHelper.RTLCenterFormat); curX -= colPrice;
        g.DrawString("الخصم", bodyBoldFont, Brushes.Black, new RectangleF(curX - colDisc, y, colDisc, 25), PrintHelper.RTLCenterFormat); curX -= colDisc;
        g.DrawString("الإجمالي", bodyBoldFont, Brushes.Black, new RectangleF(curX - colTotal, y, colTotal, 25), PrintHelper.RTLCenterFormat);

        y += 30;
        PrintHelper.DrawLine(g, y, left, right + 40);
        y += 5;

        // 5. Items Rows
        foreach (var item in _invoice.Items)
        {
            curX = right;
            g.DrawString(item.Index.ToString(), smallFont, Brushes.Black, new RectangleF(curX - colIndex, y, colIndex, 20), PrintHelper.RTLCenterFormat); curX -= colIndex;
            g.DrawString(item.ProductName, smallFont, Brushes.Black, new RectangleF(curX - colName, y, colName, 20), PrintHelper.RTLFormatRight); curX -= colName;
            g.DrawString(item.UnitName, smallFont, Brushes.Black, new RectangleF(curX - colUnit, y, colUnit, 20), PrintHelper.RTLCenterFormat); curX -= colUnit;
            g.DrawString(item.Quantity.ToString("N3"), smallFont, Brushes.Black, new RectangleF(curX - colQty, y, colQty, 20), PrintHelper.RTLCenterFormat); curX -= colQty;
            g.DrawString(item.UnitPrice.ToString("N2"), smallFont, Brushes.Black, new RectangleF(curX - colPrice, y, colPrice, 20), PrintHelper.RTLCenterFormat); curX -= colPrice;
            g.DrawString(item.Discount.ToString("N2"), smallFont, Brushes.Black, new RectangleF(curX - colDisc, y, colDisc, 20), PrintHelper.RTLCenterFormat); curX -= colDisc;
            g.DrawString(item.Total.ToString("N2"), smallFont, Brushes.Black, new RectangleF(curX - colTotal, y, colTotal, 20), PrintHelper.RTLCenterFormat);

            y += 25;

            if (y > bounds.Bottom - 150) // Basic pagination check
            {
                e.HasMorePages = true;
                return;
            }
        }

        // 6. Totals
        y += 10;
        PrintHelper.DrawLine(g, y, left, right + 40);
        y += 10;

        float totalLabelWidth = 150;
        float totalValueWidth = 100;
        float totalX = left + totalLabelWidth + totalValueWidth;

        void DrawTotalRow(string label, decimal value, Font font)
        {
            g.DrawString(label, font, Brushes.Black, new RectangleF(left, y, totalLabelWidth, 20), PrintHelper.LTRFormat);
            g.DrawString(value.ToString("N2"), font, Brushes.Black, new RectangleF(left + totalLabelWidth, y, totalValueWidth, 20), PrintHelper.RTLLeftFormat);
            y += 22;
        }

        DrawTotalRow("إجمالي القيمة:", _invoice.Totals.SubTotal, bodyFont);
        DrawTotalRow("إجمالي الخصم:", _invoice.Totals.Discount, bodyFont);
        if (_invoice.Totals.Tax > 0)
            DrawTotalRow("إجمالي الضريبة:", _invoice.Totals.Tax, bodyFont);
        
        y += 5;
        DrawTotalRow("الإجمالي النهائي:", _invoice.Totals.Total, headerFont);
        PrintHelper.DrawLine(g, y, left, left + totalLabelWidth + totalValueWidth);
        y += 5;
        DrawTotalRow("المبلغ المدفوع:", _invoice.Totals.Paid, bodyBoldFont);
        DrawTotalRow("المبلغ المتبقي:", _invoice.Totals.Due, bodyBoldFont);

        // 7. Footer / Notes
        if (!string.IsNullOrEmpty(_invoice.Notes))
        {
            y += 20;
            g.DrawString("ملاحظات:", subHeaderFont, Brushes.Black, new RectangleF(left, y, width, 20), PrintHelper.RTLFormatRight);
            y += 25;
            g.DrawString(_invoice.Notes, smallFont, Brushes.Black, new RectangleF(left, y, width, 60), PrintHelper.RTLFormatRight);
        }

        y = bounds.Bottom - 30;
        g.DrawString("شكراً لتعاملكم معنا", bodyFont, Brushes.Black, new RectangleF(left, y, width, 20), PrintHelper.RTLCenterFormat);
    }
}
