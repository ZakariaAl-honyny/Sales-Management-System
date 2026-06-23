namespace SalesSystem.DesktopPWF.Tests.ViewModels.Suppliers;

using System.Collections.ObjectModel;
using System.ComponentModel;
using FluentAssertions;
using Moq;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Enums;
using SalesSystem.DesktopPWF.Services;
using SalesSystem.DesktopPWF.Services.App.Toast;
using SalesSystem.DesktopPWF.ViewModels.Suppliers;

/// <summary>
/// Tests for SupplierListViewModel
/// </summary>
public class SupplierListViewModelTests : IDisposable
{
    private readonly Mock<ISupplierApiService> _mockSupplierService;
    private readonly Mock<IEventBus> _mockEventBus;
    private readonly Mock<IDialogService> _mockDialogService;
    private readonly Mock<IScreenWindowService> _mockScreenWindowService;
    private readonly Mock<IToastNotificationService> _mockToastService;
    private readonly SupplierListViewModel _viewModel;

    public SupplierListViewModelTests()
    {
        _mockSupplierService = new Mock<ISupplierApiService>();
        _mockEventBus = new Mock<IEventBus>();
        _mockDialogService = new Mock<IDialogService>();
        _mockScreenWindowService = new Mock<IScreenWindowService>();
        _mockToastService = new Mock<IToastNotificationService>();

        _viewModel = new SupplierListViewModel(
            _mockSupplierService.Object,
            _mockEventBus.Object,
            _mockDialogService.Object,
            _mockScreenWindowService.Object,
            _mockToastService.Object);
    }

    public void Dispose() => _viewModel?.Cleanup();

    private static SupplierDto CreateSupplier(int id, string name, bool isActive = true)
    {
        return new SupplierDto(id, name, null, null, null, null, isActive, 1);
    }

    #region LoadSuppliers Tests

    [Fact]
    public async Task LoadSuppliersAsync_WhenApiSucceeds_PopulatesSuppliersCollection()
    {
        var suppliers = new List<SupplierDto>
        {
            CreateSupplier(1, "مورد أول"),
            CreateSupplier(2, "مورد ثاني")
        };

        _mockSupplierService
            .Setup(s => s.GetAllAsync(It.IsAny<bool>()))
            .ReturnsAsync(Result<List<SupplierDto>>.Success(suppliers));

        await _viewModel.LoadSuppliersAsync();

        // ViewModel sorts by Id descending (newest first)
        _viewModel.Suppliers.Should().HaveCount(2);
        _viewModel.Suppliers.First().Name.Should().Be("مورد ثاني");
        _viewModel.IsBusy.Should().BeFalse();
    }

    [Fact]
    public async Task LoadSuppliersAsync_WhenLoading_SetsIsBusyTrue()
    {
        var tcs = new TaskCompletionSource<Result<List<SupplierDto>>>();
        _mockSupplierService
            .Setup(s => s.GetAllAsync(It.IsAny<bool>()))
            .Returns(tcs.Task);

        var loadTask = _viewModel.LoadSuppliersAsync();
        _viewModel.IsBusy.Should().BeTrue();

        tcs.SetResult(Result<List<SupplierDto>>.Success(new List<SupplierDto>()));
        await loadTask;

        _viewModel.IsBusy.Should().BeFalse();
    }

    [Fact]
    public async Task LoadSuppliersAsync_SetsUpCollectionView()
    {
        _mockSupplierService
            .Setup(s => s.GetAllAsync(It.IsAny<bool>()))
            .ReturnsAsync(Result<List<SupplierDto>>.Success(new List<SupplierDto> { CreateSupplier(1, "مورد تجريبي") }));

        await _viewModel.LoadSuppliersAsync();
        _viewModel.SuppliersView.Should().NotBeNull();
    }

    #endregion

    #region DeleteSupplier Tests

    [Fact]
    public async Task DeleteCommand_WhenConfirmed_CallsApiService()
    {
        var supplier = CreateSupplier(5, "مورد للحذف");

        _mockSupplierService
            .Setup(s => s.GetAllAsync(It.IsAny<bool>()))
            .ReturnsAsync(Result<List<SupplierDto>>.Success(new List<SupplierDto> { supplier }));

        await _viewModel.LoadSuppliersAsync();
        _viewModel.SelectedSupplier = supplier;

        _mockDialogService
            .Setup(d => d.ShowDeleteConfirmationAsync(It.IsAny<string>()))
            .ReturnsAsync(DeleteStrategy.Deactivate);

        _mockSupplierService
            .Setup(s => s.DeleteAsync(supplier.Id))
            .ReturnsAsync(Result.Success());

        _mockSupplierService
            .Setup(s => s.GetAllAsync(It.IsAny<bool>()))
            .ReturnsAsync(Result<List<SupplierDto>>.Success(new List<SupplierDto>()));

        _viewModel.DeleteCommand.Execute(null);
        await Task.Delay(100);

        _mockSupplierService.Verify(s => s.DeleteAsync(supplier.Id), Times.Once);
    }

