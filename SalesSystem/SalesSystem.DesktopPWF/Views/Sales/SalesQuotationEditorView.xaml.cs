using System;
using System.Windows;
using SalesSystem.DesktopPWF.Helpers;
using SalesSystem.DesktopPWF.ViewModels.Sales;

namespace SalesSystem.DesktopPWF.Views.Sales;

/// <summary>
/// Interaction logic for SalesQuotationEditorView.xaml
/// </summary>
public partial class SalesQuotationEditorView : Window
{
    public SalesQuotationEditorView()
    {
        InitializeComponent();
    }

    public SalesQuotationEditorView(SalesQuotationEditorViewModel viewModel) : this()
    {
        DataContext = viewModel;

        viewModel.CloseRequested += () => Dispatcher.InvokeAsync(() => Close());
        viewModel.FocusFirstInvalidFieldRequested += () =>
        {
            Dispatcher.InvokeAsync(() =>
            {
                (Helpers.ValidationFocusBehavior.FindFirstInvalid(this) ??
                Helpers.ValidationFocusBehavior.FindFirstEmptyRequired(this))?.Focus();
            });
        };
    }

    public bool IsReadOnly
    {
        get => !IsEnabled;
        set
        {
            if (value)
            {
                IsEnabled = false;
                Title = "عرض عرض سعر";
            }
        }
    }
}
