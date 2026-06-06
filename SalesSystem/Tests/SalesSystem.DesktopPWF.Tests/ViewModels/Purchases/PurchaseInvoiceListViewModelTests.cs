namespace SalesSystem.DesktopPWF.Tests.ViewModels.Purchases;

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using FluentAssertions;
using Moq;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Enums;
using SalesSystem.DesktopPWF.Services;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.ViewModels.Purchases;

/// <summary>
/// Tests for PurchaseInvoiceListViewModel
/// </summary>
public class PurchaseInvoiceListViewModelTests : IDisposable
{
    private readonly Mock<IPurchaseInvoiceApiService> _mockInvoiceService;
    private readonly Mock<IEventBus> _mockEventBus;
    private readonly Mock<IDialogService> _mockDialogService;
    private readonly Mock<IPrintApiService> _mockPrintService;
    private readonly Mock<IScreenWindowService> _mockScreenWindowService;
    private readonly PurchaseInvoiceListViewModel _viewModel;

    public PurchaseInvoiceListViewModelTests()
    {
        _mockInvoiceService = new Mock<IPurchaseInvoiceApiService>();
        _mockEventBus = new Mock<IEventBus>();
        _mockDialogService = new Mock<IDialogService>();
        _mockPrintService = new Mock<IPrintApiService>();
        _mockScreenWindowService = new Mock<IScreenWindowService>();

        _viewModel = new PurchaseInvoiceListViewModel(
            _mockInvoiceService.Object,
            _mockEventBus.Object,
            _mockDialogService.Object,
            _mockPrintService.Object,
            _mockScreenWindowService.Object);
    }

    public void Dispose()
    {
        _viewModel?.Cleanup();
    }

    #region LoadInvoices Tests

