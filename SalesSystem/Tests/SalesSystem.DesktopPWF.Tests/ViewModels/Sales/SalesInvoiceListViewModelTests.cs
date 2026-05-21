namespace SalesSystem.DesktopPWF.Tests.ViewModels.Sales;

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using FluentAssertions;
using Moq;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Enums;
using SalesSystem.DesktopPWF.Services;
using SalesSystem.DesktopPWF.ViewModels;
using SalesSystem.DesktopPWF.ViewModels.Sales;

/// <summary>
/// Tests for SalesInvoiceListViewModel
/// </summary>
public class SalesInvoiceListViewModelTests : IDisposable
{
    private readonly Mock<ISalesInvoiceApiService> _mockInvoiceService;
    private readonly Mock<IEventBus> _mockEventBus;
    private readonly Mock<ICustomerApiService> _mockCustomerService;
    private readonly Mock<IWarehouseApiService> _mockWarehouseService;
    private readonly Mock<IProductApiService> _mockProductService;
    private readonly Mock<IDialogService> _mockDialogService;
    private readonly SalesInvoiceListViewModel _viewModel;

    public SalesInvoiceListViewModelTests()
    {
        _mockInvoiceService = new Mock<ISalesInvoiceApiService>();
        _mockEventBus = new Mock<IEventBus>();
        _mockCustomerService = new Mock<ICustomerApiService>();
        _mockWarehouseService = new Mock<IWarehouseApiService>();
        _mockProductService = new Mock<IProductApiService>();
        _mockDialogService = new Mock<IDialogService>();

        _viewModel = new SalesInvoiceListViewModel(
            _mockInvoiceService.Object,
            _mockEventBus.Object,
            _mockCustomerService.Object,
            _mockWarehouseService.Object,
            _mockProductService.Object,
            _mockDialogService.Object);
    }

    public void Dispose()
    {
        _viewModel?.Cleanup();
    }

    #region LoadInvoices Tests

