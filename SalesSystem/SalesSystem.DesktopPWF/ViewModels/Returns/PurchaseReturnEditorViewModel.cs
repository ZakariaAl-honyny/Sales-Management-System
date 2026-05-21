using SalesSystem.DesktopPWF.Messaging.Messages;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.Contracts.Enums;
using Microsoft.Extensions.Logging;
using SalesSystem.DesktopPWF.Helpers;
using SalesSystem.DesktopPWF.Models;
using SalesSystem.DesktopPWF.ViewModels.Invoices;

namespace SalesSystem.DesktopPWF.ViewModels.Returns;

public class PurchaseReturnEditorViewModel : ViewModelBase
{
    private readonly IPurchaseReturnApiService _returnService;
    private readonly IPurchaseInvoiceApiService _invoiceService;
    private readonly IWarehouseApiService _warehouseService;
    private readonly ISettingsApiService _settingsService;
    private readonly IInvoicePrinter _invoicePrinter;
    private readonly IEventBus _eventBus;
    private readonly Microsoft.Extensions.Logging.ILogger<PurchaseReturnEditorViewModel> _logger;
    private readonly ISoundService _soundService;
    private readonly IDialogService _dialogService;

    private DateTime _returnDate = DateTime.Now;
    private string _notes = string.Empty;
    private int _selectedWarehouseId;
    private PurchaseInvoiceDto? _selectedInvoice;
    private ObservableCollection<WarehouseDto> _warehouses = new();
    private ObservableCollection<PurchaseReturnItemViewModel> _items = new();
    private string _searchText = string.Empty;
    private bool _isLoading;
    private bool _isEditMode;
    private string? _errorMessage;
    private InvoiceStatus _status = InvoiceStatus.Draft;
    private int? _returnId;
    private ReturnImpactSummary _impact = new();


    public PurchaseReturnEditorViewModel()
    {
        _returnService = App.GetService<IPurchaseReturnApiService>();
        _invoiceService = App.GetService<IPurchaseInvoiceApiService>();
        _warehouseService = App.GetService<IWarehouseApiService>();
        _settingsService = App.GetService<ISettingsApiService>();
        _invoicePrinter = App.GetService<IInvoicePrinter>();
        _eventBus = App.GetService<IEventBus>();
        _soundService = App.GetService<ISoundService>();
        _logger = App.GetService<Microsoft.Extensions.Logging.ILogger<PurchaseReturnEditorViewModel>>();
        _dialogService = App.GetService<IDialogService>();

        InitializeCommands();
        _ = LoadInitialDataAsync();
    }

    private void InitializeCommands()
    {
        SaveCommand = new AsyncRelayCommand(SaveAsync, () => CanSave());
        PostCommand = new AsyncRelayCommand(PostAsync, CanPost);
        CancelReturnCommand = new AsyncRelayCommand(CancelReturnAsync, CanCancel);
        CancelCommand = new RelayCommand(() => RequestClose());
        SearchInvoiceCommand = new AsyncRelayCommand(SearchInvoiceAsync, () => !IsEditMode);
        PrintA4Command = new AsyncRelayCommand(PrintA4Async, () => _returnId.HasValue);
        ProcessBarcodeCommand = new AsyncRelayCommand(async () => 
        {
            var code = SearchText;
            SearchText = string.Empty;
            if (!string.IsNullOrWhiteSpace(code))
            {
                await ProcessBarcodeAsync(code);
            }
        });
    }

