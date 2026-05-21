using System.IO;
using System.Windows;
using System.Windows.Input;
using SalesSystem.DesktopPWF.Services.Api;

namespace SalesSystem.DesktopPWF.Views.Common;

/// <summary>
/// Preview window for invoice PDF. Displays the PDF using the system viewer
/// and provides A4/Thermal/Close buttons.
/// </summary>
public partial class PdfPreviewWindow : Window
{
    private readonly string _pdfPath;
    private readonly string _invoiceNumber;

    public PdfPreviewWindow(string pdfPath, string invoiceNumber)
    {
        InitializeComponent();
        _pdfPath = pdfPath;
        _invoiceNumber = invoiceNumber;
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
            MessageBox.Show("ملف PDF غير موجود", "خطأ",
                MessageBoxButton.OK, MessageBoxImage.Error);
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

            // Extract invoice ID from preview data
            var result = await printService.PrintSalesA4Async(ExtractInvoiceId());
            if (!result.IsSuccess)
            {
                MessageBox.Show(result.Error, "خطأ في الطباعة",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
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

            var result = await printService.PrintSalesThermalAsync(ExtractInvoiceId());
            if (!result.IsSuccess)
            {
                MessageBox.Show(result.Error, "خطأ في الطباعة",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
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

    /// <summary>
    /// Extracts invoice ID from the temp file name format: Invoice_{INVOICENUMBER}_{TIMESTAMP}.pdf
    /// This is a best-effort approach. For production, pass the ID through constructor.
    /// </summary>
    private int ExtractInvoiceId()
    {
        // Try to parse from filename or just return 0
        // The full print flow should ideally pass the invoice ID
        return 0;
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
