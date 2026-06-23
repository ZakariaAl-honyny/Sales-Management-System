namespace SalesSystem.DesktopPWF.Tests.ViewModels.Dashboard;

using System.Collections.ObjectModel;

using FluentAssertions;
using Moq;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.DesktopPWF.Services;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.ViewModels;
using Xunit;

/// <summary>
/// Unit tests for DashboardViewModel
/// </summary>
public class DashboardViewModelTests : IDisposable
{
    private readonly Mock<ISalesInvoiceApiService> _mockSalesService;
    private readonly Mock<IProductApiService> _mockProductService;
    private readonly Mock<IDashboardApiService> _mockDashboardService;
    private readonly Mock<IEventBus> _mockEventBus;
    private readonly Mock<IReportApiService> _mockReportService;
    private readonly Mock<ISettingsApiService> _mockSettingsService;
    private readonly Mock<IDialogService> _mockDialogService;

    public DashboardViewModelTests()
    {
        _mockSalesService = new Mock<ISalesInvoiceApiService>();
        _mockProductService = new Mock<IProductApiService>();
        _mockDashboardService = new Mock<IDashboardApiService>();
        _mockEventBus = new Mock<IEventBus>();
        _mockReportService = new Mock<IReportApiService>();
        _mockSettingsService = new Mock<ISettingsApiService>();
        _mockDialogService = new Mock<IDialogService>();
    }

    private DashboardViewModel CreateViewModel()
    {
        return new DashboardViewModel(
            _mockEventBus.Object,
            _mockDashboardService.Object,
            _mockSalesService.Object,
            _mockProductService.Object,
            _mockReportService.Object,
            _mockSettingsService.Object,
            _mockDialogService.Object);
    }

