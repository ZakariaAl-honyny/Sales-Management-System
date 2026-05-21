using System.Windows;
using SalesSystem.DesktopPWF.ViewModels.Returns;

namespace SalesSystem.DesktopPWF.Views.Returns;

public partial class SalesReturnEditorView : Window
{
    public SalesReturnEditorView()
    {
        InitializeComponent();
    }

    public SalesReturnEditorView(SalesReturnEditorViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }
}

