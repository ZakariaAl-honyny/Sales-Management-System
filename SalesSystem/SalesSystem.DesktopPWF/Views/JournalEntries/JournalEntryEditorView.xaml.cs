using System.Windows;
using System.Windows.Controls;
using SalesSystem.DesktopPWF.ViewModels.JournalEntries;

namespace SalesSystem.DesktopPWF.Views.JournalEntries;

/// <summary>
/// Interaction logic for JournalEntryEditorView.xaml
/// Provides the UI for creating manual journal entries with account selection and debit/credit lines.
/// </summary>
public partial class JournalEntryEditorView : UserControl
{
    public JournalEntryEditorView()
    {
        InitializeComponent();

        Loaded += (s, e) =>
        {
            if (DataContext == null)
                DataContext = App.GetService<JournalEntryEditorViewModel>();
        };

        Unloaded += (s, e) =>
        {
            if (DataContext is JournalEntryEditorViewModel vm)
                vm.Cleanup();
        };
    }
}
