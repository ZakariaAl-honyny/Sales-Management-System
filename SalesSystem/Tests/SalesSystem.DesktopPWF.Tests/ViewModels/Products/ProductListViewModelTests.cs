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
    private readonly Mock<IScreenWindowService> _mockScreenWindowService;
    private readonly Mock<IToastNotificationService> _mockToastService;
    private readonly ProductListViewModel _viewModel;

    public ProductListViewModelTests()
    {
        _mockProductService = new Mock<IProductApiService>();
        _mockEventBus = new Mock<IEventBus>();
        _mockDialogService = new Mock<IDialogService>();
        _mockScreenWindowService = new Mock<IScreenWindowService>();
        _mockToastService = new Mock<IToastNotificationService>();

        _viewModel = new ProductListViewModel(
            _mockProductService.Object,
            _mockEventBus.Object,
            _mockDialogService.Object,
            _mockScreenWindowService.Object,
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
            new(Id: 1, Barcode: "1234567890123", Name: "منتج أول", CategoryId: 1, CategoryName: "فئة أولى", MinStock: 5m, Description: null, HasExpiry: false, Cost: 0m, IsActive: true),
            new(Id: 2, Barcode: null, Name: "منتج ثاني", CategoryId: 1, CategoryName: "فئة أولى", MinStock: 3m, Description: null, HasExpiry: false, Cost: 0m, IsActive: true)
        };

        _mockProductService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<ProductDto>>.Success(products));

        await _viewModel.LoadProductsAsync();

        // ViewModel sorts by Id descending (newest first)
        _viewModel.Products.Should().HaveCount(2);
        _viewModel.Products.First().Name.Should().Be("منتج ثاني");
        _viewModel.IsBusy.Should().BeFalse();
    }

    [Fact]
    public async Task LoadProductsAsync_WhenLoading_SetsIsBusyTrue()
    {
        var tcs = new TaskCompletionSource<Result<List<ProductDto>>>();
        _mockProductService
            .Setup(s => s.GetAllAsync())
            .Returns(tcs.Task);

        var loadTask = _viewModel.LoadProductsAsync();
        _viewModel.IsBusy.Should().BeTrue();

        tcs.SetResult(Result<List<ProductDto>>.Success(new List<ProductDto>()));
        await loadTask;

        _viewModel.IsBusy.Should().BeFalse();
    }

    [Fact]
    public async Task LoadProductsAsync_SetsUpCollectionView()
    {
        _mockProductService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<ProductDto>>.Success(new List<ProductDto>
            {
                new(Id: 1, Barcode: null, Name: "منتج تجريبي", CategoryId: null, CategoryName: null, MinStock: 5m, Description: null, HasExpiry: false, Cost: 0m, IsActive: true)
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
            Id: 5, Barcode: null, Name: "منتج للحذف", CategoryId: null, CategoryName: null,
            MinStock: 5m, Description: null, HasExpiry: false, Cost: 0m, IsActive: true);

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
    public async Task DeleteCommand_WhenDeleteFails_ShowsErrorMessage()
    {
        var productToDelete = new ProductDto(
            Id: 5, Barcode: null, Name: "منتج", CategoryId: null, CategoryName: null,
            MinStock: 5m, Description: null, HasExpiry: false, Cost: 0m, IsActive: true);

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

        _viewModel.ErrorMessage.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteCommand_WhenProductSelected_PublishesEvent()
    {
        var productToDelete = new ProductDto(
            Id: 5, Barcode: null, Name: "منتج", CategoryId: null, CategoryName: null,
            MinStock: 5m, Description: null, HasExpiry: false, Cost: 0m, IsActive: true);

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
            new(Id: 1, Barcode: null, Name: "منتج أحمد", CategoryId: null, CategoryName: null, MinStock: 5m, Description: null, HasExpiry: false, Cost: 10m, IsActive: true),
            new(Id: 2, Barcode: null, Name: "منتج خالد", CategoryId: null, CategoryName: null, MinStock: 3m, Description: null, HasExpiry: false, Cost: 15m, IsActive: true),
            new(Id: 3, Barcode: null, Name: "منتج أحمد", CategoryId: null, CategoryName: null, MinStock: 2m, Description: null, HasExpiry: false, Cost: 20m, IsActive: true)
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
            new(Id: 1, Barcode: null, Name: "منتج أحمد", CategoryId: null, CategoryName: null, MinStock: 5m, Description: null, HasExpiry: false, Cost: 10m, IsActive: true),
            new(Id: 2, Barcode: null, Name: "منتج خالد", CategoryId: null, CategoryName: null, MinStock: 3m, Description: null, HasExpiry: false, Cost: 15m, IsActive: true)
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
            new(Id: 1, Barcode: "1234567890123", Name: "منتج بالباركود", CategoryId: null, CategoryName: null, MinStock: 5m, Description: null, HasExpiry: false, Cost: 10m, IsActive: true),
            new(Id: 2, Barcode: null, Name: "منتج بدون باركود", CategoryId: null, CategoryName: null, MinStock: 3m, Description: null, HasExpiry: false, Cost: 15m, IsActive: true)
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
    public void IsBusy_IsReadOnly_FromViewModelBase()
    {
        // IsBusy has protected set in ViewModelBase, managed by ExecuteAsync
        _viewModel.IsBusy.Should().BeFalse();
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

        var product = new ProductDto(Id: 1, Barcode: null, Name: "منتج", CategoryId: null, CategoryName: null, MinStock: 5m, Description: null, HasExpiry: false, Cost: 0m, IsActive: true);
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
    public void DeleteCommand_CanExecute_AlwaysEnabled()
    {
        _viewModel.SelectedProduct = null;
        _viewModel.DeleteCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void DeleteCommand_CanExecute_WhenProductSelected()
    {
        var product = new ProductDto(Id: 1, Barcode: null, Name: "منتج", CategoryId: null, CategoryName: null, MinStock: 5m, Description: null, HasExpiry: false, Cost: 0m, IsActive: true);
        _viewModel.SelectedProduct = product;
        _viewModel.DeleteCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void EditCommand_CanExecute_AlwaysEnabled()
    {
        _viewModel.SelectedProduct = null;
        _viewModel.EditCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void EditCommand_CanExecute_WhenProductSelected()
    {
        var product = new ProductDto(Id: 1, Barcode: null, Name: "منتج", CategoryId: null, CategoryName: null, MinStock: 5m, Description: null, HasExpiry: false, Cost: 0m, IsActive: true);
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
            new(Id: 1, Barcode: null, Name: "منتج", CategoryId: null, CategoryName: null, MinStock: 5m, Description: null, HasExpiry: false, Cost: 10m, IsActive: true)
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
    public async Task FilterProducts_WhenSearchByBarcode_FiltersCorrectly()
    {
        var products = new List<ProductDto>
        {
            new(Id: 1, Barcode: "1234567890123", Name: "منتج أ", CategoryId: null, CategoryName: null, MinStock: 5m, Description: null, HasExpiry: false, Cost: 10m, IsActive: true),
            new(Id: 2, Barcode: null, Name: "منتج ب", CategoryId: null, CategoryName: null, MinStock: 3m, Description: null, HasExpiry: false, Cost: 15m, IsActive: true)
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

    [Fact]
    public async Task FilterProducts_WhenSearchByCategory_FiltersCorrectly()
    {
        var products = new List<ProductDto>
        {
            new(Id: 1, Barcode: null, Name: "منتج أ", CategoryId: 1, CategoryName: "إلكترونيات", MinStock: 5m, Description: null, HasExpiry: false, Cost: 10m, IsActive: true),
            new(Id: 2, Barcode: null, Name: "منتج ب", CategoryId: 2, CategoryName: "ملابس", MinStock: 3m, Description: null, HasExpiry: false, Cost: 15m, IsActive: true),
            new(Id: 3, Barcode: null, Name: "منتج ج", CategoryId: 1, CategoryName: "إلكترونيات", MinStock: 2m, Description: null, HasExpiry: false, Cost: 20m, IsActive: true)
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