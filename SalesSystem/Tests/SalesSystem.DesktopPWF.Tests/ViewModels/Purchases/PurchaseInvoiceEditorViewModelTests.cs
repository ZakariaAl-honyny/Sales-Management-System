namespace SalesSystem.DesktopPWF.Tests.ViewModels.Purchases;

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using FluentAssertions;
using Moq;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Enums;
using SalesSystem.Contracts.Requests;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.ViewModels.Purchases;
using SalesSystem.DesktopPWF.Helpers;
using SalesSystem.DesktopPWF.Models.Printing;
using SalesSystem.DesktopPWF.Services.App.Toast;

public class PurchaseInvoiceEditorViewModelTests : IDisposable
{
    private readonly Mock<IPurchaseInvoiceApiService> _mockInvoiceService;
    private readonly Mock<ISupplierApiService> _mockSupplierService;
    private readonly Mock<IWarehouseApiService> _mockWarehouseService;
    private readonly Mock<IProductApiService> _mockProductService;
    private readonly Mock<IEventBus> _mockEventBus;
    private readonly Mock<ISettingsApiService> _mockSettingsService;
    private readonly Mock<IDialogService> _mockDialogService;
    private readonly Mock<ISoundService> _mockSoundService;
    private readonly Mock<IBarcodeInputService> _mockBarcodeInputService;
    private readonly Mock<ICashBoxApiService> _cashBoxApiServiceMock;
    private readonly Mock<IPrintApiService> _printApiServiceMock;
    private readonly Mock<IToastNotificationService> _mockToastService;
    private readonly Mock<ICurrencyApiService> _mockCurrencyService;
    private readonly PurchaseInvoiceEditorViewModel _viewModel;

    public PurchaseInvoiceEditorViewModelTests()
    {
        _mockInvoiceService = new Mock<IPurchaseInvoiceApiService>();
        _mockSupplierService = new Mock<ISupplierApiService>();
        _mockWarehouseService = new Mock<IWarehouseApiService>();
        _mockProductService = new Mock<IProductApiService>();
        _mockEventBus = new Mock<IEventBus>();
        _mockSettingsService = new Mock<ISettingsApiService>();
        _mockDialogService = new Mock<IDialogService>();
        _mockSoundService = new Mock<ISoundService>();
        _mockBarcodeInputService = new Mock<IBarcodeInputService>();
        _cashBoxApiServiceMock = new Mock<ICashBoxApiService>();
        _printApiServiceMock = new Mock<IPrintApiService>();
        _mockToastService = new Mock<IToastNotificationService>();
        _mockCurrencyService = new Mock<ICurrencyApiService>();

        var suppliers = new List<SupplierDto>
        {
            new SupplierDto(Id: 1, Name: "مورد 1", Phone: null, Email: null, Address: null, TaxNumber: null, IsActive: true, PartyId: 1, AccountId: 1),
            new SupplierDto(Id: 2, Name: "مورد 2", Phone: null, Email: null, Address: null, TaxNumber: null, IsActive: true, PartyId: 1, AccountId: 1)
        };

        var warehouses = new List<WarehouseDto>
        {
            new WarehouseDto(Id: 1, Name: "������ 1", Phone: null, Address: null, Notes: null, IsActive: true),
            new WarehouseDto(Id: 2, Name: "������ 2", Phone: null, Address: null, Notes: null, IsActive: true)
        };

        var products = new List<ProductDto>
        {
            CreateProductDto(1, "منتج 1", 100m, 80m),
            CreateProductDto(2, "منتج 2", 200m, 160m)
        };

        _mockSupplierService.Setup(s => s.GetAllAsync()).ReturnsAsync(Result<List<SupplierDto>>.Success(suppliers));
        _mockWarehouseService.Setup(s => s.GetAllAsync()).ReturnsAsync(Result<List<WarehouseDto>>.Success(warehouses));
        _mockProductService.Setup(s => s.GetAllAsync()).ReturnsAsync(Result<List<ProductDto>>.Success(products));

        _viewModel = new PurchaseInvoiceEditorViewModel(
            _mockInvoiceService.Object,
            _mockEventBus.Object,
            _mockSupplierService.Object,
            _mockWarehouseService.Object,
            _mockProductService.Object,
            _mockSettingsService.Object,
            _mockDialogService.Object,
            _mockSoundService.Object,
            _mockBarcodeInputService.Object,
            _printApiServiceMock.Object,
            _mockToastService.Object,
            _mockCurrencyService.Object);
    }

    public void Dispose() { }

