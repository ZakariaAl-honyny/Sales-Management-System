using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SalesSystem.Desktop.Configuration;
using SalesSystem.Desktop.Forms;
using SalesSystem.Desktop.Services;
using SalesSystem.Desktop.Services.Http;
using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Desktop.Controls.Placeholders;

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
                
                services.AddHttpClient<IAuthApiService, AuthApiService>(client =>
                {
                    client.BaseAddress = new Uri(apiSettings.BaseUrl);
                }).AddHttpMessageHandler<AuthTokenHandler>();

                services.AddHttpClient<ICategoryApiService, CategoryApiService>(client =>
                {
                    client.BaseAddress = new Uri(apiSettings.BaseUrl);
                }).AddHttpMessageHandler<AuthTokenHandler>();

                services.AddHttpClient<IUnitApiService, UnitApiService>(client =>
                {
                    client.BaseAddress = new Uri(apiSettings.BaseUrl);
                }).AddHttpMessageHandler<AuthTokenHandler>();

                services.AddHttpClient<IWarehouseApiService, WarehouseApiService>(client =>
                {
                    client.BaseAddress = new Uri(apiSettings.BaseUrl);
                }).AddHttpMessageHandler<AuthTokenHandler>();

                services.AddHttpClient<IProductApiService, ProductApiService>(client =>
                {
                    client.BaseAddress = new Uri(apiSettings.BaseUrl);
                }).AddHttpMessageHandler<AuthTokenHandler>();

                services.AddHttpClient<ICustomerApiService, CustomerApiService>(client =>
                {
                    client.BaseAddress = new Uri(apiSettings.BaseUrl);
                }).AddHttpMessageHandler<AuthTokenHandler>();

                services.AddHttpClient<ISupplierApiService, SupplierApiService>(client =>
                {
                    client.BaseAddress = new Uri(apiSettings.BaseUrl);
                }).AddHttpMessageHandler<AuthTokenHandler>();

                services.AddHttpClient<ISalesInvoiceApiService, SalesInvoiceApiService>(client =>
                {
                    client.BaseAddress = new Uri(apiSettings.BaseUrl);
                }).AddHttpMessageHandler<AuthTokenHandler>();

                services.AddHttpClient<IPurchaseInvoiceApiService, PurchaseInvoiceApiService>(client =>
                {
                    client.BaseAddress = new Uri(apiSettings.BaseUrl);
                }).AddHttpMessageHandler<AuthTokenHandler>();

                services.AddHttpClient<IInventoryApiService, InventoryApiService>(client =>
                {
                    client.BaseAddress = new Uri(apiSettings.BaseUrl);
                }).AddHttpMessageHandler<AuthTokenHandler>();

                services.AddHttpClient<IStockTransferApiService, StockTransferApiService>(client =>
                {
                    client.BaseAddress = new Uri(apiSettings.BaseUrl);
                }).AddHttpMessageHandler<AuthTokenHandler>();

                services.AddHttpClient<ISalesReturnApiService, SalesReturnApiService>(client =>
                {
                    client.BaseAddress = new Uri(apiSettings.BaseUrl);
                }).AddHttpMessageHandler<AuthTokenHandler>();

                services.AddHttpClient<IPurchaseReturnApiService, PurchaseReturnApiService>(client =>
                {
                    client.BaseAddress = new Uri(apiSettings.BaseUrl);
                }).AddHttpMessageHandler<AuthTokenHandler>();

                services.AddHttpClient<ICustomerPaymentApiService, CustomerPaymentApiService>(client =>
                {
                    client.BaseAddress = new Uri(apiSettings.BaseUrl);
                }).AddHttpMessageHandler<AuthTokenHandler>();

                services.AddHttpClient<ISupplierPaymentApiService, SupplierPaymentApiService>(client =>
                {
                    client.BaseAddress = new Uri(apiSettings.BaseUrl);
                }).AddHttpMessageHandler<AuthTokenHandler>();

                services.AddHttpClient<IDashboardApiService, DashboardApiService>(client =>
                {
                    client.BaseAddress = new Uri(apiSettings.BaseUrl);
                }).AddHttpMessageHandler<AuthTokenHandler>();

                services.AddHttpClient<ISettingsApiService, SettingsApiService>(client =>
                {
                    client.BaseAddress = new Uri(apiSettings.BaseUrl);
                }).AddHttpMessageHandler<AuthTokenHandler>();

                services.AddHttpClient<IUserApiService, UserApiService>(client =>
                {
                    client.BaseAddress = new Uri(apiSettings.BaseUrl);
                }).AddHttpMessageHandler<AuthTokenHandler>();

                // Services
                services.AddSingleton<IEventBus, EventBus>();
                services.AddSingleton<ISessionService, SessionService>();
                services.AddSingleton<INavigationService, NavigationService>();
                services.AddSingleton<INotificationService, NotificationService>();
                services.AddSingleton<IDialogService, DialogService>();

                // Forms
                services.AddTransient<LoginForm>();
                services.AddTransient<MainForm>();

                // Placeholders
                services.AddTransient<DashboardControl>();
                services.AddTransient<ProductsControl>();
                services.AddTransient<CustomersControl>();
                services.AddTransient<SuppliersControl>();
                services.AddTransient<WarehousesControl>();
                services.AddTransient<PurchasesControl>();
                services.AddTransient<SalesControl>();
                services.AddTransient<ReturnsControl>();
                services.AddTransient<InventoryControl>();
                services.AddTransient<StockTransfersControl>();
                services.AddTransient<PaymentsControl>();
                services.AddTransient<ReportsControl>();
                services.AddTransient<SettingsControl>();
                services.AddTransient<UsersControl>();
            })
            .Build();

        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;
        
        try
        {
            var loginForm = services.GetRequiredService<LoginForm>();
            Application.Run(loginForm);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"حدث خطأ أثناء تشغيل التطبيق: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}