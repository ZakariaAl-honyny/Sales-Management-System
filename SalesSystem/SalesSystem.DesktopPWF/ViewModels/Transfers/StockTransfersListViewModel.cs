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
    private readonly IStockTransferApiService _transferService;
    private readonly IDialogService _dialogService;
    private readonly ITransferPrinter _transferPrinter;
    private readonly ISettingsApiService _settingsService;

    private string _searchText = string.Empty;
    private DateTime? _dateFrom;
    private DateTime? _dateTo;
    private byte? _statusFilter;
    private bool _isLoading;
    private string _errorMessage = string.Empty;
    private bool _isEmpty;
    private ICollectionView? _transfersView;
    private StockTransferDto? _selectedTransfer;
    private ObservableCollection<StockTransferDto> _transfers = new();

    public StockTransfersListViewModel()
    {
        _transferService = App.GetService<IStockTransferApiService>();
        _dialogService = App.GetService<IDialogService>();
        _transferPrinter = App.GetService<ITransferPrinter>();
        _settingsService = App.GetService<ISettingsApiService>();

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

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
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
        new StatusOption(null, "الكل"),
        new StatusOption((byte)InvoiceStatus.Draft, "مسودة"),
        new StatusOption((byte)InvoiceStatus.Posted, "مفتوحة"),
        new StatusOption((byte)InvoiceStatus.Cancelled, "ملغاة")
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
            IsLoading = true;
            ErrorMessage = string.Empty;

            var result = await _transferService.GetAllAsync(SearchText, DateFrom, DateTo, StatusFilter, IncludeInactive);

            if (result.IsSuccess && result.Value != null)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    Transfers.Clear();
                    foreach (var item in result.Value)
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
                ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل التحويلات", "StockTransfersListViewModel.LoadTransfersAsync", "[StockTransfersListViewModel.LoadTransfersAsync] Failed to load stock transfers list.");
                IsEmpty = Transfers.Count == 0;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = HandleException(ex, "StockTransfersListViewModel.LoadTransfersAsync", "[StockTransfersListViewModel.LoadTransfersAsync] Failed to load stock transfers list.");
        }
        finally
        {
            IsLoading = false;
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
        var vm = new StockTransferEditorViewModel();
        if (_dialogService.ShowDialog(vm))
        {
            _ = LoadTransfersAsync();
        }
    }

    private void OnView()
    {
        if (SelectedTransfer == null) return;
        var vm = new StockTransferEditorViewModel(SelectedTransfer.Id, isReadOnly: true);
        _dialogService.ShowDialog(vm);
    }

    private void OnEdit()
    {
        if (SelectedTransfer == null) return;
        var vm = new StockTransferEditorViewModel(SelectedTransfer.Id);
        if (_dialogService.ShowDialog(vm))
        {
            _ = LoadTransfersAsync();
        }
    }

    private async Task OnPost()
    {
        if (SelectedTransfer == null) return;

        var result = await _dialogService.ShowConfirmationAsync("تأكيد الترحيل", "هل أنت متأكد من ترحيل هذا التحويل؟ سيتم نقل المخزون بين المستودعات.");

        if (!result) return;

        try
        {
            IsLoading = true;
            var postResult = await _transferService.PostAsync(SelectedTransfer.Id);

            if (postResult.IsSuccess)
            {
                await LoadTransfersAsync();
            }
            else
            {
                ErrorMessage = HandleFailure(postResult.Error ?? "فشل في ترحيل التحويل", "StockTransfersListViewModel.OnPost");
                await _dialogService.ShowErrorAsync("خطأ في الترحيل", ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = HandleException(ex, "StockTransfersListViewModel.OnPost", "Failed to post transfer.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task OnCancel()
    {
        if (SelectedTransfer == null) return;

        var result = await _dialogService.ShowConfirmationAsync("تأكيد الإلغاء", "هل أنت متأكد من إلغاء هذا التحويل؟ سيتم إرجاع المخزون.");

        if (!result) return;

        try
        {
            IsLoading = true;
            var cancelResult = await _transferService.CancelAsync(SelectedTransfer.Id);

            if (cancelResult.IsSuccess)
            {
                await LoadTransfersAsync();
            }
            else
            {
                ErrorMessage = HandleFailure(cancelResult.Error ?? "فشل في إلغاء التحويل", "StockTransfersListViewModel.OnCancel");
                await _dialogService.ShowErrorAsync("خطأ في الإلغاء", ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = HandleException(ex, "StockTransfersListViewModel.OnCancel", "Failed to cancel transfer.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task OnPrint()
    {
        if (SelectedTransfer == null) return;

        IsLoading = true;
        try
        {
            var settingsResult = await _settingsService.GetSettingsAsync();
            if (!settingsResult.IsSuccess || settingsResult.Value == null) return;

            var transferResult = await _transferService.GetByIdAsync(SelectedTransfer.Id);
            if (!transferResult.IsSuccess || transferResult.Value == null) return;

            _transferPrinter.PrintPreview(
                transferResult.Value.ToPrintDto(),
                transferResult.Value.Items.ToPrintDtos(),
                settingsResult.Value.ToPrintDto());
        }
        catch (Exception ex)
        {
            ErrorMessage = $"خطأ في الطباعة: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}

/// <summary>
/// Status display option
/// </summary>
public record StatusOption(byte? Value, string Display);
