using System.Net.Http;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Serilog;
using SalesSystem.Application.Updates;
using SalesSystem.Application.Updates.Models;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.ViewModels;
using SalesSystem.DesktopPWF.ViewModels.Sales;
using SalesSystem.DesktopPWF.ViewModels.Purchases;
using SalesSystem.DesktopPWF.ViewModels.Customers;
using SalesSystem.DesktopPWF.ViewModels.Suppliers;
using SalesSystem.DesktopPWF.ViewModels.Products;
using SalesSystem.DesktopPWF.ViewModels.Users;
using SalesSystem.DesktopPWF.ViewModels.Inventory;
using SalesSystem.DesktopPWF.ViewModels.Payments;
using SalesSystem.DesktopPWF.ViewModels.Returns;
using SalesSystem.DesktopPWF.ViewModels.Transfers;
using SalesSystem.DesktopPWF.ViewModels.Categories;
using SalesSystem.DesktopPWF.ViewModels.Settings;
using SalesSystem.DesktopPWF.ViewModels.Units;
using SalesSystem.DesktopPWF.ViewModels.Updates;
using SalesSystem.DesktopPWF.ViewModels.Warehouses;
using SalesSystem.DesktopPWF.ViewModels.CashBoxes;
using SalesSystem.DesktopPWF.ViewModels.Taxes;
using SalesSystem.DesktopPWF.ViewModels.Currencies;
using SalesSystem.DesktopPWF.ViewModels.Reports;
using SalesSystem.DesktopPWF.ViewModels.Accounts;
using SalesSystem.DesktopPWF.ViewModels.JournalEntries;
using SalesSystem.DesktopPWF.ViewModels.Audit;
using SalesSystem.DesktopPWF.ViewModels.Permissions;
using SalesSystem.DesktopPWF.Views.Accounts;
using SalesSystem.DesktopPWF.Views.Updates;
using SalesSystem.DesktopPWF.Services.App.Toast;

namespace SalesSystem.DesktopPWF;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    private static IServiceProvider? _serviceProvider;

   
/// <summary>
   
/// Get service from DI container
   
