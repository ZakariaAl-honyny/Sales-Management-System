using System.Windows.Controls;

namespace SalesSystem.DesktopPWF.Views.Reports;

/// <summary>
/// Interaction logic for ExpiredProductsReportView.xaml
/// </summary>
public partial class ExpiredProductsReportView : Page
{
    public ExpiredProductsReportView()
    {
        InitializeComponent();
        DataContext = App.GetService<ViewModels.Reports.ExpiredProductsReportViewModel>();
    }
}
