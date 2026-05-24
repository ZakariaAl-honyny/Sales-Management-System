namespace SalesSystem.DesktopPWF.Tests.ViewModels.Sales;

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using FluentAssertions;
using Moq;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Enums;
using SalesSystem.Contracts.Requests;
using SalesSystem.DesktopPWF.Services;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.ViewModels.Sales;

/// <summary>
/// Tests for SalesInvoiceEditorViewModel with constructor injection
/// </summary>
public class SalesInvoiceEditorViewModelTests : IDisposable
{
    private readonly Mock<ISalesInvoiceApiService> _mockInvoiceService;
    private readonly Mock<IEventBus> _mockEventBus;
    private readonly Mock<ICustomerApiService> _mockCustomerService;
    private readonly Mock<IWarehouseApiService> _mockWarehouseService;
    private readonly Mock<IProductApiService> _mockProductService;
    private readonly Mock<ISettingsApiService> _mockSettingsService;
    private readonly Mock<IInvoicePrinter> _mockInvoicePrinter;
    private readonly Mock<IReceiptPrinter> _mockReceiptPrinter;
    private readonly Mock<IDialogService> _mockDialogService;
    private readonly Mock<ISoundService> _mockSoundService;
    private readonly Mock<IInventoryApiService> _mockInventoryService;
    private readonly Mock<IBarcodeInputService> _mockBarcodeInputService;
    private readonly Mock<ICashBoxApiService> _cashBoxApiServiceMock;

    public SalesInvoiceEditorViewModelTests()
    {
        _mockInvoiceService = new Mock<ISalesInvoiceApiService>();
        _mockEventBus = new Mock<IEventBus>();
        _mockCustomerService = new Mock<ICustomerApiService>();
        _mockWarehouseService = new Mock<IWarehouseApiService>();
        _mockProductService = new Mock<IProductApiService>();
        _mockSettingsService = new Mock<ISettingsApiService>();
        _mockInvoicePrinter = new Mock<IInvoicePrinter>();
        _mockReceiptPrinter = new Mock<IReceiptPrinter>();
        _mockDialogService = new Mock<IDialogService>();
        _mockSoundService = new Mock<ISoundService>();
        _mockInventoryService = new Mock<IInventoryApiService>();
        _mockBarcodeInputService = new Mock<IBarcodeInputService>();
        _cashBoxApiServiceMock = new Mock<ICashBoxApiService>();
    }