/// </summary>
    public static T GetService<T>() where T : class
    {
        if (_serviceProvider == null)
            throw new InvalidOperationException("Service provider not initialized");

        return _serviceProvider.GetRequiredService<T>();
    }

    private async void Application_Startup(object sender, StartupEventArgs e)
    {
        // Setup Serilog
        SetupLogging();

        // Setup global exception handlers
        SetupExceptionHandlers();

        // Configure DI
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        // Check API and database connectivity before showing UI
        var canConnect = await CheckDatabaseConnectionAsync();
        if (!canConnect)
        {
            Log.Fatal("Application shutting down due to database connection failure");
            Log.CloseAndFlush();
            Environment.Exit(1);
            return;
        }

        // Check authentication and show appropriate window
        var sessionService = _serviceProvider.GetRequiredService<ISessionService>();

        if (sessionService.IsAuthenticated)
        {
            // User is already authenticated - show main window
            var mainWindow = new MainWindow();
            mainWindow.Show();
            MainWindow = mainWindow;

            // Background checks - never block startup
            _ = ScheduleBackgroundUpdateCheckAsync();
            _ = ScheduleExpirationNotificationCheckAsync();
        }
        else
        {
            // Show login window
            var loginWindow = new LoginWindow();
            loginWindow.Show();
            MainWindow = loginWindow;
        }

        Log.Information("Application started successfully");
    }

    /// <summary>
    /// Checks API and database connectivity before startup.
    /// Shows error dialog if connection fails, with retry option.
    /// Returns true if database is reachable, false to shut down.
    /// </summary>
    private async Task<bool> CheckDatabaseConnectionAsync()
    {
        var healthService = _serviceProvider!.GetRequiredService<IDatabaseHealthCheckService>();

        while (true)
        {
            var result = await healthService.CheckAsync();

            if (result.IsDatabaseConnected)
                return true;

            var retry = await Dispatcher.InvokeAsync(() =>
            {
            var dialog = new Views.Dialogs.DatabaseErrorDialog(
                result.ErrorMessage ?? "تعذر الاتصال بقاعدة البيانات",
                () => healthService.CheckAsync());

                dialog.ShowDialog();
                return dialog.RetryClicked;
            });

            if (!retry)
                return false;

            await Task.Delay(1000);
        }
    }

    private void Application_Exit(object sender, ExitEventArgs e)
    {   

        Log.Information("Application shutting down");
        Log.CloseAndFlush();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Add appsettings configuration
        var appSettings = LoadAppSettings();
        var apiBaseUrl = appSettings?["ApiBaseUrl"] ?? "http://localhost:5221";

        // HTTP Client
        services.AddHttpClient("ApiClient", client =>
        {
            client.BaseAddress = new Uri(apiBaseUrl);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        services.AddSingleton<HttpClient>(sp =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            return httpClientFactory.CreateClient("ApiClient");
        });

        // Services
        services.AddSingleton<ISessionService, SessionService>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IScreenWindowService, ScreenWindowService>();
        services.AddSingleton<IEventBus, EventBus>();
        services.AddSingleton<ISoundService, SoundService>();
        services.AddSingleton<IBarcodeInputService, BarcodeInputService>();
        services.AddSingleton<IToastNotificationService, ToastNotificationService>();

        // Update services
        services.AddSingleton<IUpdaterService, UpdaterService>();

        // API Services
        services.AddSingleton<IAuthApiService, AuthApiService>();
        services.AddSingleton<IProductApiService, ProductApiService>();
        services.AddSingleton<ICategoryApiService, CategoryApiService>();
        services.AddSingleton<IUnitApiService, UnitApiService>();
        services.AddSingleton<ICustomerGroupApiService, CustomerGroupApiService>();
        services.AddSingleton<ICustomerApiService, CustomerApiService>();
        services.AddSingleton<ISupplierApiService, SupplierApiService>();
        services.AddSingleton<IWarehouseApiService, WarehouseApiService>();
        services.AddSingleton<ISalesInvoiceApiService, SalesInvoiceApiService>();
        services.AddSingleton<IPurchaseInvoiceApiService, PurchaseInvoiceApiService>();
        services.AddSingleton<ISalesReturnApiService, SalesReturnApiService>();
        services.AddSingleton<IPurchaseReturnApiService, PurchaseReturnApiService>();
        services.AddSingleton<IStockTransferApiService, StockTransferApiService>();
        services.AddSingleton<ICustomerPaymentApiService, CustomerPaymentApiService>();
        services.AddSingleton<ISupplierPaymentApiService, SupplierPaymentApiService>();
        services.AddSingleton<IUserApiService, UserApiService>();
        services.AddSingleton<IDashboardApiService, DashboardApiService>();
        services.AddSingleton<IReportApiService, ReportApiService>();
        services.AddSingleton<ISettingsApiService, SettingsApiService>();
        services.AddSingleton<IBackupApiService, BackupApiService>();
        services.AddSingleton<IInventoryApiService, InventoryApiService>();
        services.AddSingleton<IInventoryWriteOffApiService, InventoryWriteOffApiService>();
        services.AddSingleton<ILogsApiService, LogsApiService>();
        services.AddSingleton<IProductUnitApiService, ProductUnitApiService>();
        services.AddSingleton<IProductPriceApiService, ProductPriceApiService>();
        services.AddSingleton<IInventoryBatchApiService, InventoryBatchApiService>();
        services.AddSingleton<IProductImageApiService, ProductImageApiService>();
        services.AddSingleton<ICashBoxApiService, CashBoxApiService>();
        services.AddSingleton<IFinancialReportApiService, FinancialReportApiService>();
        services.AddSingleton<ITaxesApiService, TaxesApiService>();
        services.AddSingleton<ICurrencyApiService, CurrencyApiService>();

        // Account API Service
        services.AddSingleton<IAccountApiService, AccountApiService>();

        // Journal Entry API Service
        services.AddSingleton<IJournalEntryApiService, JournalEntryApiService>();

        // Audit & Permission Services
        services.AddSingleton<IAuditLogApiService, AuditLogApiService>();
        services.AddSingleton<IPermissionApiService, PermissionApiService>();

        // Printing
        services.AddSingleton<Services.App.IInvoicePrinter, Services.Printing.InvoicePrinter>();
        services.AddSingleton<Services.App.IReceiptPrinter, Services.Printing.ReceiptPrinter>();
        services.AddSingleton<Services.App.IPaymentPrinter, Services.Printing.PaymentPrinter>();
        services.AddSingleton<Services.App.ITransferPrinter, Services.Printing.TransferPrinter>();
        services.AddSingleton<IPrintApiService, PrintApiService>();

        // Health check
        services.AddSingleton<IDatabaseHealthCheckService, DatabaseHealthCheckService>();

        // Financial Reports API
        services.AddSingleton<IFinancialReportApiService, FinancialReportApiService>();

        // ViewModels
        services.AddTransient<LoginWindowViewModel>();
        services.AddTransient<MainViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<SalesInvoiceListViewModel>();
        services.AddTransient<SalesInvoiceEditorViewModel>();
        services.AddTransient<TouchPosViewModel>();
        services.AddTransient<PurchaseInvoiceListViewModel>();
        services.AddTransient<PurchaseInvoiceEditorViewModel>();
        services.AddTransient<ProductListViewModel>();
        services.AddTransient<ProductEditorViewModel>();
        services.AddTransient<ProductPricesListViewModel>();
        services.AddTransient<ProductPriceEditorViewModel>();
        services.AddTransient<ProductImagesViewModel>();
        services.AddTransient<InventoryBatchesViewModel>();
        services.AddTransient<CustomerListViewModel>();
        services.AddTransient<CustomerEditorViewModel>();
        services.AddTransient<SupplierListViewModel>();
        services.AddTransient<SupplierEditorViewModel>();
        services.AddTransient<UserListViewModel>();
        services.AddTransient<UserEditorViewModel>();
        services.AddTransient<WarehouseListViewModel>();
        services.AddTransient<WarehouseEditorViewModel>();
        services.AddTransient<InventoryViewModel>();
        services.AddTransient<LowStockViewModel>();
        services.AddTransient<CustomerPaymentsListViewModel>();
        services.AddTransient<CustomerPaymentEditorViewModel>();
        services.AddTransient<SupplierPaymentsListViewModel>();
        services.AddTransient<SupplierPaymentEditorViewModel>();
        services.AddTransient<SalesReturnListViewModel>();
        services.AddTransient<SalesReturnEditorViewModel>();
        services.AddTransient<PurchaseReturnListViewModel>();
        services.AddTransient<PurchaseReturnEditorViewModel>();
        services.AddTransient<StockTransfersListViewModel>();
        services.AddTransient<StockTransferEditorViewModel>();
        services.AddTransient<CategoryListViewModel>();
        services.AddTransient<CategoryEditorViewModel>();
        services.AddTransient<UnitListViewModel>();
        services.AddTransient<UnitEditorViewModel>();
        services.AddTransient<ReportsViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<CostingMethodSettingsViewModel>();
        services.AddTransient<SystemSettingsViewModel>();
        services.AddTransient<BackupViewModel>();
        services.AddTransient<ProductUnitEditorViewModel>();
        services.AddTransient<ProductUnitsListViewModel>();

        // Customer Group ViewModels
        services.AddTransient<CustomerGroupListViewModel>();

        // Tax ViewModels
        services.AddTransient<TaxesListViewModel>();
        services.AddTransient<TaxEditorViewModel>();

        // Currency ViewModels
        services.AddTransient<CurrenciesListViewModel>();
        services.AddTransient<CurrencyEditorViewModel>();

        // Account ViewModels
        services.AddTransient<AccountsListViewModel>();
        services.AddTransient<AccountEditorViewModel>();

        // Cash Box ViewModels
        services.AddTransient<CashBoxEditorViewModel>();
        services.AddTransient<CashBoxesListViewModel>();
        services.AddTransient<CashBoxTransactionsViewModel>();
        services.AddTransient<CashTransferViewModel>();
        services.AddTransient<DailyClosureViewModel>();
        services.AddTransient<ExpiredProductsReportViewModel>();

        // Journal Entry ViewModels
        services.AddTransient<JournalEntriesListViewModel>();

        // Audit & Permission ViewModels
        services.AddTransient<AuditLogListViewModel>();
        services.AddTransient<PermissionManagementViewModel>();
        services.AddTransient<PasswordChangeViewModel>();
        services.AddTransient<SetPasswordViewModel>();

        // Financial Report ViewModels
        services.AddTransient<ViewModels.Reports.IncomeStatementViewModel>();
        services.AddTransient<ViewModels.Reports.CashFlowReportViewModel>();
        services.AddTransient<ViewModels.Reports.VatReportViewModel>();
        services.AddTransient<ViewModels.Reports.AccountStatementViewModel>();
        services.AddTransient<IncomeStatementViewModel>();
        services.AddTransient<CashFlowReportViewModel>();
        services.AddTransient<VatReportViewModel>();
        services.AddTransient<AccountStatementViewModel>();
    }

    private static Dictionary<string, string>? LoadAppSettings()
    {
        // Try to load from appsettings.json if exists
        try
        {
            var settingsPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            if (System.IO.File.Exists(settingsPath))
            {
                var json = System.IO.File.ReadAllText(settingsPath);
                var settings = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                return settings;
            }
        }
        catch
        {
            // Ignore - use defaults
        }
        return null;
    }

    private static void SetupLogging()
    {
        // 1. Configure Serilog for AI-friendly error logging
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Error() // AI focuses on Errors and Fatals
            .WriteTo.File("Logs/AI_Error_Log_.txt",
                rollingInterval: RollingInterval.Day,
                outputTemplate: "=== {Timestamp:yyyy-MM-dd HH:mm:ss} ==={NewLine}Message: {Message}{NewLine}Exception: {Exception}{NewLine}--------------------------------------------------{NewLine}")
            .CreateLogger();
    }

    private void SetupExceptionHandlers()
    {
        // Handle UI thread exceptions
        DispatcherUnhandledException += App_DispatcherUnhandledException;

        // Handle non-UI thread exceptions
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

        // Handle task exceptions
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
    }

    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        // AI-Friendly Context: Include the specific area of failure
        var errorMessage = $"[UI THREAD EXCEPTION] Location: {e.Exception.Source} -> {e.Exception.TargetSite}. Context: WPF Dispatcher Unhandled Exception.";
        Log.Error(e.Exception, errorMessage);

        new Views.Dialogs.FallbackErrorDialog(
            "حدث خطأ غير متوقع في التطبيق. تم تسجيل التفاصيل في ملف السجلات.")
            .ShowDialog();

        e.Handled = true;
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            var errorMessage = $"[FATAL DOMAIN EXCEPTION] Location: {ex.Source} -> {ex.TargetSite}. Context: AppDomain Unhandled Exception. IsTerminating: {e.IsTerminating}";
            Log.Fatal(ex, errorMessage);
        }
    }

    private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        var errorMessage = $"[ASYNC TASK EXCEPTION] Location: {e.Exception.Source}. Context: TaskScheduler Unobserved Exception.";
        Log.Error(e.Exception, errorMessage);
        e.SetObserved();
    }

    // ═══════════════════════════════════════════════
    // UPDATE CHECK
    // ═══════════════════════════════════════════════

    private async Task ScheduleBackgroundUpdateCheckAsync()
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(3));
            await CheckForUpdatesInBackgroundAsync();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Background update check failed silently");
        }
    }

    /// <summary>
    /// Checks for expiring products and shows a non-intrusive notification on startup.
    /// Runs after a delay to avoid blocking the UI thread during initial load.
    /// </summary>
    private async Task ScheduleExpirationNotificationCheckAsync()
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(5));

            await Dispatcher.InvokeAsync(async () =>
            {
                try
                {
                    var productApi = _serviceProvider!.GetRequiredService<IProductApiService>();
                    var dialogService = _serviceProvider!.GetRequiredService<IDialogService>();

                    var result = await productApi.GetExpiringProductsAsync(thresholdDays: 30);

                    if (result.IsSuccess && result.Value != null && result.Value.Count > 0)
                    {
                        var count = result.Value.Count;
                        await dialogService.ShowWarningAsync(
                            "منتجات على وشك الانتهاء",
                            $"⚠️ يوجد {count} منتج على وشك انتهاء الصلاحية خلال 30 يوماً.\n\n" +
                            "يرجى مراجعة قائمة المنتجات لاتخاذ الإجراء المناسب.");
                    }
                }
                catch (Exception innerEx)
                {
                    Log.Warning(innerEx, "Expiration notifications check failed silently");
                }
            });
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to schedule expiration notification check");
        }
    }

    private async Task CheckForUpdatesInBackgroundAsync()
    {
        try
        {
            var updaterService = _serviceProvider!.GetRequiredService<IUpdaterService>();

            var result = await updaterService.CheckForUpdatesAsync();

            var updateValue = result.Value;
            if (!result.IsSuccess || updateValue == null || !updateValue.UpdateAvailable || updateValue.UpdateInfo == null)
                return;

            await Dispatcher.InvokeAsync(() =>
            {
                ShowUpdateDialog(updateValue!.UpdateInfo);
            });
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Background update check failed silently");
        }
    }

    private void ShowUpdateDialog(UpdateInfo updateInfo)
    {
        var updaterService = _serviceProvider!.GetRequiredService<IUpdaterService>();

        var viewModel = new UpdateDialogViewModel(updaterService, updateInfo);

        var dialog = new UpdateDialog(viewModel)
        {
            Owner = Current.MainWindow
        };

        dialog.ShowDialog();

        Log.Information(
            "User chose: {Action} for version {Version}",
            viewModel.Result,
            updateInfo.LatestVersion);
    }
}