    private void SetupDefaultMocks()
    {
        _mockDashboardService
            .Setup(x => x.GetSummaryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<DashboardSummaryDto>.Success(new DashboardSummaryDto(
                TotalSalesToday: 1000m,
                NumberOfSalesToday: 5,
                TotalPurchasesToday: 500m,
                LowStockItemsCount: 3,
                ActiveCustomersCount: 10,
                ActiveSuppliersCount: 5,
                TotalProductsCount: 100,
                TotalReceivables: 5000m,
                TotalPayables: 3000m,
                TotalSalesMonth: 20000m,
                TotalPurchasesMonth: 10000m)));

        _mockSalesService
            .Setup(x => x.GetAllAsync(
                It.IsAny<string?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<byte?>(),
                It.IsAny<bool>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<SalesInvoiceDto>>.Success(new List<SalesInvoiceDto>()));

        _mockProductService
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(Result<List<ProductDto>>.Success(new List<ProductDto>()));
    }

    public void Dispose()
    {
    }

    #region LoadDashboardDataCommand Tests

    [Fact]
    public async Task LoadDashboardDataCommand_Executed_LoadsAllStats()
    {
        SetupDefaultMocks();

        var viewModel = CreateViewModel();

        viewModel.RefreshCommand.Execute(null);
        await Task.Delay(200);

        viewModel.TotalCustomers.Should().Be(10, "should use dashboard summary value");
        viewModel.LowStockCount.Should().Be(3, "should use dashboard summary value");
    }

    [Fact]
    public async Task RefreshCommand_WhenAlreadyExecuting_DoesNotReload()
    {
        var callCount = 0;
        var pendingTcs = new TaskCompletionSource<Result<DashboardSummaryDto>>();

        _mockDashboardService
            .Setup(x => x.GetSummaryAsync(It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                callCount++;
                return pendingTcs.Task;
            });

        _mockSalesService
            .Setup(x => x.GetAllAsync(
                It.IsAny<string?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<byte?>(),
                It.IsAny<bool>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<SalesInvoiceDto>>.Success(new List<SalesInvoiceDto>()));

        _mockProductService
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(Result<List<ProductDto>>.Success(new List<ProductDto>()));

        var viewModel = CreateViewModel();

        // First execution — keep it pending via TaskCompletionSource
        viewModel.RefreshCommand.Execute(null);
        await Task.Delay(50);

        // Second execution while first is still running — should be ignored by AsyncRelayCommand
        viewModel.RefreshCommand.Execute(null);

        // Now let the first execution complete
        var summary = new DashboardSummaryDto(
            TotalSalesToday: 0m, NumberOfSalesToday: 0, TotalPurchasesToday: 0m,
            LowStockItemsCount: 0, ActiveCustomersCount: 0, ActiveSuppliersCount: 0,
            TotalProductsCount: 0, TotalReceivables: 0m, TotalPayables: 0m,
            TotalSalesMonth: 0m, TotalPurchasesMonth: 0m);
        pendingTcs.SetResult(Result<DashboardSummaryDto>.Success(summary));
        await Task.Delay(200);

        callCount.Should().Be(1, "concurrent execution should be prevented by AsyncRelayCommand");
    }

    #endregion

    #region EventBus Message Tests

    [Fact]
    public void SubscribeToEvents_RegistersAllEventHandlers()
    {
        // Capture the handlers when Subscribe is called
        Action<SaleInvoiceChangedMessage>? saleHandler = null;
        Action<ProductChangedMessage>? productHandler = null;
        Action<StockChangedMessage>? stockHandler = null;

        _mockEventBus
            .Setup(x => x.Subscribe(It.IsAny<Action<SaleInvoiceChangedMessage>>()))
            .Callback<Action<SaleInvoiceChangedMessage>>(h => saleHandler = h);

        _mockEventBus
            .Setup(x => x.Subscribe(It.IsAny<Action<ProductChangedMessage>>()))
            .Callback<Action<ProductChangedMessage>>(h => productHandler = h);

        _mockEventBus
            .Setup(x => x.Subscribe(It.IsAny<Action<StockChangedMessage>>()))
            .Callback<Action<StockChangedMessage>>(h => stockHandler = h);

        var viewModel = CreateViewModel();

        // Manually call SubscribeToEvents to verify handlers are registered
        var subscribeMethod = typeof(DashboardViewModel).GetMethod("SubscribeToEvents",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        subscribeMethod?.Invoke(viewModel, null);

        _mockEventBus.Verify(x => x.Subscribe<SaleInvoiceChangedMessage>(It.IsAny<Action<SaleInvoiceChangedMessage>>()), Times.Once);
        _mockEventBus.Verify(x => x.Subscribe<ProductChangedMessage>(It.IsAny<Action<ProductChangedMessage>>()), Times.Once);
        _mockEventBus.Verify(x => x.Subscribe<StockChangedMessage>(It.IsAny<Action<StockChangedMessage>>()), Times.Once);
    }

    [Fact]
    public async Task SaleInvoiceChangedMessage_Received_RefreshesData()
    {
        Action<SaleInvoiceChangedMessage>? capturedHandler = null;

        // Setup generic Subscribe<T> to capture the handler
        _mockEventBus
            .Setup(x => x.Subscribe(It.IsAny<Action<SaleInvoiceChangedMessage>>()))
            .Callback<Action<SaleInvoiceChangedMessage>>(h => capturedHandler = h);

        SetupDefaultMocks();

        var viewModel = CreateViewModel();

        // Manually subscribe to trigger handler capture
        var subscribeMethod = typeof(DashboardViewModel).GetMethod("SubscribeToEvents",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        subscribeMethod?.Invoke(viewModel, null);

        // Now invoke the captured handler (simulating message arrival)
        var message = new SaleInvoiceChangedMessage(123);
        capturedHandler?.Invoke(message);

        // Wait for any async operations
        await Task.Delay(100);

        // Verify LoadDataAsync was called via RefreshData
        // The handler calls RefreshData() which dispatches to LoadDataAsync
        _mockDashboardService.Verify(x => x.GetSummaryAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        _mockSalesService.Verify(x => x.GetAllAsync(
            It.IsAny<string?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<byte?>(),
            It.IsAny<bool>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<int?>(),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);

        viewModel.Cleanup();
    }

    [Fact]
    public async Task ProductChangedMessage_Received_RefreshesData()
    {
        Action<ProductChangedMessage>? capturedHandler = null;

        _mockEventBus
            .Setup(x => x.Subscribe(It.IsAny<Action<ProductChangedMessage>>()))
            .Callback<Action<ProductChangedMessage>>(h => capturedHandler = h);

        SetupDefaultMocks();

        var viewModel = CreateViewModel();

        var subscribeMethod = typeof(DashboardViewModel).GetMethod("SubscribeToEvents",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        subscribeMethod?.Invoke(viewModel, null);

        var message = new ProductChangedMessage(1);
        capturedHandler?.Invoke(message);

        await Task.Delay(100);

        // Verify dashboard data was refreshed
        _mockDashboardService.Verify(x => x.GetSummaryAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);

        viewModel.Cleanup();
    }

    [Fact]
    public async Task StockTransferChangedMessage_Received_RefreshesData()
    {
        Action<StockChangedMessage>? capturedHandler = null;

        _mockEventBus
            .Setup(x => x.Subscribe(It.IsAny<Action<StockChangedMessage>>()))
            .Callback<Action<StockChangedMessage>>(h => capturedHandler = h);

        SetupDefaultMocks();

        var viewModel = CreateViewModel();

        var subscribeMethod = typeof(DashboardViewModel).GetMethod("SubscribeToEvents",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        subscribeMethod?.Invoke(viewModel, null);

        var message = new StockChangedMessage(1, 5);
        capturedHandler?.Invoke(message);

        await Task.Delay(100);

        // Verify dashboard data was refreshed (stock changes affect dashboard)
        _mockDashboardService.Verify(x => x.GetSummaryAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);

        viewModel.Cleanup();
    }

    [Fact]
    public void Cleanup_RemovesEventHandlers()
    {
        var viewModel = CreateViewModel();

        viewModel.Cleanup();

        _mockEventBus.Verify(x => x.Unsubscribe<SaleInvoiceChangedMessage>(It.IsAny<Action<SaleInvoiceChangedMessage>>()), Times.Once);
        _mockEventBus.Verify(x => x.Unsubscribe<ProductChangedMessage>(It.IsAny<Action<ProductChangedMessage>>()), Times.Once);
        _mockEventBus.Verify(x => x.Unsubscribe<StockChangedMessage>(It.IsAny<Action<StockChangedMessage>>()), Times.Once);
    }

    #endregion

    #region Net Profit Calculation Tests

    [Fact]
    public async Task DashboardData_LoadsSummary_Correctly()
    {
        _mockDashboardService
            .Setup(x => x.GetSummaryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<DashboardSummaryDto>.Success(new DashboardSummaryDto(
                TotalSalesToday: 5000m,
                NumberOfSalesToday: 10,
                TotalPurchasesToday: 3000m,
                LowStockItemsCount: 5,
                ActiveCustomersCount: 25,
                ActiveSuppliersCount: 15,
                TotalProductsCount: 200,
                TotalReceivables: 15000m,
                TotalPayables: 8000m,
                TotalSalesMonth: 100000m,
                TotalPurchasesMonth: 60000m)));

        _mockSalesService
            .Setup(x => x.GetAllAsync(
                It.IsAny<string?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<byte?>(),
                It.IsAny<bool>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<SalesInvoiceDto>>.Success(new List<SalesInvoiceDto>()));

        _mockProductService
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(Result<List<ProductDto>>.Success(new List<ProductDto>()));

        var viewModel = CreateViewModel();

        viewModel.RefreshCommand.Execute(null);
        await Task.Delay(200);

        // Verify TodaySales and TodayPurchases are populated from dashboard summary
        viewModel.TodaySales.Should().Be(5000m);
        viewModel.TodayPurchases.Should().Be(3000m);
    }

    #endregion

    #region Property Tests

    [Fact]
    public void IsBusy_DefaultIsFalse()
    {
        var viewModel = CreateViewModel();

        viewModel.IsBusy.Should().BeFalse("IsBusy should be false by default");
    }

    [Fact]
    public void TotalCustomers_InitialValue_IsZero()
    {
        var viewModel = CreateViewModel();

        viewModel.TotalCustomers.Should().Be(0, "TotalCustomers should be initially zero");
    }

    #endregion

    #region Helper Methods

    private static SalesInvoiceDto CreateSalesInvoiceDto(
        int id,
        decimal totalAmount,
        byte status)
    {
        return new SalesInvoiceDto(
            Id: id,
            InvoiceNo: 1,
            CustomerId: 1,
            CustomerName: "Test Customer",
            WarehouseId: 1,
            WarehouseName: "Main Warehouse",
            InvoiceDate: DateTime.Today,
            PaymentType: 1,
            SubTotal: totalAmount,
            DiscountAmount: 0,
            TaxAmount: 0,
            OtherCharges: 0,
            NetTotal: totalAmount,
            PaidAmount: totalAmount,
            RemainingAmount: 0,
            Notes: null,
            Status: status,
            TaxId: null,
            TaxName: null,
            TaxRate: null,
            CurrencyId: null,
            ExchangeRate: null,
            CashBoxId: null,
            CashBoxName: null,
            Items: new List<SalesInvoiceLineDto>());
    }

    private static CustomerDto CreateCustomerDto(int id, string name, bool isActive)
    {
        return new CustomerDto(
            Id: id,
            Name: name,
            Phone: null,
            Email: null,
            Address: null,
            TaxNumber: null,
            CreditLimit: 0,
            IsActive: isActive,
            AccountId: 1);
    }

    private static SupplierDto CreateSupplierDto(int id, string name, bool isActive)
    {
        return new SupplierDto(
            Id: id,
            Name: name,
            Phone: null,
            Email: null,
            Address: null,
            TaxNumber: null,
            IsActive: isActive,
            AccountId: 1);
    }

    private static ProductDto CreateProductDto(int id, string name, bool isActive, decimal minStock = 0)
    {
        return new ProductDto(
            Id: id,
            Name: name,
            CategoryId: 1,
            CategoryName: "Category",
            Description: null,
            ReorderLevel: minStock,
            TrackExpiry: false,
            ImagePath: null,
            IsActive: isActive);
    }

    #endregion
}



