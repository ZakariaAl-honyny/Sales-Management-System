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
using SalesSystem.DesktopPWF.Common;
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
    private readonly IProductUnitApiService _unitService;
    private readonly IProductPriceApiService _priceService;
    private readonly IInventoryApiService? _inventoryService;
    private readonly ITaxesApiService? _taxService;
    private readonly Dictionary<int, List<ProductUnitDto>> _productUnitsCache = new();
    private readonly Dictionary<int, List<ProductPriceDto>> _productPricesCache = new();

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

    // Attachment fields
    private string? _attachmentPath;
    private string? _attachmentFileName;
    private bool _hasAttachment;
    private string? _attachmentBase64;

    private ObservableCollection<PurchaseInvoiceLineViewModel> _items = new();
    private ObservableCollection<SupplierDto> _suppliers = new();
    private ObservableCollection<WarehouseDto> _warehouses = new();
    private ObservableCollection<ProductDto> _products = new();
    private ObservableCollection<TaxDto> _taxes = new();
    private int? _taxId;

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
        IProductUnitApiService unitService,
        IProductPriceApiService priceService,
        IInventoryApiService? inventoryService = null,
        ITaxesApiService? taxService = null,
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
        _unitService = unitService ?? throw new ArgumentNullException(nameof(unitService));
        _priceService = priceService ?? throw new ArgumentNullException(nameof(priceService));
        _inventoryService = inventoryService;
        _taxService = taxService;
        _invoiceId = invoiceId;
        _isEditMode = invoiceId.HasValue;
        IsReadOnly = isReadOnly;

        if (!invoiceId.HasValue)
        {
            InvoiceNo = 0; // 0 = auto-generate via DocumentSequenceService (thread-safe)
        }

        SaveDraftCommand = new AsyncRelayCommand(SaveDraftAsync);
        SaveAndPostCommand = new AsyncRelayCommand(SaveAndPostAsync);
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
        // Populate mock expense accounts for the UI binding
        ExpenseAccounts.Add(new ExpenseAccountItem { Id = 1, Name = "أجور نقل" });
        ExpenseAccounts.Add(new ExpenseAccountItem { Id = 2, Name = "تغليف" });
        OtherChargesAccountId = 1;

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
            App.GetService<IProductUnitApiService>(),
            App.GetService<IProductPriceApiService>(),
            App.GetService<IInventoryApiService>(),
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
                // Reload stock for all lines with products when warehouse changes
                foreach (var line in Items)
                {
                    if (line.SelectedProduct != null)
                    {
                        _ = LoadStockForLineAsync(line, line.SelectedProduct.Id);
                    }
                }
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
                RemainingAmount = NetTotal - _paidAmount;
                UpdateCommandStates();
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

    // UI Binding properties for POS layout
    private int _discountTypeIndex;
    public int DiscountTypeIndex
    {
        get => _discountTypeIndex;
        set
        {
            if (SetProperty(ref _discountTypeIndex, value))
            {
                if (_selectedDiscountType != (byte)value)
                {
                    _selectedDiscountType = (byte)value;
                    OnPropertyChanged(nameof(SelectedDiscountType));
                    RecalculateTotals();
                }
            }
        }
    }

    private int? _otherChargesAccountId;
    public int? OtherChargesAccountId
    {
        get => _otherChargesAccountId;
        set => SetProperty(ref _otherChargesAccountId, value);
    }

    public class ExpenseAccountItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private ObservableCollection<ExpenseAccountItem> _expenseAccounts = new();
    public ObservableCollection<ExpenseAccountItem> ExpenseAccounts
    {
        get => _expenseAccounts;
        set => SetProperty(ref _expenseAccounts, value);
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

    private decimal _otherCharges;
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

    // Discount fields
    private byte _selectedDiscountType;
    public byte SelectedDiscountType
    {
        get => _selectedDiscountType;
        set
        {
            if (SetProperty(ref _selectedDiscountType, value))
            {
                if (_discountTypeIndex != (int)value)
                {
                    _discountTypeIndex = (int)value;
                    OnPropertyChanged(nameof(DiscountTypeIndex));
                }
                RecalculateTotals();
            }
        }
    }

    private decimal? _discountRate;
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

    // Hide tax setting
    private bool _hideTaxInPurchases;
    public bool HideTaxInPurchases
    {
        get => _hideTaxInPurchases;
        set { if (SetProperty(ref _hideTaxInPurchases, value)) OnPropertyChanged(nameof(IsTaxSectionVisible)); }
    }

    public bool IsTaxSectionVisible => !_hideTaxInPurchases;

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
    public ICommand SaveDraftCommand { get; }
    public ICommand SaveAndPostCommand { get; }
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

            var systemSettingsResult = await _settingsService.GetAllSystemSettingsAsync();
            if (systemSettingsResult.IsSuccess && systemSettingsResult.Value != null)
            {
                if (systemSettingsResult.Value.TryGetValue("HideTaxInPurchases", out var hideTaxValue)
                    && bool.TryParse(hideTaxValue, out var hideTax))
                {
                    HideTaxInPurchases = hideTax;
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
                SelectedSupplierId = invoice.SupplierId;
                InvoiceDate = invoice.InvoiceDate.ToDateTime(TimeOnly.MinValue);
                SelectedPaymentType = (byte)invoice.PaymentType;
                InvoiceDiscount = invoice.DiscountAmount;
                SelectedDiscountType = invoice.DiscountType;
                DiscountRate = invoice.DiscountRate;
                OtherCharges = invoice.OtherCharges;
                PaidAmount = invoice.PaidAmount;
                Notes = invoice.Notes;
                Status = invoice.Status;

                TaxId = invoice.TaxId;

                if (invoice.Status != (byte)InvoiceStatus.Draft)
                {
                    IsReadOnly = true;
                    OnPropertyChanged(nameof(IsReadOnly));
                }
                Items.Clear();
                foreach (var item in invoice.Items)
                {
                    var lineVm = new PurchaseInvoiceLineViewModel(Products, _soundService, _unitService, _priceService);
                    lineVm.ProductId = item.ProductId;
                    lineVm.Quantity = item.Quantity;
                    lineVm.UnitCost = item.UnitPrice;
                    lineVm.SelectedProduct = Products.FirstOrDefault(p => p.Id == item.ProductId);
                    lineVm.ProductUnitId = item.ProductUnitId;

                    // Select saved unit if available
                    if (item.ProductUnitId > 0)
                    {
                        var savedUnit = lineVm.AvailableProductUnits.FirstOrDefault(u => u.Id == item.ProductUnitId);
                        if (savedUnit != null)
                            lineVm.SelectedProductUnit = savedUnit;
                    }

                    lineVm.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(PurchaseInvoiceLineViewModel.LineTotal))
                        {
                            RecalculateTotals();
                        }
                    };
                    Items.Add(lineVm);
                    _ = LoadStockForLineAsync(lineVm, item.ProductId);
                }

                RecalculateTotals();
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل الفاتورة", "PurchaseInvoiceEditorViewModel.LoadInvoiceAsync", $"[PurchaseInvoiceEditorViewModel.LoadInvoiceAsync] Failed to load purchase invoice ID {_invoiceId}.");
                await _dialogService.ShowErrorAsync("خطأ في تحميل البيانات", ErrorMessage!);
            }
        });
    }

    private async Task SaveDraftAsync()
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

                await _dialogService.ShowInfoAsync("نجاح", "✅ تم حفظ المسودة بنجاح. يمكنك الآن الترحيل النهائي للمخزون.");
                _eventBus.Publish(new PurchaseInvoiceChangedMessage(_invoiceId.Value));
                UpdateCommandStates();
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشل في حفظ المسودة", "PurchaseInvoiceEditorViewModel.SaveDraftAsync", "[PurchaseInvoiceEditorViewModel.SaveDraftAsync] Failed to save purchase invoice draft.");
                await _dialogService.ShowErrorAsync("خطأ في حفظ المسودة", ErrorMessage!);
            }
        });
    }

    private async Task SaveAndPostAsync()
    {
        if (!await ValidateInvoice()) return;

        await ExecuteAsync(async () =>
        {
            ErrorMessage = null;
            var request = BuildRequest();

            Result<PurchaseInvoiceDto> saveResult;
            if (_isEditMode)
            {
                var updateRequest = BuildUpdateRequest();
                saveResult = await _invoiceService.UpdateAsync(_invoiceId!.Value, updateRequest);
            }
            else
            {
                saveResult = await _invoiceService.CreateAsync(request);
            }

            if (saveResult.IsSuccess && saveResult.Value != null)
            {
                _invoiceId = saveResult.Value.Id;
                _status = saveResult.Value.Status;
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

                // Now post the invoice
                var postResult = await _invoiceService.PostAsync(_invoiceId.Value);
                if (postResult.IsSuccess)
                {
                    _eventBus.Publish(new PurchaseInvoiceChangedMessage(_invoiceId.Value));
                    await _dialogService.ShowSuccessAsync("نجاح", "✅ تم حفظ وترحيل فاتورة الشراء بنجاح.");
                    RequestClose();
                }
                else
                {
                    ErrorMessage = HandleFailure(postResult.Error ?? "فشل في ترحيل الفاتورة", "PurchaseInvoiceEditorViewModel.SaveAndPostAsync",
                        $"[PurchaseInvoiceEditorViewModel.SaveAndPostAsync] Failed to post purchase invoice ID {_invoiceId} after save.");
                    await _dialogService.ShowErrorAsync("خطأ في الترحيل", ErrorMessage!);
                }
            }
            else
            {
                ErrorMessage = HandleFailure(saveResult.Error ?? "فشل في حفظ الفاتورة", "PurchaseInvoiceEditorViewModel.SaveAndPostAsync",
                    "[PurchaseInvoiceEditorViewModel.SaveAndPostAsync] Failed to save purchase invoice before posting.");
                await _dialogService.ShowErrorAsync("خطأ في الحفظ", ErrorMessage!);
            }
        });
    }

    private async Task PostAsync()
    {
        // Must have a saved invoice to post
        if (!_isEditMode || _invoiceId == null)
        {
            await _dialogService.ShowWarningAsync("ترحيل فاتورة الشراء", "يجب حفظ الفاتورة أولاً قبل الترحيل.\nاستخدم زر 'حفظ وترحيل' للحفظ والترحيل مرة واحدة.");
            return;
        }

        if (!await _dialogService.ShowConfirmationAsync("تأكيد الترحيل", "هل أنت متأكد من ترحيل هذه الفاتورة؟\nسيتم إضافة الكميات إلى المخزون.")) return;

        await ExecuteAsync(async () =>
        {
            ErrorMessage = null;
            var postResult = await _invoiceService.PostAsync(_invoiceId!.Value);
            if (postResult.IsSuccess)
            {
                await _dialogService.ShowSuccessAsync("نجاح", "تم ترحيل الفاتورة بنجاح");
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
        var line = new PurchaseInvoiceLineViewModel(Products, _soundService, _unitService, _priceService);
        line.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(PurchaseInvoiceLineViewModel.LineTotal))
            {
                RecalculateTotals();
            }
        };
        Items.Add(line);
        _ = LoadStockForLineAsync(line, line.ProductId);
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
            .Select(i => new CreatePurchaseInvoiceLineRequest(
                i.SelectedProduct!.Id,
                i.ProductUnitId > 0 ? i.ProductUnitId : 1,
                i.Quantity,
                i.UnitCost))
            .ToList();

        return new UpdatePurchaseInvoiceRequest(
            SelectedWarehouseId,
            SelectedSupplierId ?? 0,
            InvoiceDate,
            (PaymentType?)SelectedPaymentType,
            InvoiceDiscount,
            (DiscountType?)SelectedDiscountType,
            DiscountRate,
            TaxAmount,
            OtherCharges,
            PaidAmount,
            Notes,
            TaxId,
            !string.IsNullOrEmpty(AttachmentPath) ? AttachmentPath : null,
            items);
    }

    private CreatePurchaseInvoiceRequest BuildRequest()
    {
        var items = Items
            .Where(i => i.SelectedProduct != null && i.Quantity > 0)
            .Select(i => new CreatePurchaseInvoiceLineRequest(
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
            (PaymentType)SelectedPaymentType,
            InvoiceDiscount,
            (DiscountType?)SelectedDiscountType,
            DiscountRate,
            TaxAmount,
            OtherCharges,
            PaidAmount,
            Notes,
            TaxId,
            !string.IsNullOrEmpty(AttachmentPath) ? AttachmentPath : null,
            items);
    }

    private async Task LoadStockForLineAsync(PurchaseInvoiceLineViewModel line, int productId)
    {
        if (_inventoryService == null || SelectedWarehouseId <= 0 || productId <= 0) return;
        try
        {
            var result = await _inventoryService.GetStockAsync(productId, SelectedWarehouseId);
            if (result.IsSuccess)
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => line.StockInBaseUnit = result.Value);
            }
        }
        catch
        {
            /* silent — stock display is non-critical */
        }
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
            TaxAmount = netAmount > 0 ? Math.Round((netAmount * TaxRate) / (100 + TaxRate), 2) : 0;
            NetTotal = netAmount + OtherCharges;
        }
        else
        {
            TaxAmount = netAmount > 0 ? Math.Round(netAmount * (TaxRate / 100), 2) : 0;
            NetTotal = netAmount + TaxAmount + OtherCharges;
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

        var product = Products.FirstOrDefault(p => p.Name.Contains(barcode, StringComparison.OrdinalIgnoreCase));
        
        if (product == null)
        {
            // Try fetching from API via barcode (stub — barcode lookup returns not found until UnitBarcode is implemented)
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
            var line = new PurchaseInvoiceLineViewModel(Products, _soundService, _unitService, _priceService)
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
            _ = LoadStockForLineAsync(line, line.ProductId);
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
                var line = new PurchaseInvoiceLineViewModel(Products, _soundService, _unitService, _priceService)
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
                _ = LoadStockForLineAsync(line, line.ProductId);
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
                _ = LoadStockForLineAsync(targetLine, product.Id);
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
                    var line = new PurchaseInvoiceLineViewModel(Products, _soundService, _unitService, _priceService)
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
                    _ = LoadStockForLineAsync(line, line.ProductId);
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

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    private void Save(object? parameter)
    {
        if (FeeAmount <= 0)
        {
            _ = _dialogService.ShowWarningAsync("بيانات غير مكتملة", "• المبلغ يجب أن يكون أكبر من صفر");
            return;
        }

        RequestClose();
    }
}

public class PurchaseInvoiceLineViewModel : ViewModelBase
{
    private int _productId;
    private int _productUnitId;
    private ProductDto? _selectedProduct;
    private decimal _quantity = 1;
    private decimal _unitCost;
    private decimal _oldCostInDatabase;
    private decimal _lineTotalInput;  // Editable gross total (Qty × UnitCost), before discount
    private FlexibleInputCalculator.CalculationField? _lastModifiedField;
    private bool _isRecalculating;
    private decimal _stockInBaseUnit;
    private ObservableCollection<ProductUnitDto> _availableProductUnits = new();
    private ProductUnitDto? _selectedProductUnit;

    /// <summary>
    /// Stock quantity in base unit for this product in the selected warehouse.
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
    /// </summary>
    public string AvailableStockText
    {
        get
        {
            if (SelectedProductUnit == null || _stockInBaseUnit <= 0)
                return "---";
            var stockInUnit = SelectedProductUnit.IsBaseUnit
                ? _stockInBaseUnit
                : _stockInBaseUnit / SelectedProductUnit.ConversionFactor;
            if (stockInUnit < 1)
                return $"{stockInUnit:N1} {SelectedProductUnit.UnitName}";
            return $"{stockInUnit:N0} {SelectedProductUnit.UnitName}";
        }
    }

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
    private readonly IProductUnitApiService? _unitService;
    private readonly IProductPriceApiService? _priceService;
    public ObservableCollection<ProductDto> AvailableProducts { get; }

    public PurchaseInvoiceLineViewModel(
        ObservableCollection<ProductDto> products,
        ISoundService? soundService = null,
        IProductUnitApiService? unitService = null,
        IProductPriceApiService? priceService = null)
    {
        AvailableProducts = products;
        _soundService = soundService;
        _unitService = unitService;
        _priceService = priceService;
        _lineTotalInput = _quantity * _unitCost;  // Initialize: Qty (1) × Cost (0) = 0
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

    /// <summary>
    /// Available units for the selected product — populated when a product is chosen.
    /// </summary>
    public ObservableCollection<ProductUnitDto> AvailableProductUnits
    {
        get => _availableProductUnits;
        set => SetProperty(ref _availableProductUnits, value);
    }

    /// <summary>
    /// The currently selected product unit. Changing this updates the unit cost
    /// from the ProductPrices table.
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
                // Load cost for the newly selected unit
                _ = LoadCostForSelectedUnitAsync(value.Id);
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
                _oldCostInDatabase = 0m;
                OnPropertyChanged(nameof(CostChangedFromDatabase));
                OnPropertyChanged(nameof(PriceDifferenceIndicator));
                // Load available units for this product
                _ = LoadUnitsForProductAsync(value.Id);
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
                _lastModifiedField = FlexibleInputCalculator.CalculationField.Quantity;
                RecalculateFromFlexibleInput();
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
                _lastModifiedField = FlexibleInputCalculator.CalculationField.Price;
                RecalculateFromFlexibleInput();
                OnPropertyChanged(nameof(CostChangedFromDatabase));
                OnPropertyChanged(nameof(PriceDifferenceIndicator));
            }
        }
    }

    public decimal LineTotal => Quantity * UnitCost;

    /// <summary>
    /// Editable gross total (Quantity × UnitCost).
    /// When user edits this field, the system recalculates either Quantity or UnitCost
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
    /// Recalculates the third field based on which two fields the user has entered.
    /// Uses <see cref="FlexibleInputCalculator"/> to determine the missing value.
    /// A guard flag (<c>_isRecalculating</c>) prevents infinite recursion when
    /// setting computed values triggers property-changed callbacks.
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
                    _quantity, _unitCost, _lineTotalInput,
                    FlexibleInputCalculator.CalculationField.Total);

                _quantity = result.quantity;
                _unitCost = result.price;
                _lineTotalInput = result.total;

                OnPropertyChanged(nameof(Quantity));
                OnPropertyChanged(nameof(UnitCost));
                OnPropertyChanged(nameof(LineTotalInput));
            }
            else
            {
                // User edited Quantity or UnitCost — just recompute the total.
                _lineTotalInput = _quantity * _unitCost;
                OnPropertyChanged(nameof(LineTotalInput));
            }

            OnPropertyChanged(nameof(LineTotal));
        }
        finally
        {
            _isRecalculating = false;
        }
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
            System.Diagnostics.Debug.WriteLine($"Failed to load units for product {productId}: {ex.Message}");
        }
    }

    /// <summary>
    /// Loads the effective cost for the selected product unit from ProductPrices table.
    /// </summary>
    private async Task LoadCostForSelectedUnitAsync(int productUnitId)
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
                    var price = activePrices[0];
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        UnitCost = price.Price;
                        OnPropertyChanged(nameof(CostChangedFromDatabase));
                        OnPropertyChanged(nameof(PriceDifferenceIndicator));
                    });
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load cost for unit {productUnitId}: {ex.Message}");
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


