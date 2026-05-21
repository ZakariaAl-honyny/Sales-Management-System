namespace SalesSystem.DesktopPWF.Tests.ViewModels.Payments;

using System.ComponentModel;
using FluentAssertions;
using Moq;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.DesktopPWF.Helpers;
using SalesSystem.DesktopPWF.Services;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.ViewModels.Payments;

/// <summary>
/// Tests for CustomerPaymentsListViewModel
/// </summary>
public class CustomerPaymentsListViewModelTests
{
    private readonly Mock<ICustomerPaymentApiService> _mockPaymentService;
    private readonly Mock<ICustomerApiService> _mockCustomerService;
    private readonly Mock<IDialogService> _mockDialogService;
    private readonly Mock<INavigationService> _mockNavigationService;
    private readonly Mock<IPaymentPrinter> _mockPaymentPrinter;
    private readonly Mock<ISettingsApiService> _mockSettingsService;
    private readonly CustomerPaymentsListViewModel _viewModel;

    public CustomerPaymentsListViewModelTests()
    {
        _mockPaymentService = new Mock<ICustomerPaymentApiService>();
        _mockCustomerService = new Mock<ICustomerApiService>();
        _mockDialogService = new Mock<IDialogService>();
        _mockNavigationService = new Mock<INavigationService>();
        _mockPaymentPrinter = new Mock<IPaymentPrinter>();
        _mockSettingsService = new Mock<ISettingsApiService>();

        _viewModel = new CustomerPaymentsListViewModel();

        SetField("_paymentService", _mockPaymentService.Object);
        SetField("_customerService", _mockCustomerService.Object);
        SetField("_dialogService", _mockDialogService.Object);
        SetField("_navigationService", _mockNavigationService.Object);
        SetField("_paymentPrinter", _mockPaymentPrinter.Object);
        SetField("_settingsService", _mockSettingsService.Object);
    }

    private void SetField(string fieldName, object value)
    {
        var field = typeof(CustomerPaymentsListViewModel).GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(_viewModel, value);
    }

    private static CustomerPaymentDto CreatePayment(int id, string customerName, decimal amount)
    {
        return new CustomerPaymentDto(id, $"CP-{id:000}", 1, customerName, amount, 1, DateTime.Today, null, null);
    }

    #region Property Tests

    [Fact]
    public void SearchText_DefaultValue_IsEmpty() => _viewModel.SearchText.Should().BeEmpty();

    [Fact]
    public void DateFrom_DefaultValue_IsNull() => _viewModel.DateFrom.Should().BeNull();

    [Fact]
    public void DateTo_DefaultValue_IsNull() => _viewModel.DateTo.Should().BeNull();

    [Fact]
    public void IsLoading_DefaultValue_IsFalse() => _viewModel.IsLoading.Should().BeFalse();

    [Fact]
    public void ErrorMessage_DefaultValue_IsEmpty() => _viewModel.ErrorMessage.Should().BeEmpty();

    [Fact]
    public void SelectedPayment_DefaultValue_IsNull() => _viewModel.SelectedPayment.Should().BeNull();

    [Fact]
    public void Payments_InitializesWithEmptyCollection()
    {
        _viewModel.Payments.Should().NotBeNull();
        _viewModel.Payments.Should().BeEmpty();
    }

    [Fact]
    public void PaymentsCount_DefaultValue_IsZero() => _viewModel.PaymentsCount.Should().Be(0);

    #endregion

    #region PropertyChangeNotification Tests

    [Fact]
    public void SearchText_Set_NotifiesPropertyChanged()
    {
        var events = new List<string>();
        _viewModel.PropertyChanged += (s, e) => events.Add(e.PropertyName ?? string.Empty);
        _viewModel.SearchText = "بحث";
        events.Should().Contain("SearchText");
    }

    [Fact]
    public void DateFrom_Set_NotifiesPropertyChanged()
    {
        var events = new List<string>();
        _viewModel.PropertyChanged += (s, e) => events.Add(e.PropertyName ?? string.Empty);
        _viewModel.DateFrom = DateTime.Today;
        events.Should().Contain("DateFrom");
    }

    [Fact]
    public void DateTo_Set_NotifiesPropertyChanged()
    {
        var events = new List<string>();
        _viewModel.PropertyChanged += (s, e) => events.Add(e.PropertyName ?? string.Empty);
        _viewModel.DateTo = DateTime.Today;
        events.Should().Contain("DateTo");
    }