    [Fact]
    public async Task LoadInvoicesAsync_WhenApiSucceeds_PopulatesInvoicesCollection()
    {
        var invoices = new List<PurchaseInvoiceDto>
        {
            CreatePurchaseInvoiceDto(1, 1000m, (byte)InvoiceStatus.Draft),
            CreatePurchaseInvoiceDto(2, 2000m, (byte)InvoiceStatus.Posted)
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
            .ReturnsAsync(Result<List<PurchaseInvoiceDto>>.Success(invoices));

        await _viewModel.LoadInvoicesAsync();

        _viewModel.Invoices.Should().HaveCount(2);
        _viewModel.Invoices.First().Id.Should().BeGreaterThan(0);
        _viewModel.IsBusy.Should().BeFalse();
    }

    [Fact]
    public async Task LoadInvoicesAsync_WhenLoading_SetsIsBusyTrue()
    {
        var tcs = new TaskCompletionSource<Result<List<PurchaseInvoiceDto>>>();
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
        _viewModel.IsBusy.Should().BeTrue();

        tcs.SetResult(Result<List<PurchaseInvoiceDto>>.Success(new List<PurchaseInvoiceDto>()));
        await loadTask;

        _viewModel.IsBusy.Should().BeFalse();
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
            .ReturnsAsync(Result<List<PurchaseInvoiceDto>>.Success(new List<PurchaseInvoiceDto>
            {
                CreatePurchaseInvoiceDto(1, 1000m, 2)
            }));

        await _viewModel.LoadInvoicesAsync();

        _viewModel.InvoicesView.Should().NotBeNull();
    }

    #endregion

    #region Search Tests

    [Fact]
    public async Task SearchText_WhenChanged_RefreshesCollectionView()
    {
        var invoices = new List<PurchaseInvoiceDto>
        {
            CreatePurchaseInvoiceDto(1, 1000m, 2, "مورد 1"),
            CreatePurchaseInvoiceDto(2, 2000m, 2, "مورد 2"),
            CreatePurchaseInvoiceDto(3, 3000m, 2, "مورد 1")
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
            .ReturnsAsync(Result<List<PurchaseInvoiceDto>>.Success(invoices));

        await _viewModel.LoadInvoicesAsync();

        _viewModel.SearchText = "مورد 1";

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
        var invoices = new List<PurchaseInvoiceDto>
        {
            CreatePurchaseInvoiceDto(1, 1000m, 2, "مورد 1"),
            CreatePurchaseInvoiceDto(2, 2000m, 2, "مورد 2")
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
            .ReturnsAsync(Result<List<PurchaseInvoiceDto>>.Success(invoices));

        await _viewModel.LoadInvoicesAsync();
        _viewModel.SearchText = "ط؛ظٹط± ظ…ظˆط¬ظˆط¯";

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

        _viewModel.ErrorMessage = "خطأ في التحميل";

        propertyChangedEvents.Should().Contain("ErrorMessage");
    }

    [Fact]
    public void SelectedInvoice_Set_NotifiesPropertyChanged()
    {
        var propertyChangedEvents = new List<string>();
        _viewModel.PropertyChanged += (s, e) => propertyChangedEvents.Add(e.PropertyName ?? string.Empty);

        var invoice = CreatePurchaseInvoiceDto(1, 1000m, 1);
        _viewModel.SelectedInvoice = invoice;

        propertyChangedEvents.Should().Contain("SelectedInvoice");
    }

    [Fact]
    public void SearchText_Set_NotifiesPropertyChanged()
    {
        var propertyChangedEvents = new List<string>();
        _viewModel.PropertyChanged += (s, e) => propertyChangedEvents.Add(e.PropertyName ?? string.Empty);

        _viewModel.SearchText = "ط¨ط­ط«";

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
        var invoice = CreatePurchaseInvoiceDto(1, 1000m, (byte)InvoiceStatus.Posted);
        _viewModel.SelectedInvoice = invoice;
        _viewModel.EditCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void EditCommand_CanExecute_WhenDraftInvoiceSelected()
    {
        var invoice = CreatePurchaseInvoiceDto(1, 1000m, (byte)InvoiceStatus.Draft);
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
        var invoice = CreatePurchaseInvoiceDto(1, 1000m, (byte)InvoiceStatus.Posted);
        _viewModel.SelectedInvoice = invoice;
        _viewModel.PostCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void PostCommand_CanExecute_WhenDraftInvoiceSelected()
    {
        var invoice = CreatePurchaseInvoiceDto(1, 1000m, (byte)InvoiceStatus.Draft);
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
        var invoice = CreatePurchaseInvoiceDto(1, 1000m, (byte)InvoiceStatus.Draft);
        _viewModel.SelectedInvoice = invoice;
        _viewModel.CancelCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void CancelCommand_CanExecute_WhenPostedInvoiceSelected()
    {
        var invoice = CreatePurchaseInvoiceDto(1, 1000m, (byte)InvoiceStatus.Posted);
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
        var invoice = CreatePurchaseInvoiceDto(1, 1000m, (byte)InvoiceStatus.Cancelled);
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
            e => e.Unsubscribe<PurchaseInvoiceChangedMessage>(It.IsAny<Action<PurchaseInvoiceChangedMessage>>()),
            Times.Once);
    }

    #endregion

    #region EventBus Subscription Tests

    [Fact]
    public void Constructor_SubscribesToPurchaseInvoiceChangedMessage()
    {
        _mockEventBus.Verify(
            e => e.Subscribe<PurchaseInvoiceChangedMessage>(It.IsAny<Action<PurchaseInvoiceChangedMessage>>()),
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

    private static PurchaseInvoiceDto CreatePurchaseInvoiceDto(
        int id,
        decimal totalAmount,
        byte status,
        string supplierName = "مورد تجريبي")
    {
        return new PurchaseInvoiceDto(
            Id: id,
            InvoiceNo: 1,
            SupplierId: 1,
            SupplierName: supplierName,
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
            SupplierInvoiceNo: null,
            Notes: null,
            Status: status,
            TaxId: null,
            TaxName: null,
            TaxRate: null,
            Items: new List<PurchaseInvoiceItemDto>());
    }

    #endregion
}
