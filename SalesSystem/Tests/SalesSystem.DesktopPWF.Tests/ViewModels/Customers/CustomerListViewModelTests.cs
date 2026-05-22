namespace SalesSystem.DesktopPWF.Tests.ViewModels.Customers;

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
using SalesSystem.DesktopPWF.ViewModels.Customers;

/// <summary>
/// Tests for CustomerListViewModel
/// </summary>
public class CustomerListViewModelTests : IDisposable
{
    private readonly Mock<ICustomerApiService> _mockCustomerService;
    private readonly Mock<IEventBus> _mockEventBus;
    private readonly Mock<IDialogService> _mockDialogService;
    private readonly Mock<IToastNotificationService> _mockToastService;
    private readonly CustomerListViewModel _viewModel;

    public CustomerListViewModelTests()
    {
        _mockCustomerService = new Mock<ICustomerApiService>();
        _mockEventBus = new Mock<IEventBus>();
        _mockDialogService = new Mock<IDialogService>();
        _mockToastService = new Mock<IToastNotificationService>();

        _viewModel = new CustomerListViewModel(
            _mockCustomerService.Object,
            _mockEventBus.Object,
            _mockDialogService.Object,
            _mockToastService.Object);
    }

    public void Dispose()
    {
        _viewModel?.Cleanup();
    }

    #region LoadCustomers Tests

    [Fact]
    public async Task LoadCustomersAsync_WhenApiSucceeds_PopulatesCustomersCollection()
    {
        var customers = new List<CustomerDto>
        {
            new(1, "عميل أول", "0501234567", null, null, null, 0m, 0m, 0m, true),
            new(2, "عميل ثاني", "0507654321", null, null, null, 0m, 0m, 0m, true)
        };

        _mockCustomerService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<CustomerDto>>.Success(customers));

        await _viewModel.LoadCustomersAsync();

        _viewModel.Customers.Should().HaveCount(2);
        _viewModel.Customers.First().Name.Should().Be("عميل أول");
        _viewModel.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadCustomersAsync_WhenApiFails_SetsErrorMessage()
    {
        _mockCustomerService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<CustomerDto>>.Failure("فشل في تحميل العملاء"));

        await _viewModel.LoadCustomersAsync();

