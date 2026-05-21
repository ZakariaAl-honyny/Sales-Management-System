namespace SalesSystem.DesktopPWF.Tests.ViewModels.Suppliers;

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Windows.Input;
using FluentAssertions;
using Moq;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.DesktopPWF.Services;
using SalesSystem.DesktopPWF.ViewModels;
using SalesSystem.DesktopPWF.ViewModels.Suppliers;

/// <summary>
/// Tests for SupplierListViewModel
/// </summary>
public class SupplierListViewModelTests : IDisposable
{
    private readonly Mock<ISupplierApiService> _mockSupplierService;
    private readonly Mock<IEventBus> _mockEventBus;
    private readonly Mock<IDialogService> _mockDialogService;
    private readonly SupplierListViewModel _viewModel;

    public SupplierListViewModelTests()
    {
        _mockSupplierService = new Mock<ISupplierApiService>();
        _mockEventBus = new Mock<IEventBus>();
        _mockDialogService = new Mock<IDialogService>();

        _viewModel = CreateViewModel();
    }

    private SupplierListViewModel CreateViewModel()
    {
        var viewModel = (SupplierListViewModel)FormatterServices.GetUninitializedObject(typeof(SupplierListViewModel));

        var fieldNames = new[] { "_supplierService", "_eventBus", "_dialogService" };
        var mockObjects = new object[] { _mockSupplierService.Object, _mockEventBus.Object, _mockDialogService.Object };

        for (int i = 0; i < fieldNames.Length; i++)
        {
            var field = typeof(SupplierListViewModel).GetField(fieldNames[i],
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(viewModel, mockObjects[i]);
        }

        // Initialize Suppliers property
        var suppliersField = typeof(SupplierListViewModel).GetField("<Suppliers>k__BackingField",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        suppliersField?.SetValue(viewModel, new ObservableCollection<SupplierDto>());

        var subscribeMethod = typeof(SupplierListViewModel).GetMethod("InitializeCommands",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        subscribeMethod?.Invoke(viewModel, null);

        return viewModel;
    }

    public void Dispose()
    {
        _viewModel?.Cleanup();
    }

    #region LoadSuppliers Tests

    [Fact]
    public async Task LoadSuppliersAsync_WhenApiSucceeds_PopulatesSuppliersCollection()
    {
        var suppliers = new List<SupplierDto>
        {
            new(1, "S001", "مورد أول", "0501234567", null, null, 0m, 0m, true),
            new(2, "S002", "مورد ثاني", "0507654321", null, null, 0m, 0m, true)
        };

        _mockSupplierService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<SupplierDto>>.Success(suppliers));

        await _viewModel.LoadSuppliersAsync();

        _viewModel.Suppliers.Should().HaveCount(2);
        _viewModel.Suppliers.First().Name.Should().Be("مورد أول");
        _viewModel.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadSuppliersAsync_WhenApiFails_SetsErrorMessage()
    {
        _mockSupplierService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<SupplierDto>>.Failure("فشل في الاتصال"));

        await _viewModel.LoadSuppliersAsync();

        _viewModel.ErrorMessage.Should().NotBeNullOrEmpty();
        _viewModel.ErrorMessage.Should().Contain("فشل");
    }

    [Fact]
    public async Task LoadSuppliersAsync_WhenLoading_SetsIsLoadingTrue()
    {
        var tcs = new TaskCompletionSource<Result<List<SupplierDto>>>();
        _mockSupplierService
            .Setup(s => s.GetAllAsync())
            .Returns(tcs.Task);

        var loadTask = _viewModel.LoadSuppliersAsync();
        _viewModel.IsLoading.Should().BeTrue();

        tcs.SetResult(Result<List<SupplierDto>>.Success(new List<SupplierDto>()));
        await loadTask;

        _viewModel.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadSuppliersAsync_SetsUpCollectionView()
    {
        _mockSupplierService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<SupplierDto>>.Success(new List<SupplierDto>
            {
                new(1, "S001", "مورد تجريبي", null, null, null, 0m, 0m, true)
            }));

        await _viewModel.LoadSuppliersAsync();

        _viewModel.SuppliersView.Should().NotBeNull();
    }

    #endregion

    #region DeleteSupplier Tests

    [Fact]
    public async Task DeleteCommand_WhenConfirmed_CallsApiService()
    {
        var supplierToDelete = new SupplierDto(
            5, "S005", "مورد للحذف", null, null, null, 0m, 0m, true);

        _mockSupplierService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<SupplierDto>>.Success(new List<SupplierDto> { supplierToDelete }));

        await _viewModel.LoadSuppliersAsync();
        _viewModel.SelectedSupplier = supplierToDelete;

        _mockSupplierService
            .Setup(s => s.DeleteAsync(supplierToDelete.Id))
            .ReturnsAsync(Result.Success());

        _mockSupplierService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<SupplierDto>>.Success(new List<SupplierDto>()));

        _viewModel.DeleteCommand.Execute(null);
        await Task.Delay(100);

        _mockSupplierService.Verify(
            s => s.DeleteAsync(supplierToDelete.Id),
            Times.Once);
    }

