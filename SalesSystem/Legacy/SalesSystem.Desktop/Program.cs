using Serilog;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SalesSystem.Desktop.Configuration;
using SalesSystem.Desktop.Forms;
using SalesSystem.Desktop.Services;
using SalesSystem.Desktop.Controls.Returns;
using SalesSystem.Desktop.Controls.Inventory;
using SalesSystem.Desktop.Controls.StockTransfers;
using SalesSystem.Desktop.Controls.Payments;
using SalesSystem.Desktop.Controls.Settings;
using SalesSystem.Desktop.Controls.Users;
using SalesSystem.Desktop.Services.Http;
using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Desktop.Controls.Suppliers;
using SalesSystem.Desktop.Controls.Customers;
using SalesSystem.Desktop.Controls.Categories;
using SalesSystem.Desktop.Controls.Units;
using SalesSystem.Desktop.Controls.Products;
using SalesSystem.Desktop.Printing;
using SalesSystem.Desktop.Printing.Core;
using SalesSystem.Desktop.Controls.Warehouses;
using SalesSystem.Desktop.Controls.Sales;
using SalesSystem.Desktop.Controls.Purchases;
using SalesSystem.Desktop.Controls.Reports;
using SalesSystem.Desktop.Controls.Dashboard;

namespace SalesSystem.Desktop;

static class Program
{
    [STAThread]
    static void Main()
    {
        // 1. Configure Serilog for AI-friendly error logging
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Error()
            .WriteTo.File("Logs/AI_Error_Log_.txt",
                rollingInterval: RollingInterval.Day,
                outputTemplate: "=== {Timestamp:yyyy-MM-dd HH:mm:ss} ==={NewLine}Message: {Message}{NewLine}Exception: {Exception}{NewLine}--------------------------------------------------{NewLine}")
            .CreateLogger();

        try
        {
            ApplicationConfiguration.Initialize();

            // 2. Global Exception Handlers
            Application.ThreadException += GlobalUIExceptionHandler;
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            AppDomain.CurrentDomain.UnhandledException += GlobalBackgroundExceptionHandler;

            var host = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                })
                .ConfigureServices((context, services) =>
                {
                    // Configuration
                    var apiSettings = context.Configuration.GetSection("ApiSettings").Get<ApiSettings>() 
                                     ?? new ApiSettings();
                    services.AddSingleton(apiSettings);

                    // Http Services
                    services.AddTransient<AuthTokenHandler>();
                    
                    services.AddHttpClient<HttpClientService>(client =>
                    {
                        client.BaseAddress = new Uri(apiSettings.BaseUrl);
                    }).AddHttpMessageHandler<AuthTokenHandler>();

                    // API Services (Inject HttpClientService)
                    services.AddTransient<IAuthApiService, AuthApiService>();
                    services.AddTransient<IProductApiService, ProductApiService>();
                    services.AddTransient<ICategoryApiService, CategoryApiService>();
                    services.AddTransient<IUnitApiService, UnitApiService>();
                    services.AddTransient<ICustomerApiService, CustomerApiService>();
                    services.AddTransient<ISupplierApiService, SupplierApiService>();
                    services.AddTransient<IWarehouseApiService, WarehouseApiService>();
                    services.AddTransient<ISalesInvoiceApiService, SalesInvoiceApiService>();
                    services.AddTransient<IPurchaseInvoiceApiService, PurchaseInvoiceApiService>();
                    services.AddTransient<ISalesReturnApiService, SalesReturnApiService>();
                    services.AddTransient<IPurchaseReturnApiService, PurchaseReturnApiService>();
                    services.AddTransient<IStockTransferApiService, StockTransferApiService>();
                    services.AddTransient<ICustomerPaymentApiService, CustomerPaymentApiService>();
                    services.AddTransient<ISupplierPaymentApiService, SupplierPaymentApiService>();
                    services.AddTransient<IReportApiService, ReportApiService>();
                    services.AddTransient<IDashboardApiService, DashboardApiService>();
                    services.AddTransient<IInventoryApiService, InventoryApiService>();
                    services.AddTransient<ISettingsApiService, SettingsApiService>();
                    services.AddTransient<IUserApiService, UserApiService>();

                    // Core Infrastructure Services
                    services.AddSingleton<IEventBus, EventBus>();
                    services.AddSingleton<ISessionService, SessionService>();
                    services.AddSingleton<INavigationService, NavigationService>();
                    services.AddSingleton<INotificationService, NotificationService>();
                    services.AddSingleton<IDialogService, DialogService>();
                    
                    // Printing Services
                    services.AddTransient<IInvoicePrinter, InvoicePrinter>();
                    services.AddTransient<IReceiptPrinter, ReceiptPrinter>();

                    // UI Components - Essential Forms
                    services.AddTransient<LoginForm>();
                    services.AddTransient<MainForm>();
                    services.AddTransient<CategoryDialog>();
                    services.AddTransient<CategoryManagerDialog>();
                    services.AddTransient<UnitDialog>();
                    services.AddTransient<UnitManagerDialog>();
                    services.AddTransient<WarehouseEditorForm>();
                    services.AddTransient<CustomerEditorForm>();
                    services.AddTransient<SupplierEditorForm>();
                    services.AddTransient<ProductEditorForm>();
                    services.AddTransient<SalesInvoiceForm>();
                    services.AddTransient<PurchaseInvoiceForm>();
                    services.AddTransient<SalesReturnForm>();
                    services.AddTransient<PurchaseReturnForm>();
                    services.AddTransient<StockTransferForm>();
                    
                    // Module Controls
                    services.AddTransient<DashboardControl>();
                    services.AddTransient<ProductsListControl>();
                    services.AddTransient<CustomersListControl>();
                    services.AddTransient<SuppliersListControl>();
                    services.AddTransient<WarehousesListControl>();
                    services.AddTransient<PurchasesListControl>();
                    services.AddTransient<SalesListControl>();
                    services.AddTransient<ReportsListControl>();
                    services.AddTransient<ReturnsListControl>();
                    services.AddTransient<InventoryListControl>();
                    services.AddTransient<StockTransfersListControl>();
                    services.AddTransient<PaymentsListControl>();
                    services.AddTransient<SettingsListControl>();
                    services.AddTransient<UsersListControl>();
                })
                .Build();

            using var scope = host.Services.CreateScope();
            var serviceProvider = scope.ServiceProvider;
            
            var loginForm = serviceProvider.GetRequiredService<LoginForm>();
            Application.Run(loginForm);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
            MessageBox.Show($"حدث خطأ كارثي أثناء تشغيل التطبيق. تم تسجيل التفاصيل في ملف Logs.\nالخطأ: {ex.Message}", "خطأ فادح", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static void GlobalUIExceptionHandler(object sender, ThreadExceptionEventArgs e)
    {
        Log.Error(e.Exception, "Unhandled UI Thread Exception");
        MessageBox.Show("عذراً، حدث خطأ غير متوقع في واجهة المستخدم. تم تسجيل التفاصيل للدعم الفني.", "خطأ بالنظام", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private static void GlobalBackgroundExceptionHandler(object sender, UnhandledExceptionEventArgs e)
    {
        var ex = e.ExceptionObject as Exception;
        Log.Error(ex, "Unhandled Background Exception");
        MessageBox.Show("عذراً، حدث خطأ غير متوقع في العمليات الخلفية.", "خطأ بالنظام", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
