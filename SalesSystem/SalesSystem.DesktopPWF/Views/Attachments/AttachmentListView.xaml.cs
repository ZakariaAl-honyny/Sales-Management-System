using System.Windows;
using SalesSystem.DesktopPWF.ViewModels.Attachments;

namespace SalesSystem.DesktopPWF.Views.Attachments;

/// <summary>
/// Interaction logic for AttachmentListView.xaml
/// </summary>
public partial class AttachmentListView : System.Windows.Controls.UserControl
{
    private readonly AttachmentListViewModel _viewModel;

    public AttachmentListView()
    {
        InitializeComponent();
        _viewModel = new AttachmentListViewModel();
        DataContext = _viewModel;
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.LoadAttachmentsAsync();
    }

    private void SearchTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter)
        {
            _viewModel.SearchCommand.Execute(null);
        }
    }
}