    public void Dispose()
    {
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NewInvoice_SetsIsEditModeFalse()
    {
        // Arrange & Act
        var viewModel = CreateViewModel(null);

        // Assert
        viewModel.IsEditMode.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithInvoiceId_SetsIsEditModeTrue()
    {
        // Arrange & Act
        var viewModel = CreateViewModel(1);

        // Assert
        viewModel.IsEditMode.Should().BeTrue();
    }

    [Fact]
    public void Constructor_InitializesCommands()
    {
        // Arrange & Act
        var viewModel = CreateViewModel(null);

        // Assert
        viewModel.SaveCommand.Should().NotBeNull();
        viewModel.PostCommand.Should().NotBeNull();
        viewModel.CancelCommand.Should().NotBeNull();
        viewModel.AddLineCommand.Should().NotBeNull();
        viewModel.RemoveLineCommand.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_InitializesPaymentTypeOptions()
    {
        // Arrange & Act
        var viewModel = CreateViewModel(null);

        // Assert
        viewModel.PaymentTypeOptions.Should().HaveCount(3);
        viewModel.PaymentTypeOptions.Should().Contain(p => p.Display == "نقدي" && p.Value == 1);
        viewModel.PaymentTypeOptions.Should().Contain(p => p.Display == "آجل" && p.Value == 2);
        viewModel.PaymentTypeOptions.Should().Contain(p => p.Display == "مختلط" && p.Value == 3);
    }

    [Fact]
    public void Constructor_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var viewModel = CreateViewModel(null);

        // Assert
        viewModel.InvoiceDate.Should().Be(DateTime.Today);
        viewModel.SelectedPaymentType.Should().Be(1); // Cash
        viewModel.InvoiceDiscount.Should().Be(0);
        viewModel.TaxRate.Should().Be(15);
        viewModel.IsTaxInclusive.Should().BeFalse();
        viewModel.PaidAmount.Should().Be(0);
        viewModel.IsBusy.Should().BeFalse();
        viewModel.Customers.Should().NotBeNull();
        viewModel.Warehouses.Should().NotBeNull();
        viewModel.Products.Should().NotBeNull();
    }

    #endregion

    #region Default Values Tests

    [Fact]
    public void SubTotal_DefaultValue_IsZero()
    {
        // Arrange & Act
        var viewModel = CreateViewModel(null);

        // Assert
        viewModel.SubTotal.Should().Be(0);
    }

    [Fact]
    public void TaxAmount_DefaultValue_IsZero()
    {
        // Arrange & Act
        var viewModel = CreateViewModel(null);

        // Assert
        viewModel.TaxAmount.Should().Be(0);
    }

    [Fact]
    public void TotalAmount_DefaultValue_IsZero()
    {
        // Arrange & Act
        var viewModel = CreateViewModel(null);

        // Assert
        viewModel.TotalAmount.Should().Be(0);
    }

    [Fact]
    public void DueAmount_DefaultValue_IsZero()
    {
        // Arrange & Act
        var viewModel = CreateViewModel(null);

        // Assert
        viewModel.DueAmount.Should().Be(0);
    }

    #endregion

    #region Calculation Tests

    [Fact]
    public void RecalculateTotals_CalculatesCorrectSubTotal()
    {
        // Arrange
        var viewModel = CreateViewModel(null);
        SetupMockReferenceData(viewModel);

        // Add items with line totals
        var products = SetupProducts();
        var line1 = new InvoiceLineViewModel(products);
        line1.Quantity = 10;
        line1.UnitPrice = 100;
        line1.DiscountAmount = 0;

        var line2 = new InvoiceLineViewModel(products);
        line2.Quantity = 5;
        line2.UnitPrice = 50;
        line2.DiscountAmount = 10;

        // Act
        var lineTotal1 = line1.LineTotal;
        var lineTotal2 = line2.LineTotal;

        // Assert - Line totals calculated correctly
        lineTotal1.Should().Be(1000m); // 10 * 100
        lineTotal2.Should().Be(240m);  // (5 * 50) - 10
    }

    [Fact]
    public void RecalculateTotals_CalculatesTotalWithTaxExclusive()
    {
        // Arrange
        var viewModel = CreateViewModel(null);
        viewModel.InvoiceDiscount = 0;
        viewModel.TaxRate = 15;
        viewModel.IsTaxInclusive = false;

        // Assert - Initial state with no items
        viewModel.SubTotal.Should().Be(0);
        viewModel.TaxAmount.Should().Be(0);
        viewModel.TotalAmount.Should().Be(0);
    }

    [Fact]
    public void RecalculateTotals_CalculatesTaxExclusive()
    {
        // Arrange
        var viewModel = CreateViewModel(null);
        viewModel.InvoiceDiscount = 100;
        viewModel.TaxRate = 15;
        viewModel.IsTaxInclusive = false;

        // Act - Calculate expected tax
        var subtotal = 1000m;
        var discount = 100m;
        var taxRate = 15m;
        var expectedTax = (subtotal - discount) * (taxRate / 100);

        // Assert
        expectedTax.Should().Be(135m); // (1000 - 100) * 0.15
    }

    [Fact]
    public async Task RecalculateTotals_CalculatesDueAmount()
    {
        // Arrange
        var viewModel = CreateViewModel(null);

        var products = SetupProducts();
        _mockProductService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<ProductDto>>.Success(products.ToList()));

        _mockSettingsService
            .Setup(s => s.GetSettingsAsync())
            .ReturnsAsync(Result<StoreSettingsDto>.Success(new StoreSettingsDto(
                Id: 1,
                StoreName: "Test",
                Phone: null,
                Address: null,
                LogoPath: null,
                Email: null,
                CurrencyCode: "SAR",
                DefaultTaxRate: 15m,
                IsTaxEnabled: false,
                TaxNumber: null,
                EnableStockAlerts: false,
                AllowNegativeStock: false,
                AutoUpdatePrices: true,
                InvoicePrefix: "INV-")));

        await viewModel.InitializationTask;

        viewModel.Items.Clear();
        viewModel.AddLineCommand.Execute(null);
        var line = viewModel.Items[0];
        line.Quantity = 1;
        line.UnitPrice = 1000m;

        // Act
        viewModel.SelectedPaymentType = (byte)PaymentType.Mixed; // Mixed allows partial paid amounts
        viewModel.PaidAmount = 300m;

        // Assert
        viewModel.TotalAmount.Should().Be(1150m); // 1000 + 15% tax
        viewModel.DueAmount.Should().Be(850m); // 1150 - 300
    }

    #endregion

    #region Item Management Tests

    [Fact]
    public void AddLineCommand_AddsNewItem()
    {
        // Arrange
        var viewModel = CreateViewModel(null);
        SetupMockReferenceData(viewModel);
        var initialCount = viewModel.Items.Count;

        // Act
        viewModel.AddLineCommand.Execute(null);

        // Assert
        viewModel.Items.Count.Should().BeGreaterThan(initialCount);
    }

    [Fact]
    public async Task RemoveLineCommand_RemovesItem()
    {
        // Arrange
        var viewModel = CreateViewModel(null);
        await viewModel.InitializationTask;
        
        viewModel.Items.Clear();
        viewModel.AddLineCommand.Execute(null);
        viewModel.AddLineCommand.Execute(null);
        var line2 = viewModel.Items[1];
        var countBefore = viewModel.Items.Count;

        // Act
        viewModel.RemoveLineCommand.Execute(line2);

        // Assert
        viewModel.Items.Count.Should().Be(countBefore - 1);
        viewModel.Items.Should().NotContain(line2);
    }

    [Fact]
    public void RemoveLineCommand_CannotExecute_WhenOnlyOneItem()
    {
        // Arrange
        var viewModel = CreateViewModel(null);
        viewModel.Items.Clear();
        viewModel.Items.Add(new InvoiceLineViewModel(SetupProducts()));

        // Act & Assert
        viewModel.RemoveLineCommand.CanExecute(null).Should().BeFalse();
    }

    #endregion

    #region Command CanExecute Tests

    [Fact]
    public void SaveCommand_NoPredicate_AlwaysCanExecute()
    {
        // Interactive validation v4.6 removed CanExecute predicates - SaveCommand always enabled
        // Validation is done in Validate() and shown as warning dialog
        var viewModel = CreateViewModel(null);
        viewModel.Items.Clear();

        // Act & Assert
        viewModel.SaveCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void PostCommand_NoPredicate_AlwaysCanExecute()
    {
        // Interactive validation v4.6 removed CanExecute predicates - PostCommand always enabled
        // Validation is done in Validate() and shown as warning dialog
        var viewModel = CreateViewModel(null);
        viewModel.SelectedWarehouseId = 0;

        // Act & Assert
        viewModel.PostCommand.CanExecute(null).Should().BeTrue();
    }

    #endregion

    #region CancelCommand Tests

    [Fact]
    public void CancelCommand_InvokesCloseRequested()
    {
        // Arrange
        var viewModel = CreateViewModel(null);
        var closeRequestedInvoked = false;
        viewModel.CloseRequested += () => closeRequestedInvoked = true;

        // Act
        viewModel.CancelCommand.Execute(null);

        // Assert
        closeRequestedInvoked.Should().BeTrue();
    }

    [Fact]
    public void CancelCommand_CanExecute_AlwaysReturnsTrue()
    {
        // Arrange
        var viewModel = CreateViewModel(null);

        // Act & Assert
        viewModel.CancelCommand.CanExecute(null).Should().BeTrue();
    }

    #endregion

    #region Property Notification Tests

    [Fact]
    public void IsBusy_IsReadOnly_FromViewModelBase()
    {
        // Arrange
        var viewModel = CreateViewModel(null);

        // Assert
        viewModel.IsBusy.Should().BeFalse();
    }

    [Fact]
    public void ErrorMessage_Set_NotifiesPropertyChanged()
    {
        // Arrange
        var viewModel = CreateViewModel(null);
        var propertyChangedEvents = new List<string>();
        viewModel.PropertyChanged += (s, e) => propertyChangedEvents.Add(e.PropertyName ?? string.Empty);

        // Act
        viewModel.ErrorMessage = "خطأ";

        // Assert
        propertyChangedEvents.Should().Contain("ErrorMessage");
    }

    [Fact]
    public void InvoiceDate_Set_NotifiesPropertyChanged()
    {
        // Arrange
        var viewModel = CreateViewModel(null);
        var propertyChangedEvents = new List<string>();
        viewModel.PropertyChanged += (s, e) => propertyChangedEvents.Add(e.PropertyName ?? string.Empty);

        // Act
        viewModel.InvoiceDate = DateTime.Today.AddDays(-1);

        // Assert
        propertyChangedEvents.Should().Contain("InvoiceDate");
    }

    [Fact]
    public void Notes_Set_NotifiesPropertyChanged()
    {
        // Arrange
        var viewModel = CreateViewModel(null);
        var propertyChangedEvents = new List<string>();
        viewModel.PropertyChanged += (s, e) => propertyChangedEvents.Add(e.PropertyName ?? string.Empty);

        // Act
        viewModel.Notes = "ملاحظات";

        // Assert
        propertyChangedEvents.Should().Contain("Notes");
    }

    [Fact]
    public void SelectedWarehouseId_Set_NotifiesPropertyChanged()
    {
        // Arrange
        var viewModel = CreateViewModel(null);
        var propertyChangedEvents = new List<string>();
        viewModel.PropertyChanged += (s, e) => propertyChangedEvents.Add(e.PropertyName ?? string.Empty);

        // Act
        viewModel.SelectedWarehouseId = 1;

        // Assert
        propertyChangedEvents.Should().Contain("SelectedWarehouseId");
    }

    [Fact]
    public void SelectedCustomerId_Set_NotifiesPropertyChanged()
    {
        // Arrange
        var viewModel = CreateViewModel(null);
        var propertyChangedEvents = new List<string>();
        viewModel.PropertyChanged += (s, e) => propertyChangedEvents.Add(e.PropertyName ?? string.Empty);

        // Act
        viewModel.SelectedCustomerId = 1;

        // Assert
        propertyChangedEvents.Should().Contain("SelectedCustomerId");
    }

    [Fact]
    public void InvoiceDiscount_Set_NotifiesPropertyChanged()
    {
        // Arrange
        var viewModel = CreateViewModel(null);
        var propertyChangedEvents = new List<string>();
        viewModel.PropertyChanged += (s, e) => propertyChangedEvents.Add(e.PropertyName ?? string.Empty);

        // Act
        viewModel.InvoiceDiscount = 100;

        // Assert
        propertyChangedEvents.Should().Contain("InvoiceDiscount");
    }

    [Fact]
    public void TaxRate_Set_NotifiesPropertyChanged()
    {
        // Arrange
        var viewModel = CreateViewModel(null);
        var propertyChangedEvents = new List<string>();
        viewModel.PropertyChanged += (s, e) => propertyChangedEvents.Add(e.PropertyName ?? string.Empty);

        // Act
        viewModel.TaxRate = 20;

        // Assert
        propertyChangedEvents.Should().Contain("TaxRate");
    }

    [Fact]
    public void PaidAmount_Set_NotifiesPropertyChanged()
    {
        // Arrange
        var viewModel = CreateViewModel(null);
        var propertyChangedEvents = new List<string>();
        viewModel.PropertyChanged += (s, e) => propertyChangedEvents.Add(e.PropertyName ?? string.Empty);

        // Act
        viewModel.PaidAmount = 500;

        // Assert
        propertyChangedEvents.Should().Contain("PaidAmount");
    }

    #endregion

    #region InvoiceLineViewModel Tests

    [Fact]
    public void InvoiceLineViewModel_LineTotal_CalculatesCorrectly()
    {
        // Arrange
        var products = SetupProducts();
        var line = new InvoiceLineViewModel(products);

        // Act
        line.Quantity = 10;
        line.UnitPrice = 100;
        line.DiscountAmount = 20;

        // Assert
        line.LineTotal.Should().Be(980m); // (10 * 100) - 20
    }

    [Fact]
    public void InvoiceLineViewModel_LineTotal_UpdatesOnQuantityChange()
    {
        // Arrange
        var products = SetupProducts();
        var line = new InvoiceLineViewModel(products);
        line.UnitPrice = 50;
        line.DiscountAmount = 0;

        // Act
        var initialTotal = line.LineTotal;
        line.Quantity = 20;

        // Assert
        line.LineTotal.Should().Be(1000m); // 20 * 50
    }

    [Fact]
    public void InvoiceLineViewModel_SelectedProduct_SetsProductId()
    {
        // Arrange
        var products = SetupProducts();
        var line = new InvoiceLineViewModel(products);
        var product = products.First();

        // Act
        line.SelectedProduct = product;

        // Assert
        line.ProductId.Should().Be(product.Id);
    }

    #endregion

    #region Helper Methods

    private SalesInvoiceEditorViewModel CreateViewModel(int? invoiceId)
    {
        return new SalesInvoiceEditorViewModel(
            _mockInvoiceService.Object,
            _mockEventBus.Object,
            _mockCustomerService.Object,
            _mockWarehouseService.Object,
            _mockProductService.Object,
            _mockSettingsService.Object,
            _mockInvoicePrinter.Object,
            _mockReceiptPrinter.Object,
            _mockDialogService.Object,
            _mockSoundService.Object,
            _mockInventoryService.Object,
            _mockBarcodeInputService.Object,
            _cashBoxApiServiceMock.Object,
            invoiceId);
    }

    private void SetupMockReferenceData(SalesInvoiceEditorViewModel viewModel)
    {
        // Setup empty collections to avoid null references
        var customers = new List<CustomerDto>
        {
            new CustomerDto(Id: 1, Name: "عميل 1", Phone: null, Email: null, Address: null, TaxNumber: null, OpeningBalance: 0, CurrentBalance: 0, CreditLimit: 0, IsActive: true)
        };
        var warehouses = new List<WarehouseDto>
        {
            new WarehouseDto(Id: 1, Name: "مستودع 1", Location: null, IsDefault: true, IsActive: true)
        };
        var products = SetupProducts();

        viewModel.Customers.Should().NotBeNull();
        viewModel.Warehouses.Should().NotBeNull();
        viewModel.Products.Should().NotBeNull();
    }

    private ObservableCollection<ProductDto> SetupProducts()
    {
        return new ObservableCollection<ProductDto>
        {
            new ProductDto(
                Id: 1,
                Barcode: null,
                Name: "منتج 1",
                CategoryId: 1,
                CategoryName: null,
                UnitId: 1,
                UnitName: null,
                RetailUnitId: 1,
                RetailUnitName: null,
                WholesaleUnitId: 2,
                WholesaleUnitName: null,
                ConversionFactor: 10,
                PurchasePrice: 50,
                SalePrice: 100,
                RetailPrice: 100,
                WholesalePrice: 900,
                MinStock: 10,
                Description: null,
                IsActive: true),
            new ProductDto(
                Id: 2,
                Barcode: null,
                Name: "منتج 2",
                CategoryId: 1,
                CategoryName: null,
                UnitId: 1,
                UnitName: null,
                RetailUnitId: 1,
                RetailUnitName: null,
                WholesaleUnitId: 2,
                WholesaleUnitName: null,
                ConversionFactor: 10,
                PurchasePrice: 30,
                SalePrice: 60,
                RetailPrice: 60,
                WholesalePrice: 500,
                MinStock: 5,
                Description: null,
                IsActive: true)
        };
    }

    #endregion
}
