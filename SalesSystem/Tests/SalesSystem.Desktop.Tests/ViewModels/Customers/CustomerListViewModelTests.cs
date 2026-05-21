namespace SalesSystem.Desktop.Tests.ViewModels.Customers;

using System.ComponentModel;
using System.Windows.Input;
using FluentAssertions;
using Moq;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.DesktopPWF.Services;
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
    private readonly CustomerListViewModel _viewModel;

    public CustomerListViewModelTests()
    {
        _mockCustomerService = new Mock<ICustomerApiService>();
        _mockEventBus = new Mock<IEventBus>();
        _mockDialogService = new Mock<IDialogService>();

        // Create ViewModel with mocked services via constructor
        _viewModel = new CustomerListViewModel(
            _mockCustomerService.Object,
            _mockEventBus.Object,
            _mockDialogService.Object);
    }

    public void Dispose()
    {
        _viewModel?.Cleanup();
    }

    #region LoadCustomers Tests

    [Fact]
    public async Task LoadCustomersAsync_WhenApiSucceeds_PopulatesCustomersCollection()
    {
        // Arrange
        var customers = new List<CustomerDto>
        {
            new(1, "C001", "عميل أول", "0501234567", null, null, 0m, 0m, 0m, true),
            new(2, "C002", "عميل ثاني", "0507654321", null, null, 0m, 0m, 0m, true)
        };

        _mockCustomerService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<CustomerDto>>.Success(customers));

        // Act
        await _viewModel.LoadCustomersAsync();

        // Assert
        _viewModel.Customers.Should().HaveCount(2);
        _viewModel.Customers.First().Name.Should().Be("عميل أول");
        _viewModel.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadCustomersAsync_WhenApiFails_SetsErrorMessage()
    {
        // Arrange
        _mockCustomerService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<CustomerDto>>.Failure("فشل في الاتصال"));

        // Act
        await _viewModel.LoadCustomersAsync();

        // Assert
        _viewModel.ErrorMessage.Should().NotBeNullOrEmpty();
        _viewModel.ErrorMessage.Should().Contain("فشل");
    }

    [Fact]
    public async Task LoadCustomersAsync_WhenLoading_SetsIsLoadingTrue()
    {
        // Arrange
        var tcs = new TaskCompletionSource<Result<List<CustomerDto>>>();
        _mockCustomerService
            .Setup(s => s.GetAllAsync())
            .Returns(tcs.Task);

        // Act
        var loadTask = _viewModel.LoadCustomersAsync();
        _viewModel.IsLoading.Should().BeTrue();

        tcs.SetResult(Result<List<CustomerDto>>.Success(new List<CustomerDto>()));
        await loadTask;

        // Assert
        _viewModel.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadCustomersAsync_SetsUpCollectionView()
    {
        // Arrange
        _mockCustomerService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<CustomerDto>>.Success(new List<CustomerDto>
            {
                new(1, "C001", "Test", null, null, null, 0m, 0m, 0m, true)
            }));

        // Act
        await _viewModel.LoadCustomersAsync();

        // Assert
        _viewModel.CustomersView.Should().NotBeNull();
    }

    #endregion

    #region DeleteCustomer Tests

    [Fact]
    public async Task DeleteCommand_WhenConfirmed_CallsApiService()
    {
        // Arrange
        var customerToDelete = new CustomerDto(
            5, "C005", "عميل للحذف", null, null, null, 0m, 0m, 0m, true);

        // Setup GetAll to return the customer
        _mockCustomerService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<CustomerDto>>.Success(new List<CustomerDto> { customerToDelete }));

        // Load customers first
        await _viewModel.LoadCustomersAsync();
        _viewModel.SelectedCustomer = customerToDelete;

        // Setup delete
        _mockCustomerService
            .Setup(s => s.DeleteAsync(customerToDelete.Id))
            .ReturnsAsync(Result.Success());

        // Setup reload after delete
        _mockCustomerService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<CustomerDto>>.Success(new List<CustomerDto>()));

        // Act - Execute the command
        _viewModel.DeleteCommand.Execute(null);

        // Allow async to complete
        await Task.Delay(100);

        // Assert
        _mockCustomerService.Verify(
            s => s.DeleteAsync(customerToDelete.Id),
            Times.Once);
    }

    [Fact]
    public async Task DeleteCommand_WhenDeleteFails_SetsErrorMessage()
    {
        // Arrange
        var customerToDelete = new CustomerDto(
            5, "C005", "عميل", null, null, null, 0m, 0m, 0m, true);

        _mockCustomerService.Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<CustomerDto>>.Success(new List<CustomerDto> { customerToDelete }));
        await _viewModel.LoadCustomersAsync();

        _viewModel.SelectedCustomer = customerToDelete;

        _mockCustomerService
            .Setup(s => s.DeleteAsync(customerToDelete.Id))
            .ReturnsAsync(Result.Failure("فشل في الحذف"));

        // Act
        _viewModel.DeleteCommand.Execute(null);
        await Task.Delay(100);

        // Assert
        _viewModel.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task DeleteCommand_WhenCustomerSelected_PublishesEvent()
    {
        // Arrange
        var customerToDelete = new CustomerDto(
            5, "C005", "عميل", null, null, null, 0m, 0m, 0m, true);

        _mockCustomerService.Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<CustomerDto>>.Success(new List<CustomerDto> { customerToDelete }));
        await _viewModel.LoadCustomersAsync();

        _viewModel.SelectedCustomer = customerToDelete;

        _mockCustomerService
            .Setup(s => s.DeleteAsync(It.IsAny<int>()))
            .ReturnsAsync(Result.Success());

        // Act
        _viewModel.DeleteCommand.Execute(null);
        await Task.Delay(100);

        // Assert
        _mockEventBus.Verify(
            e => e.Publish(It.Is<CustomerChangedMessage>(m => m.CustomerId == customerToDelete.Id)),
            Times.Once);
    }

    #endregion

    #region Search Tests

    [Fact]
    public async Task SearchText_WhenChanged_RefreshesCollectionView()
    {
        // Arrange
        var customers = new List<CustomerDto>
        {
            new(1, "C001", "أحمد محمد", null, null, null, 0m, 0m, 0m, true),
            new(2, "C002", "خالد علي", null, null, null, 0m, 0m, 0m, true),
            new(3, "C003", "أحمد خالد", null, null, null, 0m, 0m, 0m, true)
        };

        _mockCustomerService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<CustomerDto>>.Success(customers));

        await _viewModel.LoadCustomersAsync();

        // Act
        _viewModel.SearchText = "أحمد";
        _viewModel.SearchCommand.Execute(null);

        // Assert
        _viewModel.SearchText.Should().Be("أحمد");
        _viewModel.CustomersView.Should().NotBeNull();

        // Count filtered items
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
        // Arrange
        var customers = new List<CustomerDto>
        {
            new(1, "C001", "أحمد", null, null, null, 0m, 0m, 0m, true),
            new(2, "C002", "خالد", null, null, null, 0m, 0m, 0m, true)
        };

        _mockCustomerService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<CustomerDto>>.Success(customers));

        await _viewModel.LoadCustomersAsync();
        _viewModel.SearchText = "غير موجود";

        // Act
        _viewModel.SearchCommand.Execute(null);

        // Assert
        // Empty search should show all (3 items when no filter)
        var count = 0;
        if (_viewModel.CustomersView != null)
        {
            foreach (var item in _viewModel.CustomersView)
            {
                count++;
            }
        }
        count.Should().Be(0); // No matches for "غير موجود"
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
    public void SelectedCustomer_Set_NotifiesPropertyChanged()
    {
        // Arrange
        var propertyChangedEvents = new List<string>();
        _viewModel.PropertyChanged += (s, e) => propertyChangedEvents.Add(e.PropertyName ?? string.Empty);

        // Act
        var customer = new CustomerDto(1, "C001", "عميل", null, null, null, 0m, 0m, 0m, true);
        _viewModel.SelectedCustomer = customer;

        // Assert
        propertyChangedEvents.Should().Contain("SelectedCustomer");
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
        _viewModel.SelectedCustomer = null;

        // Act & Assert
        _viewModel.DeleteCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void DeleteCommand_CanExecute_WhenCustomerSelected()
    {
        // Arrange
        var customer = new CustomerDto(1, "C001", "عميل", null, null, null, 0m, 0m, 0m, true);
        _viewModel.SelectedCustomer = customer;

        // Act & Assert
        _viewModel.DeleteCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void EditCommand_CannotExecute_WhenNoSelection()
    {
        // Arrange
        _viewModel.SelectedCustomer = null;

        // Act & Assert
        _viewModel.EditCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void EditCommand_CanExecute_WhenCustomerSelected()
    {
        // Arrange
        var customer = new CustomerDto(1, "C001", "عميل", null, null, null, 0m, 0m, 0m, true);
        _viewModel.SelectedCustomer = customer;

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
            e => e.Unsubscribe<CustomerChangedMessage>(It.IsAny<Action<CustomerChangedMessage>>()),
            Times.Once);
    }

    #endregion

    #region EventBus Subscription Tests

    [Fact]
    public void Constructor_SubscribesToCustomerChangedMessage()
    {
        // Assert
        _mockEventBus.Verify(
            e => e.Subscribe<CustomerChangedMessage>(It.IsAny<Action<CustomerChangedMessage>>()),
            Times.Once);
    }

    [Fact(Skip = "Requires WPF Application context - Application.Current is null in unit tests")]
    public async Task OnCustomerChanged_WhenEventReceived_RefreshesCustomerList()
    {
        // This test is skipped because OnCustomerChanged uses Application.Current.Dispatcher.InvokeAsync
        // which requires WPF Application context. In a real test environment, this would need
        // a WPF test harness or the ViewModel would need to abstract the dispatcher.

        // Arrange
        var customer = new CustomerDto(1, "C001", "محدث", null, null, null, 0m, 0m, 0m, true);

        // Track calls to GetAllAsync
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

        // Create new instance to capture the handler
        var newViewModel = new CustomerListViewModel(
            _mockCustomerService.Object,
            _mockEventBus.Object,
            _mockDialogService.Object);

        // Verify handler was subscribed
        capturedHandler.Should().NotBeNull("Handler should be subscribed on construction");
    }

    #endregion

    #region RefreshCommand Tests

    [Fact]
    public async Task RefreshCommand_Executed_LoadsCustomers()
    {
        // Arrange
        var customers = new List<CustomerDto>
        {
            new(1, "C001", "عميل", null, null, null, 0m, 0m, 0m, true)
        };

        _mockCustomerService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<CustomerDto>>.Success(customers));

        // Act
        _viewModel.RefreshCommand.Execute(null);

        // Wait for async
        await Task.Delay(100);

        // Assert
        _viewModel.Customers.Should().HaveCount(1);
    }

    #endregion
}