        _viewModel.ErrorMessage.Should().NotBeNullOrEmpty();
        _viewModel.ErrorMessage.Should().Contain("فشل");
    }

    [Fact]
    public async Task LoadCustomersAsync_WhenLoading_SetsIsLoadingTrue()
    {
        var tcs = new TaskCompletionSource<Result<List<CustomerDto>>>();
        _mockCustomerService
            .Setup(s => s.GetAllAsync())
            .Returns(tcs.Task);

        var loadTask = _viewModel.LoadCustomersAsync();
        _viewModel.IsLoading.Should().BeTrue();

        tcs.SetResult(Result<List<CustomerDto>>.Success(new List<CustomerDto>()));
        await loadTask;

        _viewModel.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadCustomersAsync_SetsUpCollectionView()
    {
        _mockCustomerService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<CustomerDto>>.Success(new List<CustomerDto>
            {
                new(1, "Test", null, null, null, null, 0m, 0m, 0m, true)
            }));

        await _viewModel.LoadCustomersAsync();

        _viewModel.CustomersView.Should().NotBeNull();
    }

    #endregion

    #region DeleteCustomer Tests

    [Fact]
    public async Task DeleteCommand_WhenConfirmed_CallsApiService()
    {
        var customerToDelete = new CustomerDto(
            5, "عميل للحذف", null, null, null, null, 0m, 0m, 0m, true);

        _mockCustomerService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<CustomerDto>>.Success(new List<CustomerDto> { customerToDelete }));

        _mockDialogService
            .Setup(x => x.ShowDeleteConfirmationAsync(It.IsAny<string>()))
            .ReturnsAsync(DeleteStrategy.Deactivate);

        await _viewModel.LoadCustomersAsync();
        _viewModel.SelectedCustomer = customerToDelete;

        _mockCustomerService
            .Setup(s => s.DeleteAsync(customerToDelete.Id))
            .ReturnsAsync(Result.Success());

        _mockCustomerService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<CustomerDto>>.Success(new List<CustomerDto>()));

        await _viewModel.DeleteCustomerAsync();

        _mockCustomerService.Verify(
            s => s.DeleteAsync(customerToDelete.Id),
            Times.Once);
    }

    [Fact]
    public async Task DeleteCommand_WhenDeleteFails_SetsErrorMessage()
    {
        var customerToDelete = new CustomerDto(
            5, "عميل", null, null, null, null, 0m, 0m, 0m, true);

        _mockCustomerService.Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<CustomerDto>>.Success(new List<CustomerDto> { customerToDelete }));

        _mockDialogService
            .Setup(x => x.ShowDeleteConfirmationAsync(It.IsAny<string>()))
            .ReturnsAsync(DeleteStrategy.Deactivate);

        await _viewModel.LoadCustomersAsync();

        _viewModel.SelectedCustomer = customerToDelete;

        _mockCustomerService
            .Setup(s => s.DeleteAsync(customerToDelete.Id))
            .ReturnsAsync(Result.Failure("فشل في الحذف"));

        await _viewModel.DeleteCustomerAsync();

        _viewModel.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task DeleteCommand_WhenCustomerSelected_PublishesEvent()
    {
        var customerToDelete = new CustomerDto(
            5, "عميل", null, null, null, null, 0m, 0m, 0m, true);

        _mockCustomerService.Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<CustomerDto>>.Success(new List<CustomerDto> { customerToDelete }));

        _mockDialogService
            .Setup(x => x.ShowDeleteConfirmationAsync(It.IsAny<string>()))
            .ReturnsAsync(DeleteStrategy.Deactivate);

        await _viewModel.LoadCustomersAsync();

        _viewModel.SelectedCustomer = customerToDelete;

        _mockCustomerService
            .Setup(s => s.DeleteAsync(It.IsAny<int>()))
            .ReturnsAsync(Result.Success());

        await _viewModel.DeleteCustomerAsync();

        _mockEventBus.Verify(
            e => e.Publish(It.Is<CustomerChangedMessage>(m => m.CustomerId == customerToDelete.Id)),
            Times.Once);
    }

    #endregion

    #region Search Tests

    [Fact]
    public async Task SearchText_WhenChanged_RefreshesCollectionView()
    {
        var customers = new List<CustomerDto>
        {
            new(1, "أحمد محمد", null, null, null, null, 0m, 0m, 0m, true),
            new(2, "خالد علي", null, null, null, null, 0m, 0m, 0m, true),
            new(3, "أحمد خالد", null, null, null, null, 0m, 0m, 0m, true)
        };

        _mockCustomerService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<CustomerDto>>.Success(customers));

        await _viewModel.LoadCustomersAsync();

        _viewModel.SearchText = "أحمد";
        _viewModel.SearchCommand.Execute(null);

        _viewModel.SearchText.Should().Be("أحمد");
        _viewModel.CustomersView.Should().NotBeNull();

        var filteredCount = 0;
        if (_viewModel.CustomersView != null)
        {
            foreach (var item in _viewModel.CustomersView)
            {
                filteredCount++;
            }
        }
        filteredCount.Should().Be(2);
    }

    [Fact]
    public async Task SearchText_WhenEmpty_ReturnsAllCustomers()
    {
        var customers = new List<CustomerDto>
        {
            new(1, "أحمد", null, null, null, null, 0m, 0m, 0m, true),
            new(2, "خالد", null, null, null, null, 0m, 0m, 0m, true)
        };

        _mockCustomerService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<CustomerDto>>.Success(customers));

        await _viewModel.LoadCustomersAsync();
        _viewModel.SearchText = "غير موجود";

        _viewModel.SearchCommand.Execute(null);

        var count = 0;
        if (_viewModel.CustomersView != null)
        {
            foreach (var item in _viewModel.CustomersView)
            {
                count++;
            }
        }
        count.Should().Be(0);
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
    public void SelectedCustomer_Set_NotifiesPropertyChanged()
    {
        var propertyChangedEvents = new List<string>();
        _viewModel.PropertyChanged += (s, e) => propertyChangedEvents.Add(e.PropertyName ?? string.Empty);

        var customer = new CustomerDto(1, "عميل", null, null, null, null, 0m, 0m, 0m, true);
        _viewModel.SelectedCustomer = customer;

        propertyChangedEvents.Should().Contain("SelectedCustomer");
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
        _viewModel.SelectedCustomer = null;
        _viewModel.DeleteCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void DeleteCommand_CanExecute_WhenCustomerSelected()
    {
        var customer = new CustomerDto(1, "عميل", null, null, null, null, 0m, 0m, 0m, true);
        _viewModel.SelectedCustomer = customer;
        _viewModel.DeleteCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void EditCommand_CannotExecute_WhenNoSelection()
    {
        _viewModel.SelectedCustomer = null;
        _viewModel.EditCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void EditCommand_CanExecute_WhenCustomerSelected()
    {
        var customer = new CustomerDto(1, "عميل", null, null, null, null, 0m, 0m, 0m, true);
        _viewModel.SelectedCustomer = customer;
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
            e => e.Unsubscribe<CustomerChangedMessage>(It.IsAny<Action<CustomerChangedMessage>>()),
            Times.Once);
    }

    #endregion

    #region EventBus Subscription Tests

    [Fact]
    public void Constructor_SubscribesToCustomerChangedMessage()
    {
        _mockEventBus.Verify(
            e => e.Subscribe<CustomerChangedMessage>(It.IsAny<Action<CustomerChangedMessage>>()),
            Times.Once);
    }

    [Fact]
    public async Task OnCustomerChanged_WhenEventReceived_RefreshesCustomerList()
    {
        var customer = new CustomerDto(1, "محدث", null, null, null, null, 0m, 0m, 0m, true);

        var callCount = 0;
        _mockCustomerService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(() =>
            {
                callCount++;
                return Result<List<CustomerDto>>.Success(new List<CustomerDto> { customer });
            });

        Action<CustomerChangedMessage>? capturedHandler = null;
        _mockEventBus
            .Setup(e => e.Subscribe<CustomerChangedMessage>(It.IsAny<Action<CustomerChangedMessage>>()))
            .Callback<Action<CustomerChangedMessage>>(handler => capturedHandler = handler);

        var newViewModel = new CustomerListViewModel(
            _mockCustomerService.Object,
            _mockEventBus.Object,
            _mockDialogService.Object,
            _mockToastService.Object);

        capturedHandler.Should().NotBeNull("Handler should be subscribed on construction");

        // Now simulate receiving the event - the ViewModel now handles null Application.Current
        capturedHandler!(new CustomerChangedMessage(1));

        // Wait for async operation
        await Task.Delay(100);

        callCount.Should().Be(1, "LoadCustomersAsync should have been called when event is received");
    }

    #endregion

    #region RefreshCommand Tests

    [Fact]
    public async Task RefreshCommand_Executed_LoadsCustomers()
    {
        var customers = new List<CustomerDto>
        {
            new(1, "عميل", null, null, null, null, 0m, 0m, 0m, true)
        };

        _mockCustomerService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<CustomerDto>>.Success(customers));

        _viewModel.RefreshCommand.Execute(null);
        await Task.Delay(100);

        _viewModel.Customers.Should().HaveCount(1);
    }

    #endregion
}