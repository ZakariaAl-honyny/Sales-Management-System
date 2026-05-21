namespace SalesSystem.DesktopPWF.Tests.ViewModels.Payments;

using System.Collections.ObjectModel;
using System.ComponentModel;
using FluentAssertions;
using Moq;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.DesktopPWF.Services;

/// <summary>
/// Tests for CustomerPaymentsListViewModel
/// </summary>
public class CustomerPaymentsListViewModelTests
{
    private readonly Mock<ICustomerPaymentApiService> _mockPaymentService;
    private readonly Mock<ICustomerApiService> _mockCustomerService;
    private readonly Mock<IDialogService> _mockDialogService;
    private readonly Mock<INavigationService> _mockNavigationService;
    private readonly CustomerPaymentsListViewModel _viewModel;

    public CustomerPaymentsListViewModelTests()
    {
        _mockPaymentService = new Mock<ICustomerPaymentApiService>();
        _mockCustomerService = new Mock<ICustomerApiService>();
        _mockDialogService = new Mock<IDialogService>();
        _mockNavigationService = new Mock<INavigationService>();

        _viewModel = new CustomerPaymentsListViewModel();
        
        // Inject mocks via reflection
        var paymentServiceField = typeof(CustomerPaymentsListViewModel).GetField("_paymentService",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        paymentServiceField?.SetValue(_viewModel, _mockPaymentService.Object);

        var customerServiceField = typeof(CustomerPaymentsListViewModel).GetField("_customerService",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        customerServiceField?.SetValue(_viewModel, _mockCustomerService.Object);

        var dialogServiceField = typeof(CustomerPaymentsListViewModel).GetField("_dialogService",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        dialogServiceField?.SetValue(_viewModel, _mockDialogService.Object);

        var navigationServiceField = typeof(CustomerPaymentsListViewModel).GetField("_navigationService",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        navigationServiceField?.SetValue(_viewModel, _mockNavigationService.Object);
    }

    #region Property Tests

    [Fact]
    public void SearchText_DefaultValue_IsEmpty()
    {
        _viewModel.SearchText.Should().BeEmpty();
    }

    [Fact]
    public void DateFrom_DefaultValue_IsNull()
    {
        _viewModel.DateFrom.Should().BeNull();
    }

    [Fact]
    public void DateTo_DefaultValue_IsNull()
    {
        _viewModel.DateTo.Should().BeNull();
    }

    [Fact]
    public void IsLoading_DefaultValue_IsFalse()
    {
        _viewModel.IsLoading.Should().BeFalse();
    }

    [Fact]
    public void ErrorMessage_DefaultValue_IsEmpty()
    {
        _viewModel.ErrorMessage.Should().BeEmpty();
    }

    [Fact]
    public void SelectedPayment_DefaultValue_IsNull()
    {
        _viewModel.SelectedPayment.Should().BeNull();
    }

    [Fact]
    public void Payments_InitializesWithEmptyCollection()
    {
        _viewModel.Payments.Should().NotBeNull();
        _viewModel.Payments.Should().BeEmpty();
    }

    [Fact]
    public void TotalCount_DefaultValue_IsZero()
    {
        _viewModel.TotalCount.Should().Be(0);
    }

    #endregion

    #region PropertyChangeNotification Tests

    [Fact]
    public void SearchText_Set_NotifiesPropertyChanged()
    {
        var propertyChangedEvents = new List<string>();
        _viewModel.PropertyChanged += (s, e) => propertyChangedEvents.Add(e.PropertyName ?? string.Empty);

        _viewModel.SearchText = "بحث";

        propertyChangedEvents.Should().Contain("SearchText");
    }

    [Fact]
    public void DateFrom_Set_NotifiesPropertyChanged()
    {
        var propertyChangedEvents = new List<string>();
        _viewModel.PropertyChanged += (s, e) => propertyChangedEvents.Add(e.PropertyName ?? string.Empty);

        _viewModel.DateFrom = DateTime.Today;

        propertyChangedEvents.Should().Contain("DateFrom");
    }

    [Fact]
    public void DateTo_Set_NotifiesPropertyChanged()
    {
        var propertyChangedEvents = new List<string>();
        _viewModel.PropertyChanged += (s, e) => propertyChangedEvents.Add(e.PropertyName ?? string.Empty);

        _viewModel.DateTo = DateTime.Today;

        propertyChangedEvents.Should().Contain("DateTo");
    }

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

        _viewModel.ErrorMessage = "خطأ";

        propertyChangedEvents.Should().Contain("ErrorMessage");
    }

    [Fact]
    public void SelectedPayment_Set_NotifiesPropertyChanged()
    {
        var propertyChangedEvents = new List<string>();
        _viewModel.PropertyChanged += (s, e) => propertyChangedEvents.Add(e.PropertyName ?? string.Empty);

        var payment = new CustomerPaymentDto(1, 1, "عميل", 100m, DateTime.Today, 1, "نقدي", "ملاحظات", DateTime.Today);
        _viewModel.SelectedPayment = payment;

        propertyChangedEvents.Should().Contain("SelectedPayment");
    }

    #endregion

    #region Commands Tests

    [Fact]
    public void NewCommand_IsInitialized()
    {
        _viewModel.NewCommand.Should().NotBeNull();
    }

    [Fact]
    public void ViewCommand_CannotExecute_WhenNoSelection()
    {
        _viewModel.SelectedPayment = null;
        _viewModel.ViewCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ViewCommand_CanExecute_WhenPaymentSelected()
    {
        var payment = new CustomerPaymentDto(1, 1, "عميل", 100m, DateTime.Today, 1, "نقدي", "ملاحظات", DateTime.Today);
        _viewModel.SelectedPayment = payment;
        
        _viewModel.ViewCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void EditCommand_CannotExecute_WhenNoSelection()
    {
        _viewModel.SelectedPayment = null;
        _viewModel.EditCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void EditCommand_CanExecute_WhenPaymentSelected()
    {
        var payment = new CustomerPaymentDto(1, 1, "عميل", 100m, DateTime.Today, 1, "نقدي", "ملاحظات", DateTime.Today);
        _viewModel.SelectedPayment = payment;
        
        _viewModel.EditCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void DeleteCommand_CannotExecute_WhenNoSelection()
    {
        _viewModel.SelectedPayment = null;
        _viewModel.DeleteCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void DeleteCommand_CanExecute_WhenPaymentSelected()
    {
        var payment = new CustomerPaymentDto(1, 1, "عميل", 100m, DateTime.Today, 1, "نقدي", "ملاحظات", DateTime.Today);
        _viewModel.SelectedPayment = payment;
        
        _viewModel.DeleteCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void RefreshCommand_IsInitialized()
    {
        _viewModel.RefreshCommand.Should().NotBeNull();
    }

    [Fact]
    public void SearchCommand_IsInitialized()
    {
        _viewModel.SearchCommand.Should().NotBeNull();
    }

    #endregion

    #region LoadPaymentsAsync Tests

    [Fact]
    public async Task LoadPaymentsAsync_WhenApiSucceeds_PopulatesPayments()
    {
        var payments = new List<CustomerPaymentDto>
        {
            new(1, 1, "عميل 1", 100m, DateTime.Today, 1, "نقدي", null, DateTime.Today),
            new(2, 2, "عميل 2", 200m, DateTime.Today, 1, "نقدي", null, DateTime.Today)
        };

        _mockPaymentService
            .Setup(s => s.GetAllAsync(
                It.IsAny<string?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>()))
            .ReturnsAsync(Result<List<CustomerPaymentDto>>.Success(payments));

        await _viewModel.LoadPaymentsAsync();

        _viewModel.Payments.Should().HaveCount(2);
        _viewModel.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadPaymentsAsync_WhenApiFails_SetsErrorMessage()
    {
        _mockPaymentService
            .Setup(s => s.GetAllAsync(
                It.IsAny<string?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>()))
            .ReturnsAsync(Result<List<CustomerPaymentDto>>.Failure("فشل في الاتصال"));

        await _viewModel.LoadPaymentsAsync();

        _viewModel.ErrorMessage.Should().NotBeEmpty();
        _viewModel.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadPaymentsAsync_WhenLoading_SetsIsLoadingTrue()
    {
        var tcs = new TaskCompletionSource<Result<List<CustomerPaymentDto>>>();
        _mockPaymentService
            .Setup(s => s.GetAllAsync(
                It.IsAny<string?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>()))
            .Returns(tcs.Task);

        var loadTask = _viewModel.LoadPaymentsAsync();
        _viewModel.IsLoading.Should().BeTrue();

        tcs.SetResult(Result<List<CustomerPaymentDto>>.Success(new List<CustomerPaymentDto>()));
        await loadTask;

        _viewModel.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadPaymentsAsync_UpdatesTotalCount()
    {
        var payments = new List<CustomerPaymentDto>
        {
            new(1, 1, "عميل", 100m, DateTime.Today, 1, "نقدي", null, DateTime.Today)
        };

        _mockPaymentService
            .Setup(s => s.GetAllAsync(
                It.IsAny<string?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>()))
            .ReturnsAsync(Result<List<CustomerPaymentDto>>.Success(payments));

        await _viewModel.LoadPaymentsAsync();

        _viewModel.TotalCount.Should().Be(1);
    }

    #endregion

    #region OnDelete Tests

    [Fact]
    public async Task OnDelete_WhenConfirmed_CallsDeleteApi()
    {
        var paymentToDelete = new CustomerPaymentDto(1, 1, "عميل", 100m, DateTime.Today, 1, "نقدي", null, DateTime.Today);
        
        _mockPaymentService
            .Setup(s => s.DeleteAsync(It.IsAny<int>()))
            .ReturnsAsync(Result.Success());

        _mockPaymentService
            .Setup(s => s.GetAllAsync(
                It.IsAny<string?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>()))
            .ReturnsAsync(Result<List<CustomerPaymentDto>>.Success(new List<CustomerPaymentDto>()));

        // Execute via command
        _viewModel.SelectedPayment = paymentToDelete;
        await _viewModel.DeleteCommand.ExecuteAsync(null);

        _mockPaymentService.Verify(s => s.DeleteAsync(paymentToDelete.Id), Times.Once);
    }

    #endregion
}