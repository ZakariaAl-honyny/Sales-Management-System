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

public class SalesReturnEditorViewModel : ViewModelBase
{
    private readonly ISalesReturnApiService _returnService;
    private readonly ISalesInvoiceApiService _invoiceService;
    private readonly IWarehouseApiService _warehouseService;
    private readonly ISettingsApiService _settingsService;
    private readonly IInvoicePrinter _invoicePrinter;
    private readonly IReceiptPrinter _receiptPrinter;
    private readonly IEventBus _eventBus;
    private readonly Microsoft.Extensions.Logging.ILogger<SalesReturnEditorViewModel> _logger;
    private readonly ISoundService _soundService;
    private readonly IDialogService _dialogService;

    private DateTime _returnDate = DateTime.Now;
    private string _notes = string.Empty;
    private int _selectedWarehouseId;
    private SalesInvoiceDto? _selectedInvoice;
    private ObservableCollection<WarehouseDto> _warehouses = new();
    private ObservableCollection<SalesReturnItemViewModel> _items = new();
    private string _searchText = string.Empty;
    private bool _isEditMode;
    private string? _errorMessage;
    private InvoiceStatus _status = InvoiceStatus.Draft;
    private int? _returnId;
    private ReturnImpactSummary _impact = new();


    public SalesReturnEditorViewModel()
    {
        _returnService = App.GetService<ISalesReturnApiService>();
        _invoiceService = App.GetService<ISalesInvoiceApiService>();
        _warehouseService = App.GetService<IWarehouseApiService>();
        _settingsService = App.GetService<ISettingsApiService>();
        _invoicePrinter = App.GetService<IInvoicePrinter>();
        _receiptPrinter = App.GetService<IReceiptPrinter>();
        _eventBus = App.GetService<IEventBus>();
        _soundService = App.GetService<ISoundService>();
        _logger = App.GetService<Microsoft.Extensions.Logging.ILogger<SalesReturnEditorViewModel>>();
        _dialogService = App.GetService<IDialogService>();
        SetDialogService(_dialogService);

        InitializeCommands();
        _ = LoadInitialDataAsync();
    }