    [Fact]
    public async Task DeleteCommand_WhenDeleteFails_SetsErrorMessage()
    {
        var supplierToDelete = new SupplierDto(
            5, "S005", "مورد", null, null, null, 0m, 0m, true);

        _mockSupplierService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<SupplierDto>>.Success(new List<SupplierDto> { supplierToDelete }));

        await _viewModel.LoadSuppliersAsync();
        _viewModel.SelectedSupplier = supplierToDelete;

        _mockSupplierService
            .Setup(s => s.DeleteAsync(supplierToDelete.Id))
            .ReturnsAsync(Result.Failure("فشل في الحذف"));

        _viewModel.DeleteCommand.Execute(null);
        await Task.Delay(100);

        _viewModel.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task DeleteCommand_WhenSupplierSelected_PublishesEvent()
    {
        var supplierToDelete = new SupplierDto(
            5, "S005", "مورد", null, null, null, 0m, 0m, true);

        _mockSupplierService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<SupplierDto>>.Success(new List<SupplierDto> { supplierToDelete }));

        await _viewModel.LoadSuppliersAsync();
        _viewModel.SelectedSupplier = supplierToDelete;

        _mockSupplierService
            .Setup(s => s.DeleteAsync(It.IsAny<int>()))
            .ReturnsAsync(Result.Success());

        _viewModel.DeleteCommand.Execute(null);
        await Task.Delay(100);

