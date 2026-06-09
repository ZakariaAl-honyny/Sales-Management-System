using System.Windows.Controls;
using SalesSystem.DesktopPWF.ViewModels.Sales;

namespace SalesSystem.DesktopPWF.Views.Sales;

/// <summary>
/// Interaction logic for SalesQuotationEditorView.xaml
/// </summary>
public partial class SalesQuotationEditorView : UserControl
{
    public SalesQuotationEditorView(SalesQuotationEditorViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

        // Set header title based on edit mode
        if (viewModel.IsEditMode && !string.IsNullOrEmpty(viewModel.QuotationNo))
        {
            HeaderTitle.Text = $"تعديل عرض السعر {viewModel.QuotationNo}";
        }
        else if (viewModel.IsEditMode)
        {
            HeaderTitle.Text = "تعديل عرض السعر";
        }
        else
        {
            HeaderTitle.Text = "عرض سعر جديد";
        }

        // Update header title if quotation is loaded
        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(SalesQuotationEditorViewModel.QuotationNo) && !string.IsNullOrEmpty(viewModel.QuotationNo))
            {
                HeaderTitle.Text = $"تعديل عرض السعر {viewModel.QuotationNo}";
            }
        };
    }

    /// <summary>
    /// Parameterless constructor for designer support.
    /// </summary>
    public SalesQuotationEditorView()
    {
        InitializeComponent();
        HeaderTitle.Text = "عرض سعر جديد";
    }
}
