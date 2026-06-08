using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SalesSystem.Application.Updates;
using SalesSystem.Application.Updates.Models;
using SalesSystem.DesktopPWF.ViewModels;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.Contracts.Enums;
using SalesSystem.DesktopPWF.ViewModels.Inventory;
using SalesSystem.DesktopPWF.ViewModels.Updates;
using SalesSystem.DesktopPWF.Views.Updates;
using SalesSystem.DesktopPWF.ViewModels.Products;
using SalesSystem.DesktopPWF.ViewModels.Customers;
using SalesSystem.DesktopPWF.ViewModels.Suppliers;
using SalesSystem.DesktopPWF.ViewModels.Purchases;
using SalesSystem.DesktopPWF.ViewModels.Returns;
using SalesSystem.DesktopPWF.ViewModels.Payments;
using SalesSystem.DesktopPWF.ViewModels.CashBoxes;
using SalesSystem.DesktopPWF.ViewModels.Reports;
using SalesSystem.DesktopPWF.ViewModels.Transfers;
using SalesSystem.DesktopPWF.ViewModels.Sales;
using SalesSystem.DesktopPWF.ViewModels.Users;
using SalesSystem.DesktopPWF.ViewModels.Taxes;
using SalesSystem.DesktopPWF.Messaging.Messages;

namespace SalesSystem.DesktopPWF;

public partial class MainWindow : Window
{
    private readonly ISessionService _session;
    private readonly IDialogService _dialogService;
    private readonly MainViewModel _mainViewModel;
    private bool _isLoggingOut;

    public MainWindow()
    {
        InitializeComponent();
        _session = App.GetService<ISessionService>();
        _dialogService = App.GetService<IDialogService>();
        _mainViewModel = App.GetService<MainViewModel>();

        // Set DataContext to MainViewModel for command bindings and ContentControl binding
        DataContext = _mainViewModel;

        UpdateUserInfo();

        // Navigate to dashboard on startup
        _mainViewModel.NavigateTo<DashboardViewModel>();

        Closed += (s, e) =>
        {
            if (!_isLoggingOut)
                System.Windows.Application.Current.Shutdown();
        };

        // Listen for shutdown requests (e.g., after backup restore)
        var eventBus = App.GetService<IEventBus>();
        eventBus.Subscribe<ApplicationShutdownMessage>(_ =>
        {
            System.Windows.Application.Current.Dispatcher.InvokeAsync(
                () => System.Windows.Application.Current.Shutdown());
        });
    }

    public void UpdateUserInfo()
    {
        TxtUserName.Text = _session.GetUserName() ?? "مستخدم";
        var role = _session.GetUserRole();
        TxtUserRole.Text = GetRoleDisplayName(role);

        // Update role badge color
        var badgeColor = role switch
        {
            UserRole.Admin => (System.Windows.Media.Color?)System.Windows.Media.ColorConverter.ConvertFromString("#10B981"), // Green
            UserRole.Manager => (System.Windows.Media.Color?)System.Windows.Media.ColorConverter.ConvertFromString("#3B82F6"), // Blue
            UserRole.Cashier => (System.Windows.Media.Color?)System.Windows.Media.ColorConverter.ConvertFromString("#F59E0B"), // Amber
            _ => (System.Windows.Media.Color?)System.Windows.Media.ColorConverter.ConvertFromString("#6B7280")
        };
        if (badgeColor.HasValue)
        {
            RoleBadge.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(25, badgeColor.Value.R, badgeColor.Value.G, badgeColor.Value.B));
            TxtUserRole.Foreground = new SolidColorBrush(badgeColor.Value);
        }

