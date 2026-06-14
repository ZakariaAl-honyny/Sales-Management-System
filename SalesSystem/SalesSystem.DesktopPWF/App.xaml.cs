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
using SalesSystem.DesktopPWF.ViewModels.Settings;
using SalesSystem.DesktopPWF.ViewModels.Units;
using SalesSystem.DesktopPWF.ViewModels.Updates;
using SalesSystem.DesktopPWF.ViewModels.Warehouses;
using SalesSystem.DesktopPWF.ViewModels.CashBoxes;
using SalesSystem.DesktopPWF.ViewModels.Taxes;
using SalesSystem.DesktopPWF.ViewModels.Currencies;
using SalesSystem.DesktopPWF.ViewModels.Reports;
using SalesSystem.DesktopPWF.ViewModels.CustomerReceipt;
using SalesSystem.DesktopPWF.ViewModels.InventoryCount;
using SalesSystem.DesktopPWF.ViewModels.InventoryAdjustment;
using SalesSystem.DesktopPWF.ViewModels.Notifications;
using SalesSystem.DesktopPWF.ViewModels.Attachments;
using SalesSystem.DesktopPWF.ViewModels.Accounts;
using SalesSystem.DesktopPWF.ViewModels.Accounting;
using SalesSystem.DesktopPWF.ViewModels.Branch;
using SalesSystem.DesktopPWF.ViewModels.Department;
using SalesSystem.DesktopPWF.ViewModels.Employee;
using SalesSystem.DesktopPWF.ViewModels.Bank;
using SalesSystem.DesktopPWF.ViewModels.Party;
using SalesSystem.DesktopPWF.ViewModels.Expense;
using SalesSystem.DesktopPWF.ViewModels.JournalEntries;
using SalesSystem.DesktopPWF.ViewModels.Audit;
using SalesSystem.DesktopPWF.ViewModels.Permissions;
using SalesSystem.DesktopPWF.Views.Accounts;
using SalesSystem.DesktopPWF.Views.Currencies;
using SalesSystem.DesktopPWF.Views.Updates;
using SalesSystem.DesktopPWF.Services.App.Toast;
using SalesSystem.DesktopPWF.Services.Export;
using SalesSystem.DesktopPWF.ViewModels.Roles;
using SalesSystem.DesktopPWF.ViewModels.Sessions;
using SalesSystem.DesktopPWF.ViewModels.CompanySettings;
using SalesSystem.DesktopPWF.ViewModels.DocumentSequences;
using SalesSystem.DesktopPWF.ViewModels.AccountCategories;
using SalesSystem.DesktopPWF.ViewModels.SystemAccountMappings;
using SalesSystem.DesktopPWF.ViewModels.Logs;

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
        services.AddSingleton<IUnitApiService, UnitApiService>();
        services.AddSingleton<ICustomerApiService, CustomerApiService>();
        services.AddSingleton<ISupplierApiService, SupplierApiService>();
        services.AddSingleton<IWarehouseApiService, WarehouseApiService>();
        services.AddSingleton<ISalesInvoiceApiService, SalesInvoiceApiService>();
        services.AddSingleton<IPurchaseInvoiceApiService, PurchaseInvoiceApiService>();
        services.AddSingleton<ISalesReturnApiService, SalesReturnApiService>();
        services.AddSingleton<IPurchaseReturnApiService, PurchaseReturnApiService>();
        services.AddSingleton<IWarehouseTransferApiService, WarehouseTransferApiService>();
        services.AddSingleton<ISupplierPaymentApiService, SupplierPaymentApiService>();
        services.AddSingleton<IUserApiService, UserApiService>();
        services.AddSingleton<IDashboardApiService, DashboardApiService>();
        services.AddSingleton<IReportApiService, ReportApiService>();
        services.AddSingleton<ISettingsApiService, SettingsApiService>();
        services.AddSingleton<IBackupApiService, BackupApiService>();
        services.AddSingleton<IInventoryApiService, InventoryApiService>();
        services.AddSingleton<ILogsApiService, LogsApiService>();
        services.AddSingleton<IProductUnitApiService, ProductUnitApiService>();
        services.AddSingleton<IProductPriceApiService, ProductPriceApiService>();
        services.AddSingleton<IInventoryBatchApiService, InventoryBatchApiService>();
        services.AddSingleton<ICashBoxApiService, CashBoxApiService>();
        services.AddSingleton<IFinancialReportApiService, FinancialReportApiService>();
        services.AddSingleton<ITaxesApiService, TaxesApiService>();
        services.AddSingleton<ICurrencyApiService, CurrencyApiService>();

        // New Entity API Services (v4.7+)
        services.AddSingleton<IBranchApiService, BranchApiService>();
        services.AddSingleton<IDepartmentApiService, DepartmentApiService>();
        services.AddSingleton<IBankApiService, BankApiService>();
        services.AddSingleton<IPartyApiService, PartyApiService>();
        services.AddSingleton<IProductCategoryApiService, ProductCategoryApiService>();
        services.AddSingleton<INotificationApiService, NotificationApiService>();
        services.AddSingleton<IInventoryCountApiService, InventoryCountApiService>();
        services.AddSingleton<IInventoryAdjustmentApiService, InventoryAdjustmentApiService>();
        services.AddSingleton<ICustomerReceiptApiService, CustomerReceiptApiService>();
        services.AddSingleton<IAttachmentApiService, AttachmentApiService>();
        services.AddSingleton<ISupplierPaymentApplicationApiService, SupplierPaymentApplicationApiService>();

        // Account API Service
        services.AddSingleton<IAccountApiService, AccountApiService>();

        // Employee API Service
        services.AddSingleton<IEmployeeApiService, EmployeeApiService>();

        // Expense API Service
        services.AddSingleton<IExpenseApiService, ExpenseApiService>();

        // Customer/Supplier Contact API Services
        services.AddSingleton<ICustomerContactApiService, CustomerContactApiService>();
        services.AddSingleton<ISupplierContactApiService, SupplierContactApiService>();

        // Payment Voucher API Service
        services.AddSingleton<IPaymentVoucherApiService, PaymentVoucherApiService>();
        services.AddSingleton<IReceiptVoucherApiService, ReceiptVoucherApiService>();

        // Journal Entry API Service
        services.AddSingleton<IJournalEntryApiService, JournalEntryApiService>();

        // Fiscal Year API Service
        services.AddSingleton<IFiscalYearApiService, FiscalYearApiService>();

        // Audit & Permission Services
        services.AddSingleton<IAuditLogApiService, AuditLogApiService>();
        services.AddSingleton<IPermissionApiService, PermissionApiService>();
        services.AddSingleton<IRoleApiService, RoleApiService>();
        services.AddSingleton<IUserSessionApiService, UserSessionApiService>();

        // Printing
        services.AddSingleton<Services.App.IInvoicePrinter, Services.Printing.InvoicePrinter>();
        services.AddSingleton<Services.App.IReceiptPrinter, Services.Printing.ReceiptPrinter>();
        services.AddSingleton<Services.App.IPaymentPrinter, Services.Printing.PaymentPrinter>();
        services.AddSingleton<Services.App.ITransferPrinter, Services.Printing.TransferPrinter>();
        services.AddSingleton<IPrintApiService, PrintApiService>();

        // Configuration Screens API Services
        services.AddSingleton<ICompanySettingsApiService, CompanySettingsApiService>();
        services.AddSingleton<IDocumentSequenceApiService, DocumentSequenceApiService>();
        services.AddSingleton<IAccountCategoryApiService, AccountCategoryApiService>();
        services.AddSingleton<ISystemAccountMappingApiService, SystemAccountMappingApiService>();

        // Health check
        services.AddSingleton<IDatabaseHealthCheckService, DatabaseHealthCheckService>();

        // Financial Reports API
        services.AddSingleton<IFinancialReportApiService, FinancialReportApiService>();

        // Product Import API Service
        services.AddSingleton<IProductImportApiService, ProductImportApiService>();

        // Phase 31 — Sales, Purchase, CashBox, and User Report API Services
        services.AddSingleton<ISalesReportApiService, SalesReportApiService>();
        services.AddSingleton<IPurchaseReportApiService, PurchaseReportApiService>();
        services.AddSingleton<ICashBoxReportApiService, CashBoxReportApiService>();
        services.AddSingleton<IUserReportApiService, UserReportApiService>();

        // Phase 31 — Report Export Dialog Service
        services.AddSingleton<IReportExportDialogService, ReportExportDialogService>();

        // Phase 31 — Financial Report Export Service (Excel + PDF)
        services.AddSingleton<IFinancialReportExportService, FinancialReportExportService>();

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
        services.AddTransient<ProductImportViewModel>();
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
        services.AddTransient<InventoryTransactionListViewModel>();
        services.AddTransient<InventoryTransactionEditorViewModel>();
        services.AddTransient<SupplierPaymentsListViewModel>();
        services.AddTransient<SupplierPaymentEditorViewModel>();
        services.AddTransient<SalesReturnListViewModel>();
        services.AddTransient<SalesReturnEditorViewModel>();
        services.AddTransient<PurchaseReturnListViewModel>();
        services.AddTransient<PurchaseReturnEditorViewModel>();
        services.AddTransient<WarehouseTransfersListViewModel>();
        services.AddTransient<WarehouseTransferEditorViewModel>();
        services.AddTransient<UnitListViewModel>();
        services.AddTransient<UnitEditorViewModel>();
        services.AddTransient<ReportsViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<CostingMethodSettingsViewModel>();
        services.AddTransient<SystemSettingsViewModel>();
        services.AddTransient<BackupViewModel>();
        services.AddTransient<ProductUnitEditorViewModel>();
        services.AddTransient<ProductUnitsListViewModel>();

        // Tax ViewModels
        services.AddTransient<TaxesListViewModel>();
        services.AddTransient<TaxEditorViewModel>();

        // Currency ViewModels
        services.AddTransient<CurrenciesListViewModel>();
        services.AddTransient<CurrencyEditorViewModel>();
        services.AddTransient<CurrencyRatesViewModel>();
        services.AddTransient<CurrencyRatesView>();

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
        services.AddTransient<StockBalanceReportViewModel>();
        services.AddTransient<WarehouseMovementReportViewModel>();

        // Journal Entry ViewModels
        services.AddTransient<JournalEntriesListViewModel>();
        services.AddTransient<JournalEntryEditorViewModel>();

        // Fiscal Year ViewModels
        services.AddTransient<FiscalYearListViewModel>();
        services.AddTransient<FiscalYearEditorViewModel>();

        // Configuration Screens ViewModels
        services.AddTransient<CompanySettingsViewModel>();
        services.AddTransient<DocumentSequenceListViewModel>();
        services.AddTransient<AccountCategoryListViewModel>();
        services.AddTransient<AccountCategoryEditorViewModel>();
        services.AddTransient<SystemAccountMappingListViewModel>();
        services.AddTransient<SystemAccountMappingEditorViewModel>();

        // Audit & Permission ViewModels
        services.AddTransient<AuditLogListViewModel>();
        services.AddTransient<SystemLogListViewModel>();
        services.AddTransient<SystemLogDetailViewModel>();
        services.AddTransient<PermissionManagementViewModel>();
        services.AddTransient<PasswordChangeViewModel>();
        services.AddTransient<SetPasswordViewModel>();

        // Role ViewModels
        services.AddTransient<RoleListViewModel>();
        services.AddTransient<RoleEditorViewModel>();
        services.AddTransient<RolePermissionViewModel>();

        // Session ViewModels
        services.AddTransient<UserSessionListViewModel>();

        // Financial Report ViewModels
        services.AddTransient<ViewModels.Reports.IncomeStatementViewModel>();
        services.AddTransient<ViewModels.Reports.CashFlowReportViewModel>();
        services.AddTransient<ViewModels.Reports.VatReportViewModel>();
        services.AddTransient<ViewModels.Reports.AccountStatementViewModel>();
        services.AddTransient<IncomeStatementViewModel>();
        services.AddTransient<CashFlowReportViewModel>();
        services.AddTransient<VatReportViewModel>();
        services.AddTransient<AccountStatementViewModel>();

        // Phase 31 — Financial Report ViewModels
        services.AddTransient<BalanceSheetViewModel>();
        services.AddTransient<TrialBalanceViewModel>();
        services.AddTransient<GeneralLedgerViewModel>();

        // Phase 31 — Sales Report ViewModels
        services.AddTransient<SalesByCustomerViewModel>();
        services.AddTransient<SalesByProductViewModel>();
        services.AddTransient<SalesByCategoryViewModel>();
        services.AddTransient<DailySalesViewModel>();

        // Phase 31 — Purchase Report ViewModels
        services.AddTransient<PurchasesBySupplierViewModel>();
        services.AddTransient<PurchasesByProductViewModel>();

        // Phase 31 — Cash Box Report ViewModels
        services.AddTransient<CashBoxSummaryViewModel>();
        services.AddTransient<DailyClosureReportViewModel>();

        // Phase 31 — User & Login Report ViewModels
        services.AddTransient<UserActivityViewModel>();
        services.AddTransient<LoginHistoryViewModel>();

        // Phase 31v2 — New Report ViewModels
        services.AddTransient<DetailedStockLedgerViewModel>();
        services.AddTransient<ProductProfitabilityViewModel>();
        services.AddTransient<ProfitByCustomerViewModel>();
        services.AddTransient<ReturnsReportViewModel>();
        services.AddTransient<AgingReportViewModel>();
        services.AddTransient<WorkingCapitalViewModel>();
        services.AddTransient<AccountBalancesViewModel>();

        // New Module ViewModels (CustomerReceipt, InventoryCount, InventoryAdjustment, Notifications, Attachments)
        services.AddTransient<CustomerReceiptListViewModel>();
        services.AddTransient<CustomerReceiptEditorViewModel>();
        services.AddTransient<InventoryCountListViewModel>();
        services.AddTransient<InventoryCountEditorViewModel>();
        services.AddTransient<InventoryAdjustmentListViewModel>();
        services.AddTransient<InventoryAdjustmentEditorViewModel>();
        services.AddTransient<NotificationListViewModel>();
        services.AddTransient<AttachmentListViewModel>();

        // New Module ViewModels (Branch, Department, Employee, Bank, Party, Expense)
        services.AddTransient<BranchListViewModel>();
        services.AddTransient<BranchEditorViewModel>();
        services.AddTransient<DepartmentListViewModel>();
        services.AddTransient<DepartmentEditorViewModel>();
        services.AddTransient<EmployeeListViewModel>();
        services.AddTransient<EmployeeEditorViewModel>();
        services.AddTransient<BankListViewModel>();
        services.AddTransient<BankEditorViewModel>();
        services.AddTransient<PartyListViewModel>();
        services.AddTransient<PartyEditorViewModel>();
        services.AddTransient<ExpenseListViewModel>();
        services.AddTransient<ExpenseEditorViewModel>();

        // Customer/Supplier Contact ViewModels
        services.AddTransient<CustomerContactListViewModel>();
        services.AddTransient<CustomerContactEditorViewModel>();
        services.AddTransient<SupplierContactListViewModel>();
        services.AddTransient<SupplierContactEditorViewModel>();

        // Payment Voucher ViewModels
        services.AddTransient<ViewModels.PaymentVouchers.PaymentVoucherListViewModel>();
        services.AddTransient<ViewModels.PaymentVouchers.PaymentVoucherEditorViewModel>();

        // Receipt Voucher ViewModels
        services.AddTransient<ReceiptVoucherListViewModel>();
        services.AddTransient<ReceiptVoucherEditorViewModel>();
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
