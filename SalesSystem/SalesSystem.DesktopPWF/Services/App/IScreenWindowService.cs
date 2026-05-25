using System.Windows;
using SalesSystem.DesktopPWF.ViewModels;

namespace SalesSystem.DesktopPWF.Services.App;

public interface IScreenWindowService
{
    /// <summary>
    /// Opens a ViewModel in a non-modal generic host window with cascading positioning.
    /// Convenience wrapper around OpenScreen with IsModal=false.
    /// </summary>
    void OpenNonModal(ViewModelBase viewModel, string title, double width = 800, double height = 600);

    void OpenWindow<TWindow>(object viewModel, ScreenWindowOptions? options = null)
        where TWindow : Window, new();

    void OpenWindow(Window window, ScreenWindowOptions? options = null);

    void OpenScreen(object viewModel, ScreenWindowOptions? options = null);

    void OpenScreen<TViewModel>(ScreenWindowOptions? options = null)
        where TViewModel : class;

    void CloseAll();

    IReadOnlyList<Window> OpenWindows { get; }

    event Action<Window>? WindowClosed;
}
