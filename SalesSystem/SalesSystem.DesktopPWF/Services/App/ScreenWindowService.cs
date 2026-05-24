using System.Reflection;
using System.Text;
using System.Windows;
using Serilog;
using SalesSystem.DesktopPWF.ViewModels;

namespace SalesSystem.DesktopPWF.Services.App;

public class ScreenWindowService : IScreenWindowService
{
    private readonly List<WeakReference<Window>> _openWindows = new();
    private readonly object _lock = new();

    public event Action<Window>? WindowClosed;

    public IReadOnlyList<Window> OpenWindows
    {
        get
        {
            lock (_lock)
            {
                return _openWindows
                    .Select(wr => wr.TryGetTarget(out var w) ? w : null)
                    .Where(w => w != null)
                    .ToList()!;
            }
        }
    }

    public void OpenWindow<TWindow>(object viewModel, ScreenWindowOptions? options = null)
        where TWindow : Window, new()
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            try
            {
                var window = new TWindow();
                ConfigureAndShowWindow(window, viewModel, options);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to open window of type {WindowType}", typeof(TWindow).FullName);
            }
        });
    }

    public void OpenScreen(object viewModel, ScreenWindowOptions? options = null)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            try
            {
                var viewName = viewModel.GetType().FullName!.Replace("ViewModel", "View");
                var viewType = Type.GetType(viewName);
                if (viewType == null)
                {
                    Log.Error("View type not found for ViewModel {ViewModelType}", viewModel.GetType().FullName);
                    return;
                }

                if (Activator.CreateInstance(viewType) is not Window window)
                {
                    Log.Error("Failed to create window from type {ViewType}", viewType.FullName);
                    return;
                }

                ConfigureAndShowWindow(window, viewModel, options);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to open screen for ViewModel {ViewModelType}", viewModel.GetType().FullName);
            }
        });
    }

    public void OpenScreen<TViewModel>(ScreenWindowOptions? options = null)
        where TViewModel : class
    {
        try
        {
            var viewModel = SalesSystem.DesktopPWF.App.GetService<TViewModel>();
            OpenScreen(viewModel, options);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to resolve ViewModel of type {ViewModelType}", typeof(TViewModel).FullName);
        }
    }

    public void OpenWindow(Window window, ScreenWindowOptions? options = null)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            try
            {
                options ??= new ScreenWindowOptions();

                if (!string.IsNullOrEmpty(options.Title))
                    window.Title = options.Title;
                window.Width = options.Width;
                window.Height = options.Height;
                window.ResizeMode = options.CanResize ? ResizeMode.CanResize : ResizeMode.NoResize;
                window.WindowStyle = options.Style;
                window.WindowStartupLocation = options.StartupLocation;

                if (options.Left.HasValue)
                    window.Left = options.Left.Value;
                if (options.Top.HasValue)
                    window.Top = options.Top.Value;

                if (!options.Left.HasValue || !options.Top.HasValue)
                    ApplyCascadePositioning(window);

                var mainWindow = System.Windows.Application.Current.MainWindow;
                if (mainWindow != null && mainWindow != window && window.Owner == null)
                    window.Owner = mainWindow;

                TrackWindow(window);

                if (window.DataContext is ViewModelBase vmBase)
                {
                    Action closeHandler = () => window.Close();
                    vmBase.CloseRequested += closeHandler;

                    window.Closed += (_, _) =>
                    {
                        vmBase.CloseRequested -= closeHandler;
                        vmBase.Cleanup();
                        options.OnClosed?.Invoke(window.DataContext);
                        UntrackWindow(window);
                        WindowClosed?.Invoke(window);
                    };
                }
                else if (window.DataContext != null)
                {
                    var vm = window.DataContext;
                    window.Closed += (_, _) =>
                    {
                        var cleanupMethod = vm.GetType().GetMethod("Cleanup", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
                        if (cleanupMethod != null)
                            cleanupMethod.Invoke(vm, null);

                        options.OnClosed?.Invoke(vm);
                        UntrackWindow(window);
                        WindowClosed?.Invoke(window);
                    };
                }
                else
                {
                    window.Closed += (_, _) =>
                    {
                        options.OnClosed?.Invoke(null);
                        UntrackWindow(window);
                        WindowClosed?.Invoke(window);
                    };
                }

                window.Show();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to open window {WindowTitle}", window.Title);
            }
        });
    }

    public void CloseAll()
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            List<Window> windowsToClose;
            lock (_lock)
            {
                windowsToClose = _openWindows
                    .Select(wr => wr.TryGetTarget(out var w) ? w : null)
                    .Where(w => w != null)
                    .ToList()!;
            }

            foreach (var window in windowsToClose)
            {
                try
                {
                    window.Close();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to close window");
                }
            }
        });
    }

    private void ConfigureAndShowWindow(Window window, object viewModel, ScreenWindowOptions? options)
    {
        window.DataContext = viewModel;

        options ??= new ScreenWindowOptions();

        window.Title = options.Title ?? GetAutoTitle(viewModel.GetType());
        window.Width = options.Width;
        window.Height = options.Height;
        window.ResizeMode = options.CanResize ? ResizeMode.CanResize : ResizeMode.NoResize;
        window.WindowStyle = options.Style;
        window.WindowStartupLocation = options.StartupLocation;

        if (options.Left.HasValue)
            window.Left = options.Left.Value;
        if (options.Top.HasValue)
            window.Top = options.Top.Value;

        if (!options.Left.HasValue || !options.Top.HasValue)
            ApplyCascadePositioning(window);

        var mainWindow = System.Windows.Application.Current.MainWindow;
        if (mainWindow != null && mainWindow != window && window.Owner == null)
            window.Owner = mainWindow;

        TrackWindow(window);

        if (viewModel is ViewModelBase vmBase)
        {
            Action closeHandler = () => window.Close();
            vmBase.CloseRequested += closeHandler;

            window.Closed += (_, _) =>
            {
                vmBase.CloseRequested -= closeHandler;
                vmBase.Cleanup();
                options.OnClosed?.Invoke(viewModel);
                UntrackWindow(window);
                WindowClosed?.Invoke(window);
            };
        }
        else
        {
            window.Closed += (_, _) =>
            {
                var cleanupMethod = viewModel.GetType().GetMethod("Cleanup", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
                if (cleanupMethod != null)
                    cleanupMethod.Invoke(viewModel, null);

                options.OnClosed?.Invoke(viewModel);
                UntrackWindow(window);
                WindowClosed?.Invoke(window);
            };
        }

        if (options.IsModal)
            window.ShowDialog();
        else
            window.Show();
    }

    private void ApplyCascadePositioning(Window window)
    {
        var mainWindow = System.Windows.Application.Current.MainWindow;
        if (mainWindow == null)
            return;

        int count;
        lock (_lock)
        {
            count = _openWindows.Count(wr => wr.TryGetTarget(out _));
        }

        var offset = 30 * (count % 10);
        window.Left = mainWindow.Left + offset;
        window.Top = mainWindow.Top + offset;

        var workArea = SystemParameters.WorkArea;
        if (window.Left + window.Width > workArea.Width)
            window.Left = workArea.Width - window.Width;
        if (window.Top + window.Height > workArea.Height)
            window.Top = workArea.Height - window.Height;
    }

    private void TrackWindow(Window window)
    {
        lock (_lock)
        {
            _openWindows.Add(new WeakReference<Window>(window));
        }
    }

    private void UntrackWindow(Window window)
    {
        lock (_lock)
        {
            _openWindows.RemoveAll(wr =>
            {
                if (wr.TryGetTarget(out var w))
                    return w == window;
                return true;
            });
        }
    }

    private static string GetAutoTitle(Type viewModelType)
    {
        var typeName = viewModelType.Name;

        var titles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["SalesInvoiceEditorViewModel"] = "فاتورة بيع",
            ["PurchaseInvoiceEditorViewModel"] = "فاتورة شراء",
            ["SalesReturnEditorViewModel"] = "مرتجع مبيعات",
            ["PurchaseReturnEditorViewModel"] = "مرتجع مشتريات",
            ["CustomerPaymentEditorViewModel"] = "سداد عميل",
            ["SupplierPaymentEditorViewModel"] = "سداد مورد",
            ["StockTransferEditorViewModel"] = "نقل مخزون",
            ["ProductEditorViewModel"] = "منتج",
            ["CustomerEditorViewModel"] = "عميل",
            ["SupplierEditorViewModel"] = "مورد",
        };

        if (titles.TryGetValue(typeName, out var title))
            return title;

        var name = typeName
            .Replace("ViewModel", "")
            .Replace("Editor", "")
            .Replace("List", "");

        if (string.IsNullOrWhiteSpace(name))
            return typeName;

        var result = new StringBuilder();
        for (var i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i]) && !char.IsUpper(name[i - 1]))
                result.Append(' ');
            result.Append(name[i]);
        }

        return result.ToString().Trim();
    }
}