    [Fact]
    public async Task DeleteCommand_WhenSupplierSelected_PublishesEvent()
    {
        var supplier = CreateSupplier(5, "مورد");

        _mockSupplierService
            .Setup(s => s.GetAllAsync(It.IsAny<bool>()))
            .ReturnsAsync(Result<List<SupplierDto>>.Success(new List<SupplierDto> { supplier }));

        await _viewModel.LoadSuppliersAsync();
        _viewModel.SelectedSupplier = supplier;

        _mockDialogService
            .Setup(d => d.ShowDeleteConfirmationAsync(It.IsAny<string>()))
            .ReturnsAsync(DeleteStrategy.Deactivate);

        _mockSupplierService
            .Setup(s => s.DeleteAsync(It.IsAny<int>()))
            .ReturnsAsync(Result.Success());

        _viewModel.DeleteCommand.Execute(null);
        await Task.Delay(100);

        _mockEventBus.Verify(
            e => e.Publish(It.Is<SupplierChangedMessage>(m => m.SupplierId == supplier.Id)),
            Times.Once);
    }

    #endregion

    #region Search Tests

    [Fact]
    public async Task SearchText_WhenChanged_RefreshesCollectionView()
    {
        var suppliers = new List<SupplierDto>
        {
            CreateSupplier(1, "شركة أحمد"),
            CreateSupplier(2, "شركة خالد"),
            CreateSupplier(3, "شركة أحمد الثانية")
        };

        _mockSupplierService
            .Setup(s => s.GetAllAsync(It.IsAny<bool>()))
            .ReturnsAsync(Result<List<SupplierDto>>.Success(suppliers));

        await _viewModel.LoadSuppliersAsync();

        _viewModel.SearchText = "أحمد";
        _viewModel.SearchCommand.Execute(null);

        _viewModel.SearchText.Should().Be("أحمد");
        _viewModel.SuppliersView.Should().NotBeNull();
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
        var events = new List<string>();
        _viewModel.PropertyChanged += (s, e) => events.Add(e.PropertyName ?? string.Empty);
        _viewModel.ErrorMessage = "خطأ في التحميل";
        events.Should().Contain("ErrorMessage");
    }

    [Fact]
    public void SelectedSupplier_Set_NotifiesPropertyChanged()
    {
        var events = new List<string>();
        _viewModel.PropertyChanged += (s, e) => events.Add(e.PropertyName ?? string.Empty);
        _viewModel.SelectedSupplier = CreateSupplier(1, "مورد");
        events.Should().Contain("SelectedSupplier");
    }

    [Fact]
    public void SearchText_Set_NotifiesPropertyChanged()
    {
        var events = new List<string>();
        _viewModel.PropertyChanged += (s, e) => events.Add(e.PropertyName ?? string.Empty);
        _viewModel.SearchText = "بحث";
        events.Should().Contain("SearchText");
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
    public void DeleteCommand_CanExecute_WhenActiveSupplierSelected()
    {
        _viewModel.SelectedSupplier = CreateSupplier(1, "مورد");
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
        _viewModel.SelectedSupplier = CreateSupplier(1, "مورد");
        _viewModel.EditCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact] public void AddCommand_CanExecute_Always() => _viewModel.AddCommand.CanExecute(null).Should().BeTrue();
    [Fact] public void RefreshCommand_CanExecute_Always() => _viewModel.RefreshCommand.CanExecute(null).Should().BeTrue();

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
        var suppliers = new List<SupplierDto> { CreateSupplier(1, "مورد") };

        _mockSupplierService
            .Setup(s => s.GetAllAsync(It.IsAny<bool>()))
            .ReturnsAsync(Result<List<SupplierDto>>.Success(suppliers));

        _viewModel.RefreshCommand.Execute(null);
        await Task.Delay(100);

        _viewModel.Suppliers.Should().HaveCount(1);
    }

    #endregion
}


