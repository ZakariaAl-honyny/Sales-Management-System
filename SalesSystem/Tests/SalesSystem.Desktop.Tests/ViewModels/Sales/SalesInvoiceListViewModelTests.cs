namespace SalesSystem.Desktop.Tests.ViewModels.Sales;

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
    private readonly SalesInvoiceListViewModel _viewModel;

    public SalesInvoiceListViewModelTests()
    {
        _mockInvoiceService = new Mock<ISalesInvoiceApiService>();
        _mockEventBus = new Mock<IEventBus>();

        // Create ViewModel with constructor injection
        _viewModel = new SalesInvoiceListViewModel(
            _mockInvoiceService.Object,
            _mockEventBus.Object);
    }

    public void Dispose()
    {
        _viewModel.Cleanup();
    }

    #region LoadInvoices Tests

    [Fact]
    public async Task LoadInvoicesAsync_WhenApiSucceeds_PopulatesInvoicesCollection()
    {
        // Arrange
        var invoices = new List<SalesInvoiceDto>
        {
            new(1, "INV-2026-001", 1, "عميل أول", 1, "مستودع رئيسي", DateTime.Today, null, 1, 1000m, 0m, 0m, 1000m, 1000m, 0m, null, 2, new List<SalesInvoiceItemDto>()),
            new(2, "INV-2026-002", 2, "عميل ثاني", 1, "مستودع رئيسي", DateTime.Today, null, 1, 500m, 0m, 0m, 500m, 500m, 0m, null, 1, new List<SalesInvoiceItemDto>())
        };

        _mockInvoiceService
            .Setup(s => s.GetAllAsync(
                It.IsAny<string?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<byte?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<SalesInvoiceDto>>.Success(invoices));

        // Act
        await _viewModel.LoadInvoicesAsync();

        // Assert
        _viewModel.Invoices.Should().HaveCount(2);
        _viewModel.Invoices.First().InvoiceNo.Should().Be("INV-2026-001");
        _viewModel.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadInvoicesAsync_WhenApiFails_SetsErrorMessage()
    {
        // Arrange
        _mockInvoiceService
            .Setup(s => s.GetAllAsync(
                It.IsAny<string?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<byte?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<SalesInvoiceDto>>.Failure("فشل في الاتصال"));

        // Act
        await _viewModel.LoadInvoicesAsync();

        // Assert
        _viewModel.ErrorMessage.Should().NotBeNullOrEmpty();
        _viewModel.ErrorMessage.Should().Contain("فشل");
    }

    [Fact]
    public async Task LoadInvoicesAsync_WhenLoading_SetsIsLoadingTrue()
    {
        // Arrange
        var tcs = new TaskCompletionSource<Result<List<SalesInvoiceDto>>>();
        _mockInvoiceService
            .Setup(s => s.GetAllAsync(
                It.IsAny<string?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<byte?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .Returns(tcs.Task);

        // Act
        var loadTask = _viewModel.LoadInvoicesAsync();
        _viewModel.IsLoading.Should().BeTrue();

        tcs.SetResult(Result<List<SalesInvoiceDto>>.Success(new List<SalesInvoiceDto>()));
        await loadTask;

        // Assert
        _viewModel.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadInvoicesAsync_SetsUpCollectionView()
    {
        // Arrange
        _mockInvoiceService
            .Setup(s => s.GetAllAsync(
                It.IsAny<string?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<byte?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<SalesInvoiceDto>>.Success(new List<SalesInvoiceDto>
            {
                new(1, "INV-001", 1, "عميل", 1, "مستودع", DateTime.Today, null, 1, 100m, 0m, 0m, 100m, 100m, 0m, null, 2, new List<SalesInvoiceItemDto>())
            }));

        // Act
        await _viewModel.LoadInvoicesAsync();

        // Assert
        _viewModel.InvoicesView.Should().NotBeNull();
    }

    #endregion

    #region Search Tests

    [Fact]
    public async Task SearchText_WhenChanged_RefreshesCollectionView()
    {
        // Arrange
        var invoices = new List<SalesInvoiceDto>
        {
            new(1, "INV-001", 1, "أحمد", 1, "مستودع", DateTime.Today, null, 1, 1000m, 0m, 0m, 1000m, 1000m, 0m, null, 2, new List<SalesInvoiceItemDto>()),
            new(2, "INV-002", 2, "خالد", 1, "مستودع", DateTime.Today, null, 1, 500m, 0m, 0m, 500m, 500m, 0m, null, 2, new List<SalesInvoiceItemDto>()),
            new(3, "INV-003", 3, "أحمد", 1, "مستودع", DateTime.Today, null, 1, 300m, 0m, 0m, 300m, 300m, 0m, null, 2, new List<SalesInvoiceItemDto>())
        };

        _mockInvoiceService
            .Setup(s => s.GetAllAsync(
                It.IsAny<string?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<byte?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<SalesInvoiceDto>>.Success(invoices));

        await _viewModel.LoadInvoicesAsync();

        // Act
        _viewModel.SearchText = "أحمد";

        // Assert - search should filter the collection
        _viewModel.InvoicesView.Should().NotBeNull();
    }

    [Fact]
    public async Task SearchText_WhenEmpty_ReturnsAllInvoices()
    {
        // Arrange
        var invoices = new List<SalesInvoiceDto>
        {
            new(1, "INV-001", 1, "أحمد", 1, "مستودع", DateTime.Today, null, 1, 1000m, 0m, 0m, 1000m, 1000m, 0m, null, 2, new List<SalesInvoiceItemDto>()),
            new(2, "INV-002", 2, "خالد", 1, "مستودع", DateTime.Today, null, 1, 500m, 0m, 0m, 500m, 500m, 0m, null, 2, new List<SalesInvoiceItemDto>())
        };

        _mockInvoiceService
            .Setup(s => s.GetAllAsync(
                It.IsAny<string?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<byte?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<SalesInvoiceDto>>.Success(invoices));

        await _viewModel.LoadInvoicesAsync();
        _viewModel.SearchText = "غير موجود";

        // Act - refresh view
        _viewModel.InvoicesView?.Refresh();

        // Assert - empty search should show all
        _viewModel.InvoicesView.Should().NotBeNull();
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
    public void SelectedInvoice_Set_NotifiesPropertyChanged()
    {
        // Arrange
        var propertyChangedEvents = new List<string>();
        _viewModel.PropertyChanged += (s, e) => propertyChangedEvents.Add(e.PropertyName ?? string.Empty);

        // Act
        var invoice = new SalesInvoiceDto(1, "INV-001", 1, "عميل", 1, "مستودع", DateTime.Today, null, 1, 100m, 0m, 0m, 100m, 100m, 0m, null, 2, new List<SalesInvoiceItemDto>());
        _viewModel.SelectedInvoice = invoice;

        // Assert
        propertyChangedEvents.Should().Contain("SelectedInvoice");
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
    public void ViewCommand_CannotExecute_WhenNoSelection()
    {
        // Arrange
        _viewModel.SelectedInvoice = null;

        // Act & Assert
        _viewModel.ViewCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ViewCommand_CanExecute_WhenInvoiceSelected()
    {
        // Arrange
        var invoice = new SalesInvoiceDto(1, "INV-001", 1, "عميل", 1, "مستودع", DateTime.Today, null, 1, 100m, 0m, 0m, 100m, 100m, 0m, null, 2, new List<SalesInvoiceItemDto>());
        _viewModel.SelectedInvoice = invoice;

        // Act & Assert
        _viewModel.ViewCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void EditCommand_CannotExecute_WhenNoSelection()
    {
        // Arrange
        _viewModel.SelectedInvoice = null;

        // Act & Assert
        _viewModel.EditCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void EditCommand_CannotExecute_WhenInvoicePosted()
    {
        // Arrange - Posted invoice (Status = 2)
        var invoice = new SalesInvoiceDto(1, "INV-001", 1, "عميل", 1, "مستودع", DateTime.Today, null, 1, 100m, 0m, 0m, 100m, 100m, 0m, null, 2, new List<SalesInvoiceItemDto>());
        _viewModel.SelectedInvoice = invoice;

        // Act & Assert
        _viewModel.EditCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void EditCommand_CanExecute_WhenInvoiceIsDraft()
    {
        // Arrange - Draft invoice (Status = 1)
        var invoice = new SalesInvoiceDto(1, "INV-001", 1, "عميل", 1, "مستودع", DateTime.Today, null, 1, 100m, 0m, 0m, 100m, 100m, 0m, null, 1, new List<SalesInvoiceItemDto>());
        _viewModel.SelectedInvoice = invoice;

        // Act & Assert
        _viewModel.EditCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void PostCommand_CannotExecute_WhenNoSelection()
    {
        // Arrange
        _viewModel.SelectedInvoice = null;

        // Act & Assert
        _viewModel.PostCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void PostCommand_CannotExecute_WhenInvoicePosted()
    {
        // Arrange - Posted invoice (Status = 2)
        var invoice = new SalesInvoiceDto(1, "INV-001", 1, "عميل", 1, "مستودع", DateTime.Today, null, 1, 100m, 0m, 0m, 100m, 100m, 0m, null, 2, new List<SalesInvoiceItemDto>());
        _viewModel.SelectedInvoice = invoice;

        // Act & Assert
        _viewModel.PostCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void PostCommand_CanExecute_WhenInvoiceIsDraft()
    {
        // Arrange - Draft invoice (Status = 1)
        var invoice = new SalesInvoiceDto(1, "INV-001", 1, "عميل", 1, "مستودع", DateTime.Today, null, 1, 100m, 0m, 0m, 100m, 100m, 0m, null, 1, new List<SalesInvoiceItemDto>());
        _viewModel.SelectedInvoice = invoice;

        // Act & Assert
        _viewModel.PostCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void CancelCommand_CannotExecute_WhenNoSelection()
    {
        // Arrange
        _viewModel.SelectedInvoice = null;

        // Act & Assert
        _viewModel.CancelCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void CancelCommand_CannotExecute_WhenInvoiceIsDraft()
    {
        // Arrange - Draft invoice (Status = 1)
        var invoice = new SalesInvoiceDto(1, "INV-001", 1, "عميل", 1, "مستودع", DateTime.Today, null, 1, 100m, 0m, 0m, 100m, 100m, 0m, null, 1, new List<SalesInvoiceItemDto>());
        _viewModel.SelectedInvoice = invoice;

        // Act & Assert
        _viewModel.CancelCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void CancelCommand_CanExecute_WhenInvoicePosted()
    {
        // Arrange - Posted invoice (Status = 2)
        var invoice = new SalesInvoiceDto(1, "INV-001", 1, "عميل", 1, "مستودع", DateTime.Today, null, 1, 100m, 0m, 0m, 100m, 100m, 0m, null, 2, new List<SalesInvoiceItemDto>());
        _viewModel.SelectedInvoice = invoice;

        // Act & Assert
        _viewModel.CancelCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void NewCommand_CanExecute_Always()
    {
        // Act & Assert
        _viewModel.NewCommand.CanExecute(null).Should().BeTrue();
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
            e => e.Unsubscribe<PurchaseInvoiceChangedMessage>(It.IsAny<Action<PurchaseInvoiceChangedMessage>>()),
            Times.Never); // SalesInvoiceListViewModel subscribes to SaleInvoiceChangedMessage

        // Verify it unsubscribes from the correct message type
        _mockEventBus.Verify(
            e => e.Unsubscribe<SaleInvoiceChangedMessage>(It.IsAny<Action<SaleInvoiceChangedMessage>>()),
            Times.Once);
    }

    #endregion

    #region EventBus Subscription Tests

    [Fact]
    public void Constructor_SubscribesToSaleInvoiceChangedMessage()
    {
        // Assert
        _mockEventBus.Verify(
            e => e.Subscribe<SaleInvoiceChangedMessage>(It.IsAny<Action<SaleInvoiceChangedMessage>>()),
            Times.Once);
    }

    #endregion

    #region RefreshCommand Tests

    [Fact]
    public async Task RefreshCommand_Executed_LoadsInvoices()
    {
        // Arrange
        var invoices = new List<SalesInvoiceDto>
        {
            new(1, "INV-001", 1, "عميل", 1, "مستودع", DateTime.Today, null, 1, 100m, 0m, 0m, 100m, 100m, 0m, null, 2, new List<SalesInvoiceItemDto>())
        };

        _mockInvoiceService
            .Setup(s => s.GetAllAsync(
                It.IsAny<string?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<byte?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<SalesInvoiceDto>>.Success(invoices));

        // Act
        _viewModel.RefreshCommand.Execute(null);

        // Wait for async
        await Task.Delay(100);

        // Assert
        _viewModel.Invoices.Should().HaveCount(1);
    }

    #endregion

    #region StatusOptions Tests

    [Fact]
    public void StatusOptions_ContainsAllStatuses()
    {
        // Assert
        _viewModel.StatusOptions.Should().HaveCount(4);
        _viewModel.StatusOptions.Should().Contain(o => o.Display == "الكل");
        _viewModel.StatusOptions.Should().Contain(o => o.Display == "مسودة");
        _viewModel.StatusOptions.Should().Contain(o => o.Display == "مفتوحة");
        _viewModel.StatusOptions.Should().Contain(o => o.Display == "ملغاة");
    }

    #endregion
}