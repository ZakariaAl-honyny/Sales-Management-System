using SalesSystem.DesktopPWF.Messaging.Messages;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Enums;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Services.App.Toast;
using SalesSystem.DesktopPWF.ViewModels;
using SalesSystem.DesktopPWF.Helpers;
using SalesSystem.DesktopPWF.Models.Printing;
using SalesSystem.DesktopPWF.Common;

namespace SalesSystem.DesktopPWF.ViewModels.Sales;

/// <summary>
/// ViewModel for Sales Invoice Editor
/// </summary>
public class SalesInvoiceEditorViewModel : ViewModelBase
{
    private readonly ISalesInvoiceApiService _invoiceService;
    private readonly IEventBus _eventBus;
    private readonly ICustomerApiService _customerService;
    private readonly IWarehouseApiService _warehouseService;
    private readonly IProductApiService _productService;
    private readonly ISettingsApiService _settingsService;
    private readonly IDialogService _dialogService;
    private readonly ISoundService _soundService;
    private readonly IInventoryApiService _inventoryService;
    private readonly IBarcodeInputService _barcodeService;
    private readonly ICashBoxApiService _cashBoxService;
    private readonly IPrintApiService _printApiService;
    private readonly IToastNotificationService _toastService;
    private readonly ICurrencyApiService _currencyService;
    private readonly IProductCategoryApiService _productCategoryService;
    private readonly IProductUnitApiService _unitService;
    private readonly IProductPriceApiService _priceService;
    private readonly ITaxesApiService? _taxService;
    private readonly Dictionary<int, List<ProductUnitDto>> _productUnitsCache = new();
    private readonly Dictionary<int, List<ProductPriceDto>> _productPricesCache = new();
    private int? _invoiceId;
    private int _invoiceNo;
    private int _selectedWarehouseId;
    private int? _selectedCustomerId;
    private int? _defaultCustomerId;  // auto-selected for Cash sales
    private DateTime _invoiceDate = DateTime.Today;
    private byte _paymentType = (byte)PaymentType.Cash;
    private decimal _invoiceDiscount;
    private decimal _taxRate = 15;
    private bool _isTaxInclusive;
    private decimal _paidAmount;
    private decimal _otherCharges;
    private string? _notes;
    private string _barcodeSearchText = string.Empty;
    private bool _isEditMode;
    private string? _errorMessage;
    private bool _allowNegativeStock;
    private byte _status = (byte)InvoiceStatus.Draft;
    public bool IsReadOnly { get; private set; }

    // Currency fields
    private int? _selectedCurrencyId;
    private decimal? _exchangeRate;
    private bool _isForeignCurrency;

    private ObservableCollection<CurrencyDto> _currencies = new();
    private ObservableCollection<InvoiceLineViewModel> _items = new();
    private ObservableCollection<CustomerDto> _customers = new();
    private ObservableCollection<WarehouseDto> _warehouses = new();
    private ObservableCollection<ProductDto> _products = new();
    private ObservableCollection<CashBoxDto> _cashBoxes = new();
    private CashBoxDto? _selectedCashBox;
    private ObservableCollection<TaxDto> _taxes = new();
    private int? _taxId;

    public TouchPosViewModel? TouchPosVM { get; private set; }
    public TouchPosCartViewModel? TouchPosCartVM { get; private set; }

    public enum SalesViewMode
    {
        Standard,
        Touch
    }

    private SalesViewMode _currentViewMode;
    public SalesViewMode CurrentViewMode
    {
        get => _currentViewMode;
        set => SetProperty(ref _currentViewMode, value);
    }

    public SalesInvoiceEditorViewModel(
        ISalesInvoiceApiService invoiceService,
        IEventBus eventBus,
        ICustomerApiService customerService,
        IWarehouseApiService warehouseService,
        IProductApiService productService,
        ISettingsApiService settingsService,
        IDialogService dialogService,
        ISoundService soundService,
        IInventoryApiService inventoryService,
        IBarcodeInputService barcodeService,
        ICashBoxApiService cashBoxService,
        IPrintApiService printApiService,
        IToastNotificationService toastService,
        ICurrencyApiService currencyService,
        IProductCategoryApiService productCategoryService,
        IProductUnitApiService unitService,
        IProductPriceApiService priceService,
        ITaxesApiService? taxService = null,
        int? invoiceId = null,
        bool isReadOnly = false)
    {
        _invoiceService = invoiceService;
        _eventBus = eventBus;
        _customerService = customerService;
        _warehouseService = warehouseService;
        _productService = productService;
        _settingsService = settingsService;
        _dialogService = dialogService;
        SetDialogService(dialogService);
        _soundService = soundService;
        _inventoryService = inventoryService;
        _barcodeService = barcodeService;
        _cashBoxService = cashBoxService;
        _printApiService = printApiService;
        _toastService = toastService;
        _currencyService = currencyService ?? throw new ArgumentNullException(nameof(currencyService));
        _productCategoryService = productCategoryService ?? throw new ArgumentNullException(nameof(productCategoryService));
        _unitService = unitService ?? throw new ArgumentNullException(nameof(unitService));
        _priceService = priceService ?? throw new ArgumentNullException(nameof(priceService));
        _taxService = taxService;
        _invoiceId = invoiceId;
        _isEditMode = invoiceId.HasValue;

        if (!invoiceId.HasValue)
        {
            InvoiceNo = 0; // Service will compute lastId + 1
        }
        IsReadOnly = isReadOnly;

        SaveCommand = new AsyncRelayCommand(SaveAsync);
        PostCommand = new AsyncRelayCommand(PostAsync);
        CancelCommand = new RelayCommand(Cancel);
        AddLineCommand = new RelayCommand(AddLine);
        RemoveLineCommand = new RelayCommand(RemoveLine, CanRemoveLine);
        PrintA4Command = new AsyncRelayCommand(PrintA4Async);
        PrintThermalCommand = new AsyncRelayCommand(PrintThermalAsync);
        SearchProductCommand = new RelayCommand(SearchProduct);
        SearchProductSingleCommand = new RelayCommand(SearchProductSingle);
        SearchCustomerCommand = new RelayCommand(SearchCustomer);
        ProcessBarcodeCommand = new AsyncRelayCommand(async () => 
        {
            var code = BarcodeSearchText;
            BarcodeSearchText = string.Empty;
            if (!string.IsNullOrWhiteSpace(code))
            {
                await ProcessBarcodeAsync(code);
            }
        });
        DeleteCommand = new AsyncRelayCommand(DeleteAsync, () => IsEditMode && !IsReadOnly);
        ToggleViewModeCommand = new RelayCommand(ToggleViewMode);
        StartContinuousScanCommand = new AsyncRelayCommand(StartContinuousScanAsync);

        // --- Touch POS ViewModels ---
        TouchPosVM = new TouchPosViewModel(_productCategoryService, _productService, _inventoryService);
        TouchPosCartVM = new TouchPosCartViewModel(Items, RecalculateTotals);

        // Wire: when a product is selected in Touch POS, validate stock then add to cart
        TouchPosVM.OnProductSelected = async product =>
        {
            // 1. Validate Warehouse Selection
            if (SelectedWarehouseId <= 0)
            {
                await _dialogService.ShowWarningAsync("تنبيه", "يجب اختيار المستودع أولاً");
                return;
            }

            // 2. Check Stock in Selected Warehouse (reuse barcode pattern)
            var stockResult = await _inventoryService.GetStockAsync(product.Id, SelectedWarehouseId);
            decimal currentStock = stockResult.IsSuccess ? stockResult.Value : 0;

            // Check if product is already in the cart
            var existingLine = Items.FirstOrDefault(i => i.ProductId == product.Id);
            decimal neededQuantity = (existingLine?.Quantity ?? 0) + 1m;

            if (!_allowNegativeStock && currentStock < neededQuantity)
            {
                _soundService.PlayWarning();
                if (currentStock <= 0)
                {
                    await _dialogService.ShowErrorAsync("المخزون نفذ",
                        $"المنتج: {product.Name}\nهذا المنتج نفذ من المخزون في المستودع الحالي.");
                }
                else
                {
                    await _dialogService.ShowErrorAsync("المخزون غير كافٍ",
                        $"المنتج: {product.Name}\nالمتوفر في المستودع: {currentStock:N0}\nالمطلوب: {neededQuantity:N0}");
                }
                return;
            }

            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (existingLine != null)
                {
                    existingLine.Quantity += 1m;
                    if (!existingLine.CostInBaseCurrency.HasValue)
                    {
                        existingLine.CostInBaseCurrency = 0m;
                    }
                    _soundService.PlaySuccess();
                }
                else
                {
                    var line = new InvoiceLineViewModel(Products, _soundService, _unitService, _priceService);
                    line.SetPriceFromProduct(product);
                    line.Quantity = 1m;
                    line.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName is nameof(InvoiceLineViewModel.LineTotal)
                            or nameof(InvoiceLineViewModel.Profit))
                            RecalculateTotals();
                    };
                    Items.Add(line);
                    
                    // Load stock display for this line
                    _ = LoadStockForLineAsync(line, product.Id);
                }

