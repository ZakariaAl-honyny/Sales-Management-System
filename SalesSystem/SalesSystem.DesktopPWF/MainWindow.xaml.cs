using System.Windows;
using System.Windows.Controls;
using SalesSystem.Application.Updates;
using SalesSystem.Application.Updates.Models;
using SalesSystem.DesktopPWF.ViewModels;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.Contracts.Enums;
using SalesSystem.DesktopPWF.ViewModels.Inventory;
using SalesSystem.DesktopPWF.ViewModels.Updates;
using SalesSystem.DesktopPWF.Views.Updates;

namespace SalesSystem.DesktopPWF;

public partial class MainWindow : Window
{
    private readonly ISessionService _session;
    private readonly IDialogService _dialogService;
    private bool _isLoggingOut;

    public MainWindow()
    {
        InitializeComponent();
        _session = App.GetService<ISessionService>();
        _dialogService = App.GetService<IDialogService>();

        UpdateUserInfo();
        ApplyPermissions();
        NavigateTo("Dashboard");

        Closed += (s, e) =>
        {
            if (!_isLoggingOut)
                System.Windows.Application.Current.Shutdown();
        };
    }

    private void UpdateUserInfo()
    {
        TxtUserName.Text = _session.GetUserName() ?? "مستخدم";
        TxtUserRole.Text = GetRoleDisplayName(_session.GetUserRole());
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

    /// <summary>
    /// يُطبّق الرؤية على عناصر القائمة الجانبية بناءً على صلاحيات المستخدم الحالي.
    /// يُستدعى مرة واحدة عند فتح النافذة بعد تسجيل الدخول.
    /// </summary>
    private void ApplyPermissions()
    {
        Visibility Show(Enums.Permission p)
            => _session.CanAccess(p) ? Visibility.Visible : Visibility.Collapsed;

        // ─── AllStaff (Cashier+) ─────────────────────────────────────
        NavSalesItem.Visibility            = Show(Enums.Permission.SalesInvoice);
        NavSalesReturnsItem.Visibility     = Show(Enums.Permission.SalesReturn);
        NavCustomersItem.Visibility        = Show(Enums.Permission.CustomerView);
        NavCustomerPaymentsItem.Visibility = Show(Enums.Permission.CustomerPayment);

        // ─── ManagerAndAbove ─────────────────────────────────────────
        NavPurchasesItem.Visibility        = Show(Enums.Permission.PurchaseInvoice);
        NavPurchaseReturnsItem.Visibility  = Show(Enums.Permission.PurchaseReturn);
        NavProductsItem.Visibility         = Show(Enums.Permission.ProductManagement);
        NavSuppliersItem.Visibility        = Show(Enums.Permission.SupplierManagement);
        NavSupplierPaymentsItem.Visibility = Show(Enums.Permission.SupplierManagement);
        NavStockTransfersItem.Visibility   = Show(Enums.Permission.StockTransfer);
        NavReportsItem.Visibility          = Show(Enums.Permission.Reports);
        NavLowStockItem.Visibility         = Show(Enums.Permission.Reports);
        NavExpiredProductsItem.Visibility  = Show(Enums.Permission.Reports);
        NavCategoriesItem.Visibility       = Show(Enums.Permission.ProductManagement);
        NavUnitsItem.Visibility            = Show(Enums.Permission.ProductManagement);

        // ─── AdminOnly ───────────────────────────────────────────────
        NavWarehousesItem.Visibility       = Show(Enums.Permission.WarehouseManagement);
        NavUsersItem.Visibility            = Show(Enums.Permission.UserManagement);
        NavSettingsItem.Visibility         = Show(Enums.Permission.Settings);
    }

    /// <summary>
    /// حماية ثانوية: يتحقق من الصلاحية قبل أي تنقل برمجي.
    /// يمنع الوصول حتى لو تجاوز المستخدم الواجهة.
    /// </summary>
    private bool CanNavigateTo(string tag)
    {
        return tag switch
        {
            "Purchases"        => _session.CanAccess(Enums.Permission.PurchaseInvoice),
            "PurchaseReturns"  => _session.CanAccess(Enums.Permission.PurchaseReturn),
            "Products"         => _session.CanAccess(Enums.Permission.ProductManagement),
            "Suppliers"        => _session.CanAccess(Enums.Permission.SupplierManagement),
            "SupplierPayments" => _session.CanAccess(Enums.Permission.SupplierManagement),
            "StockTransfers"   => _session.CanAccess(Enums.Permission.StockTransfer),
            "Reports"          => _session.CanAccess(Enums.Permission.Reports),
            "ExpiredProducts"  => _session.CanAccess(Enums.Permission.Reports),
            "LowStock"         => _session.CanAccess(Enums.Permission.Reports),
            "Warehouses"       => _session.CanAccess(Enums.Permission.WarehouseManagement),
            "Users"            => _session.CanAccess(Enums.Permission.UserManagement),
            "Settings"         => _session.CanAccess(Enums.Permission.Settings),
            "Categories"       => _session.CanAccess(Enums.Permission.ProductManagement),
            "Units"            => _session.CanAccess(Enums.Permission.ProductManagement),
            _ => true // Dashboard, Sales, SalesReturns, Customers, CustomerPayments
        };
    }

    private void NavigationList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (NavigationList.SelectedItem is ListBoxItem item && item.Tag is string tag)
        {
            NavigateTo(tag);
        }
    }

