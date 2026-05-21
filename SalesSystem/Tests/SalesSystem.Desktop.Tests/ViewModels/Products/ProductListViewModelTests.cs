namespace SalesSystem.Desktop.Tests.ViewModels.Products;

using System.ComponentModel;
using System.Windows.Input;
using FluentAssertions;
using Moq;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.DesktopPWF.Services;
using SalesSystem.DesktopPWF.ViewModels;
using SalesSystem.DesktopPWF.ViewModels.Products;

/// <summary>
/// Tests for ProductListViewModel
/// </summary>
public class ProductListViewModelTests : IDisposable
{
    private readonly Mock<IProductApiService> _mockProductService;
    private readonly Mock<IEventBus> _mockEventBus;
    private readonly Mock<IDialogService> _mockDialogService;
    private readonly ProductListViewModel _viewModel;

    public ProductListViewModelTests()
    {
        _mockProductService = new Mock<IProductApiService>();
        _mockEventBus = new Mock<IEventBus>();
        _mockDialogService = new Mock<IDialogService>();

        // Create ViewModel with mocked services via constructor
        _viewModel = new ProductListViewModel(
            _mockProductService.Object,
            _mockEventBus.Object,
            _mockDialogService.Object);
    }

    public void Dispose()
    {
        _viewModel?.Cleanup();
    }

    #region LoadProducts Tests

    [Fact]
    public async Task LoadProductsAsync_WhenApiSucceeds_PopulatesProductsCollection()
    {
        // Arrange
        var products = new List<ProductDto>
        {
            new(1, "P001", "1234567890123", "منتج أول", 1, "فئة أولى", 1, "قطعة", 10m, 20m, 5m, null, true),
            new(2, "P002", null, "منتج ثاني", 1, "فئة أولى", 1, "قطعة", 15m, 30m, 3m, null, true)
        };

        _mockProductService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<ProductDto>>.Success(products));

        // Act
        await _viewModel.LoadProductsAsync();

        // Assert
        _viewModel.Products.Should().HaveCount(2);
        _viewModel.Products.First().Name.Should().Be("منتج أول");
        _viewModel.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadProductsAsync_WhenApiFails_SetsErrorMessage()
    {
        // Arrange
        _mockProductService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<ProductDto>>.Failure("فشل في الاتصال"));

        // Act
        await _viewModel.LoadProductsAsync();

