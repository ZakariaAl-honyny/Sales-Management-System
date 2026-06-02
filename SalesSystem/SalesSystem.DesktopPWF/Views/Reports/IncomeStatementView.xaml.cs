using System.Windows.Controls;

namespace SalesSystem.DesktopPWF.Views.Reports;

/// <summary>
/// Interaction logic for IncomeStatementView.xaml
/// </summary>
public partial class IncomeStatementView : UserControl
{
    public IncomeStatementView()
    {
        InitializeComponent();
        DataContext = App.GetService<ViewModels.Reports.IncomeStatementViewModel>();
    }
}
