using System.Net.Http;
using System.Windows.Controls;

namespace SalesSystem.DesktopPWF.Services.App;

/// <summary>
/// Navigation service for WPF (frame-based navigation)
/// </summary>
public interface INavigationService
{
    Frame? MainFrame {
get;
set;
}
    void Navigate(Type viewType);
    void Navigate(Type viewType, object parameter);
    bool GoBack();
    event Action<Type, object?>? Navigated;
}

public class NavigationService : INavigationService
{
    private Frame? _mainFrame;

    public Frame? MainFrame
    {
        get => _mainFrame;
        set
        {
            _mainFrame = value;
            if (_mainFrame != null)
            {
                _mainFrame.Navigated += OnFrameNavigated;
            }
        }
    }

    public event Action<Type, object?>? Navigated;

    public void Navigate(Type viewType)
    {
        Navigate(viewType, null);
    }

    public void Navigate(Type viewType, object? parameter)
    {
        if (_mainFrame == null) return;

        var view = Activator.CreateInstance(viewType);
        if (view is Page page)
        {
            if (parameter != null)
            {
                page.DataContext = parameter;
            }
            _mainFrame.Navigate(page);
        }
    }

    public bool GoBack()
    {
        if (_mainFrame?.CanGoBack == true)
        {
            _mainFrame.GoBack();
            return true;
        }
        return false;
    }

    private void OnFrameNavigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
    {
        if (e.Content is Page page)
        {
            Navigated?.Invoke(page.GetType(), page.DataContext);
        }
    }
}