        CurrentDateText.Text = DateTime.Now.ToString("dddd, yyyy/MM/dd HH:mm");
    }

    private string GetRoleDisplayName(UserRole? role)
    {
        return role switch
        {
            UserRole.Admin => "مدير النظام",
            UserRole.Manager => "مدير فرع",
            UserRole.Cashier => "كاشير",
            _ => "غير معروف"
        };
    }

    private void BtnChangePassword_Click(object sender, RoutedEventArgs e)
    {
        var vm = new PasswordChangeViewModel(
            App.GetService<IAuthApiService>(),
            _dialogService,
            App.GetService<Services.App.Toast.IToastNotificationService>());
        var screenService = App.GetService<IScreenWindowService>();
        screenService.OpenScreen(vm, new ScreenWindowOptions
        {
            Title = "تغيير كلمة المرور",
            Width = 450,
            Height = 300,
            IsModal = true
        });
    }

    // ═══════════════════════════════════════════════════════════════
    // Menu navigation helpers — route through MainViewModel
    // ═══════════════════════════════════════════════════════════════

    private void ProductsMenuItem_Click(object sender, RoutedEventArgs e) => _mainViewModel.NavigateTo<ProductListViewModel>();
    private void CustomersMenuItem_Click(object sender, RoutedEventArgs e) => _mainViewModel.NavigateTo<CustomerListViewModel>();
    private void SuppliersMenuItem_Click(object sender, RoutedEventArgs e) => _mainViewModel.NavigateTo<SupplierListViewModel>();
    private void UsersMenuItem_Click(object sender, RoutedEventArgs e) => _mainViewModel.NavigateTo<UserListViewModel>();
    private void SalesInvoicesMenuItem_Click(object sender, RoutedEventArgs e) => _mainViewModel.NavigateTo<SalesInvoiceListViewModel>();
    private void PurchaseInvoicesMenuItem_Click(object sender, RoutedEventArgs e) => _mainViewModel.NavigateTo<PurchaseInvoiceListViewModel>();
    private void PurchaseOrdersMenuItem_Click(object sender, RoutedEventArgs e) => _mainViewModel.NavigateTo<PurchaseOrderListViewModel>();
    private void SalesReturnsMenuItem_Click(object sender, RoutedEventArgs e) => _mainViewModel.NavigateTo<SalesReturnListViewModel>();
    private void PurchaseReturnsMenuItem_Click(object sender, RoutedEventArgs e) => _mainViewModel.NavigateTo<PurchaseReturnListViewModel>();
    private void WarehousesMenuItem_Click(object sender, RoutedEventArgs e) => _mainViewModel.NavigateTo<WarehouseListViewModel>();
    private void StockTransfersMenuItem_Click(object sender, RoutedEventArgs e) => _mainViewModel.NavigateTo<StockTransfersListViewModel>();
    private void CustomerPaymentsMenuItem_Click(object sender, RoutedEventArgs e) => _mainViewModel.NavigateTo<CustomerPaymentsListViewModel>();
    private void SupplierPaymentsMenuItem_Click(object sender, RoutedEventArgs e) => _mainViewModel.NavigateTo<SupplierPaymentsListViewModel>();
    private void InventoryStatusMenuItem_Click(object sender, RoutedEventArgs e) => _mainViewModel.NavigateTo<InventoryViewModel>();
    private void TaxesMenuItem_Click(object sender, RoutedEventArgs e) => _mainViewModel.NavigateTo<TaxesListViewModel>();

    private async void CheckForUpdatesMenuItem_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var updaterService = App.GetService<IUpdaterService>();
            IsEnabled = false;

            var checkResult = await updaterService.CheckForUpdatesAsync();

            if (!checkResult.IsSuccess)
            {
                _ = _dialogService.ShowErrorAsync("خطأ في فحص التحديثات", $"تعذر الاتصال بخادم التحديثات.\n{checkResult.Error}");
                return;
            }

            var value = checkResult.Value;
            if (value == null || !value.UpdateAvailable || value.UpdateInfo == null)
            {
                _ = _dialogService.ShowSuccessAsync("تحديث", $"برنامجك محدّث!\nتعمل على أحدث إصدار: {updaterService.GetCurrentVersion().Value}");
                return;
            }

            var vm = new UpdateDialogViewModel(updaterService, value.UpdateInfo);
            var dialog = new UpdateDialog(vm) { Owner = this };
            dialog.ShowDialog();
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "فشل فحص التحديثات التلقائية (CheckForUpdates)");
            _ = _dialogService.ShowErrorAsync("خطأ في فحص التحديثات", "حدث خطأ غير متوقع أثناء فحص التحديثات. يرجى المحاولة لاحقاً.");
        }
        finally
        {
            IsEnabled = true;
        }
    }

    private void ProfileMenuItem_Click(object sender, RoutedEventArgs e)
    {
        _ = _dialogService.ShowInfoAsync("الملف الشخصي", $"مرحباً {_session.GetUserName()}");
    }

    private async void BtnLogout_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var confirmed = await _dialogService.ShowConfirmationAsync("تسجيل الخروج", "هل أنت متأكد من تسجيل الخروج؟");
            if (confirmed)
            {
                _isLoggingOut = true;
                _session.ClearSession();
                var loginWindow = new LoginWindow();
                loginWindow.Show();
                this.Close();
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "خطأ أثناء تسجيل الخروج");
            _ = _dialogService.ShowErrorAsync("خطأ في تسجيل الخروج", "حدث خطأ غير متوقع أثناء تسجيل الخروج");
        }
    }

    private void ReportMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.Tag is string tag)
        {
            switch (tag)
            {
                case "Inventory":
                    _mainViewModel.NavigateTo<InventoryViewModel>();
                    break;
                case "ExpiredProducts":
                    _mainViewModel.NavigateTo<ExpiredProductsReportViewModel>();
                    break;
                default:
                    _mainViewModel.NavigateTo<ReportsViewModel>();
                    break;
            }
        }
    }

    private void OpenPageInNewWindow(string title, string tag)
    {
        FrameworkElement? page = tag switch
        {
            "Sales" => new Views.Sales.SalesInvoicesListView(),
            "Purchases" => new Views.Purchases.PurchaseInvoicesListView(),
            "Products" => new Views.Products.ProductsListView(),
            "Customers" => new Views.Customers.CustomersListView(),
            "Suppliers" => new Views.Suppliers.SuppliersListView(),
            "Warehouses" => new Views.WarehousesView(),
            _ => null
        };

        if (page == null) return;

        var screenWindow = new Views.ScreenWindow();
        screenWindow.SetContent(page, page.DataContext);

        var screenService = App.GetService<IScreenWindowService>();
        screenService.OpenWindow(screenWindow, new ScreenWindowOptions
        {
            Title = title,
            Width = 1000,
            Height = 700
        });
    }

    private void OpenNewSalesWindow_Click(object sender, RoutedEventArgs e) => OpenPageInNewWindow("المبيعات", "Sales");
    private void OpenNewPurchasesWindow_Click(object sender, RoutedEventArgs e) => OpenPageInNewWindow("المشتريات", "Purchases");
    private void OpenNewWarehousesWindow_Click(object sender, RoutedEventArgs e) => OpenPageInNewWindow("المستودعات", "Warehouses");
    private void OpenNewProductsWindow_Click(object sender, RoutedEventArgs e) => OpenPageInNewWindow("المنتجات", "Products");
    private void OpenNewCustomersWindow_Click(object sender, RoutedEventArgs e) => OpenPageInNewWindow("العملاء", "Customers");
    private void OpenNewSuppliersWindow_Click(object sender, RoutedEventArgs e) => OpenPageInNewWindow("الموردين", "Suppliers");
}
