using System.Windows.Controls;
using SalesSystem.DesktopPWF.ViewModels.Purchases;

namespace SalesSystem.DesktopPWF.Views.Purchases;

/// <summary>
/// Interaction logic for PurchaseOrderEditorView.xaml
/// </summary>
public partial class PurchaseOrderEditorView : UserControl
{
    public PurchaseOrderEditorView(PurchaseOrderEditorViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

        // Set header title based on edit mode
        if (viewModel.IsEditMode && viewModel.OrderNo.HasValue)
        {
            HeaderTitle.Text = $"تعديل أمر الشراء رقم {viewModel.OrderNo}";
        }
        else if (viewModel.IsEditMode)
        {
            HeaderTitle.Text = "تعديل أمر الشراء";
        }
        else
        {
            HeaderTitle.Text = "أمر شراء جديد";
        }

        // Update header title if order is loaded
        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(PurchaseOrderEditorViewModel.OrderNo) && viewModel.OrderNo.HasValue)
            {
                HeaderTitle.Text = $"تعديل أمر الشراء رقم {viewModel.OrderNo}";
            }
        };
    }

    /// <summary>
    /// Parameterless constructor for designer support.
    /// </summary>
    public PurchaseOrderEditorView()
    {
        InitializeComponent();
        HeaderTitle.Text = "أمر شراء جديد";
    }
}
