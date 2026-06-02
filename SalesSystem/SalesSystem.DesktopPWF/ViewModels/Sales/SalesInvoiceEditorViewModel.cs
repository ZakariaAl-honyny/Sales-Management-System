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
    private readonly ICategoryApiService _categoryService;

    private int? _invoiceId;
    private int _selectedWarehouseId;
    private int? _selectedCustomerId;
    private int? _defaultCustomerId;  // auto-selected for Cash sales
    private DateTime _invoiceDate = DateTime.Today;
    private byte _paymentType = (byte)PaymentType.Cash;
    private decimal _invoiceDiscount;
    private decimal _taxRate = 15;
    private bool _isTaxInclusive;
    private decimal _paidAmount;
    private string? _notes;
    private string _barcodeSearchText = string.Empty;
    private bool _isEditMode;
    private string? _errorMessage;
    private bool _allowNegativeStock;
    private byte _status = (byte)InvoiceStatus.Draft;
    public bool IsReadOnly { get; private set; }

    private ObservableCollection<InvoiceLineViewModel> _items = new();
    private ObservableCollection<CustomerDto> _customers = new();
    private ObservableCollection<WarehouseDto> _warehouses = new();
    private ObservableCollection<ProductDto> _products = new();
    private ObservableCollection<CashBoxDto> _cashBoxes = new();
    private CashBoxDto? _selectedCashBox;

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
        ICategoryApiService categoryService,
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
        _categoryService = categoryService;
        _invoiceId = invoiceId;
        _isEditMode = invoiceId.HasValue;
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

        // --- Touch POS ViewModels ---
        TouchPosVM = new TouchPosViewModel(_categoryService, _productService, _inventoryService);
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
                    _soundService.PlaySuccess();
                }
                else
                {
                    var line = new InvoiceLineViewModel(Products, _soundService)
                    {
                        SelectedProduct = product,
                        Quantity = 1m,
                        Mode = (byte)SaleMode.Retail
                    };
                    line.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(InvoiceLineViewModel.LineTotal))
                            RecalculateTotals();
                    };
                    Items.Add(line);
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

        SaleModeOptions = new List<EnumDisplayItem>
        {
            new EnumDisplayItem { Value = (byte)SaleMode.Retail, Display = "تجزئة" },
            new EnumDisplayItem { Value = (byte)SaleMode.Wholesale, Display = "جملة" }
        };

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
                var defaultWarehouse = Warehouses.FirstOrDefault(w => w.IsDefault) ?? Warehouses.First();
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
                LogSystemError("فشل في تحميل الصناديق النقدية", "LoadCashBoxesAsync");
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
            App.GetService<ICategoryApiService>(),
            invoiceId,
            isReadOnly)
    {
    }

    #region Properties
    public int? InvoiceId => _invoiceId;
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

    public List<EnumDisplayItem> SaleModeOptions { get; }
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
                    var defaultWarehouse = Warehouses.FirstOrDefault(w => w.IsDefault) ?? Warehouses.First();
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
                if (settingsResult.Value.IsTaxEnabled)
                {
                    TaxRate = settingsResult.Value.DefaultTaxRate;
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
                    var lineVm = new InvoiceLineViewModel(Products);
                    lineVm.ProductId = item.ProductId;
                    lineVm.Quantity = item.Quantity;
                    lineVm.Mode = item.Mode;
                    lineVm.UnitPrice = item.UnitPrice;
                    lineVm.DiscountAmount = item.DiscountAmount;
                    lineVm.SelectedProduct = Products.FirstOrDefault(p => p.Id == item.ProductId);
                    
                    lineVm.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(InvoiceLineViewModel.LineTotal))
                        {
                            RecalculateTotals();
                        }
                    };
                    Items.Add(lineVm);
                }

                // TODO: Restore CashBoxId when DTO supports it
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
            var request = BuildRequest();

            Result<SalesInvoiceDto> result;
            if (_isEditMode)
            {
                result = await _invoiceService.UpdateAsync(_invoiceId!.Value, request);
            }
            else
            {
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
        var line = new InvoiceLineViewModel(Products, _soundService);
        line.PropertyChanged += async (s, e) =>
        {
            if (e.PropertyName == nameof(InvoiceLineViewModel.LineTotal))
            {
                RecalculateTotals();
            }
            else if (e.PropertyName == nameof(InvoiceLineViewModel.SelectedProduct) || e.PropertyName == nameof(InvoiceLineViewModel.Quantity))
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
            .Select(i => new CreateSalesInvoiceItemRequest(
                i.SelectedProduct!.Id,
                i.Quantity,
                i.UnitPrice,
                i.DiscountAmount,
                (SaleMode)i.Mode,
                null))
            .ToList();

        return new CreateSalesInvoiceRequest(
            SelectedWarehouseId,
            SelectedCustomerId,
            SelectedCashBox?.Id,
            InvoiceDate,
            null,
            (PaymentType)SelectedPaymentType,
            InvoiceDiscount,
            TaxAmount,
            PaidAmount,
            Notes,
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
            TotalAmount = netAmount + TaxAmount;
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
            _soundService.PlaySuccess();
        }
        else
        {
            // Add new line
            var line = new InvoiceLineViewModel(Products, _soundService)
            {
                SelectedProduct = product,
                Quantity = 1,
                UnitPrice = product.RetailPrice
            };
            
            line.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(InvoiceLineViewModel.LineTotal))
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
        }

        RecalculateTotals();
        return true;
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
                // Add new line
                var line = new InvoiceLineViewModel(Products, _soundService)
                {
                    SelectedProduct = product,
                    Quantity = 1,
                    UnitPrice = product.RetailPrice
                };
                
                line.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(InvoiceLineViewModel.LineTotal))
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
                    targetLine.UnitPrice = product.RetailPrice;
                    RecalculateTotals();
                    _soundService.PlaySuccess();
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
                    var newLine = new InvoiceLineViewModel(Products, _soundService)
                    {
                        SelectedProduct = product,
                        Quantity = 1,
                        UnitPrice = product.RetailPrice
                    };
                    newLine.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(InvoiceLineViewModel.LineTotal))
                            RecalculateTotals();
                    };
                    var lastLine = Items.LastOrDefault();
                    if (lastLine != null && lastLine.SelectedProduct == null)
                        Items.Remove(lastLine);
                    Items.Add(newLine);
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

    #endregion
}

