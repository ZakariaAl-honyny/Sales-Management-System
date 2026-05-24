using System.IO;
using System.Windows;
using System.Windows.Input;
using SalesSystem.Contracts.Common;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;

namespace SalesSystem.DesktopPWF.Views.Common;

/// <summary>
/// Preview window for invoice PDF. Displays the PDF using the system viewer
/// and provides A4/Thermal/Close buttons.
/// </summary>
public partial class PdfPreviewWindow : Window
{
    private readonly string _pdfPath;
    private readonly string _invoiceNumber;
    private readonly int _invoiceId;
    private readonly bool _isPurchase;
    private IDialogService? _dialogService;
    private IDialogService DialogService => _dialogService ??= App.GetService<IDialogService>();

    public PdfPreviewWindow(string pdfPath, string invoiceNumber, int invoiceId, bool isPurchase = false)
    {
        InitializeComponent();
        _pdfPath = pdfPath;
        _invoiceNumber = invoiceNumber;
        _invoiceId = invoiceId;
        _isPurchase = isPurchase;
        InvoiceNumberText.Text = $"فاتورة: {invoiceNumber}";

        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (File.Exists(_pdfPath))
        {
            // Navigate to the PDF file
            PdfViewer.Navigate(new Uri(_pdfPath));
        }
        else
        {
            _ = DialogService.ShowErrorAsync("خطأ في الطباعة", "ملف PDF غير موجود");
            Close();
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        // Clean up temp file
        TryDeleteFile(_pdfPath);
    }

    private async void PrintA4_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Mouse.OverrideCursor = Cursors.Wait;
            var printService = App.GetService<IPrintApiService>();

            Result result;
            if (_isPurchase)
                result = await printService.PrintPurchaseA4Async(_invoiceId);
            else
                result = await printService.PrintSalesA4Async(_invoiceId);

            if (!result.IsSuccess)
            {
                _ = DialogService.ShowErrorAsync("خطأ في الطباعة", result.Error ?? "حدث خطأ أثناء الطباعة");
            }
            else
            {
                _ = DialogService.ShowSuccessAsync("نجاح", "تم إرسال الفاتورة إلى الطابعة بنجاح");
            }
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }

    private async void PrintThermal_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Mouse.OverrideCursor = Cursors.Wait;
            var printService = App.GetService<IPrintApiService>();

            Result result;
            if (_isPurchase)
                result = await printService.PrintPurchaseThermalAsync(_invoiceId);
            else
                result = await printService.PrintSalesThermalAsync(_invoiceId);

            if (!result.IsSuccess)
            {
                _ = DialogService.ShowErrorAsync("خطأ في الطباعة", result.Error ?? "حدث خطأ أثناء الطباعة");
            }
            else
            {
                _ = DialogService.ShowSuccessAsync("نجاح", "تم إرسال الفاتورة إلى الطابعة بنجاح");
            }
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // Best-effort cleanup
        }
    }
}
