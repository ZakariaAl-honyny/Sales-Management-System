using System.Windows.Controls;

namespace SalesSystem.DesktopPWF.Views.Reports;

public partial class ReportsView : Page
{
    public ReportsView()
    {
        InitializeComponent();
        DataContext = App.GetService<ViewModels.ReportsViewModel>();
    }
}

