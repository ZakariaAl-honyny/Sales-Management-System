namespace SalesSystem.DesktopPWF.Tests.ViewModels;

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.Serialization;
using FluentAssertions;
using Moq;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.DesktopPWF.Services;
using SalesSystem.DesktopPWF.ViewModels;

/// <summary>
/// Tests for WarehouseListViewModel
/// </summary>
public class WarehouseListViewModelTests : IDisposable
{
    private readonly Mock<IWarehouseApiService> _mockWarehouseService;
    private readonly Mock<IEventBus> _mockEventBus;
    private readonly Mock<IDialogService> _mockDialogService;
    private readonly WarehouseListViewModel _viewModel;

    public WarehouseListViewModelTests()
    {
        _mockWarehouseService = new Mock<IWarehouseApiService>();
        _mockEventBus = new Mock<IEventBus>();
        _mockDialogService = new Mock<IDialogService>();

        _viewModel = CreateViewModel();
    }

    private WarehouseListViewModel CreateViewModel()
    {
        var viewModel = (WarehouseListViewModel)FormatterServices.GetUninitializedObject(typeof(WarehouseListViewModel));

        var fieldNames = new[] { "_warehouseService", "_eventBus", "_dialogService" };
        var mockObjects = new object[] { _mockWarehouseService.Object, _mockEventBus.Object, _mockDialogService.Object };

        for (int i = 0; i < fieldNames.Length; i++)
        {
            var field = typeof(WarehouseListViewModel).GetField(fieldNames[i],
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(viewModel, mockObjects[i]);
        }

        // Initialize Warehouses property
        var warehousesField = typeof(WarehouseListViewModel).GetField("<Warehouses>k__BackingField",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        warehousesField?.SetValue(viewModel, new ObservableCollection<WarehouseDto>());

        var subscribeMethod = typeof(WarehouseListViewModel).GetMethod("InitializeCommands",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        subscribeMethod?.Invoke(viewModel, null);

        return viewModel;
    }

    public void Dispose()
    {
        _viewModel?.Cleanup();
    }

    #region LoadWarehouses Tests

    [Fact]
    public async Task LoadWarehousesAsync_WhenApiSucceeds_PopulatesWarehousesCollection()
    {
        var warehouses = new List<WarehouseDto>
        {
            new(1, "W001", "المستودع الرئيسي", "الرياض", true, true),
            new(2, "W002", "المستودع الثاني", "جدة", true, true)
        };

        _mockWarehouseService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<WarehouseDto>>.Success(warehouses));

        await _viewModel.LoadWarehousesAsync();

        _viewModel.Warehouses.Should().HaveCount(2);
        _viewModel.Warehouses.First().Name.Should().Be("المستودع الرئيسي");
        _viewModel.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadWarehousesAsync_WhenApiFails_SetsErrorMessage()
    {
        _mockWarehouseService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<WarehouseDto>>.Failure("فشل في الاتصال"));

        await _viewModel.LoadWarehousesAsync();

        _viewModel.ErrorMessage.Should().NotBeNullOrEmpty();
        _viewModel.ErrorMessage.Should().Contain("فشل");
    }

    [Fact]
    public async Task LoadWarehousesAsync_WhenLoading_SetsIsLoadingTrue()
    {
        var tcs = new TaskCompletionSource<Result<List<WarehouseDto>>>();
        _mockWarehouseService
            .Setup(s => s.GetAllAsync())
            .Returns(tcs.Task);

        var loadTask = _viewModel.LoadWarehousesAsync();
        _viewModel.IsLoading.Should().BeTrue();

        tcs.SetResult(Result<List<WarehouseDto>>.Success(new List<WarehouseDto>()));
        await loadTask;

        _viewModel.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadWarehousesAsync_SetsUpCollectionView()
    {
        _mockWarehouseService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<WarehouseDto>>.Success(new List<WarehouseDto>
            {
                new(1, "W001", "مستودع تجريبي", null, true, true)
            }));

        await _viewModel.LoadWarehousesAsync();

        _viewModel.WarehousesView.Should().NotBeNull();
    }

    #endregion

    #region DeleteWarehouse Tests

    [Fact]
    public async Task DeleteCommand_WhenConfirmed_CallsApiService()
    {
        var warehouseToDelete = new WarehouseDto(5, "W005", "مستودع للحذف", null, true, true);

        _mockWarehouseService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<WarehouseDto>>.Success(new List<WarehouseDto> { warehouseToDelete }));

        await _viewModel.LoadWarehousesAsync();
        _viewModel.SelectedWarehouse = warehouseToDelete;

        _mockWarehouseService
            .Setup(s => s.DeleteAsync(warehouseToDelete.Id))
            .ReturnsAsync(Result.Success());

        _mockWarehouseService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<WarehouseDto>>.Success(new List<WarehouseDto>()));

        _viewModel.DeleteCommand.Execute(null);
        await Task.Delay(100);

        _mockWarehouseService.Verify(
            s => s.DeleteAsync(warehouseToDelete.Id),
            Times.Once);
    }

