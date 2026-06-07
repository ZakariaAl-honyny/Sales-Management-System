using System.Windows.Controls;
using SalesSystem.DesktopPWF.Helpers;
using SalesSystem.DesktopPWF.ViewModels.Users;

namespace SalesSystem.DesktopPWF.Views.Users;

public partial class UserEditorView : UserControl
{
    public UserEditorView()
    {
        InitializeComponent();
    }

    public UserEditorView(UserEditorViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }
}


