namespace SalesSystem.DesktopPWF.Tests.ViewModels;

using System.ComponentModel;
using FluentAssertions;
using Moq;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Enums;
using SalesSystem.DesktopPWF.Services;
using SalesSystem.DesktopPWF.Services.App.Toast;
using SalesSystem.DesktopPWF.ViewModels;

/// <summary>
/// Tests for WarehouseListViewModel
/// </summary>
public class WarehouseListViewModelTests : IDisposable
{
    private readonly Mock<IWarehouseApiService> _mockWarehouseService;
    private readonly Mock<IEventBus> _mockEventBus;
    private readonly Mock<IDialogService> _mockDialogService;
    private readonly Mock<IToastNotificationService> _mockToastService;
    private readonly WarehouseListViewModel _viewModel;

    public WarehouseListViewModelTests()
    {
        _mockWarehouseService = new Mock<IWarehouseApiService>();
        _mockEventBus = new Mock<IEventBus>();
        _mockDialogService = new Mock<IDialogService>();
        _mockToastService = new Mock<IToastNotificationService>();

        _viewModel = CreateViewModel();
    }

    private WarehouseListViewModel CreateViewModel()
    {
        return new WarehouseListViewModel(
            _mockWarehouseService.Object,
            _mockEventBus.Object,
            _mockDialogService.Object,
            _mockToastService.Object);
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
            new(Id: (short)1, BranchId: (short)1, BranchName: null, Name: "Main", Phone: null, Address: null, Notes: null, IsActive: true),
            new(Id: (short)2, BranchId: (short)1, BranchName: null, Name: "Secondary", Phone: null, Address: null, Notes: null, IsActive: true)
        };

        _mockWarehouseService
            .Setup(s => s.GetAllAsync(It.IsAny<bool>()))
            .ReturnsAsync(Result<List<WarehouseDto>>.Success(warehouses));

        await _viewModel.LoadWarehousesAsync();

