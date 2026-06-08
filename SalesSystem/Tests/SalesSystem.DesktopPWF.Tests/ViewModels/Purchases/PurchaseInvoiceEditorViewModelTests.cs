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
    private readonly Mock<ICurrencyApiService> _currencyServiceMock;
    private readonly Mock<IAdditionalFeeApiService> _additionalFeeServiceMock;
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
        _currencyServiceMock = new Mock<ICurrencyApiService>();
        _additionalFeeServiceMock = new Mock<IAdditionalFeeApiService>();

        _currencyServiceMock.Setup(s => s.GetAllAsync(true)).ReturnsAsync(Result<List<CurrencyDto>>.Success(new List<CurrencyDto>
        {
            new CurrencyDto(1, "ريال سعودي", "SAR", "﷼", 1.0m, true, null, true, true)
        }));

        var suppliers = new List<SupplierDto>
        {
            new SupplierDto(Id: 1, Name: "مورد 1", Phone: null, Email: null, Address: null, TaxNumber: null, OpeningBalance: 0, CurrentBalance: 0, CreditLimit: 0, IsActive: true),
            new SupplierDto(Id: 2, Name: "مورد 2", Phone: null, Email: null, Address: null, TaxNumber: null, OpeningBalance: 0, CurrentBalance: 0, CreditLimit: 0, IsActive: true)
        };

        var warehouses = new List<WarehouseDto>
        {
            new WarehouseDto(Id: 1, Name: "المستودع الرئيسي", Type: 1, Location: null, Phone: null, Address: null, ManagerName: null, IsDefault: true, IsActive: true, AccountId: null, Notes: null),
            new WarehouseDto(Id: 2, Name: "المستودع الفرعي", Type: 1, Location: null, Phone: null, Address: null, ManagerName: null, IsDefault: false, IsActive: true, AccountId: null, Notes: null)
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
            _cashBoxApiServiceMock.Object,
            _printApiServiceMock.Object,
            _mockToastService.Object,
            _currencyServiceMock.Object,
            _additionalFeeServiceMock.Object);
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
            _mockDialogService.Object, _mockSoundService.Object, _mockBarcodeInputService.Object, _cashBoxApiServiceMock.Object, _printApiServiceMock.Object, _mockToastService.Object, _currencyServiceMock.Object, _additionalFeeServiceMock.Object, invoiceId: 1);
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
            DueDate: null,
            PaymentType: 1,
            SubTotal: 1000m,
            DiscountAmount: 0,
            TaxAmount: 0,
            TotalAmount: 1000m,
            PaidAmount: 1000m,
            DueAmount: 0,
            SupplierInvoiceNo: null,
            Notes: null,
            Status: 1,
            TaxId: null,
            TaxName: null,
            TaxRate: null,
            CurrencyId: null,
            ExchangeRate: null,
            CostInBaseCurrency: null,
            AdditionalFeesTotal: 0,
            AttachmentPath: null,
            DiscountType: null,
            DiscountRate: null,
            CurrencyName: null,
            Items: new List<PurchaseInvoiceItemDto>
        {
            new PurchaseInvoiceItemDto(Id: 1, ProductId: 1, ProductName: "منتج 1", ProductUnitId: 1, ProductUnitName: "وحدة", Quantity: 10, UnitCost: 100m, DiscountAmount: 0, DiscountType: null, DiscountRate: null, LineTotal: 1000m, CostInBaseCurrency: null, AdditionalFeesAmount: 0, Mode: 1, Notes: null)
        },
            AdditionalFees: new List<AdditionalFeeDto>());

        _mockInvoiceService.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(Result<PurchaseInvoiceDto>.Success(invoice));

        var vm = new PurchaseInvoiceEditorViewModel(
            _mockInvoiceService.Object, _mockEventBus.Object, _mockSupplierService.Object,
            _mockWarehouseService.Object, _mockProductService.Object, _mockSettingsService.Object,
            _mockDialogService.Object, _mockSoundService.Object, _mockBarcodeInputService.Object, _cashBoxApiServiceMock.Object, _printApiServiceMock.Object, _mockToastService.Object, _currencyServiceMock.Object, _additionalFeeServiceMock.Object, invoiceId: 1);

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
        return new ProductDto(id, null, name, null, null, 1, "وحدة", 1, "وحدة", 1, "وحدة", 1, purchasePrice, salePrice, salePrice, salePrice * 10, 0, null, null, null, true);
    }
}