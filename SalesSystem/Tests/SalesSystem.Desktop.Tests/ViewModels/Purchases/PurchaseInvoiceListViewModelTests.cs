namespace SalesSystem.Desktop.Tests.ViewModels.Purchases;

using System.ComponentModel;
using System.Runtime.Serialization;
using System.Windows.Input;
using FluentAssertions;
using Moq;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Enums;
using SalesSystem.DesktopPWF.Services;
using SalesSystem.DesktopPWF.ViewModels;
using SalesSystem.DesktopPWF.ViewModels.Purchases;

/// <summary>
/// Tests for PurchaseInvoiceListViewModel
/// </summary>
public class PurchaseInvoiceListViewModelTests : IDisposable
{
    private readonly Mock<IPurchaseInvoiceApiService> _mockInvoiceService;
    private readonly Mock<IEventBus> _mockEventBus;
    private readonly PurchaseInvoiceListViewModel _viewModel;

    public PurchaseInvoiceListViewModelTests()
    {
        _mockInvoiceService = new Mock<IPurchaseInvoiceApiService>();
        _mockEventBus = new Mock<IEventBus>();

        // Create ViewModel using reflection (similar to DashboardViewModelTests)
        _viewModel = CreateViewModel();
    }

    private PurchaseInvoiceListViewModel CreateViewModel()
    {
        // Create ViewModel WITHOUT calling constructor (avoids App.GetService issue)
        var viewModel = (PurchaseInvoiceListViewModel)FormatterServices.GetUninitializedObject(typeof(PurchaseInvoiceListViewModel));

        // Set private fields via reflection
        var fieldNames = new[] {
            "_invoiceService",
            "_eventBus"
        };
        var mockObjects = new object[] {
            _mockInvoiceService.Object,
            _mockEventBus.Object
        };

        for (int i = 0; i < fieldNames.Length; i++)
        {
            var field = typeof(PurchaseInvoiceListViewModel).GetField(fieldNames[i],
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(viewModel, mockObjects[i]);
        }

        // Initialize backing fields for properties
        var invoicesField = typeof(PurchaseInvoiceListViewModel).GetField("_invoices",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        invoicesField?.SetValue(viewModel, new System.Collections.ObjectModel.ObservableCollection<PurchaseInvoiceDto>());

        var searchTextField = typeof(PurchaseInvoiceListViewModel).GetField("_searchText",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        searchTextField?.SetValue(viewModel, string.Empty);

        var isLoadingField = typeof(PurchaseInvoiceListViewModel).GetField("_isLoading",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        isLoadingField?.SetValue(viewModel, false);

        var errorMessageField = typeof(PurchaseInvoiceListViewModel).GetField("_errorMessage",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        errorMessageField?.SetValue(viewModel, (string?)null);

        // Initialize StatusOptions (auto-property with collection expression)
        var statusOptionsField = typeof(PurchaseInvoiceListViewModel).GetField("<StatusOptions>k__BackingField",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        statusOptionsField?.SetValue(viewModel, new List<StatusItem>
        {
            new StatusItem { Value = null, Display = "الكل" },
            new StatusItem { Value = 1, Display = "مسودة" },
            new StatusItem { Value = 2, Display = "مفتوحة" },
            new StatusItem { Value = 3, Display = "ملغاة" }
        });

        // Create and set commands (auto-properties have compiler-generated backing fields)
        var loadInvoicesMethod = typeof(PurchaseInvoiceListViewModel).GetMethod("LoadInvoicesAsync", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        
        // Invoke returns object - cast to Task and await
        var refreshCommand = new AsyncRelayCommand(async _ => await (Task)loadInvoicesMethod!.Invoke(viewModel, null)!);
        var refreshField = typeof(PurchaseInvoiceListViewModel).GetField("<RefreshCommand>k__BackingField",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        refreshField?.SetValue(viewModel, refreshCommand);

        var newCommand = new RelayCommand(() => { });
        var newField = typeof(PurchaseInvoiceListViewModel).GetField("<NewCommand>k__BackingField",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        newField?.SetValue(viewModel, newCommand);

        // Subscribe to EventBus events manually
        // Since we use GetUninitializedObject, constructor doesn't run, so we need to subscribe manually
        Action<PurchaseInvoiceChangedMessage> handler = msg => { };
        _mockEventBus.Object.Subscribe(handler);
        
        return viewModel;
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
        var invoices = new List<PurchaseInvoiceDto>
        {
            new(1, "PUR-2026-001", 1, "مورد أول", 1, "مستودع رئيسي", DateTime.Today, null, 1, 1000m, 0m, 0m, 1000m, 1000m, 0m, null, 2, new List<PurchaseInvoiceItemDto>()),
            new(2, "PUR-2026-002", 2, "مورد ثاني", 1, "مستودع رئيسي", DateTime.Today, null, 1, 500m, 0m, 0m, 500m, 500m, 0m, null, 1, new List<PurchaseInvoiceItemDto>())
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
            .ReturnsAsync(Result<List<PurchaseInvoiceDto>>.Success(invoices));

        // Act
        await _viewModel.LoadInvoicesAsync();

        // Assert
        _viewModel.Invoices.Should().HaveCount(2);
        _viewModel.Invoices.First().InvoiceNo.Should().Be("PUR-2026-001");
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
            .ReturnsAsync(Result<List<PurchaseInvoiceDto>>.Failure("فشل في الاتصال"));

        // Act
        await _viewModel.LoadInvoicesAsync();

        // Assert
        _viewModel.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task LoadInvoicesAsync_WhenLoading_SetsIsLoadingTrue()
    {
        // Arrange
        var tcs = new TaskCompletionSource<Result<List<PurchaseInvoiceDto>>>();
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

        tcs.SetResult(Result<List<PurchaseInvoiceDto>>.Success(new List<PurchaseInvoiceDto>()));
        await loadTask;

        // Assert
        _viewModel.IsLoading.Should().BeFalse();
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

    #region Command Tests

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
            Times.Once);
    }

    #endregion

    #region EventBus Subscription Tests

    [Fact]
    public void Constructor_SubscribesToPurchaseInvoiceChangedMessage()
    {
        // Assert
        _mockEventBus.Verify(
            e => e.Subscribe<PurchaseInvoiceChangedMessage>(It.IsAny<Action<PurchaseInvoiceChangedMessage>>()),
            Times.Once);
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