    private void NavigateTo(string tag)
    {
        // الحماية الثانوية — منع التنقل البرمجي بدون صلاحية
        if (!CanNavigateTo(tag))
        {
            _ = _dialogService.ShowWarningAsync("تنبيه", "ليس لديك صلاحية للوصول إلى هذه الشاشة.");
            NavigationList.SelectedItem = null;
            return;
        }

        Page? page = tag switch
        {
            "Dashboard" => new Views.DashboardView { DataContext = App.GetService<DashboardViewModel>() },
            "Sales" => new Views.Sales.SalesInvoicesListView(),
            "Purchases" => new Views.Purchases.PurchaseInvoicesListView(),
            "Products" => new Views.Products.ProductsListView(),
            "Customers" => new Views.Customers.CustomersListView(),
            "Suppliers" => new Views.Suppliers.SuppliersListView(),
            "Warehouses" => new Views.WarehousesView(),
            "CustomerPayments" => new Views.Payments.CustomerPaymentsListView(),
            "SupplierPayments" => new Views.Payments.SupplierPaymentsListView(),
            "StockTransfers" => new Views.Transfers.StockTransfersListView(),
            "Reports" => new Views.Reports.ReportsView { DataContext = App.GetService<ReportsViewModel>() },
            "SalesReturns" => new Views.Returns.SalesReturnsListView(),
            "PurchaseReturns" => new Views.Returns.PurchaseReturnsListView(),
            "Categories" => new Views.Categories.CategoriesListView(),
            "Units" => new Views.Units.UnitsListView(),
            "Users" => new Views.Users.UsersListView(),
            "Inventory" => new Views.Inventory.InventoryView(),
            "LowStock" => new Views.Inventory.LowStockView { DataContext = App.GetService<LowStockViewModel>() },
            "ExpiredProducts" => new Views.Reports.ExpiredProductsReportView(),
            "Settings" => new Views.Settings.SettingsView(),
            _ => null
        };

        if (page != null)
        {
            ContentFrame.Navigate(page);
        }
    }

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

    private void ReportMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.Tag is string tag)
        {
            NavigateTo(tag);
        }
    }

    private void ProductsMenuItem_Click(object sender, RoutedEventArgs e) => NavigateTo("Products");
    private void CustomersMenuItem_Click(object sender, RoutedEventArgs e) => NavigateTo("Customers");
    private void SuppliersMenuItem_Click(object sender, RoutedEventArgs e) => NavigateTo("Suppliers");
    private void UsersMenuItem_Click(object sender, RoutedEventArgs e) => NavigateTo("Users");
    private void SalesInvoicesMenuItem_Click(object sender, RoutedEventArgs e) => NavigateTo("Sales");
    private void PurchaseInvoicesMenuItem_Click(object sender, RoutedEventArgs e) => NavigateTo("Purchases");
    private void SalesReturnsMenuItem_Click(object sender, RoutedEventArgs e) => NavigateTo("SalesReturns");
    private void PurchaseReturnsMenuItem_Click(object sender, RoutedEventArgs e) => NavigateTo("PurchaseReturns");
    private void WarehousesMenuItem_Click(object sender, RoutedEventArgs e) => NavigateTo("Warehouses");
    private void StockTransfersMenuItem_Click(object sender, RoutedEventArgs e) => NavigateTo("StockTransfers");
    private void CustomerPaymentsMenuItem_Click(object sender, RoutedEventArgs e) => NavigateTo("CustomerPayments");
    private void SupplierPaymentsMenuItem_Click(object sender, RoutedEventArgs e) => NavigateTo("SupplierPayments");
    private void InventoryStatusMenuItem_Click(object sender, RoutedEventArgs e) => NavigateTo("Inventory");

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