    [Fact]
    public async Task DeleteCommand_WhenDeleteFails_SetsErrorMessage()
    {
        var warehouseToDelete = new WarehouseDto(5, "W005", "مستودع", null, true, true);

        _mockWarehouseService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<WarehouseDto>>.Success(new List<WarehouseDto> { warehouseToDelete }));

        await _viewModel.LoadWarehousesAsync();
        _viewModel.SelectedWarehouse = warehouseToDelete;

        _mockWarehouseService
            .Setup(s => s.DeleteAsync(warehouseToDelete.Id))
            .ReturnsAsync(Result.Failure("فشل في الحذف"));

        _viewModel.DeleteCommand.Execute(null);
        await Task.Delay(100);

        _viewModel.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task DeleteCommand_WhenWarehouseSelected_PublishesEvent()
    {
        var warehouseToDelete = new WarehouseDto(5, "W005", "مستودع", null, true, true);

        _mockWarehouseService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<WarehouseDto>>.Success(new List<WarehouseDto> { warehouseToDelete }));

        await _viewModel.LoadWarehousesAsync();
        _viewModel.SelectedWarehouse = warehouseToDelete;

        _mockWarehouseService
            .Setup(s => s.DeleteAsync(It.IsAny<int>()))
            .ReturnsAsync(Result.Success());

        _viewModel.DeleteCommand.Execute(null);
        await Task.Delay(100);

