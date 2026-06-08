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
    private readonly ILogger<PurchaseReturnEditorViewModel> _logger;
    private readonly ISoundService _soundService;
    private readonly IDialogService _dialogService;
    private readonly ICurrencyApiService _currencyService;
    private readonly ISupplierApiService _supplierService;

    private DateTime _returnDate = DateTime.Now;
    private string _notes = string.Empty;
    private int _selectedWarehouseId;
    private PurchaseInvoiceDto? _selectedInvoice;
    private ObservableCollection<WarehouseDto> _warehouses = new();
    private ObservableCollection<PurchaseReturnItemViewModel> _items = new();
    private string _searchText = string.Empty;
    private bool _isEditMode;
    private string? _errorMessage;
    private InvoiceStatus _status = InvoiceStatus.Draft;
    private int? _returnId;
    private ReturnImpactSummary _impact = new();

    private ObservableCollection<CurrencyDto> _currencies = new();
    private int? _selectedCurrencyId;
    private decimal _exchangeRate = 1.0m;
    private string? _currencyName;
    private byte _selectedDiscountType;
    private decimal? _discountRate;
    private bool _isStandaloneMode;
    private int? _standaloneSupplierId;
    private ObservableCollection<SupplierDto> _suppliers = new();
    private string? _standaloneProductName;
    private decimal _standaloneUnitPrice;

    public PurchaseReturnEditorViewModel()
    {
        _returnService = App.GetService<IPurchaseReturnApiService>();
        _invoiceService = App.GetService<IPurchaseInvoiceApiService>();
        _warehouseService = App.GetService<IWarehouseApiService>();
        _settingsService = App.GetService<ISettingsApiService>();
        _invoicePrinter = App.GetService<IInvoicePrinter>();
        _eventBus = App.GetService<IEventBus>();
        _soundService = App.GetService<ISoundService>();
        _logger = App.GetService<ILogger<PurchaseReturnEditorViewModel>>();
        _dialogService = App.GetService<IDialogService>();
        _currencyService = App.GetService<ICurrencyApiService>();
        _supplierService = App.GetService<ISupplierApiService>();
        SetDialogService(_dialogService);

        InitializeCommands();
        _ = LoadInitialDataAsync();
    }

    public PurchaseReturnEditorViewModel(
        IPurchaseReturnApiService returnService,
        IPurchaseInvoiceApiService invoiceService,
        IWarehouseApiService warehouseService,
        ISettingsApiService settingsService,
        IInvoicePrinter invoicePrinter,
        IEventBus eventBus,
        ILogger<PurchaseReturnEditorViewModel> logger,
        ISoundService soundService,
        IDialogService dialogService,
        ICurrencyApiService currencyService,
        ISupplierApiService supplierService)
    {
        _returnService = returnService;
        _invoiceService = invoiceService;
        _warehouseService = warehouseService;
        _settingsService = settingsService;
        _invoicePrinter = invoicePrinter;
        _eventBus = eventBus;
        _logger = logger;
        _soundService = soundService;
        _dialogService = dialogService;
        _currencyService = currencyService;
        _supplierService = supplierService;
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
        ProcessBarcodeCommand = new AsyncRelayCommand(async () =>
        {
            var code = SearchText;
            SearchText = string.Empty;
            if (!string.IsNullOrWhiteSpace(code))
            {
                await ProcessBarcodeAsync(code);
            }
        });
        ToggleStandaloneModeCommand = new RelayCommand(ToggleStandaloneMode);
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
                UpdateCurrencyDisplay();
            }
        }
    }

    public decimal ExchangeRate
    {
        get => _exchangeRate;
        set => SetProperty(ref _exchangeRate, value);
    }

    public string CurrencyName
    {
        get => _currencyName ?? "ريال سعودي";
        private set => SetProperty(ref _currencyName, value);
    }

    public bool IsBaseCurrency => SelectedCurrencyId == GetBaseCurrencyId();

    public byte SelectedDiscountType
    {
        get => _selectedDiscountType;
        set => SetProperty(ref _selectedDiscountType, value);
    }

    public decimal? DiscountRate
    {
        get => _discountRate;
        set => SetProperty(ref _discountRate, value);
    }

    public List<DiscountOption> DiscountTypeOptions { get; } = new()
    {
        new DiscountOption { Value = 0, Display = "مبلغ" },
        new DiscountOption { Value = 1, Display = "نسبة مئوية" }
    };

    public bool IsStandaloneMode
    {
        get => _isStandaloneMode;
        set
        {
            if (SetProperty(ref _isStandaloneMode, value))
            {
                OnPropertyChanged(nameof(IsInvoiceLinked));
            }
        }
    }

    public bool IsInvoiceLinked => !IsStandaloneMode;

    public ObservableCollection<SupplierDto> Suppliers
    {
        get => _suppliers;
        set => SetProperty(ref _suppliers, value);
    }

    public int? StandaloneSupplierId
    {
        get => _standaloneSupplierId;
        set => SetProperty(ref _standaloneSupplierId, value);
    }

    public string? StandaloneProductName
    {
        get => _standaloneProductName;
        set => SetProperty(ref _standaloneProductName, value);
    }

    public decimal StandaloneUnitPrice
    {
        get => _standaloneUnitPrice;
        set => SetProperty(ref _standaloneUnitPrice, value);
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
    public ICommand ToggleStandaloneModeCommand { get; private set; } = null!;
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
                SelectedCurrencyId = dto.CurrencyId;
                ExchangeRate = dto.ExchangeRate ?? 1.0m;
                if (dto.DiscountType.HasValue) SelectedDiscountType = dto.DiscountType.Value;
                DiscountRate = dto.DiscountRate;
                IsStandaloneMode = !dto.LinkToInvoice;

                if (dto.PurchaseInvoiceId.HasValue)
                {
                    var invResult = await _invoiceService.GetByIdAsync(dto.PurchaseInvoiceId.Value);
                    if (invResult.IsSuccess) SelectedInvoice = invResult.Value;
                }

                if (!dto.LinkToInvoice)
                {
                    StandaloneSupplierId = dto.SupplierId;
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

            var currenciesResult = await _currencyService.GetAllAsync(true);
            if (currenciesResult.IsSuccess && currenciesResult.Value != null)
            {
                Currencies = new ObservableCollection<CurrencyDto>(currenciesResult.Value);
                var baseCurrency = Currencies.FirstOrDefault(c => c.IsBaseCurrency) ?? Currencies.FirstOrDefault();
                if (baseCurrency != null)
                {
                    SelectedCurrencyId = baseCurrency.Id;
                    CurrencyName = baseCurrency.Name;
                    ExchangeRate = baseCurrency.ExchangeRateToBase;
                }
            }

            var suppliersResult = await _supplierService.GetAllAsync();
            if (suppliersResult.IsSuccess && suppliersResult.Value != null)
            {
                Suppliers = new ObservableCollection<SupplierDto>(suppliersResult.Value);
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
            CounterpartyName = IsStandaloneMode
                ? (Suppliers.FirstOrDefault(s => s.Id == StandaloneSupplierId)?.Name ?? "غير محدد")
                : (SelectedInvoice?.SupplierName ?? "غير محدد"),
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
        if (!IsStandaloneMode && SelectedInvoice == null)
        {
            await _dialogService.ShowValidationErrorsAsync("بيانات غير مكتملة", new List<string> { "يرجى اختيار فاتورة المشتريات أولاً" });
            RequestFocusFirstInvalidField();
            return false;
        }
        if (IsStandaloneMode && (!StandaloneSupplierId.HasValue || StandaloneSupplierId.Value <= 0))
        {
            await _dialogService.ShowValidationErrorsAsync("بيانات غير مكتملة", new List<string> { "يرجى اختيار المورد" });
            RequestFocusFirstInvalidField();
            return false;
        }
        if (SelectedWarehouseId <= 0)
        {
            await _dialogService.ShowValidationErrorsAsync("بيانات غير مكتملة", new List<string> { "يرجى اختيار المستودع" });
            RequestFocusFirstInvalidField();
            return false;
        }
        if (!Items.Any(i => i.ReturnQuantity > 0))
        {
            await _dialogService.ShowValidationErrorsAsync("بيانات غير مكتملة", new List<string> { "يرجى إدخال كميات المرتجع" });
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
            var request = BuildRequest();

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
                await _dialogService.ShowErrorAsync("خطأ في حفظ المرتجع", ErrorMessage);
            }
        });
    }

    private CreatePurchaseReturnRequest BuildRequest()
    {
        decimal discountAmount = 0;
        if (SelectedDiscountType == 1 && DiscountRate.HasValue)
        {
            discountAmount = TotalAmount * DiscountRate.Value / 100m;
        }

        return new CreatePurchaseReturnRequest(
            PurchaseInvoiceId: IsStandaloneMode ? null : SelectedInvoice?.Id,
            SupplierId: IsStandaloneMode ? (StandaloneSupplierId ?? 0) : (SelectedInvoice?.SupplierId ?? 0),
            WarehouseId: SelectedWarehouseId,
            ReturnDate: ReturnDate,
            CurrencyId: SelectedCurrencyId,
            ExchangeRate: ExchangeRate != 1.0m ? ExchangeRate : null,
            DiscountAmount: discountAmount,
            DiscountType: SelectedDiscountType > 0 ? SelectedDiscountType : (byte?)null,
            DiscountRate: DiscountRate,
            Notes: Notes,
            Items: Items.Where(i => i.ReturnQuantity > 0).Select(i => new ReturnItemRequest(
                ProductId: i.ProductId,
                ProductUnitId: i.ProductUnitId,
                Quantity: i.ReturnQuantity,
                UnitPrice: i.UnitPrice,
                DiscountAmount: i.DiscountAmount,
                Mode: i.Mode,
                Notes: i.Notes
            )).ToList()
        );
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

        if (!IsStandaloneMode && SelectedInvoice == null)
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

        var item = Items.FirstOrDefault(i => i.ProductName.Contains(barcode) || i.ProductId.ToString() == barcode);
        if (item != null)
        {
            if (item.ReturnQuantity < item.OriginalQuantity)
            {
                item.ReturnQuantity += 1;
                SearchText = string.Empty;
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

    private void ToggleStandaloneMode()
    {
        IsStandaloneMode = !IsStandaloneMode;
        if (IsStandaloneMode)
        {
            SelectedInvoice = null;
            Items.Clear();
        }
    }

    private void UpdateCurrencyDisplay()
    {
        if (_selectedCurrencyId.HasValue && _currencies != null)
        {
            var currency = _currencies.FirstOrDefault(c => c.Id == _selectedCurrencyId.Value);
            if (currency != null)
            {
                CurrencyName = currency.Name;
                ExchangeRate = currency.ExchangeRateToBase;
            }
        }
        OnPropertyChanged(nameof(IsBaseCurrency));
        OnPropertyChanged(nameof(CurrencyName));
    }

    private int? GetBaseCurrencyId()
    {
        return _currencies?.FirstOrDefault(c => c.IsBaseCurrency)?.Id;
    }
    #endregion
}

public class PurchaseReturnItemViewModel : ViewModelBase
{
    private decimal _returnQuantity;
    private byte _mode = 1;
    private readonly ISoundService? _soundService;

    public int ProductId { get; }
    public int ProductUnitId { get; }
    public string ProductName { get; }
    public decimal OriginalQuantity { get; }
    public decimal UnitPrice { get; }
    public decimal DiscountAmount { get; }
    public byte Mode => _mode;
    public string? Notes { get; set; }

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
        ProductUnitId = item.ProductUnitId;
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
        ProductUnitId = item.ProductUnitId;
        ProductName = item.ProductName;
        OriginalQuantity = item.Quantity;
        UnitPrice = item.UnitCost;
        DiscountAmount = item.DiscountAmount;
        _mode = item.Mode;
        _returnQuantity = item.Quantity;
        _soundService = soundService;
    }
}

public class DiscountOption
{
    public byte Value { get; set; }
    public string Display { get; set; } = string.Empty;
}
