using System.Runtime.Serialization;
using FluentAssertions;
using Moq;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.DesktopPWF.Services;
using SalesSystem.DesktopPWF.ViewModels;
using Xunit;

namespace SalesSystem.Desktop.Tests.ViewModels.Dashboard;

/// <summary>
/// Unit tests for DashboardViewModel
/// </summary>
public class DashboardViewModelTests : IDisposable
{
    private readonly Mock<ISalesInvoiceApiService> _mockSalesService;
    private readonly Mock<ICustomerApiService> _mockCustomerService;
    private readonly Mock<ISupplierApiService> _mockSupplierService;
    private readonly Mock<IProductApiService> _mockProductService;
    private readonly Mock<IEventBus> _mockEventBus;

    public DashboardViewModelTests()
    {
        _mockSalesService = new Mock<ISalesInvoiceApiService>();
        _mockCustomerService = new Mock<ICustomerApiService>();
        _mockSupplierService = new Mock<ISupplierApiService>();
        _mockProductService = new Mock<IProductApiService>();
        _mockEventBus = new Mock<IEventBus>();
    }

    private DashboardViewModel CreateViewModel()
    {
        // Create ViewModel WITHOUT calling constructor (avoids App.GetService issue)
        var viewModel = (DashboardViewModel)FormatterServices.GetUninitializedObject(typeof(DashboardViewModel));

        // Set private fields via reflection
        var fieldNames = new[] {
            "_salesInvoiceService",
            "_customerService",
            "_supplierService",
            "_productService",
            "_eventBus"
        };
        var mockObjects = new object[] {
            _mockSalesService.Object,
            _mockCustomerService.Object,
            _mockSupplierService.Object,
            _mockProductService.Object,
            _mockEventBus.Object
        };

        for (int i = 0; i < fieldNames.Length; i++)
        {
            var field = typeof(DashboardViewModel).GetField(fieldNames[i],
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(viewModel, mockObjects[i]);
        }

        // Initialize collections via backing fields (these are read-only auto-properties)
        // Auto-properties have backing fields with compiler-generated names
        var recentInvoicesField = typeof(DashboardViewModel).GetField("<RecentInvoices>k__BackingField",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        recentInvoicesField?.SetValue(viewModel, new System.Collections.ObjectModel.ObservableCollection<RecentInvoiceItem>());

        var lowStockItemsField = typeof(DashboardViewModel).GetField("<LowStockItems>k__BackingField",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        lowStockItemsField?.SetValue(viewModel, new System.Collections.ObjectModel.ObservableCollection<LowStockItem>());

        // Create and set the RefreshCommand
        var refreshCommand = new AsyncRelayCommand(async _ => await viewModel.LoadDataAsync());
        var commandField = typeof(DashboardViewModel).GetField("_refreshCommand",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        commandField?.SetValue(viewModel, refreshCommand);

        // Subscribe to EventBus events manually (normally done in constructor)
        var subscribeMethod = typeof(DashboardViewModel).GetMethod("SubscribeToEvents",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        subscribeMethod?.Invoke(viewModel, null);

        return viewModel;
    }

    public void Dispose()
    {
        // Cleanup if needed
    }

    #region LoadDashboardDataCommand Tests

    [Fact]
    public async Task LoadDashboardDataCommand_Executed_LoadsAllStats()
    {
        // Arrange
        var todayInvoices = new List<SalesInvoiceDto>
        {
            CreateSalesInvoiceDto(1, "INV-2026-001", 1000m, 2),
            CreateSalesInvoiceDto(2, "INV-2026-002", 500m, 2)
        };

        var monthInvoices = new List<SalesInvoiceDto>
        {
            CreateSalesInvoiceDto(3, "INV-2026-003", 2000m, 2),
            CreateSalesInvoiceDto(4, "INV-2026-004", 1500m, 2)
        };

        var yearInvoices = new List<SalesInvoiceDto>
        {
            CreateSalesInvoiceDto(5, "INV-2026-005", 3000m, 2),
            CreateSalesInvoiceDto(6, "INV-2026-006", 2500m, 2)
        };

        var customers = new List<CustomerDto>
        {
            CreateCustomerDto(1, "عميل 1", true),
            CreateCustomerDto(2, "عميل 2", true),
            CreateCustomerDto(3, "عميل 3", false) // inactive
        };

        var suppliers = new List<SupplierDto>
        {
            CreateSupplierDto(1, "مورد 1", true),
            CreateSupplierDto(2, "مورد 2", true)
        };

        var products = new List<ProductDto>
        {
            CreateProductDto(1, "منتج 1", true, minStock: 10),
            CreateProductDto(2, "منتج 2", true, minStock: 5),
            CreateProductDto(3, "منتج 3", true, minStock: 0), // no min stock
            CreateProductDto(4, "منتج 4", false, minStock: 10) // inactive
        };

        // Setup mocks to return data based on date range
        _mockSalesService
            .Setup(x => x.GetAllAsync(
                It.IsAny<string?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<byte?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? search, DateTime? from, DateTime? to, byte? status, int page, int pageSize, CancellationToken ct) =>
            {
                var today = DateTime.Today;
                var monthStart = new DateTime(today.Year, today.Month, 1);
                var yearStart = new DateTime(today.Year, 1, 1);

                if (from.HasValue && to.HasValue)
                {
                    if (from.Value.Date == today && to.Value.Date == today.AddDays(1).AddSeconds(-1))
                        return Result<List<SalesInvoiceDto>>.Success(todayInvoices);
                    if (from.Value.Date == monthStart)
                        return Result<List<SalesInvoiceDto>>.Success(monthInvoices);
                    if (from.Value.Date == yearStart)
                        return Result<List<SalesInvoiceDto>>.Success(yearInvoices);
                }

                return Result<List<SalesInvoiceDto>>.Success(new List<SalesInvoiceDto>());
            });

        _mockCustomerService
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(Result<List<CustomerDto>>.Success(customers));

        _mockSupplierService
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(Result<List<SupplierDto>>.Success(suppliers));

        _mockProductService
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(Result<List<ProductDto>>.Success(products));

        var viewModel = CreateViewModel();

        // Act
        await viewModel.LoadDataAsync();

        // Assert - verify all stats are loaded
        viewModel.TotalCustomers.Should().Be(2, "should count only active customers");
        viewModel.TotalSuppliers.Should().Be(2, "should count only active suppliers");
        viewModel.TotalProducts.Should().Be(3, "should count only active products");
        viewModel.LowStockCount.Should().Be(2, "should count products with minStock > 0");
    }

    [Fact]
    public async Task RefreshCommand_WhenAlreadyLoading_DoesNotReload()
    {
        // Arrange
        var callCount = 0;

        _mockSalesService
            .Setup(x => x.GetAllAsync(
                It.IsAny<string?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<byte?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                return Result<List<SalesInvoiceDto>>.Success(new List<SalesInvoiceDto>());
            });

        _mockCustomerService
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(Result<List<CustomerDto>>.Success(new List<CustomerDto>()));

        _mockSupplierService
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(Result<List<SupplierDto>>.Success(new List<SupplierDto>()));

        _mockProductService
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(Result<List<ProductDto>>.Success(new List<ProductDto>()));

        var viewModel = CreateViewModel();

        // Set loading to true
        viewModel.IsLoading = true;

        // Act
        await viewModel.LoadDataAsync();

        // Assert - loading check should prevent any API calls
        callCount.Should().Be(0, "loading should prevent API calls");
    }

    #endregion

    #region EventBus Message Tests

    [Fact]
    public void SubscribeToEvents_RegistersAllEventHandlers()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act - Verify all Subscribe calls were made
        _mockEventBus.Verify(x => x.Subscribe<SaleInvoiceChangedMessage>(It.IsAny<Action<SaleInvoiceChangedMessage>>()), Times.Once);
        _mockEventBus.Verify(x => x.Subscribe<ProductChangedMessage>(It.IsAny<Action<ProductChangedMessage>>()), Times.Once);
        _mockEventBus.Verify(x => x.Subscribe<StockChangedMessage>(It.IsAny<Action<StockChangedMessage>>()), Times.Once);
    }

    [Fact]
    public async Task SaleInvoiceChangedMessage_Received_RefreshesData()
    {
        // Arrange
        Action<SaleInvoiceChangedMessage>? capturedHandler = null;

        _mockEventBus
            .Setup(x => x.Subscribe(It.IsAny<Action<SaleInvoiceChangedMessage>>()))
            .Callback<Action<SaleInvoiceChangedMessage>>(h => capturedHandler = h);

        _mockSalesService
            .Setup(x => x.GetAllAsync(
                It.IsAny<string?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<byte?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<SalesInvoiceDto>>.Success(new List<SalesInvoiceDto>()));

        _mockCustomerService
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(Result<List<CustomerDto>>.Success(new List<CustomerDto>()));

        _mockSupplierService
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(Result<List<SupplierDto>>.Success(new List<SupplierDto>()));

        _mockProductService
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(Result<List<ProductDto>>.Success(new List<ProductDto>()));

        var viewModel = CreateViewModel();

        // Act - Simulate receiving message
        var message = new SaleInvoiceChangedMessage(123);
        capturedHandler?.Invoke(message);

        // Allow async to complete (Dispatcher.BeginInvoke)
        await Task.Delay(200);

        // Assert - Verify data was requested after message
        _mockSalesService.Verify(x => x.GetAllAsync(
            It.IsAny<string?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<byte?>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()),
            Times.AtLeastOnce());

        viewModel.Unsubscribe();
    }

    [Fact]
    public async Task ProductChangedMessage_Received_RefreshesData()
    {
        // Arrange
        Action<ProductChangedMessage>? capturedHandler = null;

        _mockEventBus
            .Setup(x => x.Subscribe(It.IsAny<Action<ProductChangedMessage>>()))
            .Callback<Action<ProductChangedMessage>>(h => capturedHandler = h);

        _mockSalesService
            .Setup(x => x.GetAllAsync(
                It.IsAny<string?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<byte?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<SalesInvoiceDto>>.Success(new List<SalesInvoiceDto>()));

        _mockCustomerService
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(Result<List<CustomerDto>>.Success(new List<CustomerDto>()));

        _mockSupplierService
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(Result<List<SupplierDto>>.Success(new List<SupplierDto>()));

        _mockProductService
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(Result<List<ProductDto>>.Success(new List<ProductDto>()));

        var viewModel = CreateViewModel();

        // Act - Simulate receiving message
        var message = new ProductChangedMessage(1);
        capturedHandler?.Invoke(message);

        // Allow async to complete
        await Task.Delay(200);

        // Assert - Verify products were requested
        _mockProductService.Verify(x => x.GetAllAsync(), Times.AtLeastOnce());

        viewModel.Unsubscribe();
    }

    [Fact]
    public void Unsubscribe_RemovesEventHandlers()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.Unsubscribe();

        // Assert - Verify Unsubscribe was called on EventBus
        _mockEventBus.Verify(x => x.Unsubscribe<SaleInvoiceChangedMessage>(It.IsAny<Action<SaleInvoiceChangedMessage>>()), Times.Once);
        _mockEventBus.Verify(x => x.Unsubscribe<ProductChangedMessage>(It.IsAny<Action<ProductChangedMessage>>()), Times.Once);
        _mockEventBus.Verify(x => x.Unsubscribe<StockChangedMessage>(It.IsAny<Action<StockChangedMessage>>()), Times.Once);
    }

    #endregion

    #region Net Profit Calculation Tests

    [Fact]
    public async Task DashboardData_CalculatesNetProfit_Correctly()
    {
        // Arrange
        var monthInvoices = new List<SalesInvoiceDto>
        {
            CreateSalesInvoiceDto(1, "INV-001", 5000m, 2),
            CreateSalesInvoiceDto(2, "INV-002", 3000m, 2)
        };

        _mockSalesService
            .Setup(x => x.GetAllAsync(
                It.IsAny<string?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<byte?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? search, DateTime? from, DateTime? to, byte? status, int page, int pageSize, CancellationToken ct) =>
            {
                // Return month invoices for any date request
                return Result<List<SalesInvoiceDto>>.Success(monthInvoices);
            });

        _mockCustomerService
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(Result<List<CustomerDto>>.Success(new List<CustomerDto>()));

        _mockSupplierService
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(Result<List<SupplierDto>>.Success(new List<SupplierDto>()));

        _mockProductService
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(Result<List<ProductDto>>.Success(new List<ProductDto>()));

        var viewModel = CreateViewModel();

        // Act
        await viewModel.LoadDataAsync();

        // Assert - MonthSales = 5000 + 3000 = 8000, MonthPurchases = 0, NetProfit = 8000 - 0 = 8000
        viewModel.MonthSales.Should().Be(8000m, "sales invoices sum should be calculated correctly");
        viewModel.NetProfit.Should().Be(8000m, "net profit should be MonthSales - MonthPurchases");
    }

    [Fact]
    public void NetProfit_InitialValue_IsZero()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Assert
        viewModel.NetProfit.Should().Be(0m, "net profit should be initially zero");
    }

    #endregion

    #region Property Tests

    [Fact]
    public void IsLoading_DefaultIsFalse()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Assert
        viewModel.IsLoading.Should().BeFalse("IsLoading should be false by default");
    }

    [Fact]
    public void TotalCustomers_InitialValue_IsZero()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Assert
        viewModel.TotalCustomers.Should().Be(0, "TotalCustomers should be initially zero");
    }

    [Fact]
    public void TotalProducts_InitialValue_IsZero()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Assert
        viewModel.TotalProducts.Should().Be(0, "TotalProducts should be initially zero");
    }

    #endregion

    #region Helper Methods

    private static SalesInvoiceDto CreateSalesInvoiceDto(
        int id,
        string invoiceNo,
        decimal totalAmount,
        byte status)
    {
        return new SalesInvoiceDto(
            Id: id,
            InvoiceNo: invoiceNo,
            CustomerId: 1,
            CustomerName: "Test Customer",
            WarehouseId: 1,
            WarehouseName: "Main Warehouse",
            InvoiceDate: DateTime.Today,
            DueDate: null,
            PaymentType: 1,
            SubTotal: totalAmount,
            DiscountAmount: 0,
            TaxAmount: 0,
            TotalAmount: totalAmount,
            PaidAmount: totalAmount,
            DueAmount: 0,
            Notes: null,
            Status: status,
            Items: new List<SalesInvoiceItemDto>());
    }

    private static CustomerDto CreateCustomerDto(int id, string name, bool isActive)
    {
        return new CustomerDto(
            Id: id,
            Code: $"C{id:D4}",
            Name: name,
            Phone: null,
            Email: null,
            Address: null,
            OpeningBalance: 0,
            CurrentBalance: 0,
            CreditLimit: 0,
            IsActive: isActive);
    }

    private static SupplierDto CreateSupplierDto(int id, string name, bool isActive)
    {
        return new SupplierDto(
            Id: id,
            Code: $"S{id:D4}",
            Name: name,
            Phone: null,
            Email: null,
            Address: null,
            OpeningBalance: 0,
            CurrentBalance: 0,
            IsActive: isActive);
    }

    private static ProductDto CreateProductDto(int id, string name, bool isActive, decimal minStock = 0)
    {
        return new ProductDto(
            Id: id,
            Code: $"P{id:D4}",
            Barcode: null,
            Name: name,
            CategoryId: 1,
            CategoryName: "Category",
            UnitId: 1,
            UnitName: "Unit",
            PurchasePrice: 100m,
            SalePrice: 150m,
            MinStock: minStock,
            Description: null,
            IsActive: isActive);
    }

    #endregion
}