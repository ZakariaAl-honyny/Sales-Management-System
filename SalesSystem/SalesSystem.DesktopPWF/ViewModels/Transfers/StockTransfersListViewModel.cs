using SalesSystem.DesktopPWF.Messaging.Messages;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.ComponentModel;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Enums;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Helpers;

namespace SalesSystem.DesktopPWF.ViewModels.Transfers;

/// <summary>
/// ViewModel for Stock Transfers List
/// </summary>
public class StockTransfersListViewModel : ViewModelBase
{
    private IStockTransferApiService? _transferService;
    private IDialogService? _dialogService;
    private ITransferPrinter? _transferPrinter;
    private ISettingsApiService? _settingsService;
    private IScreenWindowService? _screenWindowService;

    private IStockTransferApiService TransferService => _transferService ??= App.GetService<IStockTransferApiService>();
    private IDialogService DialogService => _dialogService ??= App.GetService<IDialogService>();
    private ITransferPrinter TransferPrinter => _transferPrinter ??= App.GetService<ITransferPrinter>();
    private ISettingsApiService SettingsService => _settingsService ??= App.GetService<ISettingsApiService>();
    private IScreenWindowService ScreenWindowService => _screenWindowService ??= App.GetService<IScreenWindowService>();

    private string _searchText = string.Empty;
    private DateTime? _dateFrom;
    private DateTime? _dateTo;
    private byte? _statusFilter;
    private string _errorMessage = string.Empty;
    private bool _isEmpty;
    private ICollectionView? _transfersView;
    private StockTransferDto? _selectedTransfer;
    private ObservableCollection<StockTransferDto> _transfers = new();

    public StockTransfersListViewModel()
    {
        AddCommand = new RelayCommand(OnNew);
        ViewCommand = new RelayCommand(OnView, () => SelectedTransfer != null);
        EditCommand = new RelayCommand(OnEdit, () => SelectedTransfer != null && SelectedTransfer.Status == (byte)InvoiceStatus.Draft);
        PostCommand = new AsyncRelayCommand(OnPost, () => SelectedTransfer != null && SelectedTransfer.Status == (byte)InvoiceStatus.Draft);
        CancelCommand = new AsyncRelayCommand(OnCancel, () => SelectedTransfer != null && SelectedTransfer.Status == (byte)InvoiceStatus.Posted);
        PrintCommand = new AsyncRelayCommand(OnPrint, () => SelectedTransfer != null);
        RefreshCommand = new AsyncRelayCommand(LoadTransfersAsync);
    }

    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    public DateTime? DateFrom
    {
        get => _dateFrom;
        set => SetProperty(ref _dateFrom, value);
    }

    public DateTime? DateTo
    {
        get => _dateTo;
        set => SetProperty(ref _dateTo, value);
    }

    public byte? StatusFilter
    {
        get => _statusFilter;
        set => SetProperty(ref _statusFilter, value);
    }


    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    private bool _includeInactive;
    public bool IncludeInactive
    {
        get => _includeInactive;
        set
        {
            if (SetProperty(ref _includeInactive, value))
            {
                _ = LoadTransfersAsync();
            }
        }
    }

    public bool IsEmpty
    {
        get => _isEmpty;
        private set => SetProperty(ref _isEmpty, value);
    }

    public StockTransferDto? SelectedTransfer
    {
        get => _selectedTransfer;
        set
        {
            SetProperty(ref _selectedTransfer, value);
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public ICollectionView? TransfersView
    {
        get => _transfersView;
        private set => SetProperty(ref _transfersView, value);
    }

    public ObservableCollection<StockTransferDto> Transfers
    {
        get => _transfers;
        set => SetProperty(ref _transfers, value);
    }

    public int TotalCount => Transfers.Count;

    public ObservableCollection<StatusOption> StatusOptions { get; } = new()
    {
        new StatusOption(null, "ط§ظ„ظƒظ„"),
        new StatusOption((byte)InvoiceStatus.Draft, "ظ…ط³ظˆط¯ط©"),
        new StatusOption((byte)InvoiceStatus.Posted, "ظ…ظپطھظˆط­ط©"),
        new StatusOption((byte)InvoiceStatus.Cancelled, "ظ…ظ„ط؛ط§ط©")
    };

    public ICommand AddCommand { get; }
    public ICommand ViewCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand PostCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand PrintCommand { get; }
    public ICommand RefreshCommand { get; }

    public async Task LoadTransfersAsync()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            var result = await TransferService.GetAllAsync(SearchText, DateFrom, DateTo, StatusFilter, IncludeInactive);

            if (result.IsSuccess && result.Value != null)
            {
                InvokeOnUIThread(() =>
                {
                    Transfers.Clear();
                    foreach (var item in result.Value.OrderByDescending(x => x.Id))
                    {
                        Transfers.Add(item);
                    }
                    SetupCollectionView();
                    IsEmpty = Transfers.Count == 0;
                    OnPropertyChanged(nameof(TotalCount));
                });
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "ظپط´ظ„ ظپظٹ طھط­ظ…ظٹظ„ ط§ظ„طھط­ظˆظٹظ„ط§طھ", "StockTransfersListViewModel.LoadTransfersAsync", "[StockTransfersListViewModel.LoadTransfersAsync] Failed to load stock transfers list.");
                IsEmpty = Transfers.Count == 0;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = HandleException(ex, "StockTransfersListViewModel.LoadTransfersAsync", "[StockTransfersListViewModel.LoadTransfersAsync] Failed to load stock transfers list.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void SetupCollectionView()
    {
        TransfersView = CollectionViewSource.GetDefaultView(Transfers);
        TransfersView.Filter = FilterTransfers;
    }

    private bool FilterTransfers(object obj)
    {
        if (obj is not StockTransferDto transfer) return false;

        // Date filter
        if (DateFrom.HasValue && transfer.TransferDate < DateFrom.Value) return false;
        if (DateTo.HasValue && transfer.TransferDate > DateTo.Value.AddDays(1)) return false;

        // Status filter
        if (StatusFilter.HasValue && transfer.Status != StatusFilter.Value) return false;

        // Search text filter
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var searchLower = SearchText.Trim().ToLower();
            return transfer.TransferNo.ToLower().Contains(searchLower) ||
                   (transfer.FromWarehouseName?.ToLower().Contains(searchLower) ?? false) ||
                   (transfer.ToWarehouseName?.ToLower().Contains(searchLower) ?? false);
        }

        return true;
    }

    private void OnNew()
    {
        var vm = App.GetService<StockTransferEditorViewModel>();
        ScreenWindowService.OpenScreen(vm, new ScreenWindowOptions
        {
            Title = "ظ†ظ‚ظ„ ظ…ط®ط²ظˆظ† ط¬ط¯ظٹط¯",
            OnClosed = (vm) =>
            {
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadTransfersAsync());
            }
        });
    }

    private void OnView()
    {
        if (SelectedTransfer == null) return;
        var vm = new StockTransferEditorViewModel(SelectedTransfer.Id, isReadOnly: true);
        ScreenWindowService.OpenScreen(vm, new ScreenWindowOptions
        {
            Title = "ط¹ط±ط¶ ظ†ظ‚ظ„ ظ…ط®ط²ظˆظ†"
        });
    }

    private void OnEdit()
    {
        if (SelectedTransfer == null) return;
        var vm = new StockTransferEditorViewModel(SelectedTransfer.Id);
        ScreenWindowService.OpenScreen(vm, new ScreenWindowOptions
        {
            Title = "طھط¹ط¯ظٹظ„ ظ†ظ‚ظ„ ظ…ط®ط²ظˆظ†",
            OnClosed = (vm) =>
            {
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadTransfersAsync());
            }
        });
    }