                OnPropertyChanged(nameof(Items));
                RecalculateTotals();
            });
        };

        // Wire: checkout delegates (call the existing SaveAsDraft/Post methods)
        TouchPosCartVM.OnCashCheckout = async paidAmount =>
        {
            if (decimal.TryParse(paidAmount, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var amount))
            {
                _paidAmount = amount;
                OnPropertyChanged(nameof(PaidAmount));
                _paymentType = (byte)PaymentType.Cash;
                OnPropertyChanged(nameof(PaymentType));
                await PostAsync();
            }
        };

        TouchPosCartVM.OnCardCheckout = async paidAmount =>
        {
            if (decimal.TryParse(paidAmount, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var amount))
            {
                _paidAmount = amount;
                OnPropertyChanged(nameof(PaidAmount));
                _paymentType = (byte)PaymentType.Credit;
                OnPropertyChanged(nameof(PaymentType));
                await PostAsync();
            }
        };

        TouchPosCartVM.OnDraftSave = async () =>
        {
            await SaveAsync();
        };

        // SaleModeOptions removed per analysis — per-unit pricing replaces جملة/تجزئة
        // SaleModeOptions = new List<EnumDisplayItem>
        // {
        //     new EnumDisplayItem { Value = (byte)SaleMode.Retail, Display = "تجزئة" },
        //     new EnumDisplayItem { Value = (byte)SaleMode.Wholesale, Display = "جملة" }
        // };

        InitializationTask = InitializeAsync();
    }

    public Task InitializationTask { get; private set; }

    private async Task InitializeAsync()
    {
        await LoadReferenceDataAsync();
        await LoadSettingsAsync();
        await LoadCashBoxesAsync();

        if (_isEditMode)
        {
            await LoadInvoiceAsync();
        }
        else
        {
            // Select default warehouse before adding line
            if (Warehouses.Any())
            {
                var defaultWarehouse = Warehouses.FirstOrDefault() ?? Warehouses.First();
                SelectedWarehouseId = defaultWarehouse.Id;
            }
            AddLine();
        }
    }

    private async Task LoadSettingsAsync()
    {
        try
        {
            var settingsResult = await _settingsService.GetSettingsAsync();
            if (settingsResult?.IsSuccess == true && settingsResult.Value != null)
            {
                _allowNegativeStock = settingsResult.Value.AllowNegativeStock;
            }
        }
        catch (Exception ex)
        {
            LogSystemError("فشل تحميل الإعدادات — استخدام القيم الافتراضية", "LoadSettingsAsync", ex);
        }
    }

    private async Task LoadCashBoxesAsync()
    {
        try
        {
            var result = await _cashBoxService.GetAllAsync();
            if (result.IsSuccess && result.Value != null)
            {
                CashBoxes = new ObservableCollection<CashBoxDto>(result.Value.Where(c => c.IsActive).OrderByDescending(x => x.Id));
            }
            else if (result.Error != null)
            {
                Serilog.Log.Warning("[SalesInvoiceEditor.LoadCashBoxesAsync] فشل في تحميل الصناديق النقدية: {Error}", result.Error);
            }
        }
        catch (Exception ex)
        {
            LogSystemError("فشل في تحميل الصناديق النقدية", "LoadCashBoxesAsync", ex);
        }
    }

    public SalesInvoiceEditorViewModel(int? invoiceId = null, bool isReadOnly = false)
        : this(
            App.GetService<ISalesInvoiceApiService>(),
            App.GetService<IEventBus>(),
            App.GetService<ICustomerApiService>(),
            App.GetService<IWarehouseApiService>(),
            App.GetService<IProductApiService>(),
            App.GetService<ISettingsApiService>(),
            App.GetService<IDialogService>(),
            App.GetService<ISoundService>(),
            App.GetService<IInventoryApiService>(),
            App.GetService<IBarcodeInputService>(),
            App.GetService<ICashBoxApiService>(),
            App.GetService<IPrintApiService>(),
            App.GetService<IToastNotificationService>(),
            App.GetService<ICurrencyApiService>(),
            App.GetService<IProductCategoryApiService>(),
            App.GetService<IProductUnitApiService>(),
            App.GetService<IProductPriceApiService>(),
            App.GetService<ITaxesApiService>(),
            invoiceId,
            isReadOnly)
    {
    }

    #region Properties
    public int? InvoiceId => _invoiceId;

    public int InvoiceNo
    {
        get => _invoiceNo;
        set => SetProperty(ref _invoiceNo, value);
    }

    public bool IsEditMode => _isEditMode;

    public ObservableCollection<CustomerDto> Customers
    {
        get => _customers;
        set => SetProperty(ref _customers, value);
    }

    public ObservableCollection<WarehouseDto> Warehouses
    {
        get => _warehouses;
        set => SetProperty(ref _warehouses, value);
    }

    public ObservableCollection<ProductDto> Products
    {
        get => _products;
        set => SetProperty(ref _products, value);
    }

    public ObservableCollection<CashBoxDto> CashBoxes
    {
        get => _cashBoxes;
        set => SetProperty(ref _cashBoxes, value);
    }

    public CashBoxDto? SelectedCashBox
    {
        get => _selectedCashBox;
        set => SetProperty(ref _selectedCashBox, value);
    }

    public ObservableCollection<InvoiceLineViewModel> Items
    {
        get => _items;
        set => SetProperty(ref _items, value);
    }

    public int SelectedWarehouseId
    {
        get => _selectedWarehouseId;
        set
        {
            if (SetProperty(ref _selectedWarehouseId, value))
            {
                if (TouchPosVM != null)
                    TouchPosVM.WarehouseId = value;
                _ = RefreshStockForAllLinesAsync();
                UpdateCommandStates();
            }
        }
    }

    public int? SelectedCustomerId
    {
        get => _selectedCustomerId;
        set
        {
            if (SetProperty(ref _selectedCustomerId, value))
            {
                UpdateCommandStates();
            }
        }
    }

    public DateTime InvoiceDate
    {
        get => _invoiceDate;
        set => SetProperty(ref _invoiceDate, value);
    }

    public byte SelectedPaymentType
    {
        get => _paymentType;
        set
        {
            if (SetProperty(ref _paymentType, value))
            {
                OnPropertyChanged(nameof(CanSelectCustomer));
                if (value == (byte)PaymentType.Cash)
                {
                    // Auto-select default cash customer
                    SelectedCustomerId = _defaultCustomerId;
                }
                else
                {
                    SelectedCustomerId = null;
                }
                RecalculateTotals();
            }
        }
    }

    public bool CanSelectCustomer => SelectedPaymentType != (byte)PaymentType.Cash;

    public decimal InvoiceDiscount
    {
        get => _invoiceDiscount;
        set
        {
            if (SetProperty(ref _invoiceDiscount, value))
            {
                RecalculateTotals();
            }
        }
    }

    public decimal TaxRate
    {
        get => _taxRate;
        set
        {
            if (SetProperty(ref _taxRate, value))
            {
                RecalculateTotals();
            }
        }
    }

    public bool IsTaxInclusive
    {
        get => _isTaxInclusive;
        set
        {
            if (SetProperty(ref _isTaxInclusive, value))
            {
                RecalculateTotals();
            }
        }
    }

    private string? _taxNumber;
    public string? TaxNumber
    {
        get => _taxNumber;
        set => SetProperty(ref _taxNumber, value);
    }

    public decimal PaidAmount
    {
        get => _paidAmount;
        set
        {
            if (SetProperty(ref _paidAmount, value))
            {
                RecalculateTotals();
            }
        }
    }

    public decimal OtherCharges
    {
        get => _otherCharges;
        set
        {
            if (SetProperty(ref _otherCharges, value))
            {
                RecalculateTotals();
            }
        }
    }

    public string? Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }

    public string BarcodeSearchText
    {
        get => _barcodeSearchText;
        set => SetProperty(ref _barcodeSearchText, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    // Calculated properties
    private decimal _subTotal;
    public decimal SubTotal
    {
        get => _subTotal;
        private set => SetProperty(ref _subTotal, value);
    }

    private decimal _taxAmount;
    public decimal TaxAmount
    {
        get => _taxAmount;
        private set => SetProperty(ref _taxAmount, value);
    }

    private decimal _totalAmount;
    public decimal TotalAmount
    {
        get => _totalAmount;
        private set => SetProperty(ref _totalAmount, value);
    }

    private decimal _dueAmount;
    public decimal DueAmount
    {
        get => _dueAmount;
        private set => SetProperty(ref _dueAmount, value);
    }

    public byte Status
    {
        get => _status;
        set
        {
            if (SetProperty(ref _status, value))
            {
                UpdateCommandStates();
            }
        }
    }

    // Payment type options
    public List<PaymentTypeItem> PaymentTypeOptions { get; } = new()
    {
        new PaymentTypeItem { Value = 1, Display = "نقدي" },
        new PaymentTypeItem { Value = 2, Display = "آجل" },
        new PaymentTypeItem { Value = 3, Display = "مختلط" }
    };

    // SaleModeOptions removed — pricing is per-unit, no جملة/تجزئة concept
    // public List<EnumDisplayItem> SaleModeOptions { get; }

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
        set
        {
            if (SetProperty(ref _exchangeRate, value))
                RecalculateTotals();
        }
    }

    public bool IsForeignCurrency
    {
        get => _isForeignCurrency;
        set => SetProperty(ref _isForeignCurrency, value);
    }

    // Tax properties
    public ObservableCollection<TaxDto> Taxes
    {
        get => _taxes;
        set => SetProperty(ref _taxes, value);
    }

    public int? TaxId
    {
        get => _taxId;
        set
        {
            if (SetProperty(ref _taxId, value))
            {
                if (value.HasValue)
                {
                    var selectedTax = Taxes.FirstOrDefault(t => t.Id == value.Value);
                    if (selectedTax != null)
                    {
                        TaxRate = selectedTax.Rate;
                    }
                }
            }
        }
    }
    #endregion

    #region Commands
    public ICommand SaveCommand { get; }
    public ICommand PostCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand AddLineCommand { get; }
    public ICommand RemoveLineCommand { get; }
    public ICommand PrintA4Command { get; }
    public ICommand PrintThermalCommand { get; }
    public ICommand SearchProductCommand { get; }          // Continuous: stays open
    public ICommand SearchProductSingleCommand { get; }    // Single: closes after one pick
    public ICommand SearchCustomerCommand { get; }
    public ICommand ProcessBarcodeCommand { get; }
    public ICommand StartContinuousScanCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand ToggleViewModeCommand { get; }
    #endregion

    #region Events
    #endregion

    #region Methods
    private async Task LoadReferenceDataAsync()
    {
        await ExecuteAsync(async () =>
        {
            ErrorMessage = null;
            var customersResult = await _customerService.GetAllAsync();
            if (customersResult.IsSuccess && customersResult.Value != null)
            {
                Customers = new ObservableCollection<CustomerDto>(customersResult.Value);
                var defaultCustomer = customersResult.Value.FirstOrDefault(c =>
                    c.Name.Contains("عميل نقدي") ||
                    c.Name.Contains("عميل افتراضي") ||
                    c.Name.Contains("نقدي") && c.Name.Length < 10);
                _defaultCustomerId = defaultCustomer?.Id;
                if (_paymentType == (byte)PaymentType.Cash && _defaultCustomerId.HasValue && !_isEditMode)
                    SelectedCustomerId = _defaultCustomerId;
            }

            var warehousesResult = await _warehouseService.GetAllAsync();
            if (warehousesResult.IsSuccess && warehousesResult.Value != null)
            {
                Warehouses = new ObservableCollection<WarehouseDto>(warehousesResult.Value);
                if (Warehouses.Any() && SelectedWarehouseId == 0)
                {
                    var defaultWarehouse = Warehouses.FirstOrDefault() ?? Warehouses.First();
                    SelectedWarehouseId = defaultWarehouse.Id;
                }
            }

            var productsResult = await _productService.GetAllAsync();
            if (productsResult.IsSuccess && productsResult.Value != null)
            {
                Products.Clear();
                foreach (var product in productsResult.Value)
                {
                    Products.Add(product);
                }
            }

            var settingsResult = await _settingsService.GetSettingsAsync();
            if (settingsResult.IsSuccess && settingsResult.Value != null)
            {
                TaxNumber = settingsResult.Value.TaxNumber;
                // TODO: Phase 20 — switch to Tax entity (DefaultTaxRate and IsTaxEnabled are deprecated)
                if (settingsResult.Value.IsTaxEnabled)
                {
                    TaxRate = settingsResult.Value.DefaultTaxRate;
                }
            }

            var currenciesResult = await _currencyService.GetAllAsync();
            if (currenciesResult.IsSuccess && currenciesResult.Value != null)
            {
                Currencies = new ObservableCollection<CurrencyDto>(currenciesResult.Value);
                if (!_isEditMode)
                {
                    var baseCurrency = Currencies.FirstOrDefault(c => c.IsBaseCurrency);
                    if (baseCurrency != null)
                        SelectedCurrencyId = baseCurrency.Id;
                }
            }

            // Load taxes
            if (_taxService != null)
            {
                var taxesResult = await _taxService.GetAllAsync();
                if (taxesResult.IsSuccess && taxesResult.Value != null)
                {
                    Taxes = new ObservableCollection<TaxDto>(taxesResult.Value);
                }
            }
        });
    }

    private async Task LoadInvoiceAsync()
    {
        if (!_invoiceId.HasValue) return;

        await ExecuteAsync(async () =>
        {
            ErrorMessage = null;
            var result = await _invoiceService.GetByIdAsync(_invoiceId.Value);
            if (result.IsSuccess && result.Value != null)
            {
                var invoice = result.Value;
                InvoiceNo = invoice.InvoiceNo;
                SelectedWarehouseId = invoice.WarehouseId;
                SelectedCustomerId = invoice.CustomerId;
                InvoiceDate = invoice.InvoiceDate;
                SelectedPaymentType = (byte)invoice.PaymentType;
                InvoiceDiscount = invoice.DiscountAmount;
                PaidAmount = invoice.PaidAmount;
                Notes = invoice.Notes;
                Status = invoice.Status;

                if (invoice.Status != (byte)InvoiceStatus.Draft)
                {
                    IsReadOnly = true;
                    OnPropertyChanged(nameof(IsReadOnly));
                }
                IsTaxInclusive = false;

                Items.Clear();
                foreach (var item in invoice.Items)
                {
                    var lineVm = new InvoiceLineViewModel(Products, _soundService, _unitService, _priceService);
                    lineVm.ProductId = item.ProductId;
                    lineVm.Quantity = item.Quantity;
                    lineVm.UnitPrice = item.UnitPrice;
                    lineVm.SelectedProduct = Products.FirstOrDefault(p => p.Id == item.ProductId);
                    lineVm.ProductUnitId = item.ProductUnitId;

                    // Also set the selected unit from the saved ProductUnitId
                    if (item.ProductUnitId > 0 && lineVm.AvailableProductUnits.Count > 0)
                    {
                        var savedUnit = lineVm.AvailableProductUnits.FirstOrDefault(u => u.Id == item.ProductUnitId);
                        if (savedUnit != null)
                            lineVm.SelectedProductUnit = savedUnit;
                    }

                    lineVm.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName is nameof(InvoiceLineViewModel.LineTotal)
                            or nameof(InvoiceLineViewModel.Profit))
                        {
                            RecalculateTotals();
                        }
                    };
                    Items.Add(lineVm);
                }

                // Restore tax
                TaxId = invoice.TaxId;

                // Restore currency
                SelectedCurrencyId = invoice.CurrencyId;
                ExchangeRate = invoice.ExchangeRate;

                OtherCharges = invoice.OtherCharges;

                // Load stock display for all lines
                _ = RefreshStockForAllLinesAsync();

                // Restore CashBoxId selection
                if (invoice.CashBoxId.HasValue)
                {
                    var cashBox = CashBoxes.FirstOrDefault(cb => cb.Id == invoice.CashBoxId.Value);
                    if (cashBox != null)
                        SelectedCashBox = cashBox;
                }

                RecalculateTotals();
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل الفاتورة", "SalesInvoiceEditorViewModel.LoadInvoiceAsync", $"[SalesInvoiceEditorViewModel.LoadInvoiceAsync] Failed to load invoice data for ID: {_invoiceId}");
            }
        });
    }

    private async Task SaveAsync()
    {
        if (!await ValidateInvoice()) return;

        // --- Stock validation before saving (warning only) ---
        if (!_allowNegativeStock && _selectedWarehouseId > 0 && Items.Count > 0)
        {
            var stockIssues = await ValidateStockBeforePostAsync();
            if (stockIssues.Count > 0)
            {
                var message = "تنبيه: بعض المنتجات ليس لديها مخزون كافٍ:\n\n";
                message += string.Join("\n", stockIssues.Select(s => s.Description));
                message += "\n\nيمكنك حفظ الفاتورة كمسودة ولكن قد لا تتمكن من ترحيلها.";
                await _dialogService.ShowWarningAsync("المخزون غير كافٍ", message);
            }
        }

        await ExecuteAsync(async () =>
        {
            ErrorMessage = null;
            Result<SalesInvoiceDto> result;
            if (_isEditMode)
            {
                var request = BuildUpdateRequest();
                result = await _invoiceService.UpdateAsync(_invoiceId!.Value, request);
            }
            else
            {
                var request = BuildRequest();
                result = await _invoiceService.CreateAsync(request);
            }

            if (result.IsSuccess && result.Value != null)
            {
                _invoiceId = result.Value.Id;
                _status = result.Value.Status;
                _isEditMode = true;
                
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(IsEditMode));
                OnPropertyChanged(nameof(IsReadOnly));
                
                await _dialogService.ShowSuccessAsync("نجاح", "✅ تم حفظ الفاتورة بنجاح. يمكنك الآن الترحيل النهائي إذا أردت.");
                _eventBus.Publish(new SaleInvoiceChangedMessage(_invoiceId.Value));
                UpdateCommandStates();
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشل في حفظ الفاتورة", "SalesInvoiceEditorViewModel.SaveAsync", "[SalesInvoiceEditorViewModel.SaveAsync] Failed to save sales invoice.");
                await _dialogService.ShowErrorAsync("خطأ في حفظ الفاتورة", ErrorMessage!);
            }
        });
    }

    private async Task PostAsync()
    {
        // First save if new
        if (!_isEditMode || _invoiceId == null)
        {
            await SaveAsync();
            if (_invoiceId == null) return;
        }

        // --- Stock validation before posting ---
        if (!_allowNegativeStock)
        {
            var stockIssues = await ValidateStockBeforePostAsync();
            if (stockIssues.Count > 0)
            {
                var criticalCount = stockIssues.Count(s => s.IsOutOfStock);
                var message = "تنبيه المخزون:\n\n";
                message += string.Join("\n", stockIssues.Select(s => s.Description));
                message += "\n\n";

                if (criticalCount > 0)
                {
                    message += $"❌ {criticalCount} منتج/منتجات نفذ من المخزون. لا يمكن متابعة الترحيل.";
                    await _dialogService.ShowErrorAsync("المخزون غير كافٍ", message);
                    return;
                }

                message += $"⚠️ {stockIssues.Count} منتج/منتجات الكمية المتوفرة أقل من المطلوب.\nهل تريد متابعة الترحيل على أي حال؟";
                var proceed = await _dialogService.ShowConfirmationAsync("المخزون غير كافٍ", message);
                if (!proceed) return;
            }
        }

        if (!await _dialogService.ShowConfirmationAsync("تأكيد الترحيل", "هل أنت متأكد من ترحيل هذه الفاتورة؟\nسيتم خصم الكميات من المخزون.")) return;

        await ExecuteAsync(async () =>
        {
            ErrorMessage = null;
            var postResult = await _invoiceService.PostAsync(_invoiceId!.Value);
            if (postResult.IsSuccess)
            {
                await _dialogService.ShowSuccessAsync("نجاح", "تم ترحيل الفاتورة بنجاح");
                _eventBus.Publish(new SaleInvoiceChangedMessage(_invoiceId.Value));
                RequestClose();
            }
            else
            {
                ErrorMessage = HandleFailure(postResult.Error ?? "فشل في ترحيل الفاتورة", "SalesInvoiceEditorViewModel.PostAsync", $"[SalesInvoiceEditorViewModel.PostAsync] Failed to post/confirm sales invoice ID {_invoiceId}.");
                await _dialogService.ShowErrorAsync("خطأ في الترحيل", ErrorMessage!);
            }
        });
        UpdateCommandStates();
    }

    private async Task DeleteAsync()
    {
        if (!_invoiceId.HasValue) return;

        if (!await _dialogService.ShowConfirmationAsync("تأكيد الإلغاء", "هل أنت متأكد من إلغاء هذه الفاتورة؟\nلا يمكن التراجع عن هذه العملية.")) return;

        await ExecuteAsync(async () =>
        {
            ErrorMessage = null;
            var cancelResult = await _invoiceService.CancelAsync(_invoiceId.Value);
            if (cancelResult.IsSuccess)
            {
                await _dialogService.ShowSuccessAsync("نجاح", "تم إلغاء الفاتورة بنجاح");
                _eventBus.Publish(new SaleInvoiceChangedMessage(_invoiceId.Value));
                RequestClose();
            }
            else
            {
                ErrorMessage = cancelResult.Error ?? "فشل في إلغاء الفاتورة";
                await _dialogService.ShowErrorAsync("خطأ في الإلغاء", ErrorMessage!);
            }
        });
    }

    private async Task PrintA4Async()
    {
        if (!_invoiceId.HasValue)
        {
            await _dialogService.ShowWarningAsync("طباعة الفاتورة", "يجب حفظ الفاتورة أولاً قبل الطباعة");
            return;
        }

        if (_status != (byte)InvoiceStatus.Posted)
        {
            await _dialogService.ShowWarningAsync("طباعة الفاتورة", "يجب ترحيل الفاتورة أولاً قبل طباعة A4");
            return;
        }

        await ExecuteAsync(async () =>
        {
            ErrorMessage = null;
            var result = await _printApiService.GetSalesA4PdfAsync(_invoiceId!.Value);
            if (result.IsSuccess && result.Value != null)
            {
                var previewWindow = new Views.Common.PdfPreviewWindow(
                    result.Value,
                    $"#{_invoiceId}",
                    _invoiceId!.Value);
                previewWindow.ShowDialog();
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل ملف PDF",
                    "SalesInvoiceEditorViewModel.PrintA4Async",
                    $"[SalesInvoiceEditorViewModel.PrintA4Async] Failed to get A4 PDF for invoice ID {_invoiceId}.");
                await _dialogService.ShowErrorAsync("خطأ في الطباعة", ErrorMessage!);
            }
        });
    }

    private async Task PrintThermalAsync()
    {
        if (!_invoiceId.HasValue)
        {
            await _dialogService.ShowWarningAsync("طباعة إيصال", "يجب حفظ الفاتورة أولاً قبل الطباعة");
            return;
        }

        if (_status != (byte)InvoiceStatus.Posted)
        {
            await _dialogService.ShowWarningAsync("طباعة إيصال", "يجب ترحيل الفاتورة أولاً قبل طباعة الإيصال الحراري");
            return;
        }

        await ExecuteAsync(async () =>
        {
            ErrorMessage = null;
            var result = await _printApiService.PrintSalesThermalAsync(_invoiceId!.Value);
            if (result.IsSuccess)
            {
                _toastService.ShowSuccess("تم إرسال الإيصال إلى الطابعة الحرارية بنجاح");
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشلت طباعة الإيصال الحراري",
                    "SalesInvoiceEditorViewModel.PrintThermalAsync",
                    $"[SalesInvoiceEditorViewModel.PrintThermalAsync] Thermal print failed for invoice ID {_invoiceId}.");
                await _dialogService.ShowErrorAsync("خطأ في الطباعة الحرارية", ErrorMessage!);
            }
        });
    }

    private void Cancel()
    {
        RequestClose();
    }

    private void AddLine()
    {
        var line = new InvoiceLineViewModel(Products, _soundService, _unitService, _priceService);
        line.PropertyChanged += async (s, e) =>
        {
            if (e.PropertyName is nameof(InvoiceLineViewModel.LineTotal)
                or nameof(InvoiceLineViewModel.Profit))
            {
                RecalculateTotals();
            }
            else if (e.PropertyName is nameof(InvoiceLineViewModel.SelectedProduct))
            {
                // Load stock display for this line
                if (line.SelectedProduct != null && SelectedWarehouseId > 0)
                {
                    _ = LoadStockForLineAsync(line, line.SelectedProduct.Id);
                }

                if (!_allowNegativeStock && line.SelectedProduct != null && SelectedWarehouseId > 0)
                {
                    var stockResult = await _inventoryService.GetStockAsync(line.SelectedProduct.Id, SelectedWarehouseId);
                    if (stockResult.IsSuccess && stockResult.Value < line.Quantity)
                    {
                        _ = _dialogService.ShowWarningAsync("تنبيه", $"المخزون غير كافٍ في المستودع المختار. المتوفر: {stockResult.Value}");
                    }
                }
            }
            else if (e.PropertyName is nameof(InvoiceLineViewModel.Quantity))
            {
                if (!_allowNegativeStock && line.SelectedProduct != null && SelectedWarehouseId > 0)
                {
                    var stockResult = await _inventoryService.GetStockAsync(line.SelectedProduct.Id, SelectedWarehouseId);
                    if (stockResult.IsSuccess && stockResult.Value < line.Quantity)
                    {
                        _ = _dialogService.ShowWarningAsync("تنبيه", $"المخزون غير كافٍ في المستودع المختار. المتوفر: {stockResult.Value}");
                    }
                }
            }
        };
        Items.Add(line);
        RecalculateTotals();
    }

    private void RemoveLine(object? parameter)
    {
        if (parameter is InvoiceLineViewModel line)
        {
            Items.Remove(line);
            RecalculateTotals();
        }
        else if (Items.Count > 0)
        {
            Items.RemoveAt(Items.Count - 1);
            RecalculateTotals();
        }
    }

    private bool CanRemoveLine(object? parameter)
    {
        return Items.Count > 1;
    }

    private async Task<bool> ValidateInvoice()
    {
        var errors = new List<string>();

        if (!Items.Any(i => i.SelectedProduct != null && i.Quantity > 0))
            errors.Add("• يجب إضافة صنف واحد على الأقل");

        if (SelectedWarehouseId <= 0)
            errors.Add("• يجب اختيار المستودع");

        if (SelectedPaymentType == (byte)PaymentType.Credit && !SelectedCustomerId.HasValue)
            errors.Add("• يجب اختيار العميل للفواتير الآجلة");

        if (SelectedCashBox == null && PaidAmount > 0)
            errors.Add("• يجب اختيار الصندوق النقدي عند وجود مبلغ مدفوع");

        if (errors.Any())
        {
            await _dialogService.ShowValidationErrorsAsync("بيانات غير مكتملة", errors);
            RequestFocusFirstInvalidField();
            return false;
        }

        return true;
    }

    private CreateSalesInvoiceRequest BuildRequest()
    {
        var items = Items
            .Where(i => i.SelectedProduct != null && i.Quantity > 0)
            .Select(i => new CreateSalesInvoiceLineRequest(
                i.SelectedProduct!.Id,
                i.Quantity,
                i.UnitPrice,
                i.ProductUnitId ?? 0))
            .ToList();

        return new CreateSalesInvoiceRequest(
            SelectedWarehouseId,
            InvoiceNo > 0 ? InvoiceNo : null,
            SelectedCustomerId ?? 0,
            SelectedCashBox?.Id,
            InvoiceDate,
            (PaymentType)SelectedPaymentType,
            InvoiceDiscount,
            TaxAmount,
            OtherCharges,
            PaidAmount,
            Notes,
            (short?)SelectedCurrencyId,
            ExchangeRate,
            TaxId,
            items);
    }

    private UpdateSalesInvoiceRequest BuildUpdateRequest()
    {
        var items = Items
            .Where(i => i.SelectedProduct != null && i.Quantity > 0)
            .Select(i => new CreateSalesInvoiceLineRequest(
                i.SelectedProduct!.Id,
                i.Quantity,
                i.UnitPrice,
                i.ProductUnitId ?? 0))
            .ToList();

        return new UpdateSalesInvoiceRequest(
            SelectedWarehouseId,
            SelectedCustomerId ?? 0,
            InvoiceDate,
            (PaymentType)SelectedPaymentType,
            InvoiceDiscount,
            TaxAmount,
            OtherCharges,
            PaidAmount,
            SelectedCashBox?.Id,
            Notes,
            (short?)SelectedCurrencyId,
            ExchangeRate,
            TaxId,
            items);
    }

    private void RecalculateTotals()
    {
        SubTotal = Items.Sum(i => i.LineTotal);

        decimal netAmount = SubTotal - InvoiceDiscount;

        if (IsTaxInclusive)
        {
            TaxAmount = (netAmount * TaxRate) / (100 + TaxRate);
            TotalAmount = netAmount;
        }
        else
        {
            TaxAmount = netAmount * (TaxRate / 100);
            TotalAmount = netAmount + TaxAmount + OtherCharges;
        }

        if (SelectedPaymentType == (byte)PaymentType.Cash)
        {
            PaidAmount = TotalAmount;
        }

        DueAmount = TotalAmount - PaidAmount;

        UpdateCommandStates();
    }

    public async Task HandleBarcodeInput(Key key, string? keyText = null)
    {
        var barcode = _barcodeService.ProcessKey(key, keyText);
        if (barcode != null)
        {
            await ProcessBarcodeAsync(barcode);
        }
    }

    public async Task<bool> ProcessBarcodeAsync(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode)) return false;

        // Search in the loaded products
        var product = Products.FirstOrDefault(p => p.Barcode == barcode);
        
        if (product == null)
        {
            // Try fetching from API
            var result = await _productService.GetByBarcodeAsync(barcode);
            if (result.IsSuccess && result.Value != null)
            {
                product = result.Value;
                // Optionally add to local list if it's missing but we probably want to keep local list consistent with categories etc.
            }
            else
            {
                _soundService.PlayError();
                return false;
            }
        }

        // 1. Validate Warehouse Selection
        if (SelectedWarehouseId <= 0)
        {
            _ = _dialogService.ShowWarningAsync("تنبيه", "يجب اختيار المستودع أولاً");
            return false;
        }

        // 2. Check Stock in Selected Warehouse
        var stockResult = await _inventoryService.GetStockAsync(product.Id, SelectedWarehouseId);
        decimal currentStock = stockResult.IsSuccess ? stockResult.Value : 0;

        // Check if product is already in the list to calculate total needed
        var existingLine = Items.FirstOrDefault(i => i.ProductId == product.Id);
        decimal neededQuantity = (existingLine?.Quantity ?? 0) + 1;

        if (!_allowNegativeStock && currentStock < neededQuantity)
        {
            _ = _dialogService.ShowErrorAsync("خطأ في المخزون", $"المخزون غير كافٍ في المستودع المختار. المتوفر: {currentStock}");
            return false;
        }

        if (existingLine != null)
        {
            existingLine.Quantity += 1;
            // Ensure CostInBaseCurrency is set (should have been set on first creation)
            if (!existingLine.CostInBaseCurrency.HasValue)
            {
                existingLine.CostInBaseCurrency = 0m;
            }
            _soundService.PlaySuccess();
        }
        else
        {
            // Add new line — use SetPriceFromProduct to ensure cost and price-override are set
            var line = new InvoiceLineViewModel(Products, _soundService, _unitService, _priceService);
            line.SetPriceFromProduct(product);
            line.Quantity = 1;
            
            line.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName is nameof(InvoiceLineViewModel.LineTotal)
                    or nameof(InvoiceLineViewModel.Profit))
                {
                    RecalculateTotals();
                }
            };
            
            // Remove the empty line if it exists at the end
            var lastLine = Items.LastOrDefault();
            if (lastLine != null && lastLine.SelectedProduct == null)
            {
                Items.Remove(lastLine);
            }
            
            Items.Add(line);
            
            // Load stock display for this line
            _ = LoadStockForLineAsync(line, product.Id);
        }

        RecalculateTotals();
        return true;
    }

    /// <summary>
    /// Opens a continuous barcode scan dialog. Each scanned barcode auto-adds
    /// the product to the invoice items list.
    /// </summary>
    public async Task StartContinuousScanAsync()
    {
        try
        {
            // Validate warehouse selection first
            if (SelectedWarehouseId <= 0)
            {
                await _dialogService.ShowWarningAsync("المسح المستمر", "يجب اختيار المستودع أولاً قبل بدء المسح");
                return;
            }

            var dialog = new Views.Dialogs.BarcodeScanDialog();
            if (System.Windows.Application.Current.MainWindow != null && System.Windows.Application.Current.MainWindow != dialog)
            {
                dialog.Owner = System.Windows.Application.Current.MainWindow;
            }

            // In continuous mode, each scanned barcode auto-adds product
            dialog.OnBarcodeScanned += async (barcode) =>
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    var result = await ProcessBarcodeAsync(barcode);
                    if (result)
                    {
                        _soundService?.PlaySuccess();
                    }
                    else
                    {
                        _soundService?.PlayError();
                        await _dialogService.ShowWarningAsync("المسح المستمر",
                            $"لم يتم العثور على منتج للباركود: {barcode}");
                    }
                });
            };

            dialog.ShowDialog();
        }
        catch (Exception ex)
        {
            LogSystemError("فشل في فتح نافذة المسح المستمر", "StartContinuousScanAsync", ex);
        }
    }

    private void SearchProduct(object? parameter)
    {
        var vm = new ViewModels.Products.ProductSelectionViewModel(SelectedWarehouseId);
        
        // Handle continuous selection
        vm.OnProductSelected += async (product) => 
        {
            // 1. Validate Warehouse Selection
            if (SelectedWarehouseId <= 0)
            {
                _ = _dialogService.ShowWarningAsync("تنبيه", "يجب اختيار المستودع أولاً");
                return;
            }

            // 2. Check Stock in Selected Warehouse
            var stockResult = await _inventoryService.GetStockAsync(product.Id, SelectedWarehouseId);
            decimal currentStock = stockResult.IsSuccess ? stockResult.Value : 0;

            var existingLine = Items.FirstOrDefault(i => i.ProductId == product.Id);
            decimal neededQuantity = (existingLine?.Quantity ?? 0) + 1;

            if (!_allowNegativeStock && currentStock < neededQuantity)
            {
                _ = _dialogService.ShowErrorAsync("خطأ في المخزون", $"المخزون غير كافٍ في المستودع المختار. المتوفر: {currentStock}");
                _soundService.PlayError();
                return;
            }

            if (existingLine != null)
            {
                existingLine.Quantity += 1;
                _soundService.PlaySuccess();
            }
            else
            {
                // Add new line with enhanced cost/price-override tracking
                var line = new InvoiceLineViewModel(Products, _soundService, _unitService, _priceService);
                line.SetPriceFromProduct(product);
                line.Quantity = 1;
                
                line.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName is nameof(InvoiceLineViewModel.LineTotal)
                        or nameof(InvoiceLineViewModel.Profit))
                    {
                        RecalculateTotals();
                    }
                };
                
                // Remove the empty line if it exists at the end
                var lastLine = Items.LastOrDefault();
                if (lastLine != null && lastLine.SelectedProduct == null)
                {
                    Items.Remove(lastLine);
                }
                
                Items.Add(line);
                
                // Load stock display for this line
                _ = LoadStockForLineAsync(line, product.Id);
                
                _soundService.PlaySuccess();
            }

            RecalculateTotals();
        };

        _dialogService.ShowDialog(vm);
        
        // After dialog closes, ensure there is an empty line if needed
        var finalLastLine = Items.LastOrDefault();
        if (finalLastLine != null && finalLastLine.SelectedProduct != null)
        {
            AddLine();
        }
    }

    /// <summary>
    /// Opens product selection for a SINGLE pick — closes after one selection.
    /// Used by the 🔍 button inside each invoice row.
    /// </summary>
    private void SearchProductSingle(object? parameter)
    {
        var targetLine = parameter as InvoiceLineViewModel;
        var vm = new ViewModels.Products.ProductSelectionViewModel(SelectedWarehouseId);
        bool picked = false;

        vm.OnProductSelected += async (product) =>
        {
            if (picked) return; // only one selection
            picked = true;

            if (SelectedWarehouseId <= 0)
            {
                _ = _dialogService.ShowWarningAsync("تنبيه", "يجب اختيار المستودع أولاً");
                return;
            }

            var stockResult = await _inventoryService.GetStockAsync(product.Id, SelectedWarehouseId);
            decimal currentStock = stockResult.IsSuccess ? stockResult.Value : 0;

            if (targetLine != null)
            {
                // Update the specific line this button belongs to
                if (!_allowNegativeStock && currentStock < 1)
                {
                    _ = _dialogService.ShowErrorAsync("خطأ في المخزون", $"المخزون غير كافٍ. المتوفر: {currentStock}");
                    _soundService.PlayError();
                }
                else
                {
                    targetLine.SelectedProduct = product;
                    targetLine.Quantity = 1;
                    targetLine.SetPriceFromProduct(product);
                    RecalculateTotals();
                    _soundService.PlaySuccess();
                    
                    // Load stock display for this line
                    _ = LoadStockForLineAsync(targetLine, product.Id);
                }
            }
            else
            {
                // No specific line — add as new line (same logic as barcode scan)
                var existingLine = Items.FirstOrDefault(i => i.ProductId == product.Id);
                decimal neededQty = (existingLine?.Quantity ?? 0) + 1;

                if (!_allowNegativeStock && currentStock < neededQty)
                {
                    _ = _dialogService.ShowErrorAsync("خطأ في المخزون", $"المخزون غير كافٍ. المتوفر: {currentStock}");
                    _soundService.PlayError();
                }
                else if (existingLine != null)
                {
                    existingLine.Quantity += 1;
                    RecalculateTotals();
                    _soundService.PlaySuccess();
                }
                else
                {
                    var newLine = new InvoiceLineViewModel(Products, _soundService, _unitService, _priceService);
                    newLine.SetPriceFromProduct(product);
                    newLine.Quantity = 1;
                    newLine.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName is nameof(InvoiceLineViewModel.LineTotal)
                            or nameof(InvoiceLineViewModel.Profit))
                            RecalculateTotals();
                    };
                    var lastLine = Items.LastOrDefault();
                    if (lastLine != null && lastLine.SelectedProduct == null)
                        Items.Remove(lastLine);
                    Items.Add(newLine);
                    
                    // Load stock display for this line
                    _ = LoadStockForLineAsync(newLine, product.Id);
                    
                    RecalculateTotals();
                    _soundService.PlaySuccess();
                }
            }

            // Close the dialog after picking
            System.Windows.Application.Current.Dispatcher.Invoke(() => vm.CloseDialog());
        };

        _dialogService.ShowDialog(vm);
    }

    private void SearchCustomer()
    {
        var vm = new ViewModels.Customers.CustomerSelectionViewModel();
        if (_dialogService.ShowDialog(vm) && vm.SelectedCustomer != null)
        {
            SelectedCustomerId = vm.SelectedCustomer.Id;
            _soundService.PlaySuccess();
        }
    }

    private void UpdateCommandStates()
    {
        // No-op: buttons remain enabled per interactive validation pattern (RULE-059)
    }

    private void ToggleViewMode()
    {
        CurrentViewMode = CurrentViewMode == SalesViewMode.Standard
            ? SalesViewMode.Touch
            : SalesViewMode.Standard;
    }

    /// <summary>
    /// Validates stock for all invoice items before posting.
    /// Returns list of stock issues (empty = all good).
    /// </summary>
    private async Task<List<StockIssue>> ValidateStockBeforePostAsync()
    {
        var issues = new List<StockIssue>();
        if (_allowNegativeStock || _selectedWarehouseId <= 0 || Items.Count == 0)
            return issues;

        foreach (var line in Items)
        {
            if (line.SelectedProduct == null) continue;

            var stockResult = await _inventoryService.GetStockAsync(line.SelectedProduct.Id, _selectedWarehouseId);
            if (!stockResult.IsSuccess) continue;

            var availableStock = stockResult.Value;
            var requiredQty = line.Quantity;

            if (availableStock <= 0)
            {
                issues.Add(new StockIssue(line.SelectedProduct.Name, requiredQty, availableStock, true));
            }
            else if (availableStock < requiredQty)
            {
                issues.Add(new StockIssue(line.SelectedProduct.Name, requiredQty, availableStock, false));
            }
        }

        return issues;
    }

    /// <summary>
    /// Represents a stock validation issue for a single product.
    /// </summary>
    private record StockIssue(string ProductName, decimal RequiredQty, decimal AvailableStock, bool IsOutOfStock)
    {
        public string Description => IsOutOfStock
            ? $"• {ProductName}: نفذ من المخزون (المتوفر: {AvailableStock:N0})"
            : $"• {ProductName}: الكمية المطلوبة {RequiredQty:N0} لكن المتوفر {AvailableStock:N0}";
    }

    private async Task LoadStockForLineAsync(InvoiceLineViewModel line, int productId)
    {
        if (SelectedWarehouseId <= 0) return;
        try
        {
            var result = await _inventoryService.GetStockAsync(productId, SelectedWarehouseId);
            if (result.IsSuccess)
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    line.StockInBaseUnit = result.Value);
            }
        }
        catch
        {
            // Silently fail — stock display is non-critical
        }
    }

    private async Task RefreshStockForAllLinesAsync()
    {
        if (SelectedWarehouseId <= 0) return;
        foreach (var line in Items)
        {
            if (line.SelectedProduct != null)
            {
                await LoadStockForLineAsync(line, line.SelectedProduct.Id);
            }
        }
    }

    private int GetBaseCurrencyId()
    {
        var baseCurrency = Currencies.FirstOrDefault(c => c.IsBaseCurrency);
        return baseCurrency?.Id ?? 0;
    }

    #endregion
}

