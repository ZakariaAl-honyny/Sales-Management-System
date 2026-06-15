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
using SalesSystem.DesktopPWF.Services.App.Toast;

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
    private readonly ICurrencyApiService _currencyService;
    private readonly IToastNotificationService _toastService;

    private DateTime _returnDate = DateTime.Now;
    private string _notes = string.Empty;
    private int _selectedWarehouseId;
    private PurchaseInvoiceDto? _selectedInvoice;
    private int _selectedSupplierId;
    private string _selectedSupplierName = string.Empty;
    private bool _isLinkedToInvoice = true;
    private ObservableCollection<WarehouseDto> _warehouses = new();
    private ObservableCollection<PurchaseReturnItemViewModel> _items = new();
    private string _searchText = string.Empty;
    private bool _isEditMode;
    private string? _errorMessage;
    private InvoiceStatus _status = InvoiceStatus.Draft;
    private int? _returnId;
    private ReturnImpactSummary _impact = new();

    // Currency fields
    private int? _selectedCurrencyId;
    private decimal? _exchangeRate;
    private bool _isForeignCurrency;
    private ObservableCollection<CurrencyDto> _currencies = new();


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
        _currencyService = App.GetService<ICurrencyApiService>();
        _toastService = App.GetService<IToastNotificationService>();
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
        SearchSupplierCommand = new AsyncRelayCommand(SearchSupplierAsync);
        PrintA4Command = new AsyncRelayCommand(PrintA4Async);
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

    public PurchaseInvoiceDto? SelectedInvoice
    {
        get => _selectedInvoice;
        set
        {
            if (SetProperty(ref _selectedInvoice, value))
            {
                if (value != null)
                {
                    SearchText = value.Id.ToString();
                    IsLinkedToInvoice = true;
                    SelectedSupplierId = value.SupplierId;
                    SelectedSupplierName = value.SupplierName;
                }
                UpdateItemsFromInvoice();
            }
        }
    }

    public int SelectedSupplierId
    {
        get => _selectedSupplierId;
        set => SetProperty(ref _selectedSupplierId, value);
    }

    public string SelectedSupplierName
    {
        get => _selectedSupplierName;
        set => SetProperty(ref _selectedSupplierName, value);
    }

    public bool IsLinkedToInvoice
    {
        get => _isLinkedToInvoice;
        set => SetProperty(ref _isLinkedToInvoice, value);
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

    // Currency properties
    public ObservableCollection<CurrencyDto> Currencies
    {
        get => _currencies;
        set => SetProperty(ref _currencies, value);
    }

    public int? SelectedCurrencyId
    {
        get => _selectedCurrencyId;
        set
        {
            if (SetProperty(ref _selectedCurrencyId, value))
            {
                IsForeignCurrency = value.HasValue && value.Value != GetBaseCurrencyId();
                OnPropertyChanged(nameof(IsForeignCurrency));
            }
        }
    }

    public decimal? ExchangeRate
    {
        get => _exchangeRate;
        set => SetProperty(ref _exchangeRate, value);
    }

    public bool IsForeignCurrency
    {
        get => _isForeignCurrency;
        set => SetProperty(ref _isForeignCurrency, value);
    }

    #endregion

    #region Commands
    public ICommand SaveCommand { get; private set; } = null!;
    public ICommand PostCommand { get; private set; } = null!;
    public ICommand CancelReturnCommand { get; private set; } = null!;
    public ICommand CancelCommand { get; private set; } = null!;
    public ICommand SearchInvoiceCommand { get; private set; } = null!;
    public ICommand SearchSupplierCommand { get; private set; } = null!;
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

    private async Task SearchSupplierAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            await _dialogService.ShowWarningAsync("بحث عن مورد", "يرجى إدخال اسم المورد أو رقمه في حقل البحث");
            return;
        }

        await ExecuteAsync(async () =>
        {
            ErrorMessage = null;
            var supplierService = App.GetService<ISupplierApiService>();
            var result = await supplierService.GetAllAsync();

            if (result.IsSuccess && result.Value != null)
            {
                var search = SearchText.Trim().ToLowerInvariant();
                var matches = result.Value
                    .Where(s => s.Name.ToLowerInvariant().Contains(search) || s.Id.ToString() == search)
                    .ToList();

                if (matches.Count == 1)
                {
                    var supplier = matches[0];
                    InvokeOnUIThread(() =>
                    {
                        SelectedSupplierId = supplier.Id;
                        SelectedSupplierName = supplier.Name;
                        SearchText = string.Empty;
                        if (SelectedInvoice != null && SelectedInvoice.SupplierId != supplier.Id)
                        {
                            SelectedInvoice = null; // Clear invoice if supplier changed
                        }
                    });
                }
                else if (matches.Count > 1)
                {
                    // Multiple matches — show first match
                    var supplier = matches.First();
                    InvokeOnUIThread(() =>
                    {
                        SelectedSupplierId = supplier.Id;
                        SelectedSupplierName = supplier.Name;
                        SearchText = string.Empty;
                    });
                }
                else
                {
                    InvokeOnUIThread(() =>
                    {
                        ErrorMessage = $"لم يتم العثور على مورد: {SearchText}";
                    });
                }
            }
            else
            {
                InvokeOnUIThread(() =>
                {
                    ErrorMessage = result.Error ?? "فشل في البحث عن المورد";
                });
            }
        });
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

                if (dto.PurchaseInvoiceId.HasValue)
                {
                    var invResult = await _invoiceService.GetByIdAsync(dto.PurchaseInvoiceId.Value);
                    if (invResult.IsSuccess) SelectedInvoice = invResult.Value;
                }
                else
                {
                    // Standalone return — set supplier from DTO
                    SelectedSupplierId = dto.SupplierId;
                    SelectedSupplierName = dto.SupplierName;
                    IsLinkedToInvoice = false;
                }

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

            var currenciesResult = await _currencyService.GetAllAsync();
            if (currenciesResult.IsSuccess && currenciesResult.Value != null)
            {
                Currencies = new ObservableCollection<CurrencyDto>(currenciesResult.Value);
                var baseCurrency = Currencies.FirstOrDefault(c => c.IsBaseCurrency);
                if (baseCurrency != null)
                    SelectedCurrencyId = baseCurrency.Id;
            }
        });
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
            CounterpartyName = SelectedInvoice?.SupplierName ?? SelectedSupplierName,
            CounterpartyType = "المورد",
            BalanceImpact = TotalAmount,
            TaxImpact = Items.Sum(i => {
                var lineTotal = i.LineTotal;
                return lineTotal * 0.15m / 1.15m; 
            })
        };

        Impact = summary;
    }

    private async Task<bool> Validate()
    {
        if (IsReadOnly)
        {
            await _dialogService.ShowValidationErrorsAsync("بيانات غير مكتملة", new List<string> { "الفاتورة ليست في حالة مسودة ولا يمكن تعديلها" });
            RequestFocusFirstInvalidField();
            return false;
        }
        var errors = new List<string>();
        if (SelectedInvoice == null && SelectedSupplierId <= 0)
        {
            errors.Add("يرجى اختيار فاتورة المشتريات أو اختيار المورد للمرتجع المستقل");
        }
        if (SelectedWarehouseId <= 0)
        {
            errors.Add("يرجى اختيار المستودع");
        }
        if (!Items.Any(i => i.ReturnQuantity > 0))
        {
            errors.Add("يرجى إدخال كميات المرتجع");
        }
        if (errors.Any())
        {
            await _dialogService.ShowValidationErrorsAsync("بيانات غير مكتملة", errors);
            RequestFocusFirstInvalidField();
            return false;
        }
        return true;
    }

    private async Task SaveAsync()
    {
        if (!await Validate()) return;

        await ExecuteAsync(async () =>
        {
            ErrorMessage = null;
            var returnItems = Items.Where(i => i.ReturnQuantity > 0).Select(i => new CreatePurchaseReturnItemRequest(
                ProductId: i.ProductId,
                ProductUnitId: i.ProductUnitId,
                Quantity: i.ReturnQuantity,
                UnitCost: i.UnitPrice
            )).ToList();

            var supplierId = SelectedInvoice?.SupplierId ?? SelectedSupplierId;
            if (supplierId <= 0)
            {
                ErrorMessage = "يرجى اختيار المورد";
                await _dialogService.ShowErrorAsync("خطأ في الحفظ", ErrorMessage);
                return;
            }

            var request = new CreatePurchaseReturnRequest(
                PurchaseInvoiceId: SelectedInvoice?.Id,
                SupplierId: supplierId,
                WarehouseId: SelectedWarehouseId,
                ReturnDate: ReturnDate,
                CurrencyId: SelectedCurrencyId,
                ExchangeRate: ExchangeRate,
                Notes: Notes,
                Items: returnItems
            );

            var result = await _returnService.CreateAsync(request);

            if (result.IsSuccess)
            {
                _eventBus.Publish(new PurchaseReturnChangedMessage(result.Value!.Id));
                _toastService.ShowSuccess("تم إنشاء مرتجع المشتريات بنجاح");
                RequestClose();
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشل في حفظ المرتجع", "PurchaseReturnEditorViewModel.SaveAsync", "[PurchaseReturnEditorViewModel.SaveAsync] Failed to save purchase return.");
                await _dialogService.ShowErrorAsync("خطأ في حفظ المرتجع", ErrorMessage);
            }
        });
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

        await ExecuteAsync(async () =>
        {
            ErrorMessage = null;
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
                await _dialogService.ShowErrorAsync("خطأ في الترحيل", ErrorMessage);
            }
        });
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
                _eventBus.Publish(new PurchaseReturnChangedMessage(_returnId.Value));
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
        if (!_returnId.HasValue) return;

        await ExecuteAsync(async () =>
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

    private int GetBaseCurrencyId()
    {
        var baseCurrency = Currencies.FirstOrDefault(c => c.IsBaseCurrency);
        return baseCurrency?.Id ?? 0;
    }
    #endregion
}

public class PurchaseReturnItemViewModel : ViewModelBase
{
    private decimal _returnQuantity;
    private readonly ISoundService? _soundService;

    public int ProductId { get; }
    public int ProductUnitId { get; }
    public string ProductName { get; }
    public decimal OriginalQuantity { get; }
    public decimal UnitPrice { get; }

    public decimal LineTotal => ReturnQuantity * UnitPrice;

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
        ProductUnitId = item.ProductUnitId;
        ProductName = item.ProductName;
        OriginalQuantity = item.Quantity;
        UnitPrice = item.UnitCost;
        _returnQuantity = 0;
        _soundService = soundService;
    }

    public PurchaseReturnItemViewModel(PurchaseReturnItemDto item, ISoundService? soundService = null)
    {
        ProductId = item.ProductId;
        ProductUnitId = item.ProductUnitId;
        ProductName = item.ProductName;
        OriginalQuantity = item.Quantity;
        UnitPrice = item.UnitCost;
        _returnQuantity = item.Quantity;
        _soundService = soundService;
    }
}
