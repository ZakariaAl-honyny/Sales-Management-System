using System.Windows.Forms;

namespace SalesSystem.Desktop.Controls;

public abstract class BaseModuleControl : UserControl
{
    protected readonly List<IDisposable> _subscriptions = new();

    protected abstract void RegisterSubscriptions();

    protected void AddSubscription(IDisposable subscription)
    {
        _subscriptions.Add(subscription);
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        RegisterSubscriptions();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var sub in _subscriptions)
            {
                sub.Dispose();
            }
            _subscriptions.Clear();
        }
        base.Dispose(disposing);
    }
}
