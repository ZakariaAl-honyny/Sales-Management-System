using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using SalesSystem.DesktopPWF.Models.Printing;
using SalesSystem.DesktopPWF.Helpers;
using SalesSystem.DesktopPWF.Services.App;

namespace SalesSystem.DesktopPWF.Services.Printing;

public class TransferPrinter : ITransferPrinter
{
    private TransferPrintDto? _transfer;
    private IEnumerable<InvoiceItemPrintDto>? _items;
    private StoreInfoPrintDto? _storeInfo;

    public void PrintPreview(TransferPrintDto transfer, IEnumerable<InvoiceItemPrintDto> items, StoreInfoPrintDto storeInfo)
    {
        _transfer = transfer;
        _items = items;
        _storeInfo = storeInfo;

        using var pd = new PrintDocument();
        pd.PrintPage += Pd_PrintPage;
        
        using var preview = new PrintPreviewDialog();
        preview.Document = pd;
        preview.WindowState = FormWindowState.Maximized;
        preview.ShowDialog();
    }

    private void Pd_PrintPage(object sender, PrintPageEventArgs e)
    {
        if (_transfer == null || _items == null || _storeInfo == null) return;

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
        y += 25;

        if (y < margin.Top + 100) y = margin.Top + 110;

        PrintHelper.DrawLine(g, Pens.Black, margin.Left, y, margin.Right);
        y += 10;

        // 2. Transfer Meta Info
        g.DrawString("سند تحويل مخزني", headerFont, Brushes.Black, new RectangleF(margin.Left, y, margin.Width, 30), PrintHelper.RTLFormatCenter);
        y += 35;

        g.DrawString($"رقم التحويل: {_transfer.TransferNumber}", boldFont, Brushes.Black, new RectangleF(margin.Left, y, margin.Width, 20), PrintHelper.RTLFormatRight);
        y += 20;
        g.DrawString($"التاريخ: {_transfer.Date:yyyy-MM-dd HH:mm}", bodyFont, Brushes.Black, new RectangleF(margin.Left, y, margin.Width, 20), PrintHelper.RTLFormatRight);
        y += 30;

        g.DrawRectangle(Pens.Gray, margin.Left, y, margin.Width, 40);
        g.DrawString($"من مستودع: {_transfer.FromWarehouse}", boldFont, Brushes.Black, new RectangleF(margin.Right - 380, y + 10, 350, 20), PrintHelper.RTLFormatRight);
        g.DrawString($"إلى مستودع: {_transfer.ToWarehouse}", boldFont, Brushes.Black, new RectangleF(margin.Left + 30, y + 10, 350, 20), PrintHelper.RTLFormatRight);
        y += 50;

        // 3. Items Table Header
        PrintHelper.DrawLine(g, Pens.Black, margin.Left, y, margin.Right);
        y += 5;
        
        g.DrawString("كود المنتج", boldFont, Brushes.Black, new RectangleF(margin.Right - 150, y, 150, 20), PrintHelper.RTLFormatRight);
        g.DrawString("اسم المنتج", boldFont, Brushes.Black, new RectangleF(margin.Right - 450, y, 300, 20), PrintHelper.RTLFormatRight);
        g.DrawString("الكمية المحولة", boldFont, Brushes.Black, new RectangleF(margin.Left, y, 150, 20), PrintHelper.RTLFormatLeft);

        y += 25;
        PrintHelper.DrawLine(g, Pens.Gray, margin.Left, y, margin.Right);
        y += 5;

        // 4. Items List
        foreach (var item in _items)
        {
            g.DrawString(item.ProductCode, bodyFont, Brushes.Black, new RectangleF(margin.Right - 150, y, 150, 20), PrintHelper.RTLFormatRight);
            g.DrawString(item.ProductName, bodyFont, Brushes.Black, new RectangleF(margin.Right - 450, y, 300, 20), PrintHelper.RTLFormatRight);
            string qStr = PrintHelper.FormatQuantity(item.Quantity);
            if (item.Mode == 2) qStr += " (جملة)";
            g.DrawString(qStr, boldFont, Brushes.Black, new RectangleF(margin.Left, y, 150, 20), PrintHelper.RTLFormatLeft);
            
            y += 20;
            if (y > margin.Bottom - 100) break;
        }

        y += 20;
        PrintHelper.DrawLine(g, Pens.Black, margin.Left, y, margin.Right);
        y += 10;
        g.DrawString($"ملاحظات: {_transfer.Notes}", bodyFont, Brushes.Black, new RectangleF(margin.Left, y, margin.Width, 40), PrintHelper.RTLFormatRight);
        
        y += 100;
        g.DrawString("توقيع المستلم", boldFont, Brushes.Black, new RectangleF(margin.Left + 50, y, 200, 25), PrintHelper.RTLFormatCenter);
        g.DrawString("توقيع أمين المخزن", boldFont, Brushes.Black, new RectangleF(margin.Right - 250, y, 200, 25), PrintHelper.RTLFormatCenter);
    }
}