public class InvoiceLineViewModel : ViewModelBase
{
    private int _productId;
    private ProductDto? _selectedProduct;
    private decimal _quantity = 1;
    private decimal _unitPrice;
    private decimal _discountAmount;
    private decimal _lineTotalInput;  // Editable gross total (Qty × Price), before discount
    private FlexibleInputCalculator.CalculationField? _lastModifiedField;
    private bool _isRecalculating;
    // private byte _mode; // removed — per-unit pricing
    private decimal? _costInBaseCurrency;
    private bool _isPriceOverridden;
    private int? _productUnitId;
    private decimal _stockInBaseUnit;

    private readonly ISoundService? _soundService;
    private readonly IProductUnitApiService? _unitService;
    private readonly IProductPriceApiService? _priceService;
    public ObservableCollection<ProductDto> AvailableProducts { get; }

    private ObservableCollection<ProductUnitDto> _availableProductUnits = new();
    private ProductUnitDto? _selectedProductUnit;

    public InvoiceLineViewModel(
        ObservableCollection<ProductDto> products,
        ISoundService? soundService = null,
        IProductUnitApiService? unitService = null,
        IProductPriceApiService? priceService = null)
    {
        AvailableProducts = products;
        _soundService = soundService;
        _unitService = unitService;
        _priceService = priceService;
        _lineTotalInput = _quantity * _unitPrice;  // Initialize: Qty (1) × Price (0) = 0
    }

