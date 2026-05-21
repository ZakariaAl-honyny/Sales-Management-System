using Microsoft.Extensions.DependencyInjection;
using SalesSystem.Desktop.Services.Interfaces;

namespace SalesSystem.Desktop.Services;

public sealed class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private Panel? _contentPanel;
    private UserControl? _currentControl;

    public Type? CurrentScreen { get; private set; }

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void SetContentPanel(Panel panel)
    {
        _contentPanel = panel;
    }

    public void NavigateTo<TControl>() where TControl : UserControl
    {
        if (_contentPanel == null) return;

        if (_contentPanel.InvokeRequired)
        {
            _contentPanel.Invoke(() => NavigateToInternal<TControl>());
        }
        else
        {
            NavigateToInternal<TControl>();
        }
    }

    private void NavigateToInternal<TControl>() where TControl : UserControl
    {
        if (_contentPanel == null) return;

        // Dispose previous
        if (_currentControl != null)
        {
            _currentControl.Dispose();
            _contentPanel.Controls.Clear();
        }

        // Resolve new
        var control = _serviceProvider.GetRequiredService<TControl>();
        control.Dock = DockStyle.Fill;

        _currentControl = control;
        CurrentScreen = typeof(TControl);

        _contentPanel.Controls.Add(control);
    }
}

