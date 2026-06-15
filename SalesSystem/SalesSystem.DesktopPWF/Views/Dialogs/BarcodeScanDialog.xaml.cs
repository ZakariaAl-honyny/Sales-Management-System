using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SalesSystem.DesktopPWF.Views.Dialogs;

/// <summary>
/// A continuous barcode scan dialog that auto-adds products to the invoice.
/// Each scanned barcode fires the OnBarcodeScanned event.
/// </summary>
public partial class BarcodeScanDialog : Window
{
    /// <summary>
    /// Fires when a barcode is scanned or entered. Receives the barcode string.
    /// </summary>
    public event Action<string>? OnBarcodeScanned;

    public BarcodeScanDialog()
    {
        InitializeComponent();
        PositionOverOwner();

        // Auto-focus the barcode input
        Loaded += (s, e) =>
        {
            BarcodeInput.Focus();
            BarcodeInput.SelectAll();
        };
    }

    private void PositionOverOwner()
    {
        var mainWindow = System.Windows.Application.Current.MainWindow;
        if (mainWindow != null && mainWindow != this)
        {
            Owner = mainWindow;
            Width = Owner.ActualWidth;
            Height = Owner.ActualHeight;
            Left = Owner.Left;
            Top = Owner.Top;
        }
        else
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    /// <summary>
    /// Override OnKeyDown to capture barcode scanner input (Enter key).
    /// Barcode scanners typically send the barcode string followed by Enter.
    /// </summary>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Enter && BarcodeInput.IsFocused)
        {
            var barcode = BarcodeInput.Text?.Trim();
            if (!string.IsNullOrWhiteSpace(barcode))
            {
                OnBarcodeScanned?.Invoke(barcode);
                BarcodeInput.Clear();
                BarcodeInput.Focus();
            }
            e.Handled = true;
            return;
        }

        // Escape closes the dialog
        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }
}