    /// <summary>
    /// Stock quantity in base unit for this product in the selected warehouse.
    /// Set externally by the parent VM after loading stock.
    /// </summary>
    public decimal StockInBaseUnit
    {
        get => _stockInBaseUnit;
        set
        {
            if (SetProperty(ref _stockInBaseUnit, value))
                OnPropertyChanged(nameof(AvailableStockText));
        }
    }

    /// <summary>
    /// Formatted available stock in the currently selected unit.
    /// Converts base-unit stock to selected unit using the unit's Factor.
    /// </summary>
    public string AvailableStockText
    {
        get
        {
            if (_selectedProductUnit == null || _stockInBaseUnit <= 0)
                return "---";
            var stockInUnit = _selectedProductUnit.IsBaseUnit
                ? _stockInBaseUnit
                : _stockInBaseUnit / _selectedProductUnit.ConversionFactor;
            if (stockInUnit < 1)
                return $"{stockInUnit:N1} {_selectedProductUnit.UnitName}";
            return $"{stockInUnit:N0} {_selectedProductUnit.UnitName}";
        }
    }

    public int ProductId
    {
        get => _productId;
        set => SetProperty(ref _productId, value);
    }

    /// <summary>
    /// Read-only computed property for profit display.
    /// Profit = LineTotal - (CostInBaseCurrency × Quantity)
    /// Returns null when cost is unknown.
    /// </summary>
    public decimal? Profit => CostInBaseCurrency.HasValue
        ? LineTotal - (CostInBaseCurrency.Value * Quantity)
        : null;

