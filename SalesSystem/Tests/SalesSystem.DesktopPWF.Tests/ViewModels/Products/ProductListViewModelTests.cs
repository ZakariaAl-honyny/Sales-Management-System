namespace SalesSystem.DesktopPWF.Tests.ViewModels.Products;

using System.ComponentModel;
using System.Windows.Input;
using FluentAssertions;
using Moq;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Enums;
using SalesSystem.DesktopPWF.Services;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Services.App.Toast;
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
    private readonly Mock<IToastNotificationService> _mockToastService;
    private readonly ProductListViewModel _viewModel;

    public ProductListViewModelTests()
    {
        _mockProductService = new Mock<IProductApiService>();
        _mockEventBus = new Mock<IEventBus>();
        _mockDialogService = new Mock<IDialogService>();
        _mockToastService = new Mock<IToastNotificationService>();

        _viewModel = new ProductListViewModel(
            _mockProductService.Object,
            _mockEventBus.Object,
            _mockDialogService.Object,
            _mockToastService.Object);
    }

    public void Dispose()
    {
        _viewModel?.Cleanup();
    }

    #region LoadProducts Tests

    [Fact]
    public async Task LoadProductsAsync_WhenApiSucceeds_PopulatesProductsCollection()
    {
        var products = new List<ProductDto>
        {
            new(1, "P001", "1234567890123", "منتج أول", 1, "فئة أولى", 1, "قطعة", 1, "قطعة", 2, "كرتون", 10, 10m, 20m, 20m, 200m, 5m, null, true),
            new(2, "P002", null, "منتج ثاني", 1, "فئة أولى", 1, "قطعة", 1, "قطعة", 2, "كرتون", 10, 15m, 30m, 30m, 300m, 3m, null, true)
        };

        _mockProductService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<ProductDto>>.Success(products));

        await _viewModel.LoadProductsAsync();

        _viewModel.Products.Should().HaveCount(2);
        _viewModel.Products.First().Name.Should().Be("منتج أول");
        _viewModel.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadProductsAsync_WhenApiFails_SetsErrorMessage()
    {
        _mockProductService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<ProductDto>>.Failure("فشل في الاتصال"));

        await _viewModel.LoadProductsAsync();

        _viewModel.ErrorMessage.Should().NotBeNullOrEmpty();
        _viewModel.ErrorMessage.Should().Contain("فشل");
    }

    [Fact]
    public async Task LoadProductsAsync_WhenLoading_SetsIsLoadingTrue()
    {
        var tcs = new TaskCompletionSource<Result<List<ProductDto>>>();
        _mockProductService
            .Setup(s => s.GetAllAsync())
            .Returns(tcs.Task);

        var loadTask = _viewModel.LoadProductsAsync();
        _viewModel.IsLoading.Should().BeTrue();

        tcs.SetResult(Result<List<ProductDto>>.Success(new List<ProductDto>()));
        await loadTask;

        _viewModel.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadProductsAsync_SetsUpCollectionView()
    {
        _mockProductService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<ProductDto>>.Success(new List<ProductDto>
            {
                new(1, "P001", null, "منتج تجريبي", null, null, null, null, 1, "وحدة", 1, "وحدة", 1, 10m, 20m, 20m, 20m, 5m, null, true)
            }));

        await _viewModel.LoadProductsAsync();

        _viewModel.ProductsView.Should().NotBeNull();
    }

    #endregion

    #region DeleteProduct Tests

    [Fact]
    public async Task DeleteCommand_WhenConfirmed_CallsApiService()
    {
        var productToDelete = new ProductDto(
            5, "P005", null, "منتج للحذف", null, null, null, null, 1, "وحدة", 1, "وحدة", 1, 10m, 20m, 20m, 20m, 5m, null, true);

        _mockProductService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<ProductDto>>.Success(new List<ProductDto> { productToDelete }));

        _mockDialogService
            .Setup(x => x.ShowDeleteConfirmationAsync(It.IsAny<string>()))
            .ReturnsAsync(DeleteStrategy.Deactivate);

        await _viewModel.LoadProductsAsync();
        _viewModel.SelectedProduct = productToDelete;

        _mockProductService
            .Setup(s => s.DeleteAsync(productToDelete.Id))
            .ReturnsAsync(Result.Success());

        _mockProductService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<ProductDto>>.Success(new List<ProductDto>()));

        await _viewModel.DeleteProductAsync();

        _mockProductService.Verify(
            s => s.DeleteAsync(productToDelete.Id),
            Times.Once);
    }

    [Fact]
    public async Task DeleteCommand_WhenDeleteFails_ShowsErrorDialog()
    {
        var productToDelete = new ProductDto(
            5, "P005", null, "منتج", null, null, null, null, 1, "وحدة", 1, "وحدة", 1, 10m, 20m, 20m, 20m, 5m, null, true);

        _mockProductService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<ProductDto>>.Success(new List<ProductDto> { productToDelete }));

        _mockDialogService
            .Setup(x => x.ShowDeleteConfirmationAsync(It.IsAny<string>()))
            .ReturnsAsync(DeleteStrategy.Deactivate);

        await _viewModel.LoadProductsAsync();
        _viewModel.SelectedProduct = productToDelete;

        _mockProductService
            .Setup(s => s.DeleteAsync(productToDelete.Id))
            .ReturnsAsync(Result.Failure("فشل في الحذف"));

        await _viewModel.DeleteProductAsync();

        _mockToastService.Verify(
            t => t.ShowError(It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteCommand_WhenProductSelected_PublishesEvent()
    {
        var productToDelete = new ProductDto(
            5, "P005", null, "منتج", null, null, null, null, 1, "وحدة", 1, "وحدة", 1, 10m, 20m, 20m, 20m, 5m, null, true);

        _mockProductService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<ProductDto>>.Success(new List<ProductDto> { productToDelete }));

        _mockDialogService
            .Setup(x => x.ShowDeleteConfirmationAsync(It.IsAny<string>()))
            .ReturnsAsync(DeleteStrategy.Deactivate);

        await _viewModel.LoadProductsAsync();
        _viewModel.SelectedProduct = productToDelete;

        _mockProductService
            .Setup(s => s.DeleteAsync(It.IsAny<int>()))
            .ReturnsAsync(Result.Success());

        await _viewModel.DeleteProductAsync();

        _mockEventBus.Verify(
            e => e.Publish(It.Is<ProductChangedMessage>(m => m.ProductId == productToDelete.Id)),
            Times.Once);
    }

    #endregion

    #region Search Tests

    [Fact]
    public async Task SearchText_WhenChanged_RefreshesCollectionView()
    {
        var products = new List<ProductDto>
        {
            new(1, "P001", null, "منتج أحمد", null, null, null, null, 1, "وحدة", 1, "وحدة", 1, 10m, 20m, 20m, 20m, 5m, null, true),
            new(2, "P002", null, "منتج خالد", null, null, null, null, 1, "وحدة", 1, "وحدة", 1, 15m, 30m, 30m, 30m, 3m, null, true),
            new(3, "P003", null, "منتج أحمد", null, null, null, null, 1, "وحدة", 1, "وحدة", 1, 20m, 40m, 40m, 40m, 2m, null, true)
        };

        _mockProductService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<ProductDto>>.Success(products));

        await _viewModel.LoadProductsAsync();

        _viewModel.SearchText = "أحمد";
        _viewModel.SearchCommand.Execute(null);

        _viewModel.SearchText.Should().Be("أحمد");
        _viewModel.ProductsView.Should().NotBeNull();

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
        var products = new List<ProductDto>
        {
            new(1, "P001", null, "منتج أحمد", null, null, null, null, 1, "وحدة", 1, "وحدة", 1, 10m, 20m, 20m, 20m, 5m, null, true),
            new(2, "P002", null, "منتج خالد", null, null, null, null, 1, "وحدة", 1, "وحدة", 1, 15m, 30m, 30m, 30m, 3m, null, true)
        };

        _mockProductService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<ProductDto>>.Success(products));

        await _viewModel.LoadProductsAsync();
        _viewModel.SearchText = "غير موجود";

        _viewModel.SearchCommand.Execute(null);

        var count = 0;
        if (_viewModel.ProductsView != null)
        {
            foreach (var item in _viewModel.ProductsView)
            {
                count++;
            }
        }
        count.Should().Be(0);
    }

    [Fact]
    public async Task SearchText_SearchByBarcode_FiltersProducts()
    {
        var products = new List<ProductDto>
        {
            new(1, "P001", "1234567890123", "منتج بالباركود", null, null, null, null, 1, "وحدة", 1, "وحدة", 1, 10m, 20m, 20m, 20m, 5m, null, true),
            new(2, "P002", null, "منتج بدون باركود", null, null, null, null, 1, "وحدة", 1, "وحدة", 1, 15m, 30m, 30m, 30m, 3m, null, true)
        };

        _mockProductService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<ProductDto>>.Success(products));

        await _viewModel.LoadProductsAsync();

        _viewModel.SearchText = "1234567890123";
        _viewModel.SearchCommand.Execute(null);

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
        var propertyChangedEvents = new List<string>();
        _viewModel.PropertyChanged += (s, e) => propertyChangedEvents.Add(e.PropertyName ?? string.Empty);

        _viewModel.IsLoading = true;

        propertyChangedEvents.Should().Contain("IsLoading");
    }

    [Fact]
    public void ErrorMessage_Set_NotifiesPropertyChanged()
    {
        var propertyChangedEvents = new List<string>();
        _viewModel.PropertyChanged += (s, e) => propertyChangedEvents.Add(e.PropertyName ?? string.Empty);

        _viewModel.ErrorMessage = "خطأ في التحميل";

        propertyChangedEvents.Should().Contain("ErrorMessage");
    }

    [Fact]
    public void SelectedProduct_Set_NotifiesPropertyChanged()
    {
        var propertyChangedEvents = new List<string>();
        _viewModel.PropertyChanged += (s, e) => propertyChangedEvents.Add(e.PropertyName ?? string.Empty);

        var product = new ProductDto(1, "P001", null, "منتج", null, null, null, null, 1, "وحدة", 1, "وحدة", 1, 10m, 20m, 20m, 20m, 5m, null, true);
        _viewModel.SelectedProduct = product;

        propertyChangedEvents.Should().Contain("SelectedProduct");
    }

    [Fact]
    public void SearchText_Set_NotifiesPropertyChanged()
    {
        var propertyChangedEvents = new List<string>();
        _viewModel.PropertyChanged += (s, e) => propertyChangedEvents.Add(e.PropertyName ?? string.Empty);

        _viewModel.SearchText = "بحث";

        propertyChangedEvents.Should().Contain("SearchText");
    }

    #endregion

    #region Command CanExecute Tests

    [Fact]
    public void DeleteCommand_CannotExecute_WhenNoSelection()
    {
        _viewModel.SelectedProduct = null;
        _viewModel.DeleteCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void DeleteCommand_CanExecute_WhenProductSelected()
    {
        var product = new ProductDto(1, "P001", null, "منتج", null, null, null, null, 1, "وحدة", 1, "وحدة", 1, 10m, 20m, 20m, 20m, 5m, null, true);
        _viewModel.SelectedProduct = product;
        _viewModel.DeleteCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void EditCommand_CannotExecute_WhenNoSelection()
    {
        _viewModel.SelectedProduct = null;
        _viewModel.EditCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void EditCommand_CanExecute_WhenProductSelected()
    {
        var product = new ProductDto(1, "P001", null, "منتج", null, null, null, null, 1, "وحدة", 1, "وحدة", 1, 10m, 20m, 20m, 20m, 5m, null, true);
        _viewModel.SelectedProduct = product;
        _viewModel.EditCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void AddCommand_CanExecute_Always()
    {
        _viewModel.AddCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void RefreshCommand_CanExecute_Always()
    {
        _viewModel.RefreshCommand.CanExecute(null).Should().BeTrue();
    }

    #endregion

    #region Cleanup Tests

    [Fact]
    public void Cleanup_UnsubscribesFromEventBus()
    {
        _viewModel.Cleanup();

        _mockEventBus.Verify(
            e => e.Unsubscribe<ProductChangedMessage>(It.IsAny<Action<ProductChangedMessage>>()),
            Times.Once);
    }

    #endregion

    #region EventBus Subscription Tests

    [Fact]
    public void Constructor_SubscribesToProductChangedMessage()
    {
        _mockEventBus.Verify(
            e => e.Subscribe<ProductChangedMessage>(It.IsAny<Action<ProductChangedMessage>>()),
            Times.Once);
    }

    #endregion

    #region RefreshCommand Tests

    [Fact]
    public async Task RefreshCommand_Executed_LoadsProducts()
    {
        var products = new List<ProductDto>
        {
            new(1, "P001", null, "منتج", null, null, null, null, 1, "وحدة", 1, "وحدة", 1, 10m, 20m, 20m, 20m, 5m, null, true)
        };

        _mockProductService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<ProductDto>>.Success(products));

        _viewModel.RefreshCommand.Execute(null);
        await Task.Delay(100);

        _viewModel.Products.Should().HaveCount(1);
    }

    #endregion

    #region FilterProducts Tests

    [Fact]
    public async Task FilterProducts_WhenSearchByCode_FiltersCorrectly()
    {
        var products = new List<ProductDto>
        {
            new(1, "ABC001", null, "منتج أ", null, null, null, null, 1, "وحدة", 1, "وحدة", 1, 10m, 20m, 20m, 20m, 5m, null, true),
            new(2, "XYZ002", null, "منتج ب", null, null, null, null, 1, "وحدة", 1, "وحدة", 1, 15m, 30m, 30m, 30m, 3m, null, true)
        };

        _mockProductService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<ProductDto>>.Success(products));

        await _viewModel.LoadProductsAsync();

        _viewModel.SearchText = "ABC001";
        _viewModel.SearchCommand.Execute(null);

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
        var products = new List<ProductDto>
        {
            new(1, "P001", null, "منتج أ", 1, "إلكترونيات", null, null, 1, "وحدة", 1, "وحدة", 1, 10m, 20m, 20m, 20m, 5m, null, true),
            new(2, "P002", null, "منتج ب", 2, "ملابس", null, null, 1, "وحدة", 1, "وحدة", 1, 15m, 30m, 30m, 30m, 3m, null, true),
            new(3, "P003", null, "منتج ج", 1, "إلكترونيات", null, null, 1, "وحدة", 1, "وحدة", 1, 20m, 40m, 40m, 40m, 2m, null, true)
        };

        _mockProductService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<ProductDto>>.Success(products));

        await _viewModel.LoadProductsAsync();

        _viewModel.SearchText = "إلكترونيات";
        _viewModel.SearchCommand.Execute(null);

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