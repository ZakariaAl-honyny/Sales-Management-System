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

namespace SalesSystem.DesktopPWF.ViewModels.Purchases;

public class PurchaseInvoiceEditorViewModel : ViewModelBase
{
    private readonly IPurchaseInvoiceApiService _invoiceService;
    private readonly IEventBus _eventBus;
    private readonly ISupplierApiService _supplierService;
    private readonly IWarehouseApiService _warehouseService;
    private readonly IProductApiService _productService;
    private readonly ISettingsApiService _settingsService;
    private readonly IDialogService _dialogService;
    private readonly ISoundService _soundService;
    private readonly IBarcodeInputService _barcodeService;
    private readonly ICashBoxApiService _cashBoxService;
    private readonly IPrintApiService _printApiService;
    private readonly IToastNotificationService _toastService;
    private readonly ICurrencyApiService _currencyService;
    private readonly IAdditionalFeeApiService _additionalFeeService;

    private int? _invoiceId;
    private int _invoiceNo;
    private int _selectedWarehouseId;
    private int? _selectedSupplierId;
    private int? _defaultSupplierId; // Auto-selected for Cash purchases
    private DateTime _invoiceDate = DateTime.Today;
    private byte _paymentType = (byte)PaymentType.Cash;
    private decimal _invoiceDiscount;
    private decimal _taxRate = 15;
    private string _barcodeSearchText = string.Empty;
    private bool _isTaxInclusive;
    private decimal _paidAmount;
    private string? _supplierInvoiceNo;
    private string? _notes;
    private bool _isEditMode;
    private string? _errorMessage;
    private byte _status = (byte)InvoiceStatus.Draft;
    public bool IsReadOnly { get; private set; }

    private ObservableCollection<PurchaseInvoiceLineViewModel> _items = new();
    private ObservableCollection<SupplierDto> _suppliers = new();
    private ObservableCollection<WarehouseDto> _warehouses = new();
    private ObservableCollection<ProductDto> _products = new();
    private ObservableCollection<CashBoxDto> _cashBoxes = new();
    private CashBoxDto? _selectedCashBox;

    private ObservableCollection<CurrencyDto> _currencies = new();
    private int? _selectedCurrencyId = 1;
    private decimal _exchangeRate = 1.0m;
    private byte _selectedDiscountType;
    private decimal? _discountRate;
    private ObservableCollection<AdditionalFeeDto> _additionalFees = new();
    private string? _attachmentFileName;
    private byte[]? _attachmentFileData;
    private string? _currencyName;

    public PurchaseInvoiceEditorViewModel(
        IPurchaseInvoiceApiService invoiceService,
        IEventBus eventBus,
        ISupplierApiService supplierService,
        IWarehouseApiService warehouseService,
        IProductApiService productService,
        ISettingsApiService settingsService,
        IDialogService dialogService,
        ISoundService soundService,
        IBarcodeInputService barcodeService,
        ICashBoxApiService cashBoxService,
        IPrintApiService printApiService,
        IToastNotificationService toastService,
        ICurrencyApiService currencyService,
        IAdditionalFeeApiService additionalFeeService,
        int? invoiceId = null,
        bool isReadOnly = false)
    {
        _invoiceService = invoiceService;
        _eventBus = eventBus;
        _supplierService = supplierService;
        _warehouseService = warehouseService;
        _productService = productService ?? throw new ArgumentNullException(nameof(productService));
        _settingsService = settingsService;
        _dialogService = dialogService;
        SetDialogService(dialogService);
        _soundService = soundService;
        _barcodeService = barcodeService;
        _cashBoxService = cashBoxService;
        _printApiService = printApiService;
        _toastService = toastService;
        _currencyService = currencyService ?? throw new ArgumentNullException(nameof(currencyService));
        _additionalFeeService = additionalFeeService ?? throw new ArgumentNullException(nameof(additionalFeeService));
        _invoiceId = invoiceId;
        _isEditMode = invoiceId.HasValue;
        IsReadOnly = isReadOnly;

        if (!invoiceId.HasValue)
        {
            InvoiceNo = 0; // Service will compute lastId + 1
        }

        SaveCommand = new AsyncRelayCommand(SaveAsync);
        PostCommand = new AsyncRelayCommand(PostAsync);
        CancelCommand = new RelayCommand(Cancel);
        AddLineCommand = new RelayCommand(AddLine);
        RemoveLineCommand = new RelayCommand(RemoveLine, CanRemoveLine);
        PrintA4Command = new AsyncRelayCommand(PrintA4Async);
        SearchProductCommand = new RelayCommand(SearchProduct);
        SearchProductSingleCommand = new RelayCommand(SearchProductSingle);
        SearchSupplierCommand = new RelayCommand(SearchSupplier);
        ProcessBarcodeCommand = new AsyncRelayCommand(async () => 
        {
            var code = BarcodeSearchText;
            BarcodeSearchText = string.Empty;
            if (!string.IsNullOrWhiteSpace(code))
            {
                await ProcessBarcodeAsync(code);
            }
        });
        PrintThermalCommand = new AsyncRelayCommand(PrintThermalAsync);
        UploadAttachmentCommand = new AsyncRelayCommand(UploadAttachmentAsync);
        RemoveAttachmentCommand = new RelayCommand(_ => RemoveAttachment());
        AddFeeCommand = new AsyncRelayCommand(AddFeeAsync);
        RemoveFeeCommand = new RelayCommand(RemoveFee);

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await ExecuteAsync(InitializeOperationAsync, "جاري تحميل البيانات...");
    }

