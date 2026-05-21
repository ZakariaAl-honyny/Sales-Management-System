using System.ComponentModel;
using System.Windows;
using SalesSystem.DesktopPWF.ViewModels.Updates;

namespace SalesSystem.DesktopPWF.Views.Updates;

public partial class UpdateDialog : Window
{
    private bool _allowClose;

    public UpdateDialog(UpdateDialogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        viewModel.CloseDialog = () =>
        {
            _allowClose = true;
            Close();
        };

        MouseLeftButtonDown += (_, _) => DragMove();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        var vm = (UpdateDialogViewModel)DataContext;

        if (vm.IsDownloading && !_allowClose)
        {
            e.Cancel = true;
            return;
        }

        vm.Dispose();
        base.OnClosing(e);
    }
}
