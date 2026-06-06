using System.Windows.Controls;
using SalesSystem.DesktopPWF.Helpers;
using SalesSystem.DesktopPWF.ViewModels.Currencies;

namespace SalesSystem.DesktopPWF.Views.Currencies;

public partial class CurrencyEditorView : UserControl
{
    public CurrencyEditorView()
    {
        InitializeComponent();
    }

    public CurrencyEditorView(CurrencyEditorViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }
}
