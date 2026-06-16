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
/// Tests for SupplierPaymentsListViewModel
/// </summary>
public class SupplierPaymentsListViewModelTests
{
    private readonly Mock<ISupplierPaymentApiService> _mockPaymentService;
    private readonly Mock<ISupplierApiService> _mockSupplierService;
    private readonly Mock<IDialogService> _mockDialogService;
    private readonly Mock<IPaymentPrinter> _mockPaymentPrinter;
    private readonly Mock<ISettingsApiService> _mockSettingsService;
    private readonly SupplierPaymentsListViewModel _viewModel;

    public SupplierPaymentsListViewModelTests()
    {
        _mockPaymentService = new Mock<ISupplierPaymentApiService>();
        _mockSupplierService = new Mock<ISupplierApiService>();
        _mockDialogService = new Mock<IDialogService>();
        _mockPaymentPrinter = new Mock<IPaymentPrinter>();
        _mockSettingsService = new Mock<ISettingsApiService>();

        _viewModel = new SupplierPaymentsListViewModel();

        SetField("_paymentService", _mockPaymentService.Object);
        SetField("_supplierService", _mockSupplierService.Object);
        SetField("_dialogService", _mockDialogService.Object);
        SetField("_paymentPrinter", _mockPaymentPrinter.Object);
        SetField("_settingsService", _mockSettingsService.Object);
    }

    private void SetField(string fieldName, object value)
    {
        var field = typeof(SupplierPaymentsListViewModel).GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(_viewModel, value);
    }

    private static SupplierPaymentDto CreatePayment(int id, string supplierName, decimal amount)
    {
        return new SupplierPaymentDto(id, $"SP-{id:000}", 1, supplierName, amount, 1, null, null, DateOnly.FromDateTime(DateTime.Today), null, null);
    }

    #region Property Tests

    [Fact] public void SearchText_DefaultValue_IsEmpty() => _viewModel.SearchText.Should().BeEmpty();
    [Fact] public void DateFrom_DefaultValue_IsNull() => _viewModel.DateFrom.Should().BeNull();
    [Fact] public void DateTo_DefaultValue_IsNull() => _viewModel.DateTo.Should().BeNull();
    [Fact] public void IsBusy_DefaultValue_IsFalse() => _viewModel.IsBusy.Should().BeFalse();
    [Fact] public void ErrorMessage_DefaultValue_IsEmpty() => _viewModel.ErrorMessage.Should().BeEmpty();
    [Fact] public void SelectedPayment_DefaultValue_IsNull() => _viewModel.SelectedPayment.Should().BeNull();

    [Fact]
    public void Payments_InitializesWithEmptyCollection()
    {
        _viewModel.Payments.Should().NotBeNull();
        _viewModel.Payments.Should().BeEmpty();
    }

    [Fact] public void PaymentsCount_DefaultValue_IsZero() => _viewModel.PaymentsCount.Should().Be(0);

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
        _viewModel.ErrorMessage = "خطأ";
        events.Should().Contain("ErrorMessage");
    }

    [Fact]
    public void SelectedPayment_Set_NotifiesPropertyChanged()
    {
        var events = new List<string>();
        _viewModel.PropertyChanged += (s, e) => events.Add(e.PropertyName ?? string.Empty);
        _viewModel.SelectedPayment = CreatePayment(1, "مورد", 100m);
        events.Should().Contain("SelectedPayment");
    }

    #endregion

    #region Commands Tests

    [Fact] public void NewCommand_IsInitialized() => _viewModel.NewCommand.Should().NotBeNull();
    [Fact] public void RefreshCommand_IsInitialized() => _viewModel.RefreshCommand.Should().NotBeNull();

    [Fact]
    public void ViewCommand_CannotExecute_WhenNoSelection()
    {
        _viewModel.SelectedPayment = null;
        _viewModel.ViewCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ViewCommand_CanExecute_WhenPaymentSelected()
    {
        _viewModel.SelectedPayment = CreatePayment(1, "مورد", 100m);
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
        _viewModel.SelectedPayment = CreatePayment(1, "مورد", 100m);
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
        _viewModel.SelectedPayment = CreatePayment(1, "مورد", 100m);
        _viewModel.DeleteCommand.CanExecute(null).Should().BeTrue();
    }

    #endregion

    #region LoadPaymentsAsync Tests

    [Fact]
    public async Task LoadPaymentsAsync_WhenApiSucceeds_PopulatesPayments()
    {
        var payments = new List<SupplierPaymentDto>
        {
            CreatePayment(1, "مورد 1", 100m),
            CreatePayment(2, "مورد 2", 200m)
        };

        _mockPaymentService
            .Setup(s => s.GetAllAsync(It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(Result<List<SupplierPaymentDto>>.Success(payments));

        await _viewModel.LoadPaymentsAsync();

        _viewModel.Payments.Should().HaveCount(2);
        _viewModel.IsBusy.Should().BeFalse();
    }

    [Fact]
    public async Task LoadPaymentsAsync_WhenApiFails_SetsErrorMessage()
    {
        _mockPaymentService
            .Setup(s => s.GetAllAsync(It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(Result<List<SupplierPaymentDto>>.Failure("فشل في الاتصال"));

        await _viewModel.LoadPaymentsAsync();

        _viewModel.ErrorMessage.Should().NotBeEmpty();
        _viewModel.IsBusy.Should().BeFalse();
    }

    [Fact]
    public async Task LoadPaymentsAsync_WhenLoading_SetsIsBusyTrue()
    {
        var tcs = new TaskCompletionSource<Result<List<SupplierPaymentDto>>>();
        _mockPaymentService
            .Setup(s => s.GetAllAsync(It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .Returns(tcs.Task);

        var loadTask = _viewModel.LoadPaymentsAsync();
        _viewModel.IsBusy.Should().BeTrue();

        tcs.SetResult(Result<List<SupplierPaymentDto>>.Success(new List<SupplierPaymentDto>()));
        await loadTask;

        _viewModel.IsBusy.Should().BeFalse();
    }

    [Fact]
    public async Task LoadPaymentsAsync_UpdatesPaymentsCount()
    {
        var payments = new List<SupplierPaymentDto> { CreatePayment(1, "مورد", 100m) };

        _mockPaymentService
            .Setup(s => s.GetAllAsync(It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(Result<List<SupplierPaymentDto>>.Success(payments));

        await _viewModel.LoadPaymentsAsync();

        _viewModel.PaymentsCount.Should().Be(1);
    }

    #endregion
}