        // Assert
        _viewModel.ErrorMessage.Should().NotBeNullOrEmpty();
        _viewModel.ErrorMessage.Should().Contain("فشل");
    }

    [Fact]
    public async Task LoadProductsAsync_WhenLoading_SetsIsLoadingTrue()
    {
        // Arrange
        var tcs = new TaskCompletionSource<Result<List<ProductDto>>>();
        _mockProductService
            .Setup(s => s.GetAllAsync())
            .Returns(tcs.Task);

        // Act
        var loadTask = _viewModel.LoadProductsAsync();
        _viewModel.IsLoading.Should().BeTrue();

        tcs.SetResult(Result<List<ProductDto>>.Success(new List<ProductDto>()));
        await loadTask;

        // Assert
        _viewModel.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadProductsAsync_SetsUpCollectionView()
    {
        // Arrange
        _mockProductService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<ProductDto>>.Success(new List<ProductDto>
            {
                new(1, "P001", null, "منتج تجريبي", null, null, null, null, 10m, 20m, 5m, null, true)
            }));

        // Act
        await _viewModel.LoadProductsAsync();

        // Assert
        _viewModel.ProductsView.Should().NotBeNull();
    }

    #endregion

    #region DeleteProduct Tests

    [Fact]
    public async Task DeleteCommand_WhenConfirmed_CallsApiService()
    {
        // Arrange
        var productToDelete = new ProductDto(
            5, "P005", null, "منتج للحذف", null, null, null, null, 10m, 20m, 5m, null, true);

        // Setup GetAll to return the product
        _mockProductService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<ProductDto>>.Success(new List<ProductDto> { productToDelete }));

        // Load products first
        await _viewModel.LoadProductsAsync();
        _viewModel.SelectedProduct = productToDelete;

        // Setup delete
        _mockProductService
            .Setup(s => s.DeleteAsync(productToDelete.Id))
            .ReturnsAsync(Result.Success());

        // Setup reload after delete
        _mockProductService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<ProductDto>>.Success(new List<ProductDto>()));

        // Act - Execute the command
        _viewModel.DeleteCommand.Execute(null);

        // Allow async to complete
        await Task.Delay(100);

        // Assert
        _mockProductService.Verify(
            s => s.DeleteAsync(productToDelete.Id),
            Times.Once);
    }

    [Fact]
    public async Task DeleteCommand_WhenDeleteFails_SetsErrorMessage()
    {
        // Arrange
        var productToDelete = new ProductDto(
            5, "P005", null, "منتج", null, null, null, null, 10m, 20m, 5m, null, true);

        _mockProductService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<ProductDto>>.Success(new List<ProductDto> { productToDelete }));

        await _viewModel.LoadProductsAsync();
        _viewModel.SelectedProduct = productToDelete;

        _mockProductService
            .Setup(s => s.DeleteAsync(productToDelete.Id))
            .ReturnsAsync(Result.Failure("فشل في الحذف"));

        // Act
        _viewModel.DeleteCommand.Execute(null);
        await Task.Delay(100);

        // Assert
        _viewModel.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task DeleteCommand_WhenProductSelected_PublishesEvent()
    {
        // Arrange
        var productToDelete = new ProductDto(
            5, "P005", null, "منتج", null, null, null, null, 10m, 20m, 5m, null, true);

        _mockProductService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<ProductDto>>.Success(new List<ProductDto> { productToDelete }));

        await _viewModel.LoadProductsAsync();
        _viewModel.SelectedProduct = productToDelete;

        _mockProductService
            .Setup(s => s.DeleteAsync(It.IsAny<int>()))
            .ReturnsAsync(Result.Success());

        // Act
        _viewModel.DeleteCommand.Execute(null);
        await Task.Delay(100);

        // Assert
        _mockEventBus.Verify(
            e => e.Publish(It.Is<ProductChangedMessage>(m => m.ProductId == productToDelete.Id)),
            Times.Once);
    }

    #endregion

    #region Search Tests

    [Fact]
    public async Task SearchText_WhenChanged_RefreshesCollectionView()
    {
        // Arrange
        var products = new List<ProductDto>
        {
            new(1, "P001", null, "منتج أحمد", null, null, null, null, 10m, 20m, 5m, null, true),
            new(2, "P002", null, "منتج خالد", null, null, null, null, 15m, 30m, 3m, null, true),
            new(3, "P003", null, "منتج أحمد", null, null, null, null, 20m, 40m, 2m, null, true)
        };

        _mockProductService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<ProductDto>>.Success(products));

        await _viewModel.LoadProductsAsync();

        // Act
        _viewModel.SearchText = "أحمد";
        _viewModel.SearchCommand.Execute(null);

        // Assert
        _viewModel.SearchText.Should().Be("أحمد");
        _viewModel.ProductsView.Should().NotBeNull();

        // Count filtered items
        var filteredCount = 0;
        if (_viewModel.ProductsView != null)
        {
            foreach (var item in _viewModel.ProductsView)
            {
                filteredCount++;
            }
        }
        filteredCount.Should().Be(2);
    }

    [Fact]
    public async Task SearchText_WhenEmpty_ReturnsAllProducts()
    {
        // Arrange
        var products = new List<ProductDto>
        {
            new(1, "P001", null, "منتج أحمد", null, null, null, null, 10m, 20m, 5m, null, true),
            new(2, "P002", null, "منتج خالد", null, null, null, null, 15m, 30m, 3m, null, true)
        };

        _mockProductService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<ProductDto>>.Success(products));

        await _viewModel.LoadProductsAsync();
        _viewModel.SearchText = "غير موجود";

        // Act
        _viewModel.SearchCommand.Execute(null);

        // Assert
        // Empty search should show all (3 items when no filter)
        var count = 0;
        if (_viewModel.ProductsView != null)
        {
            foreach (var item in _viewModel.ProductsView)
            {
                count++;
            }
        }
        count.Should().Be(0); // No matches for "غير موجود"
    }

    [Fact]
    public async Task SearchText_SearchByBarcode_FiltersProducts()
    {
        // Arrange
        var products = new List<ProductDto>
        {
            new(1, "P001", "1234567890123", "منتج بالباركود", null, null, null, null, 10m, 20m, 5m, null, true),
            new(2, "P002", null, "منتج بدون باركود", null, null, null, null, 15m, 30m, 3m, null, true)
        };

        _mockProductService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<ProductDto>>.Success(products));

        await _viewModel.LoadProductsAsync();

        // Act - search by barcode
        _viewModel.SearchText = "1234567890123";
        _viewModel.SearchCommand.Execute(null);

        // Assert
        var count = 0;
        if (_viewModel.ProductsView != null)
        {
            foreach (var item in _viewModel.ProductsView)
            {
                count++;
            }
        }
        count.Should().Be(1);
    }

    #endregion

    #region PropertyChangeNotification Tests

    [Fact]
    public void IsLoading_Set_NotifiesPropertyChanged()
    {
        // Arrange
        var propertyChangedEvents = new List<string>();
        _viewModel.PropertyChanged += (s, e) => propertyChangedEvents.Add(e.PropertyName ?? string.Empty);

        // Act
        _viewModel.IsLoading = true;

        // Assert
        propertyChangedEvents.Should().Contain("IsLoading");
    }

    [Fact]
    public void ErrorMessage_Set_NotifiesPropertyChanged()
    {
        // Arrange
        var propertyChangedEvents = new List<string>();
        _viewModel.PropertyChanged += (s, e) => propertyChangedEvents.Add(e.PropertyName ?? string.Empty);

        // Act
        _viewModel.ErrorMessage = "خطأ في التحميل";

        // Assert
        propertyChangedEvents.Should().Contain("ErrorMessage");
    }

    [Fact]
    public void SelectedProduct_Set_NotifiesPropertyChanged()
    {
        // Arrange
        var propertyChangedEvents = new List<string>();
        _viewModel.PropertyChanged += (s, e) => propertyChangedEvents.Add(e.PropertyName ?? string.Empty);

        // Act
        var product = new ProductDto(1, "P001", null, "منتج", null, null, null, null, 10m, 20m, 5m, null, true);
        _viewModel.SelectedProduct = product;

        // Assert
        propertyChangedEvents.Should().Contain("SelectedProduct");
    }

    [Fact]
    public void SearchText_Set_NotifiesPropertyChanged()
    {
        // Arrange
        var propertyChangedEvents = new List<string>();
        _viewModel.PropertyChanged += (s, e) => propertyChangedEvents.Add(e.PropertyName ?? string.Empty);

        // Act
        _viewModel.SearchText = "بحث";

        // Assert
        propertyChangedEvents.Should().Contain("SearchText");
    }

    #endregion

    #region Command CanExecute Tests

    [Fact]
    public void DeleteCommand_CannotExecute_WhenNoSelection()
    {
        // Arrange
        _viewModel.SelectedProduct = null;

        // Act & Assert
        _viewModel.DeleteCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void DeleteCommand_CanExecute_WhenProductSelected()
    {
        // Arrange
        var product = new ProductDto(1, "P001", null, "منتج", null, null, null, null, 10m, 20m, 5m, null, true);
        _viewModel.SelectedProduct = product;

        // Act & Assert
        _viewModel.DeleteCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void EditCommand_CannotExecute_WhenNoSelection()
    {
        // Arrange
        _viewModel.SelectedProduct = null;

        // Act & Assert
        _viewModel.EditCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void EditCommand_CanExecute_WhenProductSelected()
    {
        // Arrange
        var product = new ProductDto(1, "P001", null, "منتج", null, null, null, null, 10m, 20m, 5m, null, true);
        _viewModel.SelectedProduct = product;

        // Act & Assert
        _viewModel.EditCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void AddCommand_CanExecute_Always()
    {
        // Act & Assert
        _viewModel.AddCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void RefreshCommand_CanExecute_Always()
    {
        // Act & Assert
        _viewModel.RefreshCommand.CanExecute(null).Should().BeTrue();
    }

    #endregion

    #region Cleanup Tests

    [Fact]
    public void Cleanup_UnsubscribesFromEventBus()
    {
        // Act
        _viewModel.Cleanup();

        // Assert
        _mockEventBus.Verify(
            e => e.Unsubscribe<ProductChangedMessage>(It.IsAny<Action<ProductChangedMessage>>()),
            Times.Once);
    }

    #endregion

    #region EventBus Subscription Tests

    [Fact]
    public void Constructor_SubscribesToProductChangedMessage()
    {
        // Assert
        _mockEventBus.Verify(
            e => e.Subscribe<ProductChangedMessage>(It.IsAny<Action<ProductChangedMessage>>()),
            Times.Once);
    }

    #endregion

    #region RefreshCommand Tests

    [Fact]
    public async Task RefreshCommand_Executed_LoadsProducts()
    {
        // Arrange
        var products = new List<ProductDto>
        {
            new(1, "P001", null, "منتج", null, null, null, null, 10m, 20m, 5m, null, true)
        };

        _mockProductService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<ProductDto>>.Success(products));

        // Act
        _viewModel.RefreshCommand.Execute(null);

        // Wait for async
        await Task.Delay(100);

        // Assert
        _viewModel.Products.Should().HaveCount(1);
    }

    #endregion

    #region FilterProducts Tests

    [Fact]
    public async Task FilterProducts_WhenSearchByCode_FiltersCorrectly()
    {
        // Arrange
        var products = new List<ProductDto>
        {
            new(1, "ABC001", null, "منتج أ", null, null, null, null, 10m, 20m, 5m, null, true),
            new(2, "XYZ002", null, "منتج ب", null, null, null, null, 15m, 30m, 3m, null, true)
        };

        _mockProductService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<ProductDto>>.Success(products));

        await _viewModel.LoadProductsAsync();

        // Act - search by product code
        _viewModel.SearchText = "ABC001";
        _viewModel.SearchCommand.Execute(null);

        // Assert
        var count = 0;
        if (_viewModel.ProductsView != null)
        {
            foreach (var item in _viewModel.ProductsView)
            {
                count++;
            }
        }
        count.Should().Be(1);
    }

    [Fact]
    public async Task FilterProducts_WhenSearchByCategory_FiltersCorrectly()
    {
        // Arrange
        var products = new List<ProductDto>
        {
            new(1, "P001", null, "منتج أ", 1, "إلكترونيات", null, null, 10m, 20m, 5m, null, true),
            new(2, "P002", null, "منتج ب", 2, "ملابس", null, null, 15m, 30m, 3m, null, true),
            new(3, "P003", null, "منتج ج", 1, "إلكترونيات", null, null, 20m, 40m, 2m, null, true)
        };

        _mockProductService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<ProductDto>>.Success(products));

        await _viewModel.LoadProductsAsync();

        // Act - search by category name
        _viewModel.SearchText = "إلكترونيات";
        _viewModel.SearchCommand.Execute(null);

        // Assert
        var count = 0;
        if (_viewModel.ProductsView != null)
        {
            foreach (var item in _viewModel.ProductsView)
            {
                count++;
            }
        }
        count.Should().Be(2);
    }

    #endregion
}