public class InvoiceLineViewModel : ViewModelBase
{
    private int _productId;
    private ProductDto? _selectedProduct;
    private decimal _quantity = 1;
    private decimal _unitPrice;
    private decimal _discountAmount;
    private byte _mode = (byte)SaleMode.Retail;

    private readonly ISoundService? _soundService;
    public ObservableCollection<ProductDto> AvailableProducts { get; }

    public InvoiceLineViewModel(ObservableCollection<ProductDto> products, ISoundService? soundService = null)
    {
        AvailableProducts = products;
        _soundService = soundService;
    }

    public int ProductId
    {
        get => _productId;
        set => SetProperty(ref _productId, value);
    }

    public ProductDto? SelectedProduct
    {
        get => _selectedProduct;
        set
        {
            if (SetProperty(ref _selectedProduct, value) && value != null)
            {
                ProductId = value.Id;
                ClearErrors(nameof(ProductName));
                if (UnitPrice == 0)
                {
                    UnitPrice = Mode == (byte)SaleMode.Wholesale 
                        ? value.WholesalePrice 
                        : value.RetailPrice;
                }
            }
        }
    }

    public string ProductName => SelectedProduct?.Name ?? string.Empty;

    public byte Mode
    {
        get => _mode;
        set
        {
            if (SetProperty(ref _mode, value))
            {
                if (SelectedProduct != null)
                {
                    UnitPrice = value == (byte)SaleMode.Wholesale 
                        ? SelectedProduct.WholesalePrice 
                        : SelectedProduct.RetailPrice;
                }
                OnPropertyChanged(nameof(LineTotal));
            }
        }
    }

    public decimal Quantity
    {
        get => _quantity;
        set
        {
            if (SetProperty(ref _quantity, value))
            {
                ValidateQuantity();
                OnPropertyChanged(nameof(LineTotal));
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
                OnPropertyChanged(nameof(LineTotal));
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
            }
        }
    }

    public decimal LineTotal => (Quantity * UnitPrice) - DiscountAmount;

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

public class EnumDisplayItem
{
    public byte Value { get; set; }
    public string Display { get; set; } = string.Empty;
}
