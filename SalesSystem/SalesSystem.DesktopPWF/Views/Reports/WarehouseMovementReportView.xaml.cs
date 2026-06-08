using System.Windows.Controls;

namespace SalesSystem.DesktopPWF.Views.Reports;

/// <summary>
/// Interaction logic for WarehouseMovementReportView.xaml
/// </summary>
public partial class WarehouseMovementReportView : UserControl
{
    public WarehouseMovementReportView()
    {
        InitializeComponent();
        DataContext = App.GetService<ViewModels.Reports.WarehouseMovementReportViewModel>();
    }
}
