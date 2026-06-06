using System.Windows.Controls;

namespace SalesSystem.DesktopPWF.Views.Reports;

/// <summary>
/// Interaction logic for VatReportView.xaml
/// </summary>
public partial class VatReportView : UserControl
{
    public VatReportView()
    {
        InitializeComponent();
        DataContext = App.GetService<ViewModels.Reports.VatReportViewModel>();
    }
}
