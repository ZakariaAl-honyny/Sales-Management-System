namespace SalesSystem.DesktopPWF.Tests.ViewModels.Products;

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using FluentAssertions;
using Moq;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.DesktopPWF.Services;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.ViewModels.Products;

/// <summary>
/// Tests for ProductEditorViewModel with constructor injection
/// </summary>
public class ProductEditorViewModelTests : IDisposable
{
    private readonly Mock<IProductApiService> _mockProductService;
    private readonly Mock<ICategoryApiService> _mockCategoryService;
    private readonly Mock<IUnitApiService> _mockUnitService;
    private readonly Mock<IEventBus> _mockEventBus;
    private readonly Mock<IDialogService> _mockDialogService;

    public ProductEditorViewModelTests()
    {
        _mockProductService = new Mock<IProductApiService>();
        _mockCategoryService = new Mock<ICategoryApiService>();
        _mockUnitService = new Mock<IUnitApiService>();
        _mockEventBus = new Mock<IEventBus>();
        _mockDialogService = new Mock<IDialogService>();
    }

    public void Dispose()
    {
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithServices_InitializesCommands()
    {
        // Arrange & Act
        var viewModel = new ProductEditorViewModel(
            _mockProductService.Object,
            _mockCategoryService.Object,
            _mockUnitService.Object,
            _mockEventBus.Object,
            _mockDialogService.Object);

        // Assert
        viewModel.SaveCommand.Should().NotBeNull();
        viewModel.CancelCommand.Should().NotBeNull();
        viewModel.LoadLookupDataCommand.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_NewProduct_SetsIsEditModeFalse()
    {
        // Arrange & Act
        var viewModel = new ProductEditorViewModel(
            _mockProductService.Object,
            _mockCategoryService.Object,
            _mockUnitService.Object,
            _mockEventBus.Object,
            _mockDialogService.Object);

        // Assert
        viewModel.IsEditMode.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithProductDto_SetsIsEditModeTrue()
    {
        // Arrange
        var product = new ProductDto(
            Id: 1,
            Barcode: null,
            Name: "منتج تجريبي",
            CategoryId: 1,
            CategoryName: "تصنيف 1",
            UnitId: 1,
            UnitName: "وحدة",
            RetailUnitId: 1,
            RetailUnitName: "وحدة",
            WholesaleUnitId: 2,
            WholesaleUnitName: "كرتون",
            ConversionFactor: 10,
            PurchasePrice: 100,
            SalePrice: 200,
            RetailPrice: 200,
            WholesalePrice: 1800,
            MinStock: 10,
            Description: null,
            ExpirationDate: null,
            ImagePath: null,
            IsActive: true);

        // Act - Use the constructor that accepts ProductDto and services
        var viewModel = new ProductEditorViewModel(
            product,
            _mockProductService.Object,
            _mockCategoryService.Object,
            _mockUnitService.Object,
            _mockEventBus.Object,
            _mockDialogService.Object);

        // Assert
        viewModel.IsEditMode.Should().BeTrue();
    }

    [Fact]
    public void Constructor_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var viewModel = new ProductEditorViewModel(
            _mockProductService.Object,
            _mockCategoryService.Object,
            _mockUnitService.Object,
            _mockEventBus.Object,
            _mockDialogService.Object);

        // Assert
        viewModel.Name.Should().BeEmpty();
        viewModel.Barcode.Should().BeEmpty();
        viewModel.PurchasePrice.Should().Be(0);
        viewModel.RetailPrice.Should().Be(0);
        viewModel.MinStock.Should().Be(0);
        viewModel.IsActive.Should().BeTrue();
        viewModel.IsBusy.Should().BeFalse();
        viewModel.Categories.Should().NotBeNull();
        viewModel.Units.Should().NotBeNull();
    }

    [Fact]
    public void Title_NewProduct_ReturnsAddTitle()
    {
        // Arrange
        var viewModel = new ProductEditorViewModel(
            _mockProductService.Object,
            _mockCategoryService.Object,
            _mockUnitService.Object,
            _mockEventBus.Object,
            _mockDialogService.Object);

        // Assert
        viewModel.Title.Should().Be("إضافة منتج جديد");
    }

    [Fact]
    public void Title_EditProduct_ReturnsEditTitle()
    {
        // Arrange
        var product = new ProductDto(
            Id: 1,
            Barcode: null,
            Name: "منتج تجريبي",
            CategoryId: 1,
            CategoryName: "تصنيف 1",
            UnitId: 1,
            UnitName: "وحدة",
            RetailUnitId: 1,
            RetailUnitName: "وحدة",
            WholesaleUnitId: 2,
            WholesaleUnitName: "كرتون",
            ConversionFactor: 10,
            PurchasePrice: 100,
            SalePrice: 200,
            RetailPrice: 200,
            WholesalePrice: 1800,
            MinStock: 10,
            Description: null,
            ExpirationDate: null,
            ImagePath: null,
            IsActive: true);

        var viewModel = new ProductEditorViewModel(
            product,
            _mockProductService.Object,
            _mockCategoryService.Object,
            _mockUnitService.Object,
            _mockEventBus.Object,
            _mockDialogService.Object);

        // Assert
        viewModel.Title.Should().Be("تعديل منتج");
    }

    #endregion

    #region Property Notification Tests

    [Fact]
    public void Name_Set_NotifiesPropertyChanged()
    {
        // Arrange
        var viewModel = new ProductEditorViewModel(
            _mockProductService.Object,
            _mockCategoryService.Object,
            _mockUnitService.Object,
            _mockEventBus.Object,
            _mockDialogService.Object);
        var propertyChangedEvents = new List<string>();
        viewModel.PropertyChanged += (s, e) => propertyChangedEvents.Add(e.PropertyName ?? string.Empty);

        // Act
        viewModel.Name = "منتج جديد";

        // Assert
        propertyChangedEvents.Should().Contain("Name");
    }

    [Fact]
    public void SalePrice_Set_NotifiesPropertyChanged()
    {
        // Arrange
        var viewModel = new ProductEditorViewModel(
            _mockProductService.Object,
            _mockCategoryService.Object,
            _mockUnitService.Object,
            _mockEventBus.Object,
            _mockDialogService.Object);
        var propertyChangedEvents = new List<string>();
        viewModel.PropertyChanged += (s, e) => propertyChangedEvents.Add(e.PropertyName ?? string.Empty);

        // Act
        viewModel.RetailPrice = 100;

        // Assert
        propertyChangedEvents.Should().Contain("RetailPrice");
    }

    [Fact]
    public void IsBusy_IsReadOnly_FromViewModelBase()
    {
        // Arrange
        var viewModel = new ProductEditorViewModel(
            _mockProductService.Object,
            _mockCategoryService.Object,
            _mockUnitService.Object,
            _mockEventBus.Object,
            _mockDialogService.Object);

        // Assert
        viewModel.IsBusy.Should().BeFalse();
    }

    [Fact]
    public void ErrorMessage_Set_NotifiesPropertyChanged()
    {
        // Arrange
        var viewModel = new ProductEditorViewModel(
            _mockProductService.Object,
            _mockCategoryService.Object,
            _mockUnitService.Object,
            _mockEventBus.Object,
            _mockDialogService.Object);
        var propertyChangedEvents = new List<string>();
        viewModel.PropertyChanged += (s, e) => propertyChangedEvents.Add(e.PropertyName ?? string.Empty);

        // Act
        viewModel.ErrorMessage = "خطأ";

        // Assert
        propertyChangedEvents.Should().Contain("ErrorMessage");
    }

    [Fact]
    public void IsActive_Set_NotifiesPropertyChanged()
    {
        // Arrange
        var viewModel = new ProductEditorViewModel(
            _mockProductService.Object,
            _mockCategoryService.Object,
            _mockUnitService.Object,
            _mockEventBus.Object,
            _mockDialogService.Object);
        var propertyChangedEvents = new List<string>();
        viewModel.PropertyChanged += (s, e) => propertyChangedEvents.Add(e.PropertyName ?? string.Empty);

        // Act
        viewModel.IsActive = false;

        // Assert
        propertyChangedEvents.Should().Contain("IsActive");
    }

    #endregion

    #region CancelCommand Tests

    [Fact]
    public void CancelCommand_WhenExecuted_InvokesCloseRequested()
    {
        // Arrange
        var viewModel = new ProductEditorViewModel(
            _mockProductService.Object,
            _mockCategoryService.Object,
            _mockUnitService.Object,
            _mockEventBus.Object,
            _mockDialogService.Object);
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
        var viewModel = new ProductEditorViewModel(
            _mockProductService.Object,
            _mockCategoryService.Object,
            _mockUnitService.Object,
            _mockEventBus.Object,
            _mockDialogService.Object);

        // Act & Assert
        viewModel.CancelCommand.CanExecute(null).Should().BeTrue();
    }

    #endregion

    #region Category Selection Tests

    [Fact]
    public void SelectedCategory_WhenSet_SetsCategoryId()
    {
        // Arrange
        var viewModel = new ProductEditorViewModel(
            _mockProductService.Object,
            _mockCategoryService.Object,
            _mockUnitService.Object,
            _mockEventBus.Object,
            _mockDialogService.Object);
        var category = new CategoryDto(Id: 1, Name: "فئة تجريبية", Description: null, IsActive: true);

        // Act
        viewModel.SelectedCategory = category;

        // Assert
        viewModel.CategoryId.Should().Be(1);
    }

    [Fact]
    public void SelectedCategory_WhenSetToNull_SetsCategoryIdToNull()
    {
        // Arrange
        var viewModel = new ProductEditorViewModel(
            _mockProductService.Object,
            _mockCategoryService.Object,
            _mockUnitService.Object,
            _mockEventBus.Object,
            _mockDialogService.Object);
        viewModel.SelectedCategory = new CategoryDto(Id: 1, Name: "فئة تجريبية", Description: null, IsActive: true);

        // Act
        viewModel.SelectedCategory = null;

        // Assert
        viewModel.CategoryId.Should().BeNull();
    }

    [Fact]
    public void SelectedCategory_WhenSet_NotifiesPropertyChanged()
    {
        // Arrange
        var viewModel = new ProductEditorViewModel(
            _mockProductService.Object,
            _mockCategoryService.Object,
            _mockUnitService.Object,
            _mockEventBus.Object,
            _mockDialogService.Object);
        var propertyChangedEvents = new List<string>();
        viewModel.PropertyChanged += (s, e) => propertyChangedEvents.Add(e.PropertyName ?? string.Empty);

        // Act
        viewModel.SelectedCategory = new CategoryDto(Id: 1, Name: "فئة تجريبية", Description: null, IsActive: true);

        // Assert
        propertyChangedEvents.Should().Contain("SelectedCategory");
    }

    #endregion

    #region Unit Selection Tests

    [Fact]
    public void SelectedUnit_WhenSet_SetsUnitId()
    {
        // Arrange
        var viewModel = new ProductEditorViewModel(
            _mockProductService.Object,
            _mockCategoryService.Object,
            _mockUnitService.Object,
            _mockEventBus.Object,
            _mockDialogService.Object);
        var unit = new UnitDto(Id: 1, Name: "وحدة تجريبية", Symbol: "م", IsActive: true);

        // Act
        viewModel.SelectedUnit = unit;

        // Assert
        viewModel.UnitId.Should().Be(1);
    }

    [Fact]
    public void SelectedUnit_WhenSetToNull_SetsUnitIdToNull()
    {
        // Arrange
        var viewModel = new ProductEditorViewModel(
            _mockProductService.Object,
            _mockCategoryService.Object,
            _mockUnitService.Object,
            _mockEventBus.Object,
            _mockDialogService.Object);
        viewModel.SelectedUnit = new UnitDto(Id: 1, Name: "وحدة تجريبية", Symbol: "م", IsActive: true);

        // Act
        viewModel.SelectedUnit = null;

        // Assert
        viewModel.UnitId.Should().BeNull();
    }

    #endregion

    #region Helper Methods

    private static ProductDto CreateTestProductDto(
        int id,
        string name)
    {
        return new ProductDto(
            Id: id,
            Barcode: null,
            Name: name,
            CategoryId: 1,
            CategoryName: "فئة تجريبية",
            UnitId: 1,
            UnitName: "وحدة تجريبية",
            RetailUnitId: 1,
            RetailUnitName: "وحدة تجريبية",
            WholesaleUnitId: 2,
            WholesaleUnitName: "كرتون",
            ConversionFactor: 10,
            PurchasePrice: 50,
            SalePrice: 100,
            RetailPrice: 100,
            WholesalePrice: 900,
            MinStock: 10,
            Description: null,
            ExpirationDate: null,
            ImagePath: null,
            IsActive: true);
    }

    #endregion
}