    private async Task InitializeOperationAsync()
    {
        await LoadReferenceDataAsync();
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

    private async Task LoadCashBoxesAsync()
    {
        try
        {
            var result = await _cashBoxService.GetAllAsync();
            if (result.IsSuccess && result.Value != null)
            {
                CashBoxes = new ObservableCollection<CashBoxDto>(result.Value.Where(c => c.IsActive).OrderByDescending(x => x.Id));
            }
            else
            {
                Serilog.Log.Warning("[PurchaseInvoiceEditor.LoadCashBoxesAsync] فشل في تحميل الصناديق النقدية");
            }
        }
        catch (Exception ex)
        {
            LogSystemError("فشل في تحميل الصناديق النقدية", "LoadCashBoxesAsync", ex);
        }
    }

    public PurchaseInvoiceEditorViewModel(int? invoiceId = null, bool isReadOnly = false)
        : this(
            App.GetService<IPurchaseInvoiceApiService>(),
            App.GetService<IEventBus>(),
            App.GetService<ISupplierApiService>(),
            App.GetService<IWarehouseApiService>(),
            App.GetService<IProductApiService>(),
            App.GetService<ISettingsApiService>(),
            App.GetService<IDialogService>(),
            App.GetService<ISoundService>(),
            App.GetService<IBarcodeInputService>(),
            App.GetService<ICashBoxApiService>(),
            App.GetService<IPrintApiService>(),
            App.GetService<IToastNotificationService>(),
            App.GetService<ICurrencyApiService>(),
            App.GetService<IAdditionalFeeApiService>(),
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

    public ObservableCollection<SupplierDto> Suppliers
    {
        get => _suppliers;
        set => SetProperty(ref _suppliers, value);
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

    public ObservableCollection<PurchaseInvoiceLineViewModel> Items
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
                UpdateCommandStates();
            }
        }
    }

