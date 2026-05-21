namespace SalesSystem.DesktopPWF.Tests.ViewModels.Transfers;

using System.Collections.ObjectModel;
using System.ComponentModel;
using FluentAssertions;
using Moq;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Enums;
using SalesSystem.DesktopPWF.Services;

/// <summary>
/// Tests for StockTransfersListViewModel
/// </summary>
public class StockTransfersListViewModelTests
{
    private readonly Mock<IStockTransferApiService> _mockTransferService;
    private readonly Mock<IDialogService> _mockDialogService;
    private readonly StockTransfersListViewModel _viewModel;

    public StockTransfersListViewModelTests()
    {
        _mockTransferService = new Mock<IStockTransferApiService>();
        _mockDialogService = new Mock<IDialogService>();

        _viewModel = new StockTransfersListViewModel();
        
        // Inject mocks via reflection
        var transferServiceField = typeof(StockTransfersListViewModel).GetField("_transferService",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        transferServiceField?.SetValue(_viewModel, _mockTransferService.Object);

        var dialogServiceField = typeof(StockTransfersListViewModel).GetField("_dialogService",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        dialogServiceField?.SetValue(_viewModel, _mockDialogService.Object);
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
    public void StatusFilter_DefaultValue_IsNull()
    {
        _viewModel.StatusFilter.Should().BeNull();
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
    public void SelectedTransfer_DefaultValue_IsNull()
    {
        _viewModel.SelectedTransfer.Should().BeNull();
    }

    [Fact]
    public void Transfers_InitializesWithEmptyCollection()
    {
        _viewModel.Transfers.Should().NotBeNull();
        _viewModel.Transfers.Should().BeEmpty();
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
    public void StatusFilter_Set_NotifiesPropertyChanged()
    {
        var propertyChangedEvents = new List<string>();
        _viewModel.PropertyChanged += (s, e) => propertyChangedEvents.Add(e.PropertyName ?? string.Empty);

        _viewModel.StatusFilter = (byte)InvoiceStatus.Draft;

        propertyChangedEvents.Should().Contain("StatusFilter");
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
    public void SelectedTransfer_Set_NotifiesPropertyChanged()
    {
        var propertyChangedEvents = new List<string>();
        _viewModel.PropertyChanged += (s, e) => propertyChangedEvents.Add(e.PropertyName ?? string.Empty);

        var transfer = new StockTransferDto(1, "TRF-001", 1, "مستودع أ", 2, "مستودع ب", DateTime.Today, null, 1, 0m, null, 1);
        _viewModel.SelectedTransfer = transfer;

        propertyChangedEvents.Should().Contain("SelectedTransfer");
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
        _viewModel.SelectedTransfer = null;
        _viewModel.ViewCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ViewCommand_CanExecute_WhenTransferSelected()
    {
        var transfer = new StockTransferDto(1, "TRF-001", 1, "مستودع أ", 2, "مستودع ب", DateTime.Today, null, 1, 0m, null, 1);
        _viewModel.SelectedTransfer = transfer;
        
        _viewModel.ViewCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void EditCommand_CannotExecute_WhenNoSelection()
    {
        _viewModel.SelectedTransfer = null;
        _viewModel.EditCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void EditCommand_CanExecute_WhenDraftTransferSelected()
    {
        var transfer = new StockTransferDto(1, "TRF-001", 1, "مستودع أ", 2, "مستودع ب", DateTime.Today, null, 1, 0m, null, (byte)InvoiceStatus.Draft);
        _viewModel.SelectedTransfer = transfer;
        
        _viewModel.EditCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void PostCommand_CannotExecute_WhenNoSelection()
    {
        _viewModel.SelectedTransfer = null;
        _viewModel.PostCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void PostCommand_CanExecute_WhenDraftTransferSelected()
    {
        var transfer = new StockTransferDto(1, "TRF-001", 1, "مستودع أ", 2, "مستودع ب", DateTime.Today, null, 1, 0m, null, (byte)InvoiceStatus.Draft);
        _viewModel.SelectedTransfer = transfer;
        
        _viewModel.PostCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void CancelCommand_CannotExecute_WhenNoSelection()
    {
        _viewModel.SelectedTransfer = null;
        _viewModel.CancelCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void CancelCommand_CanExecute_WhenPostedTransferSelected()
    {
        var transfer = new StockTransferDto(1, "TRF-001", 1, "مستودع أ", 2, "مستودع ب", DateTime.Today, null, 1, 0m, null, (byte)InvoiceStatus.Posted);
        _viewModel.SelectedTransfer = transfer;
        
        _viewModel.CancelCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void RefreshCommand_IsInitialized()
    {
        _viewModel.RefreshCommand.Should().NotBeNull();
    }

    #endregion

    #region LoadTransfersAsync Tests

    [Fact]
    public async Task LoadTransfersAsync_WhenApiSucceeds_PopulatesTransfers()
    {
        var transfers = new List<StockTransferDto>
        {
            new(1, "TRF-001", 1, "مستودع أ", 2, "مستودع ب", DateTime.Today, null, 1, 0m, null, (byte)InvoiceStatus.Draft),
            new(2, "TRF-002", 1, "مستودع ج", 2, "مستودع د", DateTime.Today, null, 1, 0m, null, (byte)InvoiceStatus.Posted)
        };

        _mockTransferService
            .Setup(s => s.GetAllAsync(
                It.IsAny<string?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<byte?>()))
            .ReturnsAsync(Result<List<StockTransferDto>>.Success(transfers));

        await _viewModel.LoadTransfersAsync();

        _viewModel.Transfers.Should().HaveCount(2);
        _viewModel.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadTransfersAsync_WhenApiFails_SetsErrorMessage()
    {
        _mockTransferService
            .Setup(s => s.GetAllAsync(
                It.IsAny<string?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<byte?>()))
            .ReturnsAsync(Result<List<StockTransferDto>>.Failure("فشل في الاتصال"));

        await _viewModel.LoadTransfersAsync();

        _viewModel.ErrorMessage.Should().NotBeEmpty();
        _viewModel.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadTransfersAsync_WhenLoading_SetsIsLoadingTrue()
    {
        var tcs = new TaskCompletionSource<Result<List<StockTransferDto>>>();
        _mockTransferService
            .Setup(s => s.GetAllAsync(
                It.IsAny<string?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<byte?>()))
            .Returns(tcs.Task);

        var loadTask = _viewModel.LoadTransfersAsync();
        _viewModel.IsLoading.Should().BeTrue();

        tcs.SetResult(Result<List<StockTransferDto>>.Success(new List<StockTransferDto>()));
        await loadTask;

        _viewModel.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadTransfersAsync_UpdatesTotalCount()
    {
        var transfers = new List<StockTransferDto>
        {
            new(1, "TRF-001", 1, "مستودع أ", 2, "مستودع ب", DateTime.Today, null, 1, 0m, null, (byte)InvoiceStatus.Draft)
        };

        _mockTransferService
            .Setup(s => s.GetAllAsync(
                It.IsAny<string?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<byte?>()))
            .ReturnsAsync(Result<List<StockTransferDto>>.Success(transfers));

        await _viewModel.LoadTransfersAsync();

        _viewModel.TotalCount.Should().Be(1);
    }

    #endregion

    #region StatusOptions Tests

    [Fact]
    public void StatusOptions_ContainsAllStatusOptions()
    {
        _viewModel.StatusOptions.Should().HaveCount(4);
    }

    #endregion
}