    private void InitializeCommands()
    {
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        PostCommand = new AsyncRelayCommand(PostAsync);
        CancelReturnCommand = new AsyncRelayCommand(CancelReturnAsync);
        CancelCommand = new RelayCommand(() => RequestClose());
        SearchInvoiceCommand = new AsyncRelayCommand(SearchInvoiceAsync);
        PrintA4Command = new AsyncRelayCommand(PrintA4Async);
        PrintReceiptCommand = new AsyncRelayCommand(PrintReceiptAsync);
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
            }
        }
    }

    public SalesInvoiceDto? SelectedInvoice
    {
        get => _selectedInvoice;
        set
        {
            if (SetProperty(ref _selectedInvoice, value))
            {
                if (value != null) SearchText = value.Id.ToString();
                UpdateItemsFromInvoice();
            }
        }
    }

    public ObservableCollection<WarehouseDto> Warehouses
    {
        get => _warehouses;
        set => SetProperty(ref _warehouses, value);
    }

    public ObservableCollection<SalesReturnItemViewModel> Items
    {
        get => _items;
        set => SetProperty(ref _items, value);
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
            }
        }
    }

    public bool IsDraft => Status == InvoiceStatus.Draft;
    public bool IsPosted => Status == InvoiceStatus.Posted;
    public bool IsCancelled => Status == InvoiceStatus.Cancelled;
    public bool IsReadOnly => Status != InvoiceStatus.Draft;

    public string Title => IsEditMode ? $"مرتجع مبيعات - {StatusText}" : "مرتجع مبيعات جديد";

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
    public ICommand PrintReceiptCommand { get; private set; } = null!;
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

        await ExecuteAsync(async () =>
        {
            ErrorMessage = null;
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
        });

        ShowInvoiceSelectionDialog();
    }

    private void ShowInvoiceSelectionDialog()
    {
        var dialogService = App.GetService<IDialogService>();
        var viewModel = new SalesInvoiceSelectionViewModel();
        
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
        await ExecuteAsync(async () =>
        {
            ErrorMessage = null;
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
        });
    }

    public async Task LoadReturnAsync(int id)
    {
        _returnId = id;
        IsEditMode = true;

        await ExecuteAsync(async () =>
        {
            ErrorMessage = null;
            await LoadInitialDataAsync();

            var result = await _returnService.GetByIdAsync(id);
            if (result.IsSuccess && result.Value != null)
            {
                var dto = result.Value;
                ReturnDate = dto.ReturnDate;
                Notes = dto.Notes ?? string.Empty;
                SelectedWarehouseId = dto.WarehouseId;
                Status = (InvoiceStatus)dto.Status;

                if (dto.SalesInvoiceId.HasValue)
                {
                    var invResult = await _invoiceService.GetByIdAsync(dto.SalesInvoiceId.Value);
                    if (invResult.IsSuccess) SelectedInvoice = invResult.Value;
                }

                Items.Clear();
                foreach (var item in dto.Items)
                {
                    var lineVm = new SalesReturnItemViewModel(item, _soundService);
                    lineVm.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(SalesReturnItemViewModel.LineTotal))
                            OnPropertyChanged(nameof(TotalAmount));
                    };
                    Items.Add(lineVm);
                }
                OnPropertyChanged(nameof(TotalAmount));
                UpdateImpactAnalysis();
            }
            else
            {
                ErrorMessage = "فشل في تحميل بيانات المرتجع";
            }
        });
    }

    private async Task LoadInitialDataAsync()
    {
        await ExecuteAsync(async () =>
        {
            var warehouseResult = await _warehouseService.GetAllAsync();
            if (warehouseResult.IsSuccess && warehouseResult.Value != null)
            {
                Warehouses = new ObservableCollection<WarehouseDto>(warehouseResult.Value);
                if (Warehouses.Any()) SelectedWarehouseId = Warehouses.First().Id;
            }
        });
    }

    private void UpdateItemsFromInvoice()
    {
        Items.Clear();
        if (SelectedInvoice == null || SelectedInvoice.Items == null) return;

        foreach (var item in SelectedInvoice.Items)
        {
            var lineVm = new SalesReturnItemViewModel(item, _soundService);
            lineVm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(SalesReturnItemViewModel.LineTotal))
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
            CounterpartyName = SelectedInvoice?.CustomerName ?? "غير محدد",
            CounterpartyType = "العميل",
            BalanceImpact = TotalAmount, // For credit sales, it reduces balance
            TaxImpact = Items.Sum(i => {
                var lineTotal = i.LineTotal;
                // Assuming 15% tax for calculation preview if not specified
                return lineTotal * 0.15m / 1.15m; 
            })
        };

        Impact = summary;
    }

    private async Task<bool> ValidateAsync()
    {
        if (SelectedInvoice == null)
        {
            await _dialogService.ShowValidationErrorsAsync("بيانات غير مكتملة", new List<string> { "يجب اختيار فاتورة بيع لإنشاء مرتجع" });
            RequestFocusFirstInvalidField();
            return false;
        }
        if (SelectedWarehouseId <= 0)
        {
            await _dialogService.ShowValidationErrorsAsync("بيانات غير مكتملة", new List<string> { "يجب اختيار المستودع" });
            RequestFocusFirstInvalidField();
            return false;
        }
        if (!Items.Any(i => i.ReturnQuantity > 0))
        {
            await _dialogService.ShowValidationErrorsAsync("بيانات غير مكتملة", new List<string> { "يجب إدخال كمية المرتجع لمنتج واحد على الأقل" });
            RequestFocusFirstInvalidField();
            return false;
        }
        return true;
    }

    private async Task SaveAsync()
    {
        if (!await ValidateAsync()) return;

        await ExecuteAsync(async () =>
        {
            ErrorMessage = null;
            var request = new CreateSalesReturnRequest(
                SalesInvoiceId: SelectedInvoice?.Id,
                CustomerId: SelectedInvoice?.CustomerId,
                WarehouseId: SelectedWarehouseId,
                ReturnDate: ReturnDate,
                Notes: Notes,
                Items: Items.Where(i => i.ReturnQuantity > 0).Select(i => new ReturnItemRequest(
                    ProductId: i.ProductId,
                    ProductUnitId: 1,
                    Quantity: i.ReturnQuantity,
                    UnitPrice: i.UnitPrice,
                    DiscountAmount: i.DiscountAmount,
                    Mode: i.Mode
                )).ToList()
            );

            var result = await _returnService.CreateAsync(request);

            if (result.IsSuccess)
            {
                _returnId = result.Value!.Id;
                IsEditMode = true;
                Status = (InvoiceStatus)result.Value!.Status;
                
                _eventBus.Publish(new SalesReturnChangedMessage(_returnId.Value));
                await _dialogService.ShowSuccessAsync("نجاح", "تم إنشاء مرتجع المبيعات بنجاح");
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشل في حفظ المرتجع", "SalesReturnEditorViewModel.SaveAsync", "[SalesReturnEditorViewModel.SaveAsync] Failed to save sales return.");
                await _dialogService.ShowErrorAsync("خطأ في حفظ المرتجع", ErrorMessage);
            }
        });
    }

    private async Task PostAsync()
    {
        // 1. Auto-save if new
        if (!_returnId.HasValue)
        {
            await SaveAsync();
            if (!_returnId.HasValue) return;
        }

        var confirmMessage = $"هل أنت متأكد من ترحيل هذا المرتجع؟\n\n" +
                             $"📦 الأثر على المخزون: سيتم إضافة {Impact.StockQuantityImpact} قطعة إلى مستودع {Impact.WarehouseName}.\n" +
                             $"💰 الأثر المالي: سيتم خصم {Impact.BalanceImpact:N2} من مديونية العميل {Impact.CounterpartyName}.\n\n" +
                             $"لا يمكن التعديل بعد الترحيل.";

        var confirm = await _dialogService.ShowConfirmationAsync("تأكيد الترحيل - تحليل الأثر", confirmMessage);
        if (!confirm) return;

        await ExecuteAsync(async () =>
        {
            ErrorMessage = null;
            var result = await _returnService.PostAsync(_returnId.Value);
            if (result.IsSuccess)
            {
                Status = InvoiceStatus.Posted;
                _eventBus.Publish(new SalesReturnChangedMessage(_returnId.Value));
                await _dialogService.ShowSuccessAsync("تم الترحيل", "تم ترحيل المرتجع بنجاح");
                RequestClose();
            }
            else
            {
                ErrorMessage = result.Error ?? "فشل في ترحيل المرتجع";
                await _dialogService.ShowErrorAsync("خطأ في الترحيل", ErrorMessage);
            }
        });
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
        var item = Items.FirstOrDefault(i => i.ProductName.Contains(barcode) || i.ProductId.ToString() == barcode);
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
                await _dialogService.ShowWarningAsync("تنبيه", $"الكمية المرتجعة لا يمكن أن تتجاوز الكمية المباعة ({item.OriginalQuantity})");
            }
        }
        else
        {
            // If not found by code, maybe it's another invoice scan?
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

    private async Task CancelReturnAsync()
    {
        if (!_returnId.HasValue) return;

        var confirm = await _dialogService.ShowConfirmationAsync("تأكيد الإلغاء", "هل أنت متأكد من إلغاء هذا المرتجع؟");
        if (!confirm) return;

        await ExecuteAsync(async () =>
        {
            ErrorMessage = null;
            var result = await _returnService.CancelAsync(_returnId.Value);
            if (result.IsSuccess)
            {
                Status = InvoiceStatus.Cancelled;
                _eventBus.Publish(new SalesReturnChangedMessage(_returnId.Value));
                await _dialogService.ShowSuccessAsync("تم الإلغاء", "تم إلغاء المرتجع بنجاح");
            }
            else
            {
                ErrorMessage = result.Error ?? "فشل في إلغاء المرتجع";
                await _dialogService.ShowErrorAsync("خطأ في الإلغاء", ErrorMessage);
            }
        });
    }

    private async Task PrintA4Async()
    {
        await PrepareAndPrint(_invoicePrinter);
    }

    private async Task PrintReceiptAsync()
    {
        await PrepareAndPrint(_receiptPrinter);
    }

    private async Task PrepareAndPrint(IPrinterService printer)
    {
        if (!_returnId.HasValue) return;

        await ExecuteAsync(async () =>
        {
            var settingsResult = await _settingsService.GetSettingsAsync();
            if (!settingsResult.IsSuccess || settingsResult.Value == null) return;

            var returnResult = await _returnService.GetByIdAsync(_returnId.Value);
            if (!returnResult.IsSuccess || returnResult.Value == null) return;

            printer.PrintPreview(
                returnResult.Value.ToPrintDto(),
                returnResult.Value.Items.ToPrintDtos(),
                returnResult.Value.ToTotalsPrintDto(),
                settingsResult.Value.ToPrintDto());
        });
    }
    #endregion
}

public class SalesReturnItemViewModel : ViewModelBase
{
    private decimal _returnQuantity;
    private byte _mode = 1;
    private readonly ISoundService? _soundService;

    public int ProductId { get; }
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

    public SalesReturnItemViewModel(SalesInvoiceItemDto item, ISoundService? soundService = null)
    {
        ProductId = item.ProductId;
        ProductName = item.ProductName;
        OriginalQuantity = item.Quantity;
        UnitPrice = item.UnitPrice;
        DiscountAmount = item.DiscountAmount;
        _mode = item.Mode;
        _returnQuantity = 0;
        _soundService = soundService;
    }

    public SalesReturnItemViewModel(SalesReturnItemDto item, ISoundService? soundService = null)
    {
        ProductId = item.ProductId;
        ProductName = item.ProductName;
        OriginalQuantity = item.Quantity; // In case of viewing, we might need a different property but for simplicity
        UnitPrice = item.UnitPrice;
        DiscountAmount = item.DiscountAmount;
        _mode = item.Mode;
        _returnQuantity = item.Quantity;
        _soundService = soundService;
    }
}