    public int? SelectedSupplierId
    {
        get => _selectedSupplierId;
        set
        {
            if (SetProperty(ref _selectedSupplierId, value))
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
                if (value == (byte)PaymentType.Cash)
                {
                    SelectedSupplierId = _defaultSupplierId;
                }
                else
                {
                    SelectedSupplierId = null;
                }
                RecalculateTotals();
            }
        }
    }

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

    public string? SupplierInvoiceNo
    {
        get => _supplierInvoiceNo;
        set => SetProperty(ref _supplierInvoiceNo, value);
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

    public List<EnumDisplayItem> SaleModeOptions { get; } = new()
    {
        new EnumDisplayItem { Value = (byte)SaleMode.Retail, Display = "تجزئة" },
        new EnumDisplayItem { Value = (byte)SaleMode.Wholesale, Display = "جملة" }
    };

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
                RecalculateTotals();
            }
        }
    }

    public decimal ExchangeRate
    {
        get => _exchangeRate;
        set
        {
            if (SetProperty(ref _exchangeRate, value))
            {
                RecalculateTotals();
            }
        }
    }

    public byte SelectedDiscountType
    {
        get => _selectedDiscountType;
        set
        {
            if (SetProperty(ref _selectedDiscountType, value))
            {
                RecalculateTotals();
            }
        }
    }

    public decimal? DiscountRate
    {
        get => _discountRate;
        set
        {
            if (SetProperty(ref _discountRate, value))
            {
                RecalculateTotals();
            }
        }
    }

    public ObservableCollection<AdditionalFeeDto> AdditionalFees
    {
        get => _additionalFees;
        set => SetProperty(ref _additionalFees, value);
    }

    public string? AttachmentFileName
    {
        get => _attachmentFileName;
        set
        {
            if (SetProperty(ref _attachmentFileName, value))
            {
                OnPropertyChanged(nameof(HasAttachment));
            }
        }
    }

    public byte[]? AttachmentFileData
    {
        get => _attachmentFileData;
        set => SetProperty(ref _attachmentFileData, value);
    }

    public bool HasAttachment => !string.IsNullOrEmpty(AttachmentFileName);

    public string CurrencyName
    {
        get => _currencyName ?? "ريال سعودي";
        private set => SetProperty(ref _currencyName, value);
    }

    public bool IsBaseCurrency => SelectedCurrencyId == GetBaseCurrencyId();

    public decimal ImportTotalInBaseCurrency => SubTotal * ExchangeRate;

    public List<DiscountOption> DiscountTypeOptions { get; } = new()
    {
        new DiscountOption { Value = 0, Display = "مبلغ" },
        new DiscountOption { Value = 1, Display = "نسبة مئوية" }
    };
    #endregion

    #region Commands
    public ICommand SaveCommand { get; }
    public ICommand PostCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand AddLineCommand { get; }
    public ICommand RemoveLineCommand { get; }
    public ICommand PrintA4Command { get; }
    public ICommand SearchProductCommand { get; }
    public ICommand SearchProductSingleCommand { get; }
    public ICommand SearchSupplierCommand { get; }
    public ICommand ProcessBarcodeCommand { get; }
    public ICommand PrintThermalCommand { get; }
    public ICommand UploadAttachmentCommand { get; }
    public ICommand RemoveAttachmentCommand { get; }
    public ICommand AddFeeCommand { get; }
    public ICommand RemoveFeeCommand { get; }
    #endregion

    #region Methods
    private async Task LoadReferenceDataAsync()
    {
        await ExecuteAsync(async () =>
        {
            ErrorMessage = null;
            var suppliersResult = await _supplierService.GetAllAsync();
            if (suppliersResult.IsSuccess && suppliersResult.Value != null)
            {
                Suppliers = new ObservableCollection<SupplierDto>(suppliersResult.Value);
                var defaultSupplier = suppliersResult.Value.FirstOrDefault(s =>
                    s.Name.Contains("مورد نقدي") ||
                    s.Name.Contains("مورد افتراضي") ||
                    s.Name.Contains("نقدي") && s.Name.Length < 10);
                _defaultSupplierId = defaultSupplier?.Id;
                if (_paymentType == (byte)PaymentType.Cash && _defaultSupplierId.HasValue && !_isEditMode)
                    SelectedSupplierId = _defaultSupplierId;
            }

            var warehousesResult = await _warehouseService.GetAllAsync();
            if (warehousesResult.IsSuccess && warehousesResult.Value != null)
            {
                Warehouses = new ObservableCollection<WarehouseDto>(warehousesResult.Value);
                if (Warehouses.Any() && SelectedWarehouseId == 0)
                {
                    SelectedWarehouseId = Warehouses.First().Id;
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
                SelectedSupplierId = invoice.SupplierId;
                InvoiceDate = invoice.InvoiceDate;
                SelectedPaymentType = (byte)invoice.PaymentType;
                InvoiceDiscount = invoice.DiscountAmount;
                PaidAmount = invoice.PaidAmount;
                SupplierInvoiceNo = invoice.SupplierInvoiceNo;
                Notes = invoice.Notes;
                Status = invoice.Status;

                SelectedCurrencyId = invoice.CurrencyId;
                ExchangeRate = invoice.ExchangeRate ?? 1.0m;
                if (invoice.DiscountType.HasValue) SelectedDiscountType = invoice.DiscountType.Value;
                DiscountRate = invoice.DiscountRate;
                AttachmentFileName = invoice.AttachmentPath;
                OnPropertyChanged(nameof(HasAttachment));

                if (invoice.AdditionalFees != null)
                {
                    AdditionalFees = new ObservableCollection<AdditionalFeeDto>(invoice.AdditionalFees);
                }

                if (invoice.Status != (byte)InvoiceStatus.Draft)
                {
                    IsReadOnly = true;
                    OnPropertyChanged(nameof(IsReadOnly));
                }
                Items.Clear();
                foreach (var item in invoice.Items)
                {
                    var lineVm = new PurchaseInvoiceLineViewModel(Products);
                    lineVm.ProductId = item.ProductId;
                    lineVm.Quantity = item.Quantity;
                    lineVm.UnitCost = item.UnitCost;
                    lineVm.DiscountAmount = item.DiscountAmount;
                    lineVm.SelectedProduct = Products.FirstOrDefault(p => p.Id == item.ProductId);
                    lineVm.ProductUnitId = item.ProductUnitId;
                    if (item.DiscountType.HasValue) lineVm.DiscountType = item.DiscountType.Value;
                    lineVm.DiscountRate = item.DiscountRate;
                    lineVm.CostInBaseCurrency = item.CostInBaseCurrency;
                    lineVm.AdditionalFeesAmount = item.AdditionalFeesAmount;
                    lineVm.Notes = item.Notes;
                    
                    lineVm.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(PurchaseInvoiceLineViewModel.LineTotal))
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
                ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل الفاتورة", "PurchaseInvoiceEditorViewModel.LoadInvoiceAsync", $"[PurchaseInvoiceEditorViewModel.LoadInvoiceAsync] Failed to load purchase invoice ID {_invoiceId}.");
            }
        });
    }

    private async Task SaveAsync()
    {
        if (!await ValidateInvoice()) return;

        await ExecuteAsync(async () =>
        {
            ErrorMessage = null;
            var request = BuildRequest();

            Result<PurchaseInvoiceDto> result;
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
                
                await _dialogService.ShowInfoAsync("نجاح", "✅ تم حفظ فاتورة الشراء بنجاح. يمكنك الآن الترحيل النهائي للمخزون.");
                _eventBus.Publish(new PurchaseInvoiceChangedMessage(_invoiceId.Value));
                UpdateCommandStates();
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشل في حفظ الفاتورة", "PurchaseInvoiceEditorViewModel.SaveAsync", "[PurchaseInvoiceEditorViewModel.SaveAsync] Failed to save purchase invoice.");
                await _dialogService.ShowErrorAsync("خطأ في الحفظ", ErrorMessage!);
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

        if (!await _dialogService.ShowConfirmationAsync("تأكيد الترحيل", "هل أنت متأكد من ترحيل هذه الفاتورة؟\nسيتم إضافة الكميات إلى المخزون.")) return;

        await ExecuteAsync(async () =>
        {
            ErrorMessage = null;
            var postResult = await _invoiceService.PostAsync(_invoiceId!.Value);
            if (postResult.IsSuccess)
            {
                await _dialogService.ShowInfoAsync("نجاح", "تم ترحيل الفاتورة بنجاح");
                _eventBus.Publish(new PurchaseInvoiceChangedMessage(_invoiceId.Value));
                RequestClose();
            }
            else
            {
                ErrorMessage = HandleFailure(postResult.Error ?? "فشل في ترحيل الفاتورة", "PurchaseInvoiceEditorViewModel.PostAsync", $"[PurchaseInvoiceEditorViewModel.PostAsync] Failed to post/confirm purchase invoice ID {_invoiceId}.");
                await _dialogService.ShowErrorAsync("خطأ في الترحيل", ErrorMessage!);
            }
        });
        UpdateCommandStates();
    }

    private async Task PrintA4Async()
    {
        if (!_invoiceId.HasValue)
        {
            await _dialogService.ShowWarningAsync("طباعة فاتورة الشراء", "يجب حفظ الفاتورة أولاً قبل الطباعة");
            return;
        }

        if (_status != (byte)InvoiceStatus.Posted)
        {
            await _dialogService.ShowWarningAsync("طباعة فاتورة الشراء", "يجب ترحيل الفاتورة أولاً قبل طباعة A4");
            return;
        }

        await ExecuteAsync(async () =>
        {
            ErrorMessage = null;
            var result = await _printApiService.GetPurchaseA4PdfAsync(_invoiceId!.Value);
            if (result.IsSuccess && result.Value != null)
            {
                var previewWindow = new Views.Common.PdfPreviewWindow(
                    result.Value,
                    $"#{_invoiceId}",
                    _invoiceId!.Value,
                    isPurchase: true);
                previewWindow.ShowDialog();
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل ملف PDF",
                    "PurchaseInvoiceEditorViewModel.PrintA4Async",
                    $"[PurchaseInvoiceEditorViewModel.PrintA4Async] Failed to get A4 PDF for purchase invoice ID {_invoiceId}.");
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
            var result = await _printApiService.PrintPurchaseThermalAsync(_invoiceId!.Value);
            if (result.IsSuccess)
            {
                _toastService.ShowSuccess("تم إرسال الإيصال إلى الطابعة الحرارية بنجاح");
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشلت طباعة الإيصال الحراري",
                    "PurchaseInvoiceEditorViewModel.PrintThermalAsync",
                    $"[PurchaseInvoiceEditorViewModel.PrintThermalAsync] Thermal print failed for invoice ID {_invoiceId}.");
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
        var line = new PurchaseInvoiceLineViewModel(Products, _soundService);
        line.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(PurchaseInvoiceLineViewModel.LineTotal))
            {
                RecalculateTotals();
            }
        };
        Items.Add(line);
        RecalculateTotals();
    }

    private void RemoveLine(object? parameter)
    {
        if (parameter is PurchaseInvoiceLineViewModel line)
        {
            Items.Remove(line);
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

        if (SelectedSupplierId <= 0)
            errors.Add("• يجب اختيار المورد");

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

    private CreatePurchaseInvoiceRequest BuildRequest()
    {
        var items = Items
            .Where(i => i.SelectedProduct != null && i.Quantity > 0)
            .Select(i => new CreatePurchaseInvoiceItemRequest(
                i.SelectedProduct!.Id,
                i.ProductUnitId > 0 ? i.ProductUnitId : (i.Mode == (byte)SaleMode.Wholesale ? (i.SelectedProduct!.WholesaleUnitId ?? 0) : (i.SelectedProduct!.RetailUnitId ?? 0)),
                i.Quantity,
                i.UnitCost,
                i.DiscountAmount,
                i.DiscountType > 0 ? i.DiscountType : null,
                i.DiscountRate,
                (SaleMode)i.Mode,
                i.Notes))
            .ToList();

        string? attachmentBase64 = null;
        if (AttachmentFileData != null && AttachmentFileData.Length > 0)
        {
            attachmentBase64 = Convert.ToBase64String(AttachmentFileData);
        }

        var additionalFees = AdditionalFees
            .Select(f => new CreateAdditionalFeeRequest(
                string.Empty, // FeeName — mapped from Description in further integration
                f.FeeAmount,
                f.DistributionMethod,
                f.AccountId))
            .ToList();

        return new CreatePurchaseInvoiceRequest(
            SelectedWarehouseId,
            SelectedSupplierId ?? 0,
            InvoiceNo > 0 ? InvoiceNo : null,
            InvoiceDate,
            null, // DueDate
            (PaymentType)SelectedPaymentType,
            SelectedCashBox?.Id,
            InvoiceDiscount,
            SelectedDiscountType > 0 ? SelectedDiscountType : (byte?)null,
            DiscountRate,
            TaxAmount,
            PaidAmount,
            SelectedCurrencyId,
            ExchangeRate != 1.0m ? ExchangeRate : null,
            Notes,
            SupplierInvoiceNo,
            attachmentBase64,
            AttachmentFileName,
            items,
            additionalFees);
    }

    private void RecalculateTotals()
    {
        // UI preview only — authoritative calculation happens in Domain entity
        SubTotal = Items.Sum(i => i.LineTotal);

        decimal discountAmount;
        if (SelectedDiscountType == 1 && DiscountRate.HasValue)
        {
            discountAmount = SubTotal * DiscountRate.Value / 100m;
        }
        else
        {
            discountAmount = InvoiceDiscount;
        }

        decimal netAmount = SubTotal - discountAmount;

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

        OnPropertyChanged(nameof(ImportTotalInBaseCurrency));

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

        var product = Products.FirstOrDefault(p => p.Barcode == barcode);
        
        if (product == null)
        {
            // Try fetching from API
            var result = await _productService.GetByBarcodeAsync(barcode);
            if (result.IsSuccess && result.Value != null)
            {
                product = result.Value;
            }
            else
            {
                _soundService.PlayError();
                return false;
            }
        }

        var existingLine = Items.FirstOrDefault(i => i.ProductId == product.Id);
        if (existingLine != null)
        {
            existingLine.Quantity += 1;
            _soundService.PlaySuccess();
        }
        else
        {
            var line = new PurchaseInvoiceLineViewModel(Products, _soundService)
            {
                SelectedProduct = product,
                Quantity = 1,
                UnitCost = product.PurchasePrice
            };
            
            line.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(PurchaseInvoiceLineViewModel.LineTotal))
                {
                    RecalculateTotals();
                }
            };
            
            var lastLine = Items.LastOrDefault();
            if (lastLine != null && lastLine.SelectedProduct == null)
            {
                Items.Remove(lastLine);
            }
            
            Items.Add(line);
            _soundService.PlaySuccess();
        }

        RecalculateTotals();
        return true;
    }

    private void SearchProduct(object? parameter)
    {
        var vm = new ViewModels.Products.ProductSelectionViewModel(SelectedWarehouseId);
        
        // Handle continuous selection
        vm.OnProductSelected += (product) => 
        {
            var existingLine = Items.FirstOrDefault(i => i.ProductId == product.Id);
            if (existingLine != null)
            {
                existingLine.Quantity += 1;
                _soundService.PlaySuccess();
            }
            else
            {
                var line = new PurchaseInvoiceLineViewModel(Products, _soundService)
                {
                    SelectedProduct = product,
                    Quantity = 1,
                    UnitCost = product.PurchasePrice
                };
                
                line.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(PurchaseInvoiceLineViewModel.LineTotal))
                    {
                        RecalculateTotals();
                    }
                };
                
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

    private void SearchProductSingle(object? parameter)
    {
        var targetLine = parameter as PurchaseInvoiceLineViewModel;
        var vm = new ViewModels.Products.ProductSelectionViewModel(SelectedWarehouseId);
        bool picked = false;

        vm.OnProductSelected += (product) =>
        {
            if (picked) return;
            picked = true;

            if (targetLine != null)
            {
                targetLine.SelectedProduct = product;
                targetLine.Quantity = 1;
                targetLine.UnitCost = product.PurchasePrice;
                RecalculateTotals();
                _soundService.PlaySuccess();
            }
            else
            {
                var existingLine = Items.FirstOrDefault(i => i.ProductId == product.Id);
                if (existingLine != null)
                {
                    existingLine.Quantity += 1;
                    _soundService.PlaySuccess();
                }
                else
                {
                    var line = new PurchaseInvoiceLineViewModel(Products, _soundService)
                    {
                        SelectedProduct = product,
                        Quantity = 1,
                        UnitCost = product.PurchasePrice
                    };
                    line.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(PurchaseInvoiceLineViewModel.LineTotal))
                            RecalculateTotals();
                    };
                    var lastLine = Items.LastOrDefault();
                    if (lastLine != null && lastLine.SelectedProduct == null)
                        Items.Remove(lastLine);
                    Items.Add(line);
                    _soundService.PlaySuccess();
                }
                RecalculateTotals();
            }

            System.Windows.Application.Current.Dispatcher.Invoke(() => vm.CloseDialog());
        };

        _dialogService.ShowDialog(vm);
    }

    private void SearchSupplier()
    {
        var vm = new ViewModels.Suppliers.SupplierSelectionViewModel();
        if (_dialogService.ShowDialog(vm) && vm.SelectedSupplier != null)
        {
            SelectedSupplierId = vm.SelectedSupplier.Id;
            _soundService.PlaySuccess();
        }
    }

    private void UpdateCommandStates()
    {
        // No-op: buttons remain enabled per interactive validation pattern (RULE-059)
    }

    private async Task UploadAttachmentAsync()
    {
        await ExecuteAsync(async () =>
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "ملفات PDF|*.pdf|صور|*.jpg;*.jpeg;*.png|جميع الملفات|*.*",
                Title = "اختيار ملف مرفق للفاتورة"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                AttachmentFileName = System.IO.Path.GetFileName(openFileDialog.FileName);
                AttachmentFileData = await System.IO.File.ReadAllBytesAsync(openFileDialog.FileName);
                _toastService.ShowSuccess($"تم إرفاق الملف: {AttachmentFileName}");
            }
        }, "جاري رفع الملف...");
    }

    private void RemoveAttachment()
    {
        AttachmentFileName = null;
        AttachmentFileData = null;
        OnPropertyChanged(nameof(HasAttachment));
        _toastService.ShowSuccess("تم إزالة المرفق");
    }

    private async Task AddFeeAsync()
    {
        // Open a simple input dialog for fee entry
        var feeVm = new AdditionalFeeInputViewModel(_dialogService);
        if (_dialogService.ShowDialog(feeVm) && feeVm.Result != null)
        {
            AdditionalFees.Add(feeVm.Result);
        }
    }

    private void RemoveFee(object? parameter)
    {
        if (parameter is AdditionalFeeDto fee)
        {
            AdditionalFees.Remove(fee);
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

public class AdditionalFeeInputViewModel : ViewModelBase
{
    private readonly IDialogService _dialogService;
    private decimal _feeAmount;
    private byte _distributionMethod;
    public AdditionalFeeInputViewModel(IDialogService dialogService)
    {
        _dialogService = dialogService;
        SaveCommand = new RelayCommand(Save);
        CancelCommand = new RelayCommand(_ => RequestClose());
    }

    public decimal FeeAmount
    {
        get => _feeAmount;
        set => SetProperty(ref _feeAmount, value);
    }

    public byte DistributionMethod
    {
        get => _distributionMethod;
        set => SetProperty(ref _distributionMethod, value);
    }

    public List<EnumDisplayItem> DistributionMethodOptions { get; } = new()
    {
        new EnumDisplayItem { Value = 1, Display = "حسب التكلفة" },
        new EnumDisplayItem { Value = 2, Display = "حسب الكمية" }
    };

    public AdditionalFeeDto? Result { get; private set; }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    private void Save(object? parameter)
    {
        if (FeeAmount <= 0)
        {
            _ = _dialogService.ShowWarningAsync("بيانات غير مكتملة", "• المبلغ يجب أن يكون أكبر من صفر");
            return;
        }
        Result = new AdditionalFeeDto(0, 0, string.Empty, FeeAmount, DistributionMethod, null);
        RequestClose();
    }
}

public class PurchaseInvoiceLineViewModel : ViewModelBase
{
    private int _productId;
    private ProductDto? _selectedProduct;
    private decimal _quantity = 1;
    private decimal _unitCost;
    private decimal _discountAmount;
    private decimal _oldCostInDatabase;
    private int _productUnitId;
    private byte _discountType;
    private decimal? _discountRate;
    private decimal? _costInBaseCurrency;
    private decimal _additionalFeesAmount;
    private string? _notes;

    public bool CostChangedFromDatabase =>
        _oldCostInDatabase > 0 &&
        Math.Abs(UnitCost - _oldCostInDatabase) > 0.0001m;

    public string PriceDifferenceIndicator
    {
        get
        {
            if (!CostChangedFromDatabase) return string.Empty;
            var diff = UnitCost - _oldCostInDatabase;
            var direction = diff > 0 ? "↑ ارتفع" : "↓ انخفض";
            return $"🔄 {direction} عن السعر القديم ({_oldCostInDatabase:N2}) | سيتم تحديث التكلفة في بطاقة الصنف عند الحفظ";
        }
    }

    private readonly ISoundService? _soundService;
    public ObservableCollection<ProductDto> AvailableProducts { get; }

    public PurchaseInvoiceLineViewModel(ObservableCollection<ProductDto> products, ISoundService? soundService = null)
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
                ProductUnitId = Mode == (byte)SaleMode.Wholesale
                    ? (value.WholesaleUnitId ?? value.RetailUnitId ?? 0)
                    : (value.RetailUnitId ?? value.WholesaleUnitId ?? 0);
                ClearErrors(nameof(ProductName));
                _oldCostInDatabase = value.PurchasePrice;
                OnPropertyChanged(nameof(CostChangedFromDatabase));
                OnPropertyChanged(nameof(PriceDifferenceIndicator));
                if (UnitCost == 0)
                {
                    UnitCost = Mode == (byte)SaleMode.Wholesale
                        ? value.PurchasePrice * value.ConversionFactor
                        : value.PurchasePrice;
                }
            }
        }
    }

    public string ProductName => SelectedProduct?.Name ?? string.Empty;

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

    public decimal UnitCost
    {
        get => _unitCost;
        set
        {
            if (SetProperty(ref _unitCost, value))
            {
                ValidateUnitCost();
                OnPropertyChanged(nameof(LineTotal));
                OnPropertyChanged(nameof(CostChangedFromDatabase));
                OnPropertyChanged(nameof(PriceDifferenceIndicator));
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

    public int ProductUnitId
    {
        get => _productUnitId;
        set => SetProperty(ref _productUnitId, value);
    }

    public byte DiscountType
    {
        get => _discountType;
        set
        {
            if (SetProperty(ref _discountType, value))
            {
                OnPropertyChanged(nameof(LineTotal));
            }
        }
    }

    public decimal? DiscountRate
    {
        get => _discountRate;
        set
        {
            if (SetProperty(ref _discountRate, value))
            {
                OnPropertyChanged(nameof(LineTotal));
            }
        }
    }

    public decimal? CostInBaseCurrency
    {
        get => _costInBaseCurrency;
        set => SetProperty(ref _costInBaseCurrency, value);
    }

    public decimal AdditionalFeesAmount
    {
        get => _additionalFeesAmount;
        set => SetProperty(ref _additionalFeesAmount, value);
    }

    public string? Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }

    public List<DiscountOption> DiscountTypeOptions { get; } = new()
    {
        new DiscountOption { Value = 0, Display = "مبلغ" },
        new DiscountOption { Value = 1, Display = "نسبة مئوية" }
    };

    private byte _mode = (byte)SaleMode.Retail;
    public byte Mode
    {
        get => _mode;
        set
        {
            if (SetProperty(ref _mode, value))
            {
                if (SelectedProduct != null)
                {
                    UnitCost = value == (byte)SaleMode.Wholesale
                        ? SelectedProduct.PurchasePrice * SelectedProduct.ConversionFactor
                        : SelectedProduct.PurchasePrice;
                    ProductUnitId = value == (byte)SaleMode.Wholesale
                        ? (SelectedProduct.WholesaleUnitId ?? SelectedProduct.RetailUnitId ?? 0)
                        : (SelectedProduct.RetailUnitId ?? SelectedProduct.WholesaleUnitId ?? 0);
                }
                OnPropertyChanged(nameof(LineTotal));
            }
        }
    }

    public decimal LineTotal
    {
        get
        {
            var lineTotal = Quantity * UnitCost;
            if (DiscountType == 1 && DiscountRate.HasValue)
            {
                return lineTotal - (lineTotal * DiscountRate.Value / 100m);
            }
            return lineTotal - DiscountAmount;
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

    private void ValidateUnitCost()
    {
        ClearErrors(nameof(UnitCost));
        if (UnitCost < 0)
        {
            AddError(nameof(UnitCost), "التكلفة لا يمكن أن تكون سالبة");
        }
    }

    private void ValidateDiscountType()
    {
        ClearErrors(nameof(DiscountType));
        if (DiscountType > 1)
        {
            AddError(nameof(DiscountType), "نوع الخصم غير صالح");
        }
    }

    private void ValidateDiscountRate()
    {
        ClearErrors(nameof(DiscountRate));
        if (DiscountType == 1 && (!DiscountRate.HasValue || DiscountRate.Value < 0 || DiscountRate.Value > 100))
        {
            AddError(nameof(DiscountRate), "نسبة الخصم يجب أن تكون بين 0 و 100");
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

public class DiscountOption
{
    public byte Value { get; set; }
    public string Display { get; set; } = string.Empty;
}