    /// <summary>
    /// Cost per unit in base currency (from Product.Cost).
    /// Used to compute profit display.
    /// </summary>
    public decimal? CostInBaseCurrency
    {
        get => _costInBaseCurrency;
        set => SetProperty(ref _costInBaseCurrency, value);
    }

    /// <summary>
    /// Indicates whether the unit price was manually overridden from the product's default price.
    /// </summary>
    public bool IsPriceOverridden
    {
        get => _isPriceOverridden;
        set => SetProperty(ref _isPriceOverridden, value);
    }

    /// <summary>
    /// The specific ProductUnitId used for this line (retail or wholesale unit).
    /// </summary>
    public int? ProductUnitId
    {
        get => _productUnitId;
        set => SetProperty(ref _productUnitId, value);
    }

    /// <summary>
    /// Available units for the selected product — populated when a product is chosen.
    /// </summary>
    public ObservableCollection<ProductUnitDto> AvailableProductUnits
    {
        get => _availableProductUnits;
        set => SetProperty(ref _availableProductUnits, value);
    }

    /// <summary>
    /// The currently selected product unit. Changing this updates the unit price
    /// from the ProductPrices table for the selected currency.
    /// </summary>
    public ProductUnitDto? SelectedProductUnit
    {
        get => _selectedProductUnit;
        set
        {
            if (SetProperty(ref _selectedProductUnit, value) && value != null)
            {
                ProductUnitId = value.Id;
                OnPropertyChanged(nameof(SelectedProductUnitName));
                OnPropertyChanged(nameof(AvailableStockText));
                // Load price for the newly selected unit
                _ = LoadPriceForSelectedUnitAsync(value.Id);
            }
        }
    }