    private async Task OnPost()
    {
        if (SelectedTransfer == null) return;

        var result = await DialogService.ShowConfirmationAsync("طھط£ظƒظٹط¯ ط§ظ„طھط±ط­ظٹظ„", "ظ‡ظ„ ط£ظ†طھ ظ…طھط£ظƒط¯ ظ…ظ† طھط±ط­ظٹظ„ ظ‡ط°ط§ ط§ظ„طھط­ظˆظٹظ„طں ط³ظٹطھظ… ظ†ظ‚ظ„ ط§ظ„ظ…ط®ط²ظˆظ† ط¨ظٹظ† ط§ظ„ظ…ط³طھظˆط¯ط¹ط§طھ.");

        if (!result) return;

        try
        {
            IsBusy = true;
            var postResult = await TransferService.PostAsync(SelectedTransfer.Id);

            if (postResult.IsSuccess)
            {
                await LoadTransfersAsync();
            }
            else
            {
                ErrorMessage = HandleFailure(postResult.Error ?? "ظپط´ظ„ ظپظٹ طھط±ط­ظٹظ„ ط§ظ„طھط­ظˆظٹظ„", "StockTransfersListViewModel.OnPost");
                await DialogService.ShowErrorAsync("ط®ط·ط£ ظپظٹ ط§ظ„طھط±ط­ظٹظ„", ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = HandleException(ex, "StockTransfersListViewModel.OnPost", "Failed to post transfer.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task OnCancel()
    {
        if (SelectedTransfer == null) return;

        var result = await DialogService.ShowConfirmationAsync("طھط£ظƒظٹط¯ ط§ظ„ط¥ظ„ط؛ط§ط،", "ظ‡ظ„ ط£ظ†طھ ظ…طھط£ظƒط¯ ظ…ظ† ط¥ظ„ط؛ط§ط، ظ‡ط°ط§ ط§ظ„طھط­ظˆظٹظ„طں ط³ظٹطھظ… ط¥ط±ط¬ط§ط¹ ط§ظ„ظ…ط®ط²ظˆظ†.");

        if (!result) return;

        try
        {
            IsBusy = true;
            var cancelResult = await TransferService.CancelAsync(SelectedTransfer.Id);

            if (cancelResult.IsSuccess)
            {
                await LoadTransfersAsync();
            }
            else
            {
                ErrorMessage = HandleFailure(cancelResult.Error ?? "ظپط´ظ„ ظپظٹ ط¥ظ„ط؛ط§ط، ط§ظ„طھط­ظˆظٹظ„", "StockTransfersListViewModel.OnCancel");
                await DialogService.ShowErrorAsync("ط®ط·ط£ ظپظٹ ط§ظ„ط¥ظ„ط؛ط§ط،", ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = HandleException(ex, "StockTransfersListViewModel.OnCancel", "Failed to cancel transfer.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task OnPrint()
    {
        if (SelectedTransfer == null) return;

        IsBusy = true;
        try
        {
            var settingsResult = await SettingsService.GetSettingsAsync();
            if (!settingsResult.IsSuccess || settingsResult.Value == null) return;

            var transferResult = await TransferService.GetByIdAsync(SelectedTransfer.Id);
            if (!transferResult.IsSuccess || transferResult.Value == null) return;

            TransferPrinter.PrintPreview(
                transferResult.Value.ToPrintDto(),
                transferResult.Value.Items.ToPrintDtos(),
                settingsResult.Value.ToPrintDto());
        }
        catch (Exception ex)
        {
            ErrorMessage = $"ط®ط·ط£ ظپظٹ ط§ظ„ط·ط¨ط§ط¹ط©: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}

/// <summary>
/// Status display option
/// </summary>
public record StatusOption(byte? Value, string Display);




