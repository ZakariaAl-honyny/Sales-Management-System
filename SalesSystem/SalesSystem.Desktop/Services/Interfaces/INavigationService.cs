using System.Windows.Forms;

namespace SalesSystem.Desktop.Services.Interfaces;

public interface INavigationService
{
    void SetContentPanel(Panel panel);
    void NavigateTo<TControl>() where TControl : UserControl;
    Type? CurrentScreen { get; }
}