        _mockEventBus.Verify(
            e => e.Publish(It.Is<WarehouseChangedMessage>(m => m.WarehouseId == warehouseToDelete.Id)),
            Times.Once);
    }

    #endregion

    #region Search Tests

    [Fact]
    public async Task SearchText_WhenChanged_RefreshesCollectionView()
    {
        var warehouses = new List<WarehouseDto>
        {
            new(1, "W001", "مستودع الرياض", "الرياض", true, true),
            new(2, "W002", "مستودع جدة", "جدة", true, true),
            new(3, "W003", "مستودع الرياض الفرعي", "الرياض", true, true)
        };

        _mockWarehouseService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<WarehouseDto>>.Success(warehouses));

        await _viewModel.LoadWarehousesAsync();

        _viewModel.SearchText = "الرياض";
        _viewModel.SearchCommand.Execute(null);

        _viewModel.SearchText.Should().Be("الرياض");
        _viewModel.WarehousesView.Should().NotBeNull();

        var filteredCount = 0;
        if (_viewModel.WarehousesView != null)
        {
            foreach (var item in _viewModel.WarehousesView)
            {
                filteredCount++;
            }
        }
        filteredCount.Should().Be(2);
    }

    [Fact]
    public async Task SearchText_WhenEmpty_ReturnsAllWarehouses()
    {
        var warehouses = new List<WarehouseDto>
        {
            new(1, "W001", "مستودع الرياض", null, true, true),
            new(2, "W002", "مستودع جدة", null, true, true)
        };

        _mockWarehouseService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<WarehouseDto>>.Success(warehouses));

        await _viewModel.LoadWarehousesAsync();
        _viewModel.SearchText = "غير موجود";

        _viewModel.SearchCommand.Execute(null);

        var count = 0;
        if (_viewModel.WarehousesView != null)
        {
            foreach (var item in _viewModel.WarehousesView)
            {
                count++;
            }
        }
        count.Should().Be(0);
    }

    [Fact]
    public async Task SearchText_SearchByCode_FiltersWarehouses()
    {
        var warehouses = new List<WarehouseDto>
        {
            new(1, "WH001", "مستودع أ", null, true, true),
            new(2, "WH002", "مستودع ب", null, true, true)
        };

        _mockWarehouseService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<WarehouseDto>>.Success(warehouses));

        await _viewModel.LoadWarehousesAsync();

        _viewModel.SearchText = "WH001";
        _viewModel.SearchCommand.Execute(null);

        var count = 0;
        if (_viewModel.WarehousesView != null)
        {
            foreach (var item in _viewModel.WarehousesView)
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
    public void SelectedWarehouse_Set_NotifiesPropertyChanged()
    {
        var propertyChangedEvents = new List<string>();
        _viewModel.PropertyChanged += (s, e) => propertyChangedEvents.Add(e.PropertyName ?? string.Empty);

        var warehouse = new WarehouseDto(1, "W001", "مستودع", null, true, true);
        _viewModel.SelectedWarehouse = warehouse;

        propertyChangedEvents.Should().Contain("SelectedWarehouse");
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
        _viewModel.SelectedWarehouse = null;
        _viewModel.DeleteCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void DeleteCommand_CanExecute_WhenWarehouseSelected()
    {
        var warehouse = new WarehouseDto(1, "W001", "مستودع", null, true, true);
        _viewModel.SelectedWarehouse = warehouse;
        _viewModel.DeleteCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void EditCommand_CannotExecute_WhenNoSelection()
    {
        _viewModel.SelectedWarehouse = null;
        _viewModel.EditCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void EditCommand_CanExecute_WhenWarehouseSelected()
    {
        var warehouse = new WarehouseDto(1, "W001", "مستودع", null, true, true);
        _viewModel.SelectedWarehouse = warehouse;
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
            e => e.Unsubscribe<WarehouseChangedMessage>(It.IsAny<Action<WarehouseChangedMessage>>()),
            Times.Once);
    }

    #endregion

    #region EventBus Subscription Tests

    [Fact]
    public void Constructor_SubscribesToWarehouseChangedMessage()
    {
        _mockEventBus.Verify(
            e => e.Subscribe<WarehouseChangedMessage>(It.IsAny<Action<WarehouseChangedMessage>>()),
            Times.Once);
    }

    #endregion

    #region RefreshCommand Tests

    [Fact]
    public async Task RefreshCommand_Executed_LoadsWarehouses()
    {
        var warehouses = new List<WarehouseDto>
        {
            new(1, "W001", "مستودع", null, true, true)
        };

        _mockWarehouseService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<WarehouseDto>>.Success(warehouses));

        _viewModel.RefreshCommand.Execute(null);
        await Task.Delay(100);

        _viewModel.Warehouses.Should().HaveCount(1);
    }

    #endregion

    #region FilterWarehouses Tests

    [Fact]
    public async Task FilterWarehouses_WhenSearchByName_FiltersCorrectly()
    {
        var warehouses = new List<WarehouseDto>
        {
            new(1, "W001", "مستودع رئيسي", null, true, true),
            new(2, "W002", "مستودع فرعي", null, true, true)
        };

        _mockWarehouseService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<WarehouseDto>>.Success(warehouses));

        await _viewModel.LoadWarehousesAsync();

        _viewModel.SearchText = "رئيسي";
        _viewModel.SearchCommand.Execute(null);

        var count = 0;
        if (_viewModel.WarehousesView != null)
        {
            foreach (var item in _viewModel.WarehousesView)
            {
                count++;
            }
        }
        count.Should().Be(1);
    }

    #endregion
}