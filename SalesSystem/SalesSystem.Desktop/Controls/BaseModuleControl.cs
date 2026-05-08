namespace SalesSystem.Desktop.Controls;

public abstract class BaseModuleControl : UserControl
{
    private readonly List<IDisposable> _subscriptions = new();

    protected abstract void RegisterSubscriptions();

    protected void AddSubscription(IDisposable subscription)
        => _subscriptions.Add(subscription);

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        RegisterSubscriptions();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var s in _subscriptions) s.Dispose();
            _subscriptions.Clear();
        }
        base.Dispose(disposing);
    }
}