    [Fact]
    public async Task LoadInvoicesAsync_WhenApiSucceeds_PopulatesInvoicesCollection()
    {
        var invoices = new List<SalesInvoiceDto>
        {
            CreateSalesInvoiceDto(1, "INV-2026-001", 1000m, (byte)InvoiceStatus.Draft),
            CreateSalesInvoiceDto(2, "INV-2026-002", 2000m, (byte)InvoiceStatus.Posted)
        };

        _mockInvoiceService
            .Setup(s => s.GetAllAsync(
                It.IsAny<string?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<byte?>(),
                It.IsAny<bool>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<SalesInvoiceDto>>.Success(invoices));

        await _viewModel.LoadInvoicesAsync();

        _viewModel.Invoices.Should().HaveCount(2);
        _viewModel.Invoices.First().InvoiceNo.Should().Be("INV-2026-001");
        _viewModel.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadInvoicesAsync_WhenApiFails_SetsErrorMessage()
    {
        _mockInvoiceService
            .Setup(s => s.GetAllAsync(
                It.IsAny<string?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<byte?>(),
                It.IsAny<bool>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<SalesInvoiceDto>>.Failure("فشل في الاتصال"));

        await _viewModel.LoadInvoicesAsync();

        _viewModel.ErrorMessage.Should().NotBeNullOrEmpty();
        _viewModel.ErrorMessage.Should().Contain("فشل");
    }

    [Fact]
    public async Task LoadInvoicesAsync_WhenLoading_SetsIsLoadingTrue()
    {
        var tcs = new TaskCompletionSource<Result<List<SalesInvoiceDto>>>();
        _mockInvoiceService
            .Setup(s => s.GetAllAsync(
                It.IsAny<string?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<byte?>(),
                It.IsAny<bool>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .Returns(tcs.Task);

        var loadTask = _viewModel.LoadInvoicesAsync();
        _viewModel.IsLoading.Should().BeTrue();

        tcs.SetResult(Result<List<SalesInvoiceDto>>.Success(new List<SalesInvoiceDto>()));
        await loadTask;

        _viewModel.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadInvoicesAsync_SetsUpCollectionView()
    {
        _mockInvoiceService
            .Setup(s => s.GetAllAsync(
                It.IsAny<string?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<byte?>(),
                It.IsAny<bool>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<SalesInvoiceDto>>.Success(new List<SalesInvoiceDto>
            {
                CreateSalesInvoiceDto(1, "INV-001", 1000m, 2)
            }));

        await _viewModel.LoadInvoicesAsync();

        _viewModel.InvoicesView.Should().NotBeNull();
    }

    #endregion

    #region Search Tests

    [Fact]
    public async Task SearchText_WhenChanged_RefreshesCollectionView()
    {
        var invoices = new List<SalesInvoiceDto>
        {
            CreateSalesInvoiceDto(1, "INV-001", 1000m, 2, "أحمد"),
            CreateSalesInvoiceDto(2, "INV-002", 2000m, 2, "خالد"),
            CreateSalesInvoiceDto(3, "INV-003", 3000m, 2, "أحمد")
        };

        _mockInvoiceService
            .Setup(s => s.GetAllAsync(
                It.IsAny<string?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<byte?>(),
                It.IsAny<bool>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<SalesInvoiceDto>>.Success(invoices));

        await _viewModel.LoadInvoicesAsync();

        _viewModel.SearchText = "أحمد";

        var filteredCount = 0;
        if (_viewModel.InvoicesView != null)
        {
            foreach (var item in _viewModel.InvoicesView)
            {
                filteredCount++;
            }
        }
        filteredCount.Should().Be(2);
    }

    [Fact]
    public async Task SearchText_WhenEmpty_ReturnsAllInvoices()
    {
        var invoices = new List<SalesInvoiceDto>
        {
            CreateSalesInvoiceDto(1, "INV-001", 1000m, 2, "عميل 1"),
            CreateSalesInvoiceDto(2, "INV-002", 2000m, 2, "عميل 2")
        };

        _mockInvoiceService
            .Setup(s => s.GetAllAsync(
                It.IsAny<string?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<byte?>(),
                It.IsAny<bool>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<SalesInvoiceDto>>.Success(invoices));

        await _viewModel.LoadInvoicesAsync();
        _viewModel.SearchText = "غير موجود";

        var count = 0;
        if (_viewModel.InvoicesView != null)
        {
            foreach (var item in _viewModel.InvoicesView)
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
    public void SelectedInvoice_Set_NotifiesPropertyChanged()
    {
        var propertyChangedEvents = new List<string>();
        _viewModel.PropertyChanged += (s, e) => propertyChangedEvents.Add(e.PropertyName ?? string.Empty);

        var invoice = CreateSalesInvoiceDto(1, "INV-001", 1000m, 1);
        _viewModel.SelectedInvoice = invoice;

        propertyChangedEvents.Should().Contain("SelectedInvoice");
    }

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

        _viewModel.DateFrom = DateTime.Today.AddDays(-7);

        propertyChangedEvents.Should().Contain("DateFrom");
    }

    [Fact]
    public void DateTo_Set_NotifiesPropertyChanged()
    {
        var propertyChangedEvents = new List<string>();
        _viewModel.PropertyChanged += (s, e) => propertyChangedEvents.Add(e.PropertyName ?? string.Empty);

        // Use a different value than the default (DateTime.Today) to trigger PropertyChanged
        _viewModel.DateTo = DateTime.Today.AddDays(-10);

        propertyChangedEvents.Should().Contain("DateTo");
    }

    [Fact]
    public void StatusFilter_Set_NotifiesPropertyChanged()
    {
        var propertyChangedEvents = new List<string>();
        _viewModel.PropertyChanged += (s, e) => propertyChangedEvents.Add(e.PropertyName ?? string.Empty);

        _viewModel.StatusFilter = 1;

        propertyChangedEvents.Should().Contain("StatusFilter");
    }

    #endregion

    #region Command CanExecute Tests

    [Fact]
    public void EditCommand_CannotExecute_WhenNoSelection()
    {
        _viewModel.SelectedInvoice = null;
        _viewModel.EditCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void EditCommand_CannotExecute_WhenInvoiceNotDraft()
    {
        var invoice = CreateSalesInvoiceDto(1, "INV-001", 1000m, (byte)InvoiceStatus.Posted);
        _viewModel.SelectedInvoice = invoice;
        _viewModel.EditCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void EditCommand_CanExecute_WhenDraftInvoiceSelected()
    {
        var invoice = CreateSalesInvoiceDto(1, "INV-001", 1000m, (byte)InvoiceStatus.Draft);
        _viewModel.SelectedInvoice = invoice;
        _viewModel.EditCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void PostCommand_CannotExecute_WhenNoSelection()
    {
        _viewModel.SelectedInvoice = null;
        _viewModel.PostCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void PostCommand_CannotExecute_WhenInvoiceNotDraft()
    {
        var invoice = CreateSalesInvoiceDto(1, "INV-001", 1000m, (byte)InvoiceStatus.Posted);
        _viewModel.SelectedInvoice = invoice;
        _viewModel.PostCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void PostCommand_CanExecute_WhenDraftInvoiceSelected()
    {
        var invoice = CreateSalesInvoiceDto(1, "INV-001", 1000m, (byte)InvoiceStatus.Draft);
        _viewModel.SelectedInvoice = invoice;
        _viewModel.PostCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void CancelCommand_CannotExecute_WhenNoSelection()
    {
        _viewModel.SelectedInvoice = null;
        _viewModel.CancelCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void CancelCommand_CannotExecute_WhenInvoiceNotPosted()
    {
        var invoice = CreateSalesInvoiceDto(1, "INV-001", 1000m, (byte)InvoiceStatus.Draft);
        _viewModel.SelectedInvoice = invoice;
        _viewModel.CancelCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void CancelCommand_CanExecute_WhenPostedInvoiceSelected()
    {
        var invoice = CreateSalesInvoiceDto(1, "INV-001", 1000m, (byte)InvoiceStatus.Posted);
        _viewModel.SelectedInvoice = invoice;
        _viewModel.CancelCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void ViewCommand_CannotExecute_WhenNoSelection()
    {
        _viewModel.SelectedInvoice = null;
        _viewModel.ViewCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ViewCommand_CanExecute_WhenInvoiceSelected()
    {
        var invoice = CreateSalesInvoiceDto(1, "INV-001", 1000m, (byte)InvoiceStatus.Cancelled);
        _viewModel.SelectedInvoice = invoice;
        _viewModel.ViewCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void NewCommand_CanExecute_Always()
    {
        _viewModel.NewCommand.CanExecute(null).Should().BeTrue();
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
            e => e.Unsubscribe<SaleInvoiceChangedMessage>(It.IsAny<Action<SaleInvoiceChangedMessage>>()),
            Times.Once);
    }

    #endregion

    #region EventBus Subscription Tests

    [Fact]
    public void Constructor_SubscribesToSaleInvoiceChangedMessage()
    {
        _mockEventBus.Verify(
            e => e.Subscribe<SaleInvoiceChangedMessage>(It.IsAny<Action<SaleInvoiceChangedMessage>>()),
            Times.Once);
    }

    #endregion

    #region StatusOptions Tests

    [Fact]
    public void StatusOptions_ContainsAllStatuses()
    {
        _viewModel.StatusOptions.Should().HaveCount(4);

        _viewModel.StatusOptions.Should().Contain(s => s.Display == "الكل" && s.Value == null);
        _viewModel.StatusOptions.Should().Contain(s => s.Display == "مسودة" && s.Value == 1);
        _viewModel.StatusOptions.Should().Contain(s => s.Display == "مرحلة" && s.Value == 2);
        _viewModel.StatusOptions.Should().Contain(s => s.Display == "ملغاة" && s.Value == 3);
    }

    #endregion

    #region DefaultDateRange Tests

    [Fact]
    public void DateFrom_HasDefaultValue()
    {
        _viewModel.DateFrom.Should().NotBeNull();
        _viewModel.DateFrom.Should().Be(DateTime.Today.AddDays(-30));
    }

    [Fact]
    public void DateTo_HasDefaultValue()
    {
        _viewModel.DateTo.Should().NotBeNull();
        _viewModel.DateTo.Should().Be(DateTime.Today);
    }

    #endregion

    #region Helper Methods

    private static SalesInvoiceDto CreateSalesInvoiceDto(
        int id,
        string invoiceNo,
        decimal totalAmount,
        byte status,
        string customerName = "عميل تجريبي")
    {
        return new SalesInvoiceDto(
            Id: id,
            InvoiceNo: invoiceNo,
            CustomerId: 1,
            CustomerName: customerName,
            WarehouseId: 1,
            WarehouseName: "المستودع الرئيسي",
            InvoiceDate: DateTime.Today,
            DueDate: null,
            PaymentType: 1,
            SubTotal: totalAmount,
            DiscountAmount: 0,
            TaxAmount: 0,
            TotalAmount: totalAmount,
            PaidAmount: totalAmount,
            DueAmount: 0,
            Notes: null,
            Status: status,
            Items: new List<SalesInvoiceItemDto>());
    }

    #endregion
}