    [Fact]
    public void Constructor_NewInvoice_SetsIsEditModeFalse() => _viewModel.IsEditMode.Should().BeFalse();

    [Fact]
    public void Constructor_WithInvoiceId_SetsIsEditModeTrue()
    {
        var vmWithId = new PurchaseInvoiceEditorViewModel(
            _mockInvoiceService.Object, _mockEventBus.Object, _mockSupplierService.Object,
            _mockWarehouseService.Object, _mockProductService.Object, _mockSettingsService.Object,
            _mockDialogService.Object, _mockSoundService.Object, _mockBarcodeInputService.Object, _printApiServiceMock.Object, _mockToastService.Object, _mockCurrencyService.Object, invoiceId: 1);
        vmWithId.IsEditMode.Should().BeTrue();
    }

    [Fact]
    public void Constructor_InitializesCommands()
    {
        _viewModel.SaveCommand.Should().NotBeNull();
        _viewModel.PostCommand.Should().NotBeNull();
        _viewModel.CancelCommand.Should().NotBeNull();
        _viewModel.AddLineCommand.Should().NotBeNull();
        _viewModel.RemoveLineCommand.Should().NotBeNull();
    }

    [Fact]
    public async Task LoadInvoiceAsync_WhenExists_PopulatesFields()
    {
        var invoice = new PurchaseInvoiceDto(
            Id: 1,
            InvoiceNo: 1,
            SupplierId: 1,
            SupplierName: "مورد تجريبي",
            WarehouseId: 1,
            WarehouseName: "المستودع الرئيسي",
            InvoiceDate: DateTime.Today,
            PaymentType: 1,
            SubTotal: 1000m,
            DiscountAmount: 0,
            TaxAmount: 0,
            OtherCharges: 0,
            NetTotal: 1000m,
            PaidAmount: 1000m,
            RemainingAmount: 0,
            Notes: null,
            Status: 1,
            TaxId: null,
            TaxName: null,
            TaxRate: null,
            CurrencyId: null,
            ExchangeRate: null,
            Items: new List<PurchaseInvoiceLineDto>
        {
            new PurchaseInvoiceLineDto(1, 1, "منتج 1", 1, null, 10, 100m, 1000m)
        });

        _mockInvoiceService.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(Result<PurchaseInvoiceDto>.Success(invoice));

        var vm = new PurchaseInvoiceEditorViewModel(
            _mockInvoiceService.Object, _mockEventBus.Object, _mockSupplierService.Object,
            _mockWarehouseService.Object, _mockProductService.Object, _mockSettingsService.Object,
            _mockDialogService.Object, _mockSoundService.Object, _mockBarcodeInputService.Object, _printApiServiceMock.Object, _mockToastService.Object, _mockCurrencyService.Object, invoiceId: 1);

        await Task.Delay(100);
        vm.SelectedWarehouseId.Should().Be(1);
    }

    [Fact]
    public void RecalculateTotals_CalculatesCorrectSubTotal()
    {
        var line = _viewModel.Items.First();
        line.SelectedProduct = _viewModel.Products.First();
        line.Quantity = 10;
        line.UnitCost = 100m;
        _viewModel.SubTotal.Should().Be(1000m);
    }

    [Fact]
    public void PostCommand_NoPredicate_AlwaysCanExecute_WhenNoWarehouseSelected()
    {
        // Interactive validation v4.6 removed CanExecute predicates
        // Validation is done in Validate() and shown as warning dialog
        _viewModel.SelectedWarehouseId = 0;
        _viewModel.SelectedSupplierId = 1;
        _viewModel.Items.First().SelectedProduct = _viewModel.Products.First();
        _viewModel.Items.First().Quantity = 10;
        _viewModel.PostCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void PostCommand_NoPredicate_AlwaysCanExecute_WhenNoSupplierSelected()
    {
        // Interactive validation v4.6 removed CanExecute predicates
        // Validation is done in Validate() and shown as warning dialog
        _viewModel.SelectedWarehouseId = 1;
        _viewModel.SelectedSupplierId = 0;
        _viewModel.Items.First().SelectedProduct = _viewModel.Products.First();
        _viewModel.Items.First().Quantity = 10;
        _viewModel.PostCommand.CanExecute(null).Should().BeTrue();
    }

    private static ProductDto CreateProductDto(int id, string name, decimal salePrice, decimal purchasePrice)
    {
        return new ProductDto(Id: id, Name: name, CategoryId: 1, CategoryName: null, Barcode: null, Description: null, ReorderLevel: 0m, TrackExpiry: false, ImagePath: null, IsActive: true);
    }
}



