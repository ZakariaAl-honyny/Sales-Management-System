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
using Microsoft.Win32;

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
    private readonly IPrintApiService _printApiService;
    private readonly IToastNotificationService _toastService;
    private readonly ICurrencyApiService _currencyService;

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
    private string? _notes;
    private bool _isEditMode;
    private string? _errorMessage;
    private byte _status = (byte)InvoiceStatus.Draft;
    public bool IsReadOnly { get; private set; }

    // Currency fields
    private int? _selectedCurrencyId;
    private decimal? _exchangeRate;
    private bool _isForeignCurrency;

    // Attachment fields
    private string? _attachmentPath;
    private string? _attachmentFileName;
    private bool _hasAttachment;
    private string? _attachmentBase64;

    private ObservableCollection<PurchaseInvoiceLineViewModel> _items = new();
    private ObservableCollection<SupplierDto> _suppliers = new();
    private ObservableCollection<WarehouseDto> _warehouses = new();
    private ObservableCollection<ProductDto> _products = new();
    private ObservableCollection<CurrencyDto> _currencies = new();

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
        IPrintApiService printApiService,
        IToastNotificationService toastService,
        ICurrencyApiService currencyService,
        int? invoiceId = null,
        bool isReadOnly = false)
    {
        _invoiceService = invoiceService ?? throw new ArgumentNullException(nameof(invoiceService));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _supplierService = supplierService ?? throw new ArgumentNullException(nameof(supplierService));
        _warehouseService = warehouseService;
        _productService = productService ?? throw new ArgumentNullException(nameof(productService));
        _settingsService = settingsService;
        _dialogService = dialogService;
        SetDialogService(dialogService);
        _soundService = soundService;
        _barcodeService = barcodeService;
        _printApiService = printApiService;
        _toastService = toastService;
        _currencyService = currencyService ?? throw new ArgumentNullException(nameof(currencyService));
        _invoiceId = invoiceId;
        _isEditMode = invoiceId.HasValue;
        IsReadOnly = isReadOnly;

        if (!invoiceId.HasValue)
        {
            InvoiceNo = 0; // 0 = auto-generate via DocumentSequenceService (thread-safe)
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
        BrowseAttachmentCommand = new RelayCommand(BrowseAttachment);
        RemoveAttachmentCommand = new RelayCommand(RemoveAttachment);

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await ExecuteAsync(InitializeOperationAsync, "جاري تحميل البيانات...");
    }

    private async Task InitializeOperationAsync()
    {
        await LoadReferenceDataAsync();

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
            App.GetService<IPrintApiService>(),
            App.GetService<IToastNotificationService>(),
            App.GetService<ICurrencyApiService>(),
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

    private decimal _netTotal;
    public decimal NetTotal
    {
        get => _netTotal;
        private set => SetProperty(ref _netTotal, value);
    }

    private decimal _remainingAmount;
    public decimal RemainingAmount
    {
        get => _remainingAmount;
        private set => SetProperty(ref _remainingAmount, value);
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

    // Attachment properties
    public string? AttachmentPath
    {
        get => _attachmentPath;
        set => SetProperty(ref _attachmentPath, value);
    }

    public string? AttachmentFileName
    {
        get => _attachmentFileName;
        set => SetProperty(ref _attachmentFileName, value);
    }

    public bool HasAttachment
    {
        get => _hasAttachment;
        set => SetProperty(ref _hasAttachment, value);
    }

    public string? AttachmentBase64
    {
        get => _attachmentBase64;
        set => SetProperty(ref _attachmentBase64, value);
    }
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
    public ICommand BrowseAttachmentCommand { get; }
    public ICommand RemoveAttachmentCommand { get; }
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
                Notes = invoice.Notes;
                Status = invoice.Status;

                // Restore currency
                SelectedCurrencyId = invoice.CurrencyId;
                ExchangeRate = invoice.ExchangeRate;

                // Restore attachment
                if (!string.IsNullOrEmpty(invoice.AttachmentPath))
                {
                    _attachmentPath = invoice.AttachmentPath;
                    _attachmentFileName = System.IO.Path.GetFileName(invoice.AttachmentPath);
                    _hasAttachment = true;
                    OnPropertyChanged(nameof(AttachmentPath));
                    OnPropertyChanged(nameof(AttachmentFileName));
                    OnPropertyChanged(nameof(HasAttachment));
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
                    lineVm.SelectedProduct = Products.FirstOrDefault(p => p.Id == item.ProductId);
                    
                    lineVm.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(PurchaseInvoiceLineViewModel.LineTotal))
                        {
                            RecalculateTotals();
                        }
                    };
                    Items.Add(lineVm);
                }

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
                var updateRequest = BuildUpdateRequest();
                result = await _invoiceService.UpdateAsync(_invoiceId!.Value, updateRequest);
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

                // Upload attachment if there is pending attachment data
                if (!string.IsNullOrEmpty(_attachmentBase64) && _invoiceId.HasValue)
                {
                    var uploadResult = await _invoiceService.UploadAttachmentAsync(
                        _invoiceId.Value, _attachmentBase64, _attachmentFileName ?? "attachment.jpg");
                    if (uploadResult.IsSuccess)
                    {
                        _attachmentPath = uploadResult.Value;
                        OnPropertyChanged(nameof(AttachmentPath));
                    }
                    else
                    {
                        Serilog.Log.Warning("Failed to upload attachment for invoice {InvoiceId}: {Error}",
                            _invoiceId.Value, uploadResult.Error);
                    }
                }

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

        if (errors.Any())
        {
            await _dialogService.ShowValidationErrorsAsync("بيانات غير مكتملة", errors);
            RequestFocusFirstInvalidField();
            return false;
        }

        return true;
    }

    private UpdatePurchaseInvoiceRequest BuildUpdateRequest()
    {
        var items = Items
            .Where(i => i.SelectedProduct != null && i.Quantity > 0)
            .Select(i => new CreatePurchaseInvoiceItemRequest(
                i.SelectedProduct!.Id,
                i.ProductUnitId > 0 ? i.ProductUnitId : 1,
                i.Quantity,
                i.UnitCost))
            .ToList();

        return new UpdatePurchaseInvoiceRequest(
            SelectedWarehouseId,
            SelectedSupplierId ?? 0,
            InvoiceDate,
            null,
            (PaymentType)SelectedPaymentType,
            InvoiceDiscount,
            TaxAmount,
            PaidAmount,
            SelectedCurrencyId,
            ExchangeRate,
            Notes,
            items);
    }

    private CreatePurchaseInvoiceRequest BuildRequest()
    {
        var items = Items
            .Where(i => i.SelectedProduct != null && i.Quantity > 0)
            .Select(i => new CreatePurchaseInvoiceItemRequest(
                i.SelectedProduct!.Id,
                i.ProductUnitId > 0 ? i.ProductUnitId : 1,
                i.Quantity,
                i.UnitCost))
            .ToList();

        return new CreatePurchaseInvoiceRequest(
            SelectedWarehouseId,
            SelectedSupplierId ?? 0,
            InvoiceNo > 0 ? InvoiceNo : null,
            InvoiceDate,
            null,
            (PaymentType)SelectedPaymentType,
            InvoiceDiscount,
            TaxAmount,
            PaidAmount,
            SelectedCurrencyId,
            ExchangeRate,
            Notes,
            items);
    }

    private void RecalculateTotals()
    {
        // UI preview only — authoritative calculation happens in Domain entity
        SubTotal = Items.Sum(i => i.LineTotal);

        decimal netAmount = SubTotal - InvoiceDiscount;

        if (IsTaxInclusive)
        {
            TaxAmount = netAmount > 0 ? Math.Round((netAmount * TaxRate) / (100 + TaxRate), 2) : 0;
            NetTotal = netAmount;
        }
        else
        {
            TaxAmount = netAmount > 0 ? Math.Round(netAmount * (TaxRate / 100), 2) : 0;
            NetTotal = netAmount + TaxAmount;
        }

        if (SelectedPaymentType == (byte)PaymentType.Cash)
        {
            PaidAmount = NetTotal;
        }

        RemainingAmount = NetTotal - PaidAmount;

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
                UnitCost = 0m // Phase 25: TODO — use AverageCost from ProductUnit via ProductDto
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
                    UnitCost = 0m // Phase 25: TODO — use AverageCost from ProductUnit via ProductDto
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
                targetLine.UnitCost = 0m; // Phase 25: TODO — use AverageCost from ProductUnit via ProductDto
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
                        UnitCost = 0m // Phase 25: TODO — use AverageCost from ProductUnit via ProductDto
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

    // Attachment methods
    private void BrowseAttachment()
    {
        var openFileDialog = new OpenFileDialog
        {
            Title = "اختر ملف المرفق",
            Filter = "PDF Files (*.pdf)|*.pdf|Image Files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|All Files (*.*)|*.*",
            CheckFileExists = true
        };

        if (openFileDialog.ShowDialog() == true)
        {
            AttachmentPath = openFileDialog.FileName;
            AttachmentFileName = System.IO.Path.GetFileName(openFileDialog.FileName);
            HasAttachment = true;

            try
            {
                var fileBytes = System.IO.File.ReadAllBytes(openFileDialog.FileName);
                AttachmentBase64 = Convert.ToBase64String(fileBytes);
            }
            catch (Exception ex)
            {
                LogSystemError("فشل في قراءة الملف المرفق", "PurchaseInvoiceEditorViewModel.BrowseAttachment", ex);
                AttachmentBase64 = null;
                HasAttachment = false;
            }
        }
    }

    private void RemoveAttachment()
    {
        AttachmentPath = null;
        AttachmentFileName = null;
        AttachmentBase64 = null;
        HasAttachment = false;
    }

    private int GetBaseCurrencyId()
    {
        var baseCurrency = Currencies.FirstOrDefault(c => c.IsBaseCurrency);
        return baseCurrency?.Id ?? 0;
    }
    #endregion
}

public class PurchaseInvoiceLineViewModel : ViewModelBase
{
    private int _productId;
    private int _productUnitId;
    private ProductDto? _selectedProduct;
    private decimal _quantity = 1;
    private decimal _unitCost;
    private decimal _oldCostInDatabase;

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

    public int ProductUnitId
    {
        get => _productUnitId;
        set => SetProperty(ref _productUnitId, value);
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
                _oldCostInDatabase = 0m;
                ProductUnitId = 1; // Phase 25: TODO — replace with ProductDto.DefaultPurchaseUnitId when added to DTO
                OnPropertyChanged(nameof(CostChangedFromDatabase));
                OnPropertyChanged(nameof(PriceDifferenceIndicator));
                // Phase 25: TODO — set UnitCost from ProductDto.AverageCost when added to DTO
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

    public decimal LineTotal => Quantity * UnitCost;

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
}

public class PaymentTypeItem
{
    public byte Value { get; set; }
    public string Display { get; set; } = string.Empty;
}


