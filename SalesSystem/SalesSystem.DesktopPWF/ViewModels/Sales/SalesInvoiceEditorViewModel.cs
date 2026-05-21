using SalesSystem.DesktopPWF.Messaging.Messages;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Enums;
using SalesSystem.Contracts.Requests;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
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
    private readonly IInvoicePrinter _invoicePrinter;
    private readonly IReceiptPrinter _receiptPrinter;
    private readonly IDialogService _dialogService;
    private readonly ISoundService _soundService;
    private readonly IInventoryApiService _inventoryService;
    private readonly IBarcodeInputService _barcodeService;

    private int? _invoiceId;
    private string? _invoiceNo;
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
    private bool _isLoading;
    private bool _isEditMode;
    private string? _errorMessage;
    private bool _allowNegativeStock;
    private byte _status = (byte)InvoiceStatus.Draft;
    public bool IsReadOnly { get; private set; }

    private ObservableCollection<InvoiceLineViewModel> _items = new();
    private ObservableCollection<CustomerDto> _customers = new();
    private ObservableCollection<WarehouseDto> _warehouses = new();
    private ObservableCollection<ProductDto> _products = new();

    public SalesInvoiceEditorViewModel(
        ISalesInvoiceApiService invoiceService,
        IEventBus eventBus,
        ICustomerApiService customerService,
        IWarehouseApiService warehouseService,
        IProductApiService productService,
        ISettingsApiService settingsService,
        IInvoicePrinter invoicePrinter,
        IReceiptPrinter receiptPrinter,
        IDialogService dialogService,
        ISoundService soundService,
        IInventoryApiService inventoryService,
        IBarcodeInputService barcodeService,
        int? invoiceId = null,
        bool isReadOnly = false)
    {
        _invoiceService = invoiceService;
        _eventBus = eventBus;
        _customerService = customerService;
        _warehouseService = warehouseService;
        _productService = productService;
        _settingsService = settingsService;
        _invoicePrinter = invoicePrinter;
        _receiptPrinter = receiptPrinter;
        _dialogService = dialogService;
        _soundService = soundService;
        _inventoryService = inventoryService;
        _barcodeService = barcodeService;
        _invoiceId = invoiceId;
        _isEditMode = invoiceId.HasValue;
        IsReadOnly = isReadOnly;

        SaveCommand = new AsyncRelayCommand(SaveAsync, () => CanSave());
        PostCommand = new AsyncRelayCommand(PostAsync, CanPost);
        CancelCommand = new RelayCommand(Cancel);
        AddLineCommand = new RelayCommand(AddLine);
        RemoveLineCommand = new RelayCommand(RemoveLine, CanRemoveLine);
        PrintA4Command = new AsyncRelayCommand(PrintA4Async, CanPrint);
        PrintReceiptCommand = new AsyncRelayCommand(PrintReceiptAsync, CanPrint);
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
        catch
        {
            // Silently ignore — settings are non-critical for basic functionality
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
            App.GetService<IInvoicePrinter>(),
            App.GetService<IReceiptPrinter>(),
            App.GetService<IDialogService>(),
            App.GetService<ISoundService>(),
            App.GetService<IInventoryApiService>(),
            App.GetService<IBarcodeInputService>(),
            invoiceId,
            isReadOnly)
    {
    }

    #region Properties
    public int? InvoiceId => _invoiceId;
    public string? InvoiceNo
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
    public ICommand PrintReceiptCommand { get; }
    public ICommand SearchProductCommand { get; }          // Continuous: stays open
    public ICommand SearchProductSingleCommand { get; }    // Single: closes after one pick
    public ICommand SearchCustomerCommand { get; }
    public ICommand ProcessBarcodeCommand { get; }
    public ICommand DeleteCommand { get; }
    #endregion

    #region Events
    #endregion

    #region Methods
    private async Task LoadReferenceDataAsync()
    {
        try
        {
            IsLoading = true;
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
                    // Select default warehouse or first one
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
        }
        catch (Exception ex)
        {
            ErrorMessage = HandleException(ex, "SalesInvoiceEditorViewModel.LoadReferenceDataAsync", "[SalesInvoiceEditorViewModel.LoadReferenceDataAsync] Failed to load customers, products, or warehouses.");
        }
        finally
        {
            if (!_isEditMode) IsLoading = false;
        }
    }

    private async Task LoadInvoiceAsync()
    {
        if (!_invoiceId.HasValue) return;

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var result = await _invoiceService.GetByIdAsync(_invoiceId.Value);
            if (result.IsSuccess && result.Value != null)
            {
                var invoice = result.Value;
                _invoiceNo = invoice.InvoiceNo;
                SelectedWarehouseId = invoice.WarehouseId;
                SelectedCustomerId = invoice.CustomerId;
                InvoiceDate = invoice.InvoiceDate;
                SelectedPaymentType = (byte)invoice.PaymentType;
                InvoiceDiscount = invoice.DiscountAmount;
                PaidAmount = invoice.PaidAmount;
                Notes = invoice.Notes;
                Status = invoice.Status;

                // If posted or cancelled, make it read-only automatically
                if (invoice.Status != (byte)InvoiceStatus.Draft)
                {
                    IsReadOnly = true;
                    OnPropertyChanged(nameof(IsReadOnly));
                }
                IsTaxInclusive = false; // Default
                OnPropertyChanged(nameof(InvoiceNo));

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

                RecalculateTotals();
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل الفاتورة", "SalesInvoiceEditorViewModel.LoadInvoiceAsync", $"[SalesInvoiceEditorViewModel.LoadInvoiceAsync] Failed to load invoice data for ID: {_invoiceId}");
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = HandleException(ex, "SalesInvoiceEditorViewModel.LoadInvoiceAsync", $"[SalesInvoiceEditorViewModel.LoadInvoiceAsync] Failed to load invoice data for ID: {_invoiceId}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task SaveAsync()
    {
        if (!ValidateInvoice()) return;

        IsLoading = true;
        ErrorMessage = null;

        try
        {
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
                _invoiceNo = result.Value.InvoiceNo;
                _status = result.Value.Status;
                _isEditMode = true;
                
                OnPropertyChanged(nameof(InvoiceNo));
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(IsEditMode));
                OnPropertyChanged(nameof(IsReadOnly));
                
                _dialogService.ShowSuccessAsync("نجاح", "✅ تم حفظ الفاتورة بنجاح. يمكنك الآن الترحيل النهائي إذا أردت.");
                _eventBus.Publish(new SaleInvoiceChangedMessage(_invoiceId.Value));
                UpdateCommandStates();
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشل في حفظ الفاتورة", "SalesInvoiceEditorViewModel.SaveAsync", "[SalesInvoiceEditorViewModel.SaveAsync] Failed to save sales invoice.");
                _ = _dialogService.ShowErrorAsync("خطأ", ErrorMessage!);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = HandleException(ex, "SalesInvoiceEditorViewModel.SaveAsync", "[SalesInvoiceEditorViewModel.SaveAsync] Failed to save sales invoice.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task PostAsync()
    {
        // First save if new
        if (!_isEditMode || _invoiceId == null)
        {
            await SaveAsync();
            if (_invoiceId == null) return;
        }

        if (!await _dialogService.ShowConfirmationAsync("تأكيد الترحيل", "هل أنت متأكد من ترحيل هذه الفاتورة؟\nسيتم خصم الكميات من المخزون.")) return;

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var postResult = await _invoiceService.PostAsync(_invoiceId!.Value);
            if (postResult.IsSuccess)
            {
                _ = _dialogService.ShowSuccessAsync("نجاح", "تم ترحيل الفاتورة بنجاح");
                _eventBus.Publish(new SaleInvoiceChangedMessage(_invoiceId.Value));
                RequestClose();
            }
            else
            {
                ErrorMessage = HandleFailure(postResult.Error ?? "فشل في ترحيل الفاتورة", "SalesInvoiceEditorViewModel.PostAsync", $"[SalesInvoiceEditorViewModel.PostAsync] Failed to post/confirm sales invoice ID {_invoiceId}.");
                _ = _dialogService.ShowErrorAsync("خطأ", ErrorMessage!);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = HandleException(ex, "SalesInvoiceEditorViewModel.PostAsync", $"[SalesInvoiceEditorViewModel.PostAsync] Failed to post/confirm sales invoice ID {_invoiceId}.");
            _ = _dialogService.ShowErrorAsync("خطأ", ErrorMessage!);
        }
        finally
        {
            IsLoading = false;
            UpdateCommandStates();
        }
    }

    private async Task DeleteAsync()
    {
        if (!_invoiceId.HasValue) return;

        if (!await _dialogService.ShowConfirmationAsync("تأكيد الإلغاء", "هل أنت متأكد من إلغاء هذه الفاتورة؟\nلا يمكن التراجع عن هذه العملية.")) return;

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var cancelResult = await _invoiceService.CancelAsync(_invoiceId.Value);
            if (cancelResult.IsSuccess)
            {
                _ = _dialogService.ShowSuccessAsync("نجاح", "تم إلغاء الفاتورة بنجاح");
                _eventBus.Publish(new SaleInvoiceChangedMessage(_invoiceId.Value));
                RequestClose();
            }
            else
            {
                ErrorMessage = cancelResult.Error ?? "فشل في إلغاء الفاتورة";
                _ = _dialogService.ShowErrorAsync("خطأ", ErrorMessage!);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = HandleException(ex, "SalesInvoiceEditorViewModel.DeleteAsync", $"[SalesInvoiceEditorViewModel.DeleteAsync] Failed to cancel invoice ID {_invoiceId}.");
            _ = _dialogService.ShowErrorAsync("خطأ", ErrorMessage!);
        }
        finally
        {
            IsLoading = false;
        }
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
        if (!_invoiceId.HasValue) return;

        IsLoading = true;
        try
        {
            // 1. Get Store Settings
            var settingsResult = await _settingsService.GetSettingsAsync();
            if (!settingsResult.IsSuccess || settingsResult.Value == null)
            {
                var error = HandleFailure(settingsResult.Error ?? "فشل في تحميل إعدادات المتجر", "SalesInvoiceEditorViewModel.PrepareAndPrint", "[SalesInvoiceEditorViewModel.PrepareAndPrint] Failed to load store settings for printing.");
                _dialogService.ShowError(error, "خطأ");
                return;
            }
            
            // 2. Get Full Invoice Data
            var invoiceResult = await _invoiceService.GetByIdAsync(_invoiceId.Value);
            if (!invoiceResult.IsSuccess || invoiceResult.Value == null)
            {
                var error = HandleFailure(invoiceResult.Error ?? "فشل في تحميل بيانات الفاتورة", "SalesInvoiceEditorViewModel.PrepareAndPrint", $"[SalesInvoiceEditorViewModel.PrepareAndPrint] Failed to load invoice data for printing ID {_invoiceId}.");
                _dialogService.ShowError(error, "خطأ");
                return;
            }

            // 3. Map to Print DTOs
            var storeInfo = settingsResult.Value.ToPrintDto();
            var invoice = invoiceResult.Value.ToPrintDto();
            var items = invoiceResult.Value.Items.ToPrintDtos();
            var totals = invoiceResult.Value.ToTotalsPrintDto();

            // 4. Show Preview
            printer.PrintPreview(invoice, items, totals, storeInfo);
        }
        catch (Exception ex)
        {
            var error = HandleException(ex, "SalesInvoiceEditorViewModel.PrepareAndPrint", $"[SalesInvoiceEditorViewModel.PrepareAndPrint] Unexpected error during print preparation for invoice ID {_invoiceId}.");
            _dialogService.ShowError(error, "خطأ");
        }
        finally
        {
            IsLoading = false;
        }
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

    private bool CanSave()
    {
        return !HasErrors && Items.Any(i => i.SelectedProduct != null && i.Quantity > 0);
    }

    private bool CanPost()
    {
        return CanSave() && SelectedWarehouseId > 0 && Status == (byte)InvoiceStatus.Draft;
    }

    private bool CanPrint()
    {
        return _invoiceId.HasValue;
    }

    private bool ValidateInvoice()
    {
        var errors = new List<string>();

        if (!Items.Any(i => i.SelectedProduct != null && i.Quantity > 0))
            errors.Add("• يجب إضافة صنف واحد على الأقل");

        if (SelectedWarehouseId <= 0)
            errors.Add("• يجب اختيار المستودع");

        if (SelectedPaymentType == (byte)PaymentType.Credit && !SelectedCustomerId.HasValue)
            errors.Add("• يجب اختيار العميل للفواتير الآجلة");

        if (errors.Any())
        {
            string errorMsg = "يرجى إكمال البيانات الإلزامية التالية:\n\n" + string.Join("\n", errors);
            _ = _dialogService.ShowWarningAsync("بيانات غير مكتملة", errorMsg);
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
            Application.Current.Dispatcher.Invoke(() => vm.CloseDialog());
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
        (SaveCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (PostCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (PrintA4Command as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (PrintReceiptCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
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

    public bool CanSave => !HasErrors && SelectedProduct != null && Quantity > 0;
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
