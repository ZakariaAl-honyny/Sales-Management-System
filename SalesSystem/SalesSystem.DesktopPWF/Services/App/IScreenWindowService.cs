using System.Windows;

namespace SalesSystem.DesktopPWF.Services.App;

public interface IScreenWindowService
{
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