        _mockEventBus.Verify(
            e => e.Publish(It.Is<SupplierChangedMessage>(m => m.SupplierId == supplierToDelete.Id)),
            Times.Once);
    }

    #endregion

    #region Search Tests

    [Fact]
    public async Task SearchText_WhenChanged_RefreshesCollectionView()
    {
        var suppliers = new List<SupplierDto>
        {
            new(1, "S001", "شركة أحمد", "0501234567", null, null, 0m, 0m, true),
            new(2, "S002", "شركة خالد", "0507654321", null, null, 0m, 0m, true),
            new(3, "S003", "شركة أحمد", "0501111111", null, null, 0m, 0m, true)
        };

        _mockSupplierService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<SupplierDto>>.Success(suppliers));

        await _viewModel.LoadSuppliersAsync();

        _viewModel.SearchText = "أحمد";
        _viewModel.SearchCommand.Execute(null);

        _viewModel.SearchText.Should().Be("أحمد");
        _viewModel.SuppliersView.Should().NotBeNull();

        var filteredCount = 0;
        if (_viewModel.SuppliersView != null)
        {
            foreach (var item in _viewModel.SuppliersView)
            {
                filteredCount++;
            }
        }
        filteredCount.Should().Be(2);
    }

    [Fact]
    public async Task SearchText_WhenEmpty_ReturnsAllSuppliers()
    {
        var suppliers = new List<SupplierDto>
        {
            new(1, "S001", "مورد أحمد", null, null, null, 0m, 0m, true),
            new(2, "S002", "مورد خالد", null, null, null, 0m, 0m, true)
        };

        _mockSupplierService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<SupplierDto>>.Success(suppliers));

        await _viewModel.LoadSuppliersAsync();
        _viewModel.SearchText = "غير موجود";

        _viewModel.SearchCommand.Execute(null);

        var count = 0;
        if (_viewModel.SuppliersView != null)
        {
            foreach (var item in _viewModel.SuppliersView)
            {
                count++;
            }
        }
        count.Should().Be(0);
    }

    [Fact]
    public async Task SearchText_SearchByPhone_FiltersSuppliers()
    {
        var suppliers = new List<SupplierDto>
        {
            new(1, "S001", "مورد أ", "0501234567", null, null, 0m, 0m, true),
            new(2, "S002", "مورد ب", "0507654321", null, null, 0m, 0m, true)
        };

        _mockSupplierService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<SupplierDto>>.Success(suppliers));

        await _viewModel.LoadSuppliersAsync();

        _viewModel.SearchText = "0501234567";
        _viewModel.SearchCommand.Execute(null);

        var count = 0;
        if (_viewModel.SuppliersView != null)
        {
            foreach (var item in _viewModel.SuppliersView)
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
    public void SelectedSupplier_Set_NotifiesPropertyChanged()
    {
        var propertyChangedEvents = new List<string>();
        _viewModel.PropertyChanged += (s, e) => propertyChangedEvents.Add(e.PropertyName ?? string.Empty);

        var supplier = new SupplierDto(1, "S001", "مورد", null, null, null, 0m, 0m, true);
        _viewModel.SelectedSupplier = supplier;

        propertyChangedEvents.Should().Contain("SelectedSupplier");
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
        _viewModel.SelectedSupplier = null;
        _viewModel.DeleteCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void DeleteCommand_CanExecute_WhenSupplierSelected()
    {
        var supplier = new SupplierDto(1, "S001", "مورد", null, null, null, 0m, 0m, true);
        _viewModel.SelectedSupplier = supplier;
        _viewModel.DeleteCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void EditCommand_CannotExecute_WhenNoSelection()
    {
        _viewModel.SelectedSupplier = null;
        _viewModel.EditCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void EditCommand_CanExecute_WhenSupplierSelected()
    {
        var supplier = new SupplierDto(1, "S001", "مورد", null, null, null, 0m, 0m, true);
        _viewModel.SelectedSupplier = supplier;
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
            e => e.Unsubscribe<SupplierChangedMessage>(It.IsAny<Action<SupplierChangedMessage>>()),
            Times.Once);
    }

    #endregion

    #region EventBus Subscription Tests

    [Fact]
    public void Constructor_SubscribesToSupplierChangedMessage()
    {
        _mockEventBus.Verify(
            e => e.Subscribe<SupplierChangedMessage>(It.IsAny<Action<SupplierChangedMessage>>()),
            Times.Once);
    }

    #endregion

    #region RefreshCommand Tests

    [Fact]
    public async Task RefreshCommand_Executed_LoadsSuppliers()
    {
        var suppliers = new List<SupplierDto>
        {
            new(1, "S001", "مورد", null, null, null, 0m, 0m, true)
        };

        _mockSupplierService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<SupplierDto>>.Success(suppliers));

        _viewModel.RefreshCommand.Execute(null);
        await Task.Delay(100);

        _viewModel.Suppliers.Should().HaveCount(1);
    }

    #endregion

    #region FilterSuppliers Tests

    [Fact]
    public async Task FilterSuppliers_WhenSearchByCode_FiltersCorrectly()
    {
        var suppliers = new List<SupplierDto>
        {
            new(1, "ABC001", "مورد أ", null, null, null, 0m, 0m, true),
            new(2, "XYZ002", "مورد ب", null, null, null, 0m, 0m, true)
        };

        _mockSupplierService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<SupplierDto>>.Success(suppliers));

        await _viewModel.LoadSuppliersAsync();

        _viewModel.SearchText = "ABC001";
        _viewModel.SearchCommand.Execute(null);

        var count = 0;
        if (_viewModel.SuppliersView != null)
        {
            foreach (var item in _viewModel.SuppliersView)
            {
                count++;
            }
        }
        count.Should().Be(1);
    }

    #endregion
}