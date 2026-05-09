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
using SalesSystem.Desktop.Services.Api.Interfaces;
using SalesSystem.Desktop.Services.Api;
using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Desktop.Controls.Suppliers;
using SalesSystem.Desktop.Controls.Customers;
using SalesSystem.Desktop.Controls.Categories;
using SalesSystem.Desktop.Controls.Units;
using SalesSystem.Desktop.Controls.Products;
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
        ApplicationConfiguration.Initialize();

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

                // API Services (Modular Architecture)
                services.AddScoped<SalesSystem.Desktop.Services.Api.Interfaces.IAuthApiService, SalesSystem.Desktop.Services.Api.AuthApiService>();
                services.AddScoped<SalesSystem.Desktop.Services.Api.Interfaces.IProductApiService, SalesSystem.Desktop.Services.Api.ProductApiService>();
                services.AddScoped<SalesSystem.Desktop.Services.Api.Interfaces.ICategoryApiService, SalesSystem.Desktop.Services.Api.CategoryApiService>();
                services.AddScoped<SalesSystem.Desktop.Services.Api.Interfaces.IUnitApiService, SalesSystem.Desktop.Services.Api.UnitApiService>();
                services.AddScoped<SalesSystem.Desktop.Services.Api.Interfaces.ICustomerApiService, SalesSystem.Desktop.Services.Api.CustomerApiService>();
                services.AddScoped<SalesSystem.Desktop.Services.Api.Interfaces.ISupplierApiService, SalesSystem.Desktop.Services.Api.SupplierApiService>();
                services.AddScoped<SalesSystem.Desktop.Services.Api.Interfaces.IWarehouseApiService, SalesSystem.Desktop.Services.Api.WarehouseApiService>();
                services.AddScoped<SalesSystem.Desktop.Services.Api.Interfaces.ISalesInvoiceApiService, SalesSystem.Desktop.Services.Api.SalesInvoiceApiService>();
                services.AddScoped<SalesSystem.Desktop.Services.Api.Interfaces.IPurchaseInvoiceApiService, SalesSystem.Desktop.Services.Api.PurchaseInvoiceApiService>();
                services.AddScoped<SalesSystem.Desktop.Services.Api.Interfaces.ISalesReturnApiService, SalesSystem.Desktop.Services.Api.SalesReturnApiService>();
                services.AddScoped<SalesSystem.Desktop.Services.Api.Interfaces.IPurchaseReturnApiService, SalesSystem.Desktop.Services.Api.PurchaseReturnApiService>();
                services.AddScoped<SalesSystem.Desktop.Services.Api.Interfaces.IStockTransferApiService, SalesSystem.Desktop.Services.Api.StockTransferApiService>();
                services.AddScoped<SalesSystem.Desktop.Services.Api.Interfaces.ICustomerPaymentApiService, SalesSystem.Desktop.Services.Api.CustomerPaymentApiService>();
                services.AddScoped<SalesSystem.Desktop.Services.Api.Interfaces.ISupplierPaymentApiService, SalesSystem.Desktop.Services.Api.SupplierPaymentApiService>();
                services.AddScoped<SalesSystem.Desktop.Services.Api.Interfaces.IReportApiService, SalesSystem.Desktop.Services.Api.ReportApiService>();
                services.AddScoped<SalesSystem.Desktop.Services.Api.Interfaces.IDashboardApiService, SalesSystem.Desktop.Services.Api.DashboardApiService>();
                services.AddScoped<SalesSystem.Desktop.Services.Api.Interfaces.IInventoryApiService, SalesSystem.Desktop.Services.Api.InventoryApiService>();
                services.AddScoped<SalesSystem.Desktop.Services.Api.Interfaces.ISettingsApiService, SalesSystem.Desktop.Services.Api.SettingsApiService>();
                services.AddScoped<SalesSystem.Desktop.Services.Api.Interfaces.IUserApiService, SalesSystem.Desktop.Services.Api.UserApiService>();

                // Core Infrastructure Services
                services.AddSingleton<IEventBus, EventBus>();
                services.AddSingleton<ISessionService, SessionService>();
                services.AddSingleton<INavigationService, NavigationService>();
                services.AddSingleton<INotificationService, NotificationService>();
                services.AddSingleton<IDialogService, DialogService>();

                // UI Components - Forms
                services.AddTransient<LoginForm>();
                services.AddTransient<MainForm>();
                services.AddTransient<ProductEditorForm>();
                services.AddTransient<CustomerEditorForm>();
                services.AddTransient<SupplierEditorForm>();
                services.AddTransient<CategoryDialog>();
                services.AddTransient<CategoryManagerDialog>();
                services.AddTransient<UnitDialog>();
                services.AddTransient<UnitManagerDialog>();
                services.AddTransient<WarehouseEditorForm>();
                services.AddTransient<SalesInvoiceForm>();
                services.AddTransient<PurchaseInvoiceForm>();
                services.AddTransient<SalesReturnForm>();
                services.AddTransient<PurchaseReturnForm>();
                services.AddTransient<StockTransferForm>();
                services.AddTransient<CustomerPaymentDialog>();
                services.AddTransient<SupplierPaymentDialog>();
                
                // Factories

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
                
                // Placeholder Controls (Redirect to placeholders until fully implemented)
            })
            .Build();

        using var scope = host.Services.CreateScope();
        var serviceProvider = scope.ServiceProvider;
        
        try
        {
            var loginForm = serviceProvider.GetRequiredService<LoginForm>();
            Application.Run(loginForm);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"ط­ط¯ط« ط®ط·ط£ ط£ط«ظ†ط§ط، طھط´ط؛ظٹظ„ ط§ظ„طھط·ط¨ظٹظ‚: {ex.Message}", "ط®ط·ط£", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