    /// <summary>
    /// Display name of the currently selected product unit.
    /// </summary>
    public string? SelectedProductUnitName => SelectedProductUnit?.UnitName;

    public ProductDto? SelectedProduct
    {
        get => _selectedProduct;
        set
        {
            if (SetProperty(ref _selectedProduct, value) && value != null)
            {
                ProductId = value.Id;
                ClearErrors(nameof(ProductName));

                // Set cost from product's Cost
                CostInBaseCurrency = null;

                // Reset price override flag
                IsPriceOverridden = false;

                // Load available units for this product
                _ = LoadUnitsForProductAsync(value.Id);

                // Price is set asynchronously by LoadPriceForSelectedUnitAsync 
                // when the default unit is auto-selected below
            }
        }
    }

    public string ProductName => SelectedProduct?.Name ?? string.Empty;

    // Mode (جملة/تجزئة) removed — pricing is per-unit only
    // public byte Mode
    // {
    //     get => _mode;
    //     set
    //     {
    //         if (SetProperty(ref _mode, value))
    //         {
    //             if (SelectedProduct != null)
    //             {
    //                 UnitPrice = GetDefaultPrice(SelectedProduct);
    //                 ProductUnitId = 0; // 0 = service auto-determines
    //                 IsPriceOverridden = false;
    //             }
    //             OnPropertyChanged(nameof(LineTotal));
    //         }
    //     }
    // }

