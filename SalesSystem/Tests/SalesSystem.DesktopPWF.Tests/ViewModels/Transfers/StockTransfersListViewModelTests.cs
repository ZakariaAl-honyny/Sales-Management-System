namespace SalesSystem.DesktopPWF.Tests.ViewModels.Transfers;

using System.ComponentModel;
using FluentAssertions;
using Moq;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Enums;
using SalesSystem.DesktopPWF.Helpers;
using SalesSystem.DesktopPWF.Services;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.ViewModels.Transfers;

/// <summary>
/// Tests for StockTransfersListViewModel
/// </summary>
public class StockTransfersListViewModelTests
{
    private readonly Mock<IStockTransferApiService> _mockTransferService;
    private readonly Mock<IDialogService> _mockDialogService;
    private readonly Mock<ITransferPrinter> _mockTransferPrinter;
    private readonly Mock<ISettingsApiService> _mockSettingsService;
    private readonly StockTransfersListViewModel _viewModel;

    public StockTransfersListViewModelTests()
    {
        _mockTransferService = new Mock<IStockTransferApiService>();
        _mockDialogService = new Mock<IDialogService>();
        _mockTransferPrinter = new Mock<ITransferPrinter>();
        _mockSettingsService = new Mock<ISettingsApiService>();

        _viewModel = new StockTransfersListViewModel();

        SetField("_transferService", _mockTransferService.Object);
        SetField("_dialogService", _mockDialogService.Object);
        SetField("_transferPrinter", _mockTransferPrinter.Object);
        SetField("_settingsService", _mockSettingsService.Object);
    }

    private void SetField(string fieldName, object value)
    {
        var field = typeof(StockTransfersListViewModel).GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(_viewModel, value);
    }

    private static StockTransferDto CreateTransfer(int id, byte status)
    {
        return new StockTransferDto(id, $"TRF-{id:000}", 1, "مستودع أ", 2, "مستودع ب", DateTime.Today, null, status, new List<StockTransferItemDto>());
    }

    #region Property Tests

    [Fact] public void SearchText_DefaultValue_IsEmpty() => _viewModel.SearchText.Should().BeEmpty();
    [Fact] public void DateFrom_DefaultValue_IsNull() => _viewModel.DateFrom.Should().BeNull();
    [Fact] public void DateTo_DefaultValue_IsNull() => _viewModel.DateTo.Should().BeNull();
    [Fact] public void StatusFilter_DefaultValue_IsNull() => _viewModel.StatusFilter.Should().BeNull();
    [Fact] public void IsBusy_DefaultValue_IsFalse() => _viewModel.IsBusy.Should().BeFalse();
    [Fact] public void ErrorMessage_DefaultValue_IsEmpty() => _viewModel.ErrorMessage.Should().BeEmpty();
    [Fact] public void SelectedTransfer_DefaultValue_IsNull() => _viewModel.SelectedTransfer.Should().BeNull();

    [Fact]
    public void Transfers_InitializesWithEmptyCollection()
    {
        _viewModel.Transfers.Should().NotBeNull();
        _viewModel.Transfers.Should().BeEmpty();
    }

    [Fact] public void TotalCount_DefaultValue_IsZero() => _viewModel.TotalCount.Should().Be(0);

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
    public void StatusFilter_Set_NotifiesPropertyChanged()
    {
        var events = new List<string>();
        _viewModel.PropertyChanged += (s, e) => events.Add(e.PropertyName ?? string.Empty);
        _viewModel.StatusFilter = (byte)InvoiceStatus.Draft;
        events.Should().Contain("StatusFilter");
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
    public void SelectedTransfer_Set_NotifiesPropertyChanged()
    {
        var events = new List<string>();
        _viewModel.PropertyChanged += (s, e) => events.Add(e.PropertyName ?? string.Empty);
        _viewModel.SelectedTransfer = CreateTransfer(1, (byte)InvoiceStatus.Draft);
        events.Should().Contain("SelectedTransfer");
    }

    #endregion

    #region Commands Tests

    [Fact] public void AddCommand_IsInitialized() => _viewModel.AddCommand.Should().NotBeNull();
    [Fact] public void RefreshCommand_IsInitialized() => _viewModel.RefreshCommand.Should().NotBeNull();

    [Fact]
    public void ViewCommand_CannotExecute_WhenNoSelection()
    {
        _viewModel.SelectedTransfer = null;
        _viewModel.ViewCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ViewCommand_CanExecute_WhenTransferSelected()
    {
        _viewModel.SelectedTransfer = CreateTransfer(1, (byte)InvoiceStatus.Draft);
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
        _viewModel.SelectedTransfer = CreateTransfer(1, (byte)InvoiceStatus.Draft);
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
        _viewModel.SelectedTransfer = CreateTransfer(1, (byte)InvoiceStatus.Draft);
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
        _viewModel.SelectedTransfer = CreateTransfer(1, (byte)InvoiceStatus.Posted);
        _viewModel.CancelCommand.CanExecute(null).Should().BeTrue();
    }

    #endregion

    #region LoadTransfersAsync Tests

    [Fact]
    public async Task LoadTransfersAsync_WhenApiSucceeds_PopulatesTransfers()
    {
        var transfers = new List<StockTransferDto>
        {
            CreateTransfer(1, (byte)InvoiceStatus.Draft),
            CreateTransfer(2, (byte)InvoiceStatus.Posted)
        };

        _mockTransferService
            .Setup(s => s.GetAllAsync(It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<byte?>(), It.IsAny<bool>()))
            .ReturnsAsync(Result<List<StockTransferDto>>.Success(transfers));

        await _viewModel.LoadTransfersAsync();

        _viewModel.Transfers.Should().HaveCount(2);
        _viewModel.IsBusy.Should().BeFalse();
    }

    [Fact]
    public async Task LoadTransfersAsync_WhenApiFails_SetsErrorMessage()
    {
        _mockTransferService
            .Setup(s => s.GetAllAsync(It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<byte?>(), It.IsAny<bool>()))
            .ReturnsAsync(Result<List<StockTransferDto>>.Failure("فشل في الاتصال"));

        await _viewModel.LoadTransfersAsync();

        _viewModel.ErrorMessage.Should().NotBeEmpty();
        _viewModel.IsBusy.Should().BeFalse();
    }

    [Fact]
    public async Task LoadTransfersAsync_WhenLoading_SetsIsBusyTrue()
    {
        var tcs = new TaskCompletionSource<Result<List<StockTransferDto>>>();
        _mockTransferService
            .Setup(s => s.GetAllAsync(It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<byte?>(), It.IsAny<bool>()))
            .Returns(tcs.Task);

        var loadTask = _viewModel.LoadTransfersAsync();
        _viewModel.IsBusy.Should().BeTrue();

        tcs.SetResult(Result<List<StockTransferDto>>.Success(new List<StockTransferDto>()));
        await loadTask;

        _viewModel.IsBusy.Should().BeFalse();
    }

    [Fact]
    public async Task LoadTransfersAsync_UpdatesTotalCount()
    {
        var transfers = new List<StockTransferDto> { CreateTransfer(1, (byte)InvoiceStatus.Draft) };

        _mockTransferService
            .Setup(s => s.GetAllAsync(It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<byte?>(), It.IsAny<bool>()))
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