    [Fact]
    public void IsLoading_Set_NotifiesPropertyChanged()
    {
        var events = new List<string>();
        _viewModel.PropertyChanged += (s, e) => events.Add(e.PropertyName ?? string.Empty);
        _viewModel.IsLoading = true;
        events.Should().Contain("IsLoading");
    }

    [Fact]
    public void ErrorMessage_Set_NotifiesPropertyChanged()
    {
        var events = new List<string>();
        _viewModel.PropertyChanged += (s, e) => events.Add(e.PropertyName ?? string.Empty);
        _viewModel.ErrorMessage = "خطأ";
        events.Should().Contain("ErrorMessage");
    }

    [Fact]
    public void SelectedPayment_Set_NotifiesPropertyChanged()
    {
        var events = new List<string>();
        _viewModel.PropertyChanged += (s, e) => events.Add(e.PropertyName ?? string.Empty);
        _viewModel.SelectedPayment = CreatePayment(1, "عميل", 100m);
        events.Should().Contain("SelectedPayment");
    }

    #endregion

    #region Commands Tests

    [Fact] public void NewCommand_IsInitialized() => _viewModel.NewCommand.Should().NotBeNull();
    [Fact] public void RefreshCommand_IsInitialized() => _viewModel.RefreshCommand.Should().NotBeNull();
    [Fact] public void SearchCommand_IsInitialized() => _viewModel.SearchCommand.Should().NotBeNull();

    [Fact]
    public void ViewCommand_CannotExecute_WhenNoSelection()
    {
        _viewModel.SelectedPayment = null;
        _viewModel.ViewCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ViewCommand_CanExecute_WhenPaymentSelected()
    {
        _viewModel.SelectedPayment = CreatePayment(1, "عميل", 100m);
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
        _viewModel.SelectedPayment = CreatePayment(1, "عميل", 100m);
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
        _viewModel.SelectedPayment = CreatePayment(1, "عميل", 100m);
        _viewModel.DeleteCommand.CanExecute(null).Should().BeTrue();
    }

    #endregion

    #region LoadPaymentsAsync Tests

    [Fact]
    public async Task LoadPaymentsAsync_WhenApiSucceeds_PopulatesPayments()
    {
        var payments = new List<CustomerPaymentDto>
        {
            CreatePayment(1, "عميل 1", 100m),
            CreatePayment(2, "عميل 2", 200m)
        };

        _mockPaymentService
            .Setup(s => s.GetAllAsync(It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(Result<List<CustomerPaymentDto>>.Success(payments));

        await _viewModel.LoadPaymentsAsync();

        _viewModel.Payments.Should().HaveCount(2);
        _viewModel.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadPaymentsAsync_WhenApiFails_SetsErrorMessage()
    {
        _mockPaymentService
            .Setup(s => s.GetAllAsync(It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
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
            .Setup(s => s.GetAllAsync(It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .Returns(tcs.Task);

        var loadTask = _viewModel.LoadPaymentsAsync();
        _viewModel.IsLoading.Should().BeTrue();

        tcs.SetResult(Result<List<CustomerPaymentDto>>.Success(new List<CustomerPaymentDto>()));
        await loadTask;

        _viewModel.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadPaymentsAsync_UpdatesPaymentsCount()
    {
        var payments = new List<CustomerPaymentDto> { CreatePayment(1, "عميل", 100m) };

        _mockPaymentService
            .Setup(s => s.GetAllAsync(It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(Result<List<CustomerPaymentDto>>.Success(payments));

        await _viewModel.LoadPaymentsAsync();

        _viewModel.PaymentsCount.Should().Be(1);
    }

    #endregion

    #region OnDelete Tests

    [Fact]
    public async Task OnDelete_WhenConfirmed_CallsDeleteApi()
    {
        var payment = CreatePayment(1, "عميل", 100m);

        _mockDialogService
            .Setup(d => d.ShowConfirmationAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        _mockPaymentService
            .Setup(s => s.DeleteAsync(It.IsAny<int>()))
            .ReturnsAsync(Result.Success());

        _mockPaymentService
            .Setup(s => s.GetAllAsync(It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(Result<List<CustomerPaymentDto>>.Success(new List<CustomerPaymentDto>()));

        _viewModel.SelectedPayment = payment;
        await ((dynamic)_viewModel.DeleteCommand).ExecuteAsync(null);

        _mockPaymentService.Verify(s => s.DeleteAsync(payment.Id), Times.Once);
    }

    #endregion
}