    #region Properties
    public DateTime ReturnDate
    {
        get => _returnDate;
        set => SetProperty(ref _returnDate, value);
    }

    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    public string Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }

    public int SelectedWarehouseId
    {
        get => _selectedWarehouseId;
        set
        {
            if (SetProperty(ref _selectedWarehouseId, value))
            {
                UpdateImpactAnalysis();
                (SaveCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public PurchaseInvoiceDto? SelectedInvoice
    {
        get => _selectedInvoice;
        set
        {
            if (SetProperty(ref _selectedInvoice, value))
            {
                if (value != null) SearchText = value.InvoiceNo;
                UpdateItemsFromInvoice();
                (SaveCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public ObservableCollection<WarehouseDto> Warehouses
    {
        get => _warehouses;
        set => SetProperty(ref _warehouses, value);
    }

    public ObservableCollection<PurchaseReturnItemViewModel> Items
    {
        get => _items;
        set => SetProperty(ref _items, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public bool IsEditMode
    {
        get => _isEditMode;
        set => SetProperty(ref _isEditMode, value);
    }

    public InvoiceStatus Status
    {
        get => _status;
        set
        {
            if (SetProperty(ref _status, value))
            {
                OnPropertyChanged(nameof(IsDraft));
                OnPropertyChanged(nameof(IsPosted));
                OnPropertyChanged(nameof(IsCancelled));
                OnPropertyChanged(nameof(IsReadOnly));
                UpdateCommandStates();
            }
        }
    }

    public bool IsDraft => Status == InvoiceStatus.Draft;
    public bool IsPosted => Status == InvoiceStatus.Posted;
    public bool IsCancelled => Status == InvoiceStatus.Cancelled;
    public bool IsReadOnly => Status != InvoiceStatus.Draft;

    public string Title => IsEditMode ? $"مرتجع مشتريات - {StatusText}" : "مرتجع مشتريات جديد";

    public string StatusText => Status switch
    {
        InvoiceStatus.Draft => "مسودة",
        InvoiceStatus.Posted => "مرحل",
        InvoiceStatus.Cancelled => "ملغي",
        _ => "غير معروف"
    };

    public decimal TotalAmount => Items.Sum(i => i.LineTotal);

    public ReturnImpactSummary Impact
    {
        get => _impact;
        set => SetProperty(ref _impact, value);
    }
    #endregion

    #region Commands
    public ICommand SaveCommand { get; private set; } = null!;
    public ICommand PostCommand { get; private set; } = null!;
    public ICommand CancelReturnCommand { get; private set; } = null!;
    public ICommand CancelCommand { get; private set; } = null!;
    public ICommand SearchInvoiceCommand { get; private set; } = null!;
    public ICommand PrintA4Command { get; private set; } = null!;
    public ICommand ProcessBarcodeCommand { get; private set; } = null!;
    #endregion

    #region Methods
    private async Task SearchInvoiceAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            ShowInvoiceSelectionDialog();
            return;
        }

        IsLoading = true;
        try
        {
            var result = await _invoiceService.GetAllAsync(search: SearchText, status: 2, pageSize: 2);
            
            if (result.IsSuccess && result.Value != null && result.Value.Count == 1)
            {
                var fullInvoiceResult = await _invoiceService.GetByIdAsync(result.Value[0].Id);
                if (fullInvoiceResult.IsSuccess && fullInvoiceResult.Value != null)
                {
                    InvokeOnUIThread(() =>
                    {
                        SelectedInvoice = fullInvoiceResult.Value;
                        SearchText = string.Empty;
                    });
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            HandleException(ex, "PurchaseReturnEditorViewModel.SearchInvoiceAsync", "[PurchaseReturnEditorViewModel.SearchInvoiceAsync] Error searching purchase invoice.");
            InvokeOnUIThread(() =>
            {
                ErrorMessage = "حدث خطأ أثناء البحث عن الفاتورة";
            });
        }
        finally
        {
            InvokeOnUIThread(() =>
            {
                IsLoading = false;
            });
        }

        ShowInvoiceSelectionDialog();
    }

    private void ShowInvoiceSelectionDialog()
    {
        var dialogService = App.GetService<IDialogService>();
        var viewModel = new PurchaseInvoiceSelectionViewModel();
        
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            viewModel.SearchText = SearchText;
        }

        if (dialogService.ShowDialog(viewModel) && viewModel.SelectedInvoice != null)
        {
            _ = LoadFullInvoiceAsync(viewModel.SelectedInvoice.Id);
            SearchText = string.Empty;
        }
    }

    private async Task LoadFullInvoiceAsync(int invoiceId)
    {
        IsLoading = true;
        try
        {
            var result = await _invoiceService.GetByIdAsync(invoiceId);
            if (result.IsSuccess && result.Value != null)
            {
                InvokeOnUIThread(() =>
                {
                    SelectedInvoice = result.Value;
                });
            }
            else
            {
                InvokeOnUIThread(() =>
                {
                    ErrorMessage = result.Error ?? "فشل في تحميل تفاصيل الفاتورة";
                });
            }
        }
        catch (Exception ex)
        {
            HandleException(ex, "PurchaseReturnEditorViewModel.LoadFullPurchaseInvoiceAsync", "[PurchaseReturnEditorViewModel.LoadFullPurchaseInvoiceAsync] Error loading full purchase invoice details.");
            InvokeOnUIThread(() =>
            {
                ErrorMessage = "حدث خطأ غير متوقع أثناء تحميل تفاصيل الفاتورة";
            });
        }
        finally
        {
            InvokeOnUIThread(() =>
            {
                IsLoading = false;
            });
        }
    }

    public async Task LoadReturnAsync(int id)
    {
        _returnId = id;
        IsEditMode = true;
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            await LoadInitialDataAsync(); // Load warehouses

            var result = await _returnService.GetByIdAsync(id);
            if (result.IsSuccess && result.Value != null)
            {
                var dto = result.Value;
                ReturnDate = dto.ReturnDate;
                Notes = dto.Notes ?? string.Empty;
                SelectedWarehouseId = dto.WarehouseId;
                Status = (InvoiceStatus)dto.Status;

                // Load invoice details if linked
                if (dto.PurchaseInvoiceId.HasValue)
                {
                    var invResult = await _invoiceService.GetByIdAsync(dto.PurchaseInvoiceId.Value);
                    if (invResult.IsSuccess) SelectedInvoice = invResult.Value;
                }

                // Populate items
                Items.Clear();
                foreach (var item in dto.Items)
                {
                    var lineVm = new PurchaseReturnItemViewModel(item, _soundService);
                    lineVm.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(PurchaseReturnItemViewModel.LineTotal))
                        {
                            OnPropertyChanged(nameof(TotalAmount));
                            UpdateImpactAnalysis();
                        }
                    };
                    Items.Add(lineVm);
                }
                OnPropertyChanged(nameof(TotalAmount));
                UpdateImpactAnalysis();
                UpdateCommandStates();
            }
            else
            {
                ErrorMessage = "فشل في تحميل بيانات المرتجع";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "حدث خطأ أثناء تحميل البيانات";
            _logger.LogError(ex, "Error loading purchase return {Id}", id);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadInitialDataAsync()
    {
        IsLoading = true;
        try
        {
            var warehouseResult = await _warehouseService.GetAllAsync();
            if (warehouseResult.IsSuccess && warehouseResult.Value != null)
            {
                Warehouses = new ObservableCollection<WarehouseDto>(warehouseResult.Value);
                if (Warehouses.Any()) SelectedWarehouseId = Warehouses.First().Id;
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void UpdateItemsFromInvoice()
    {
        Items.Clear();
        if (SelectedInvoice == null || SelectedInvoice.Items == null) return;

        foreach (var item in SelectedInvoice.Items)
        {
            var lineVm = new PurchaseReturnItemViewModel(item, _soundService);
            lineVm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(PurchaseReturnItemViewModel.LineTotal))
                {
                    OnPropertyChanged(nameof(TotalAmount));
                    UpdateImpactAnalysis();
                }
            };
            Items.Add(lineVm);
        }
        OnPropertyChanged(nameof(TotalAmount));
        UpdateImpactAnalysis();
    }

    private void UpdateImpactAnalysis()
    {
        var warehouse = Warehouses.FirstOrDefault(w => w.Id == SelectedWarehouseId);
        
        var summary = new ReturnImpactSummary
        {
            TotalReturnAmount = TotalAmount,
            StockQuantityImpact = Items.Sum(i => i.ReturnQuantity),
            WarehouseName = warehouse?.Name ?? "غير محدد",
            CounterpartyName = SelectedInvoice?.SupplierName ?? "غير محدد",
            CounterpartyType = "المورد",
            BalanceImpact = TotalAmount,
            TaxImpact = Items.Sum(i => {
                var lineTotal = i.LineTotal;
                return lineTotal * 0.15m / 1.15m; 
            })
        };

        Impact = summary;
    }

    private bool CanSave()
    {
        if (IsReadOnly) return false;
        if (SelectedInvoice == null) return false;
        if (SelectedWarehouseId <= 0) return false;
        if (!Items.Any(i => i.ReturnQuantity > 0)) return false;
        return true;
    }

    private bool CanPost()
    {
        return IsEditMode && Status == InvoiceStatus.Draft && _returnId.HasValue;
    }

    private bool CanCancel()
    {
        return IsEditMode && Status != InvoiceStatus.Cancelled && _returnId.HasValue;
    }

    private void UpdateCommandStates()
    {
        (SaveCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (PostCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (CancelReturnCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (SearchInvoiceCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (PrintA4Command as AsyncRelayCommand)?.RaiseCanExecuteChanged();
    }

    private async Task SaveAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var request = new CreatePurchaseReturnRequest(
                PurchaseInvoiceId: SelectedInvoice?.Id,
                SupplierId: SelectedInvoice?.SupplierId ?? 0,
                WarehouseId: SelectedWarehouseId,
                ReturnDate: ReturnDate,
                Notes: Notes,
                Items: Items.Where(i => i.ReturnQuantity > 0).Select(i => new ReturnItemRequest(
                    ProductId: i.ProductId,
                    Quantity: i.ReturnQuantity,
                    UnitPrice: i.UnitPrice,
                    DiscountAmount: i.DiscountAmount,
                    Mode: i.Mode
                )).ToList()
            );

            var result = await _returnService.CreateAsync(request);

            if (result.IsSuccess)
            {
                _eventBus.Publish(new PurchaseReturnChangedMessage(result.Value!.Id));
                await _dialogService.ShowSuccessAsync("نجاح", "تم إنشاء مرتجع المشتريات بنجاح");
                RequestClose();
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشل في حفظ المرتجع", "PurchaseReturnEditorViewModel.SaveAsync", "[PurchaseReturnEditorViewModel.SaveAsync] Failed to save purchase return.");
                await _dialogService.ShowErrorAsync("خطأ", ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = HandleException(ex, "PurchaseReturnEditorViewModel.SaveAsync", "[PurchaseReturnEditorViewModel.SaveAsync] Failed to save purchase return.");
            await _dialogService.ShowErrorAsync("خطأ", ErrorMessage);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task PostAsync()
    {
        if (!_returnId.HasValue) return;

        var confirmMessage = $"هل أنت متأكد من ترحيل هذا المرتجع؟\n\n" +
                             $"📦 الأثر على المخزون: سيتم خصم {Impact.StockQuantityImpact} قطعة من مستودع {Impact.WarehouseName}.\n" +
                             $"💰 الأثر المالي: سيتم خصم {Impact.BalanceImpact:N2} من مديونية المورد {Impact.CounterpartyName}.\n\n" +
                             $"لا يمكن التعديل بعد الترحيل.";

        var confirm = await _dialogService.ShowConfirmationAsync("تأكيد الترحيل - تحليل الأثر", confirmMessage);
        if (!confirm) return;

        IsLoading = true;
        try
        {
            var result = await _returnService.PostAsync(_returnId.Value);
            if (result.IsSuccess)
            {
                Status = InvoiceStatus.Posted;
                _eventBus.Publish(new PurchaseReturnChangedMessage(_returnId.Value));
                await _dialogService.ShowSuccessAsync("تم الترحيل", "تم ترحيل المرتجع بنجاح");
            }
            else
            {
                ErrorMessage = result.Error ?? "فشل في ترحيل المرتجع";
                await _dialogService.ShowErrorAsync("خطأ", ErrorMessage);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task CancelReturnAsync()
    {
        if (!_returnId.HasValue) return;

        var confirm = await _dialogService.ShowConfirmationAsync("تأكيد الإلغاء", "هل أنت متأكد من إلغاء هذا المرتجع؟");
        if (!confirm) return;

        IsLoading = true;
        try
        {
            var result = await _returnService.CancelAsync(_returnId.Value);
            if (result.IsSuccess)
            {
                Status = InvoiceStatus.Cancelled;
                _eventBus.Publish(new PurchaseReturnChangedMessage(_returnId.Value));
                await _dialogService.ShowSuccessAsync("تم الإلغاء", "تم إلغاء المرتجع بنجاح");
            }
            else
            {
                ErrorMessage = result.Error ?? "فشل في إلغاء المرتجع";
                await _dialogService.ShowErrorAsync("خطأ", ErrorMessage);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task PrintA4Async()
    {
        if (!_returnId.HasValue) return;

        IsLoading = true;
        try
        {
            var settingsResult = await _settingsService.GetSettingsAsync();
            if (!settingsResult.IsSuccess || settingsResult.Value == null) return;

            var returnResult = await _returnService.GetByIdAsync(_returnId.Value);
            if (!returnResult.IsSuccess || returnResult.Value == null) return;

            _invoicePrinter.PrintPreview(
                returnResult.Value.ToPrintDto(),
                returnResult.Value.Items.ToPrintDtos(),
                returnResult.Value.ToTotalsPrintDto(),
                settingsResult.Value.ToPrintDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error preparing print for purchase return {Id}", _returnId);
            await _dialogService.ShowErrorAsync("خطأ", "حدث خطأ أثناء تحضير الطباعة");
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task<bool> ProcessBarcodeAsync(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode)) return false;

        // If no invoice selected, search for invoice
        if (SelectedInvoice == null)
        {
            SearchText = barcode;
            await SearchInvoiceAsync();
            if (SelectedInvoice != null)
            {
                _soundService.PlaySuccess();
                return true;
            }
            _soundService.PlayError();
            return false;
        }

        // If invoice selected, find product by code or barcode
        var item = Items.FirstOrDefault(i => i.ProductCode == barcode);
        if (item != null)
        {
            if (item.ReturnQuantity < item.OriginalQuantity)
            {
                item.ReturnQuantity += 1;
                SearchText = string.Empty; // Clear after success
                _soundService.PlaySuccess();
                return true;
            }
            else
            {
                _soundService.PlayError();
                await _dialogService.ShowWarningAsync("تنبيه", $"الكمية المرتجعة لا يمكن أن تتجاوز الكمية المشتراة ({item.OriginalQuantity})");
            }
        }
        else
        {
            // Allow re-scanning invoice if current one has no items returned?
            if (!Items.Any(i => i.ReturnQuantity > 0))
            {
                SearchText = barcode;
                await SearchInvoiceAsync();
                if (SelectedInvoice != null)
                {
                    _soundService.PlaySuccess();
                    return true;
                }
            }
            _soundService.PlayError();
        }

        return false;
    }
    #endregion
}

public class PurchaseReturnItemViewModel : ViewModelBase
{
    private decimal _returnQuantity;
    private byte _mode = 1;
    private readonly ISoundService? _soundService;

    public int ProductId { get; }
    public string ProductCode { get; }
    public string ProductName { get; }
    public decimal OriginalQuantity { get; }
    public decimal UnitPrice { get; }
    public decimal DiscountAmount { get; }
    public byte Mode => _mode;

    public decimal LineTotal => ReturnQuantity * (UnitPrice - (OriginalQuantity > 0 ? DiscountAmount / OriginalQuantity : 0));

    public decimal ReturnQuantity
    {
        get => _returnQuantity;
        set
        {
            if (value < 0) value = 0;
            if (value > OriginalQuantity) value = OriginalQuantity;
            if (SetProperty(ref _returnQuantity, value))
            {
                OnPropertyChanged(nameof(LineTotal));
                _soundService?.PlaySuccess();
            }
        }
    }

    public PurchaseReturnItemViewModel(PurchaseInvoiceItemDto item, ISoundService? soundService = null)
    {
        ProductId = item.ProductId;
        ProductCode = item.ProductCode ?? string.Empty;
        ProductName = item.ProductName;
        OriginalQuantity = item.Quantity;
        UnitPrice = item.UnitCost;
        DiscountAmount = item.DiscountAmount;
        _mode = item.Mode;
        _returnQuantity = 0;
        _soundService = soundService;
    }

    public PurchaseReturnItemViewModel(PurchaseReturnItemDto item, ISoundService? soundService = null)
    {
        ProductId = item.ProductId;
        ProductCode = item.ProductCode ?? string.Empty;
        ProductName = item.ProductName;
        OriginalQuantity = item.Quantity;
        UnitPrice = item.UnitCost;
        DiscountAmount = item.DiscountAmount;
        _mode = item.Mode;
        _returnQuantity = item.Quantity;
        _soundService = soundService;
    }
}
