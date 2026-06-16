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
/// ViewModel for Warehouse Transfers List
/// </summary>
public class WarehouseTransfersListViewModel : ViewModelBase
{
    private IWarehouseTransferApiService? _transferService;
    private ITransferPrinter? _transferPrinter;
    private ISettingsApiService? _settingsService;
    private IScreenWindowService? _screenWindowService;

    private IWarehouseTransferApiService TransferService => _transferService ??= App.GetService<IWarehouseTransferApiService>();
    private ITransferPrinter TransferPrinter => _transferPrinter ??= App.GetService<ITransferPrinter>();
    private ISettingsApiService SettingsService => _settingsService ??= App.GetService<ISettingsApiService>();
    private IScreenWindowService ScreenWindowService => _screenWindowService ??= App.GetService<IScreenWindowService>();

    // Uses 'new' to suppress CS0108 (inherited member hiding).
    // Test uses SetField("_dialogService", mock) before property is accessed.
    private new IDialogService DialogService => _dialogService ??= App.GetService<IDialogService>();
    private IDialogService? _dialogService;

    private string _searchText = string.Empty;
    private DateTime? _dateFrom;
    private DateTime? _dateTo;
    private byte? _statusFilter;
    private string _errorMessage = string.Empty;
    private bool _isEmpty;
    private ICollectionView? _transfersView;
    private WarehouseTransferDto? _selectedTransfer;
    private ObservableCollection<WarehouseTransferDto> _transfers = new();

    public WarehouseTransfersListViewModel()
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

    public WarehouseTransferDto? SelectedTransfer
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

    public ObservableCollection<WarehouseTransferDto> Transfers
    {
        get => _transfers;
        set => SetProperty(ref _transfers, value);
    }

    public int TotalCount => Transfers.Count;

    public ObservableCollection<StatusOption> StatusOptions { get; } = new()
    {
        new StatusOption(null, "الكل"),
        new StatusOption((byte)InvoiceStatus.Draft, "مسودة"),
        new StatusOption((byte)InvoiceStatus.Posted, "منشور"),
        new StatusOption((byte)InvoiceStatus.Cancelled, "ملغية")
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
                ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل التحويلات", "WarehouseTransfersListViewModel.LoadTransfersAsync", "[WarehouseTransfersListViewModel.LoadTransfersAsync] Failed to load warehouse transfers list.");
                IsEmpty = Transfers.Count == 0;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = HandleException(ex, "WarehouseTransfersListViewModel.LoadTransfersAsync", "[WarehouseTransfersListViewModel.LoadTransfersAsync] Failed to load warehouse transfers list.");
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
        if (obj is not WarehouseTransferDto transfer) return false;

        // Date filter
        if (DateFrom.HasValue && transfer.CreatedAt < DateFrom.Value) return false;
        if (DateTo.HasValue && transfer.CreatedAt > DateTo.Value.AddDays(1)) return false;

        // Status filter
        if (StatusFilter.HasValue && transfer.Status != StatusFilter.Value) return false;

        // Search text filter
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var searchLower = SearchText.Trim().ToLower();
            return transfer.TransferNo.ToString().Contains(searchLower) ||
                   (transfer.SourceWarehouseName?.ToLower().Contains(searchLower) ?? false) ||
                   (transfer.DestinationWarehouseName?.ToLower().Contains(searchLower) ?? false);
        }

        return true;
    }

    private void OnNew()
    {
        var vm = App.GetService<WarehouseTransferEditorViewModel>();
        ScreenWindowService.OpenScreen(vm, new ScreenWindowOptions
        {
            Title = "نقل مخزون جديد",
            OnClosed = (vm) =>
            {
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadTransfersAsync());
            }
        });
    }

    private void OnView()
    {
        if (SelectedTransfer == null) return;
        var vm = new WarehouseTransferEditorViewModel(SelectedTransfer.Id, isReadOnly: true);
        ScreenWindowService.OpenScreen(vm, new ScreenWindowOptions
        {
            Title = "عرض نقل مخزون"
        });
    }

    private void OnEdit()
    {
        if (SelectedTransfer == null) return;
        var vm = new WarehouseTransferEditorViewModel(SelectedTransfer.Id);
        ScreenWindowService.OpenScreen(vm, new ScreenWindowOptions
        {
            Title = "تعديل نقل مخزون",
            OnClosed = (vm) =>
            {
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadTransfersAsync());
            }
        });
    }

    private async Task OnPost()
    {
        if (SelectedTransfer == null) return;

        var result = await DialogService.ShowConfirmationAsync("تأكيد الترحيل", "هل أنت متأكد من ترحيل هذا التحويل؟ سيتم نقل المخزون بين المستودعات.");

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
                ErrorMessage = HandleFailure(postResult.Error ?? "فشل في ترحيل التحويل", "WarehouseTransfersListViewModel.OnPost");
                await DialogService.ShowErrorAsync("خطأ في الترحيل", ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = HandleException(ex, "WarehouseTransfersListViewModel.OnPost", "Failed to post transfer.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task OnCancel()
    {
        if (SelectedTransfer == null) return;

        var result = await DialogService.ShowConfirmationAsync("تأكيد الإلغاء", "هل أنت متأكد من إلغاء هذا التحويل؟ سيتم إرجاع المخزون.");

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
                ErrorMessage = HandleFailure(cancelResult.Error ?? "فشل في إلغاء التحويل", "WarehouseTransfersListViewModel.OnCancel");
                await DialogService.ShowErrorAsync("خطأ في الإلغاء", ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = HandleException(ex, "WarehouseTransfersListViewModel.OnCancel", "Failed to cancel transfer.");
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
                transferResult.Value.Lines.ToPrintDtos(),
                settingsResult.Value.ToPrintDto());
        }
        catch (Exception ex)
        {
            ErrorMessage = $"خطأ في الطباعة: {ex.Message}";
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