    public decimal Quantity
    {
        get => _quantity;
        set
        {
            if (SetProperty(ref _quantity, value))
            {
                ValidateQuantity();
                _lastModifiedField = FlexibleInputCalculator.CalculationField.Quantity;
                RecalculateFromFlexibleInput();
                OnPropertyChanged(nameof(Profit));
                _soundService?.PlaySuccess();
            }
        }
    }

    public decimal UnitPrice
    {
        get => _unitPrice;
        set
        {
            if (SetProperty(ref _unitPrice, value))
            {
                ValidateUnitPrice();
                _lastModifiedField = FlexibleInputCalculator.CalculationField.Price;
                RecalculateFromFlexibleInput();
                OnPropertyChanged(nameof(Profit));

                // Check if price was overridden from default
                if (SelectedProduct != null)
                {
                    UpdatePriceOverrideState(SelectedProduct);
                }
            }
        }
    }

    public decimal DiscountAmount
    {
        get => _discountAmount;
        set
        {
            if (SetProperty(ref _discountAmount, value))
            {
                OnPropertyChanged(nameof(LineTotal));
                OnPropertyChanged(nameof(Profit));
            }
        }
    }

    public decimal LineTotal => (Quantity * UnitPrice) - DiscountAmount;

    /// <summary>
    /// Editable gross total (Quantity × UnitPrice), before discount.
    /// When user edits this field, the system recalculates either Quantity or UnitPrice
    /// depending on which was last modified.
    /// </summary>
    public decimal LineTotalInput
    {
        get => _lineTotalInput;
        set
        {
            if (SetProperty(ref _lineTotalInput, value))
            {
                ValidateLineTotalInput();
                _lastModifiedField = FlexibleInputCalculator.CalculationField.Total;
                RecalculateFromFlexibleInput();
            }
        }
    }

