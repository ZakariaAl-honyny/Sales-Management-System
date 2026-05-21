using System.Windows;
using SalesSystem.DesktopPWF.ViewModels.Categories;

namespace SalesSystem.DesktopPWF.Views.Categories;

public partial class CategoryEditorView : Window
{
    public CategoryEditorView()
    {
        InitializeComponent();
    }

    public CategoryEditorView(CategoryEditorViewModel viewModel) : this()
    {
        DataContext = viewModel;
        viewModel.CloseRequested += () => Close();
    }
}

