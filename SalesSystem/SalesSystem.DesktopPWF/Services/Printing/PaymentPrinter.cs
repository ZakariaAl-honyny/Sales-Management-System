using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using SalesSystem.DesktopPWF.Models.Printing;
using SalesSystem.DesktopPWF.Helpers;
using SalesSystem.DesktopPWF.Services.App;

namespace SalesSystem.DesktopPWF.Services.Printing;

public class PaymentPrinter : IPaymentPrinter
{
    private PaymentPrintDto? _payment;
    private StoreInfoPrintDto? _storeInfo;

    public void PrintPreview(PaymentPrintDto payment, StoreInfoPrintDto storeInfo)
    {
        _payment = payment;
        _storeInfo = storeInfo;

        using var pd = new PrintDocument();
        pd.PrintPage += Pd_PrintPage;
        
        using var preview = new PrintPreviewDialog();
        preview.Document = pd;
        preview.WindowState = FormWindowState.Maximized;
        preview.ShowDialog();
    }

    public void Print(PaymentPrintDto payment, StoreInfoPrintDto storeInfo, string? printerName = null)
    {
        _payment = payment;
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
        if (_payment == null || _storeInfo == null) return;

        var g = e.Graphics!;
        var margin = e.MarginBounds;
        float y = margin.Top;

        // Fonts
        var titleFont = new Font("Segoe UI", 18, FontStyle.Bold);
        var headerFont = new Font("Segoe UI", 14, FontStyle.Bold);
        var bodyFont = new Font("Segoe UI", 12);
        var boldFont = new Font("Segoe UI", 12, FontStyle.Bold);

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
        y += 20;

        // 2. Document Title
        g.DrawString(_payment.TypeName, titleFont, Brushes.Black, new RectangleF(margin.Left, y, margin.Width, 40), PrintHelper.RTLFormatCenter);
        y += 60;

        // 3. Payment Details Card
        float startY = y;
        g.DrawRectangle(Pens.Black, margin.Left, y, margin.Width, 250);
        y += 10;

        g.DrawString($"رقم السند: {_payment.PaymentNumber}", boldFont, Brushes.Black, new RectangleF(margin.Left + 20, y, margin.Width - 40, 25), PrintHelper.RTLFormatRight);
        y += 35;
        g.DrawString($"التاريخ: {_payment.Date:yyyy-MM-dd}", bodyFont, Brushes.Black, new RectangleF(margin.Left + 20, y, margin.Width - 40, 25), PrintHelper.RTLFormatRight);
        y += 35;
        
        PrintHelper.DrawLine(g, Pens.LightGray, margin.Left + 10, y, margin.Right - 10);
        y += 10;

        g.DrawString($"يصرف لـ / استلمنا من: {_payment.Name}", boldFont, Brushes.Black, new RectangleF(margin.Left + 20, y, margin.Width - 40, 30), PrintHelper.RTLFormatRight);
        y += 40;

        g.DrawString($"مبلغ وقدره: {PrintHelper.FormatCurrency(_payment.Amount)}", titleFont, Brushes.Black, new RectangleF(margin.Left + 20, y, margin.Width - 40, 40), PrintHelper.RTLFormatRight);
        y += 45;

        g.DrawString($"فقط وقدره: {_payment.AmountWord}", bodyFont, Brushes.Black, new RectangleF(margin.Left + 20, y, margin.Width - 40, 30), PrintHelper.RTLFormatRight);
        y += 40;

        // 4. Notes & Method
        y = startY + 260;
        g.DrawString($"طريقة الدفع: {_payment.PaymentMethod}", bodyFont, Brushes.Black, new RectangleF(margin.Left, y, margin.Width, 25), PrintHelper.RTLFormatRight);
        y += 30;
        g.DrawString($"وذلك عن: {_payment.Notes}", bodyFont, Brushes.Black, new RectangleF(margin.Left, y, margin.Width, 60), PrintHelper.RTLFormatRight);
        
        y += 80;

        // 5. Signatures
        float sigY = y;
        g.DrawString("توقيع المستلم", boldFont, Brushes.Black, new RectangleF(margin.Left + 50, sigY, 200, 25), PrintHelper.RTLFormatCenter);
        g.DrawString("توقيع المحاسب", boldFont, Brushes.Black, new RectangleF(margin.Right - 250, sigY, 200, 25), PrintHelper.RTLFormatCenter);
        
        y += 60;
        PrintHelper.DrawLine(g, Pens.Black, margin.Left + 50, y, margin.Left + 250);
        PrintHelper.DrawLine(g, Pens.Black, margin.Right - 250, y, margin.Right - 50);
        
        y += 100;
        g.DrawString("شكراً لتعاملكم معنا", headerFont, Brushes.Black, new RectangleF(margin.Left, y, margin.Width, 30), PrintHelper.RTLFormatCenter);
    }
}