    /// <summary>
    /// Sets the price from a product based on the given sale mode (retail/wholesale).
    /// </summary>
    public void SetPriceFromProduct(ProductDto product)
    {
        if (product != null)
        {
            // Price is set asynchronously by LoadPriceForSelectedUnitAsync
            // when the unit is selected via SelectedProductUnit
            ProductUnitId = 0; // 0 = service auto-determines
            CostInBaseCurrency = null;
            IsPriceOverridden = false;
        }
    }

    /// <summary>
    /// Recalculates based on user input.
    /// When the user explicitly edits the LineTotal (Total) column, the
    /// <see cref="FlexibleInputCalculator"/> determines whether to compute
    /// Quantity or UnitPrice using the two entered values.
    /// When the user edits Quantity or UnitPrice, the LineTotal is simply
    /// recalculated as Quantity × UnitPrice (the total column is NOT used as
    /// an anchor — it was only auto-set from a prior field edit, not user-entered).
    /// A guard flag (<c>_isRecalculating</c>) prevents infinite recursion.
    /// </summary>
    private void RecalculateFromFlexibleInput()
    {
        if (_isRecalculating) return;
        _isRecalculating = true;
        try
        {
            if (_lastModifiedField == FlexibleInputCalculator.CalculationField.Total)
            {
                // User explicitly edited LineTotalInput — use the calculator
                // to determine Quantity or UnitPrice from the two known values.
                var result = FlexibleInputCalculator.Calculate(
                    _quantity, _unitPrice, _lineTotalInput,
                    FlexibleInputCalculator.CalculationField.Total);

                _quantity = result.quantity;
                _unitPrice = result.price;
                _lineTotalInput = result.total;

                OnPropertyChanged(nameof(Quantity));
                OnPropertyChanged(nameof(UnitPrice));
                OnPropertyChanged(nameof(LineTotalInput));
            }
            else
            {
                // User edited Quantity or UnitPrice — just recompute the total.
                // Do NOT treat the current LineTotalInput as a user-entered anchor.
                _lineTotalInput = _quantity * _unitPrice;
                OnPropertyChanged(nameof(LineTotalInput));
            }

            OnPropertyChanged(nameof(LineTotal));
        }
        finally
        {
            _isRecalculating = false;
        }
    }

    private void ValidateLineTotalInput()
    {
        ClearErrors(nameof(LineTotalInput));
        if (LineTotalInput < 0)
        {
            AddError(nameof(LineTotalInput), "الإجمالي لا يمكن أن يكون سالباً");
        }
    }

    /// <summary>
    /// Updates the IsPriceOverridden flag by comparing current UnitPrice with the product's default price.
    /// The default price is loaded asynchronously by LoadPriceForSelectedUnitAsync,
    /// which sets IsPriceOverridden = false after successfully loading the default price.
    /// </summary>
    private void UpdatePriceOverrideState(ProductDto product)
    {
        // Price override tracking is handled by LoadPriceForSelectedUnitAsync
        // which sets IsPriceOverridden = false after loading the default price.
        // When the user modifies UnitPrice via the UI, UnitPrice.set triggers this,
        // but the async loading means we defer to the LoadPriceForSelectedUnitAsync flow.
    }

    /// <summary>
    /// Loads available product units for the given product ID from the API.
    /// Auto-selects the base unit when found.
    /// </summary>
    private async Task LoadUnitsForProductAsync(int productId)
    {
        if (_unitService == null) return;
        try
        {
            var result = await _unitService.GetByProductIdAsync(productId);
            if (result.IsSuccess && result.Value != null)
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    AvailableProductUnits = new ObservableCollection<ProductUnitDto>(result.Value);
                    // Auto-select base unit if available, otherwise first unit
                    var defaultUnit = result.Value.FirstOrDefault(u => u.IsBaseUnit)
                        ?? result.Value.FirstOrDefault();
                    if (defaultUnit != null)
                    {
                        SelectedProductUnit = defaultUnit;
                    }
                });
            }
        }
        catch (Exception ex)
        {
            // Silently fail — user can enter price manually
            System.Diagnostics.Debug.WriteLine($"Failed to load units for product {productId}: {ex.Message}");
        }
    }

    /// <summary>
    /// Loads the effective price for the selected product unit from the ProductPrices table.
    /// Uses the invoice's selected currency (or base currency as fallback).
    /// </summary>
    private async Task LoadPriceForSelectedUnitAsync(int productUnitId)
    {
        if (_priceService == null) return;
        try
        {
            var result = await _priceService.GetByProductUnitAsync(productUnitId);
            if (result.IsSuccess && result.Value != null && result.Value.Count > 0)
            {
                var activePrices = result.Value.Where(p => p.IsCurrentlyEffective).ToList();
                if (activePrices.Count > 0)
                {
                    // Take the first active price (preferred) or any price
                    var price = activePrices[0];
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        UnitPrice = price.Price;
                        IsPriceOverridden = false;
                        OnPropertyChanged(nameof(Profit));
                    });
                }
            }
        }
        catch (Exception ex)
        {
            // Silently fail — user can enter price manually
            System.Diagnostics.Debug.WriteLine($"Failed to load price for unit {productUnitId}: {ex.Message}");
        }
    }

    private void ValidateProductId()
    {
        ClearErrors(nameof(ProductName));
        if (ProductId <= 0 && SelectedProduct == null)
        {
            AddError(nameof(ProductName), "يجب اختيار منتج");
        }
    }

    private void ValidateQuantity()
    {
        ClearErrors(nameof(Quantity));
        if (Quantity <= 0)
        {
            AddError(nameof(Quantity), "الكمية يجب أن تكون أكبر من صفر");
        }
        else if (Quantity > 999999)
        {
            AddError(nameof(Quantity), "الكمية كبيرة جداً");
        }
    }

    private void ValidateUnitPrice()
    {
        ClearErrors(nameof(UnitPrice));
        if (UnitPrice < 0)
        {
            AddError(nameof(UnitPrice), "السعر لا يمكن أن يكون سالباً");
        }
    }

}

public class PaymentTypeItem
{
    public byte Value { get; set; }
    public string Display { get; set; } = string.Empty;
}


