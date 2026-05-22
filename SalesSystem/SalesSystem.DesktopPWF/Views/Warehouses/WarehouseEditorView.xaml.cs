using System.Windows;
using SalesSystem.DesktopPWF.Helpers;

namespace SalesSystem.DesktopPWF.Views;

/// <summary>
/// Interaction logic for WarehouseEditorView.xaml
/// </summary>
public partial class WarehouseEditorView : Window
{
    public WarehouseEditorView()
    {
        InitializeComponent();
    }

    public WarehouseEditorView(ViewModels.WarehouseEditorViewModel viewModel) : this()
    {
        DataContext = viewModel;
        viewModel.CloseRequested += () => Close();
        viewModel.FocusFirstInvalidFieldRequested += () =>
        {
            Dispatcher.InvokeAsync(() =>
            {
                ValidationFocusBehavior.FindFirstInvalid(this)?.Focus();
            });
        };
    }
}