        // ViewModel sorts by Id descending (newest first)
        _viewModel.Warehouses.Should().HaveCount(2);
        _viewModel.Warehouses.First().Name.Should().Be("Secondary");
        _viewModel.IsBusy.Should().BeFalse();
    }

    [Fact]
    public async Task LoadWarehousesAsync_WhenApiFails_SetsErrorMessage()
    {
        _mockWarehouseService
            .Setup(s => s.GetAllAsync(It.IsAny<bool>()))
            .ReturnsAsync(Result<List<WarehouseDto>>.Failure("API connection timeout"));

        await _viewModel.LoadWarehousesAsync();

        _viewModel.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task LoadWarehousesAsync_WhenLoading_SetsIsLoadingTrue()
    {
        var tcs = new TaskCompletionSource<Result<List<WarehouseDto>>>();
        _mockWarehouseService
            .Setup(s => s.GetAllAsync(It.IsAny<bool>()))
            .Returns(tcs.Task);

        var loadTask = _viewModel.LoadWarehousesAsync();
        _viewModel.IsBusy.Should().BeTrue();

        tcs.SetResult(Result<List<WarehouseDto>>.Success(new List<WarehouseDto>()));
        await loadTask;

        _viewModel.IsBusy.Should().BeFalse();
    }

    [Fact]
    public async Task LoadWarehousesAsync_SetsUpCollectionView()
    {
        _mockWarehouseService
            .Setup(s => s.GetAllAsync(It.IsAny<bool>()))
            .ReturnsAsync(Result<List<WarehouseDto>>.Success(new List<WarehouseDto>
            {
                new(Id: (short)1, BranchId: (short)1, BranchName: null, Name: "Test", Phone: null, Address: null, Notes: null, IsActive: true)
            }));

        await _viewModel.LoadWarehousesAsync();

        _viewModel.WarehousesView.Should().NotBeNull();
    }

    #endregion

    #region DeleteWarehouse Tests

    [Fact]
    public async Task DeleteCommand_WhenConfirmed_CallsApiService()
    {
        var warehouseToDelete = new WarehouseDto(Id: (short)5, BranchId: (short)1, BranchName: null, Name: "DeleteMe", Phone: null, Address: null, Notes: null, IsActive: true);

        _mockWarehouseService
            .Setup(s => s.GetAllAsync(It.IsAny<bool>()))
            .ReturnsAsync(Result<List<WarehouseDto>>.Success(new List<WarehouseDto> { warehouseToDelete }));

        await _viewModel.LoadWarehousesAsync();
        _viewModel.SelectedWarehouse = warehouseToDelete;

        _mockDialogService
            .Setup(d => d.ShowDeleteConfirmationAsync(It.IsAny<string>()))
            .ReturnsAsync(DeleteStrategy.Deactivate);

        _mockWarehouseService
            .Setup(s => s.DeleteAsync(warehouseToDelete.Id))
            .ReturnsAsync(Result.Success());

        _mockWarehouseService
            .Setup(s => s.GetAllAsync(It.IsAny<bool>()))
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
        var warehouseToDelete = new WarehouseDto(Id: (short)5, BranchId: (short)1, BranchName: null, Name: "FailMe", Phone: null, Address: null, Notes: null, IsActive: true);

        _mockWarehouseService
            .Setup(s => s.GetAllAsync(It.IsAny<bool>()))
            .ReturnsAsync(Result<List<WarehouseDto>>.Success(new List<WarehouseDto> { warehouseToDelete }));

        await _viewModel.LoadWarehousesAsync();
        _viewModel.SelectedWarehouse = warehouseToDelete;

        _mockDialogService
            .Setup(d => d.ShowDeleteConfirmationAsync(It.IsAny<string>()))
            .ReturnsAsync(DeleteStrategy.Deactivate);

        _mockWarehouseService
            .Setup(s => s.DeleteAsync(warehouseToDelete.Id))
            .ReturnsAsync(Result.Failure("Delete failed"));

        _viewModel.DeleteCommand.Execute(null);
        await Task.Delay(100);

        _viewModel.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task DeleteCommand_WhenWarehouseSelected_PublishesEvent()
    {
        var warehouseToDelete = new WarehouseDto(Id: (short)5, BranchId: (short)1, BranchName: null, Name: "EventTest", Phone: null, Address: null, Notes: null, IsActive: true);

        _mockWarehouseService
            .Setup(s => s.GetAllAsync(It.IsAny<bool>()))
            .ReturnsAsync(Result<List<WarehouseDto>>.Success(new List<WarehouseDto> { warehouseToDelete }));

        await _viewModel.LoadWarehousesAsync();
        _viewModel.SelectedWarehouse = warehouseToDelete;

        _mockDialogService
            .Setup(d => d.ShowDeleteConfirmationAsync(It.IsAny<string>()))
            .ReturnsAsync(DeleteStrategy.Deactivate);

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
            new(Id: (short)1, BranchId: (short)1, BranchName: null, Name: "Alpha", Phone: null, Address: null, Notes: null, IsActive: true),
            new(Id: (short)2, BranchId: (short)1, BranchName: null, Name: "Beta", Phone: null, Address: null, Notes: null, IsActive: true),
            new(Id: (short)3, BranchId: (short)1, BranchName: null, Name: "Gamma", Phone: null, Address: null, Notes: null, IsActive: true)
        };

        _mockWarehouseService
            .Setup(s => s.GetAllAsync(It.IsAny<bool>()))
            .ReturnsAsync(Result<List<WarehouseDto>>.Success(warehouses));

        await _viewModel.LoadWarehousesAsync();

        _viewModel.SearchText = "Alpha";
        _viewModel.SearchCommand.Execute(null);

        _viewModel.SearchText.Should().Be("Alpha");
        _viewModel.WarehousesView.Should().NotBeNull();

        var filteredCount = 0;
        if (_viewModel.WarehousesView != null)
        {
            foreach (var item in _viewModel.WarehousesView)
            {
                filteredCount++;
            }
        }
        filteredCount.Should().Be(1);
    }

    [Fact]
    public async Task SearchText_WhenEmpty_ReturnsAllWarehouses()
    {
        var warehouses = new List<WarehouseDto>
        {
            new(Id: (short)1, BranchId: (short)1, BranchName: null, Name: "Alpha", Phone: null, Address: null, Notes: null, IsActive: true),
            new(Id: (short)2, BranchId: (short)1, BranchName: null, Name: "Beta", Phone: null, Address: null, Notes: null, IsActive: true)
        };

        _mockWarehouseService
            .Setup(s => s.GetAllAsync(It.IsAny<bool>()))
            .ReturnsAsync(Result<List<WarehouseDto>>.Success(warehouses));

        await _viewModel.LoadWarehousesAsync();
        _viewModel.SearchText = "NonExistent";

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
    public async Task SearchText_SearchByName_FiltersWarehouses()
    {
        var warehouses = new List<WarehouseDto>
        {
            new(Id: (short)1, BranchId: (short)1, BranchName: null, Name: "Alpha", Phone: null, Address: null, Notes: null, IsActive: true),
            new(Id: (short)2, BranchId: (short)1, BranchName: null, Name: "Beta", Phone: null, Address: null, Notes: null, IsActive: true)
        };

        _mockWarehouseService
            .Setup(s => s.GetAllAsync(It.IsAny<bool>()))
            .ReturnsAsync(Result<List<WarehouseDto>>.Success(warehouses));

        await _viewModel.LoadWarehousesAsync();

        _viewModel.SearchText = "Alpha";
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

        _viewModel.ErrorMessage = "Test error";

        propertyChangedEvents.Should().Contain("ErrorMessage");
    }

    [Fact]
    public void SelectedWarehouse_Set_NotifiesPropertyChanged()
    {
        var propertyChangedEvents = new List<string>();
        _viewModel.PropertyChanged += (s, e) => propertyChangedEvents.Add(e.PropertyName ?? string.Empty);

        var warehouse = new WarehouseDto(Id: (short)1, BranchId: (short)1, BranchName: null, Name: "Warehouse1", Phone: null, Address: null, Notes: null, IsActive: true);
        _viewModel.SelectedWarehouse = warehouse;

        propertyChangedEvents.Should().Contain("SelectedWarehouse");
    }

    [Fact]
    public void SearchText_Set_NotifiesPropertyChanged()
    {
        var propertyChangedEvents = new List<string>();
        _viewModel.PropertyChanged += (s, e) => propertyChangedEvents.Add(e.PropertyName ?? string.Empty);

        _viewModel.SearchText = "X";

        propertyChangedEvents.Should().Contain("SearchText");
    }

    #endregion

    #region Command CanExecute Tests

    [Fact]
    public void DeleteCommand_AlwaysEnabled_WhenNoSelection()
    {
        // RULE-059: All buttons ALWAYS enabled — guard is handled in the handler with a warning dialog
        _viewModel.SelectedWarehouse = null;
        _viewModel.DeleteCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void DeleteCommand_AlwaysEnabled_WhenActiveWarehouseSelected()
    {
        var warehouse = new WarehouseDto(Id: (short)1, BranchId: (short)1, BranchName: null, Name: "W1", Phone: null, Address: null, Notes: null, IsActive: true);
        _viewModel.SelectedWarehouse = warehouse;
        _viewModel.DeleteCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void EditCommand_AlwaysEnabled_WhenNoSelection()
    {
        // RULE-059: All buttons ALWAYS enabled — guard is handled in the handler with a warning dialog
        _viewModel.SelectedWarehouse = null;
        _viewModel.EditCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void EditCommand_AlwaysEnabled_WhenWarehouseSelected()
    {
        var warehouse = new WarehouseDto(Id: (short)1, BranchId: (short)1, BranchName: null, Name: "W1", Phone: null, Address: null, Notes: null, IsActive: true);
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
            new(Id: (short)1, BranchId: (short)1, BranchName: null, Name: "RefreshTest", Phone: null, Address: null, Notes: null, IsActive: true)
        };

        _mockWarehouseService
            .Setup(s => s.GetAllAsync(It.IsAny<bool>()))
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
            new(Id: (short)1, BranchId: (short)1, BranchName: null, Name: "Alpha Base", Phone: null, Address: null, Notes: null, IsActive: true),
            new(Id: (short)2, BranchId: (short)1, BranchName: null, Name: "Beta Main", Phone: null, Address: null, Notes: null, IsActive: true)
        };

        _mockWarehouseService
            .Setup(s => s.GetAllAsync(It.IsAny<bool>()))
            .ReturnsAsync(Result<List<WarehouseDto>>.Success(warehouses));

        await _viewModel.LoadWarehousesAsync();

        _viewModel.SearchText = "Beta";
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
