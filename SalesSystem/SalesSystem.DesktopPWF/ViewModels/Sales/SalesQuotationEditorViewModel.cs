using SalesSystem.DesktopPWF.Messaging.Messages;
using System.Collections.ObjectModel;
using System.Windows.Input;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Services.App.Toast;

namespace SalesSystem.DesktopPWF.ViewModels.Sales;

/// <summary>
/// ViewModel for Sales Quotation Editor
/// </summary>
public class SalesQuotationEditorViewModel : ViewModelBase
{
    private readonly ISalesQuotationApiService _quotationService;
    private readonly IEventBus _eventBus;
    private readonly ICustomerApiService _customerService;
    private readonly IWarehouseApiService _warehouseService;
    private readonly IProductApiService _productService;
    private readonly ICurrencyApiService _currencyService;
    private readonly IProductUnitApiService _unitService;
    private readonly IDialogService _dialogService;
    private readonly IToastNotificationService _toastService;

    private int? _quotationId;
    private int _quotationNo;
    private DateTime _quotationDate = DateTime.Today;
    private DateTime _validUntil = DateTime.Today.AddDays(7);
    private int? _selectedCustomerId;
    private int? _selectedWarehouseId;
    private int? _selectedCurrencyId;
    private decimal? _exchangeRate;
    private bool _isForeignCurrency;
    private decimal _discountAmount;
    private decimal _taxAmount;
    private string? _notes;
    private string? _termsAndConditions;
    private string? _errorMessage;
    private bool _isEditMode;
    private bool _isReadOnly;
    private byte _status;

    private ObservableCollection<QuotationLineItem> _items = new();
    private ObservableCollection<CustomerDto> _customers = new();
    private ObservableCollection<WarehouseDto> _warehouses = new();
    private ObservableCollection<CurrencyDto> _currencies = new();

    // Parameterless constructor for designer / direct instantiation
    public SalesQuotationEditorViewModel()
        : this(
            App.GetService<ISalesQuotationApiService>(),
            App.GetService<IEventBus>(),
            App.GetService<ICustomerApiService>(),
            App.GetService<IWarehouseApiService>(),
            App.GetService<IProductApiService>(),
            App.GetService<ICurrencyApiService>(),
            App.GetService<IProductUnitApiService>(),
            App.GetService<IDialogService>(),
            App.GetService<IToastNotificationService>())
    {
    }

    // Convenience constructor for list ViewModel calling with just ID + readOnly
    public SalesQuotationEditorViewModel(int? quotationId, bool isReadOnly = false)
        : this(
            App.GetService<ISalesQuotationApiService>(),
            App.GetService<IEventBus>(),
            App.GetService<ICustomerApiService>(),
            App.GetService<IWarehouseApiService>(),
            App.GetService<IProductApiService>(),
            App.GetService<ICurrencyApiService>(),
            App.GetService<IProductUnitApiService>(),
            App.GetService<IDialogService>(),
            App.GetService<IToastNotificationService>(),
            quotationId,
            isReadOnly)
    {
    }

    public SalesQuotationEditorViewModel(
        ISalesQuotationApiService quotationService,
        IEventBus eventBus,
        ICustomerApiService customerService,
        IWarehouseApiService warehouseService,
        IProductApiService productService,
        ICurrencyApiService currencyService,
        IProductUnitApiService unitService,
        IDialogService dialogService,
        IToastNotificationService toastService,
        int? quotationId = null,
        bool isReadOnly = false)
    {
        _quotationService = quotationService ?? throw new ArgumentNullException(nameof(quotationService));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _customerService = customerService ?? throw new ArgumentNullException(nameof(customerService));
        _warehouseService = warehouseService ?? throw new ArgumentNullException(nameof(warehouseService));
        _productService = productService ?? throw new ArgumentNullException(nameof(productService));
        _currencyService = currencyService ?? throw new ArgumentNullException(nameof(currencyService));
        _unitService = unitService ?? throw new ArgumentNullException(nameof(unitService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        SetDialogService(dialogService);
        _toastService = toastService ?? throw new ArgumentNullException(nameof(toastService));

        _quotationId = quotationId;
        _isEditMode = quotationId.HasValue;
        _isReadOnly = isReadOnly;

        if (!quotationId.HasValue)
        {
            QuotationNo = 0; // Service will compute next number
        }

        SaveCommand = new AsyncRelayCommand(SaveAsync);
        AddLineCommand = new RelayCommand(AddLine);
        RemoveLineCommand = new RelayCommand(RemoveLine, CanRemoveLine);
        CancelCommand = new RelayCommand(RequestClose);

        _ = LoadLookupDataAsync();
        if (quotationId.HasValue)
        {
            _ = LoadQuotationAsync(quotationId.Value);
        }
    }

    #region Properties

    public int? QuotationId { get => _quotationId; private set => SetProperty(ref _quotationId, value); }

    public int QuotationNo
    {
        get => _quotationNo;
        set => SetProperty(ref _quotationNo, value);
    }

    public DateTime QuotationDate
    {
        get => _quotationDate;
        set => SetProperty(ref _quotationDate, value);
    }

    public DateTime ValidUntil
    {
        get => _validUntil;
        set => SetProperty(ref _validUntil, value);
    }

    public int? SelectedCustomerId
    {
        get => _selectedCustomerId;
        set => SetProperty(ref _selectedCustomerId, value);
    }

    public int? SelectedWarehouseId
    {
        get => _selectedWarehouseId;
        set => SetProperty(ref _selectedWarehouseId, value);
    }

    public int? SelectedCurrencyId
    {
        get => _selectedCurrencyId;
        set
        {
            if (SetProperty(ref _selectedCurrencyId, value))
            {
                IsForeignCurrency = value.HasValue && value.Value > 0;
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

    public decimal DiscountAmount
    {
        get => _discountAmount;
        set
        {
            if (SetProperty(ref _discountAmount, value))
            {
                RecalculateTotals();
            }
        }
    }

    public decimal TaxAmount
    {
        get => _taxAmount;
        set
        {
            if (SetProperty(ref _taxAmount, value))
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

    public string? TermsAndConditions
    {
        get => _termsAndConditions;
        set => SetProperty(ref _termsAndConditions, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public bool IsReadOnly
    {
        get => _isReadOnly;
        private set => SetProperty(ref _isReadOnly, value);
    }

    public ObservableCollection<QuotationLineItem> Items
    {
        get => _items;
        set => SetProperty(ref _items, value);
    }

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

    public ObservableCollection<CurrencyDto> Currencies
    {
        get => _currencies;
        set => SetProperty(ref _currencies, value);
    }

    // Computed totals
    private decimal _subTotal;
    public decimal SubTotal
    {
        get => _subTotal;
        private set => SetProperty(ref _subTotal, value);
    }

    private decimal _totalAmount;
    public decimal TotalAmount
    {
        get => _totalAmount;
        private set => SetProperty(ref _totalAmount, value);
    }

    #endregion

    #region Commands

    public ICommand SaveCommand { get; }
    public ICommand AddLineCommand { get; }
    public ICommand RemoveLineCommand { get; }
    public ICommand CancelCommand { get; }

    #endregion

    #region Methods

    private async Task LoadLookupDataAsync()
    {
        await ExecuteAsync(async () =>
        {
            var customersResult = await _customerService.GetAllAsync();
            if (customersResult.IsSuccess && customersResult.Value != null)
            {
                InvokeOnUIThread(() =>
                {
                    Customers.Clear();
                    foreach (var c in customersResult.Value) Customers.Add(c);
                });
            }

            var warehousesResult = await _warehouseService.GetAllAsync();
            if (warehousesResult.IsSuccess && warehousesResult.Value != null)
            {
                InvokeOnUIThread(() =>
                {
                    Warehouses.Clear();
                    foreach (var w in warehousesResult.Value) Warehouses.Add(w);
                });
            }

            var currenciesResult = await _currencyService.GetAllAsync();
            if (currenciesResult.IsSuccess && currenciesResult.Value != null)
            {
                InvokeOnUIThread(() =>
                {
                    Currencies.Clear();
                    foreach (var c in currenciesResult.Value) Currencies.Add(c);
                });
            }
        });
    }

    private async Task LoadQuotationAsync(int id)
    {
        await ExecuteAsync(async () =>
        {
            ErrorMessage = null;
            var result = await _quotationService.GetByIdAsync(id);

            if (result.IsSuccess && result.Value != null)
            {
                var q = result.Value;
                InvokeOnUIThread(() =>
                {
                    QuotationId = q.Id;
                    QuotationNo = q.QuotationNo;
                    QuotationDate = q.QuotationDate;
                    ValidUntil = q.ValidUntil;
                    SelectedCustomerId = q.CustomerId;
                    SelectedWarehouseId = q.WarehouseId;
                    SelectedCurrencyId = q.CurrencyId;
                    ExchangeRate = q.ExchangeRate;
                    IsForeignCurrency = q.CurrencyId.HasValue && q.CurrencyId.Value > 0;
                    DiscountAmount = q.DiscountAmount;
                    TaxAmount = q.TaxAmount;
                    Notes = q.Notes;
                    TermsAndConditions = q.TermsAndConditions;
                    _status = q.Status;

                    Items.Clear();
                    if (q.Items != null)
                    {
                        foreach (var item in q.Items)
                        {
                            Items.Add(new QuotationLineItem
                            {
                                ProductId = item.ProductId,
                                ProductName = item.ProductName,
                                ProductUnitId = item.ProductUnitId,
                                UnitName = item.UnitName,
                                Quantity = item.Quantity,
                                UnitPrice = item.UnitPrice,
                                DiscountAmount = item.DiscountAmount,
                                TaxAmount = item.TaxAmount,
                                LineTotal = item.LineTotal,
                                Notes = item.Notes
                            });
                        }
                    }
                    RecalculateTotals();
                });
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل عرض السعر", "SalesQuotationEditorViewModel.LoadQuotationAsync");
                await _dialogService.ShowErrorAsync("خطأ في تحميل البيانات", ErrorMessage!);
            }
        });
    }

    private void AddLine()
    {
        Items.Add(new QuotationLineItem
        {
            Quantity = 1,
            UnitPrice = 0
        });
    }

    private void RemoveLine()
    {
        var item = Items.FirstOrDefault();
        if (item != null) Items.Remove(item);
    }

    private bool CanRemoveLine()
    {
        return Items.Count > 0;
    }

    private void RecalculateTotals()
    {
        var subTotal = Items.Sum(i => i.LineTotal);
        SubTotal = subTotal;
        TotalAmount = subTotal - DiscountAmount + TaxAmount;

        // Notify each item's computed property
        foreach (var item in Items)
        {
            item.RecalculateLineTotal();
        }
    }

    private bool Validate()
    {
        var errors = new List<string>();

        if (!SelectedCustomerId.HasValue || SelectedCustomerId.Value <= 0)
            errors.Add("• العميل مطلوب");
        if (!SelectedWarehouseId.HasValue || SelectedWarehouseId.Value <= 0)
            errors.Add("• المستودع مطلوب");
        if (QuotationDate == default)
            errors.Add("• تاريخ العرض مطلوب");
        if (ValidUntil == default || ValidUntil <= QuotationDate)
            errors.Add("• تاريخ الصلاحية يجب أن يكون بعد تاريخ العرض");
        if (Items.Count == 0)
            errors.Add("• يجب إضافة صنف واحد على الأقل");

        foreach (var item in Items)
        {
            if (item.ProductId <= 0)
                errors.Add("• المنتج مطلوب في جميع الأصناف");
            if (item.Quantity <= 0)
                errors.Add("• الكمية يجب أن تكون أكبر من صفر لجميع الأصناف");
            if (item.UnitPrice < 0)
                errors.Add("• السعر لا يمكن أن يكون سالباً");
        }

        if (errors.Any())
        {
            _ = _dialogService.ShowWarningAsync("بيانات غير مكتملة",
                "يرجى إكمال البيانات الإلزامية التالية:\n\n" + string.Join("\n", errors));
            return false;
        }
        return true;
    }

    private async Task SaveAsync()
    {
        if (!Validate()) return;

        await ExecuteAsync(async () =>
        {
            ErrorMessage = null;

            // Build line items
            var itemRequests = Items.Select(i => new CreateSalesQuotationItemRequest(
                ProductId: i.ProductId,
                ProductUnitId: i.ProductUnitId > 0 ? i.ProductUnitId : 1,
                Quantity: i.Quantity,
                UnitPrice: i.UnitPrice,
                DiscountAmount: i.DiscountAmount,
                Notes: i.Notes
            )).ToList();

            // Map currency and warehouse to short
            short warehouseId = SelectedWarehouseId.HasValue ? (short)SelectedWarehouseId.Value : (short)0;
            short currencyId = IsForeignCurrency && SelectedCurrencyId.HasValue ? (short)SelectedCurrencyId.Value : (short)0;

            if (_isEditMode && _quotationId.HasValue)
            {
                var updateRequest = new UpdateSalesQuotationRequest(
                    QuotationDate: QuotationDate == default ? null : QuotationDate,
                    ValidUntil: ValidUntil == default ? null : ValidUntil,
                    CustomerId: SelectedCustomerId!.Value,
                    WarehouseId: warehouseId,
                    CurrencyId: currencyId,
                    ExchangeRate: ExchangeRate,
                    PaymentType: 1, // Default Cash
                    DiscountAmount: DiscountAmount,
                    TaxAmount: TaxAmount,
                    Notes: Notes,
                    TermsAndConditions: TermsAndConditions,
                    Items: itemRequests);

                var result = await _quotationService.UpdateAsync(_quotationId.Value, updateRequest);

                if (result.IsSuccess && result.Value != null)
                {
                    _toastService.ShowSuccess("تم حفظ عرض السعر بنجاح");
                    _eventBus.Publish(new SalesQuotationChangedMessage(_quotationId.Value));
                    RequestClose();
                }
                else
                {
                    ErrorMessage = HandleFailure(result.Error ?? "فشل في حفظ عرض السعر", "SalesQuotationEditorViewModel.SaveAsync");
                    await _dialogService.ShowErrorAsync("خطأ في حفظ عرض السعر", ErrorMessage!);
                }
            }
            else
            {
                var createRequest = new CreateSalesQuotationRequest(
                    QuotationNo: 0,
                    QuotationDate: QuotationDate == default ? null : QuotationDate,
                    ValidUntil: ValidUntil == default ? null : ValidUntil,
                    CustomerId: SelectedCustomerId!.Value,
                    WarehouseId: warehouseId,
                    CurrencyId: currencyId,
                    ExchangeRate: ExchangeRate,
                    PaymentType: 1, // Default Cash
                    DiscountAmount: DiscountAmount,
                    TaxAmount: TaxAmount,
                    Notes: Notes,
                    TermsAndConditions: TermsAndConditions,
                    Items: itemRequests);

                var result = await _quotationService.CreateAsync(createRequest);

                if (result.IsSuccess && result.Value != null)
                {
                    _quotationId = result.Value.Id;
                    _quotationNo = result.Value.QuotationNo;
                    _isEditMode = true;
                    _toastService.ShowSuccess("تم إنشاء عرض السعر بنجاح");
                    _eventBus.Publish(new SalesQuotationChangedMessage(result.Value.Id));
                    RequestClose();
                }
                else
                {
                    ErrorMessage = HandleFailure(result.Error ?? "فشل في إنشاء عرض السعر", "SalesQuotationEditorViewModel.SaveAsync");
                    await _dialogService.ShowErrorAsync("خطأ في حفظ عرض السعر", ErrorMessage!);
                }
            }
        });
    }

    #endregion
}

/// <summary>
/// Line item for sales quotation with INotifyPropertyChanged for two-way binding
/// </summary>
public class QuotationLineItem : System.ComponentModel.INotifyPropertyChanged
{
    private int _productId;
    private string? _productName;
    private int _productUnitId;
    private string? _unitName;
    private decimal _quantity = 1;
    private decimal _unitPrice;
    private decimal _discountAmount;
    private decimal _taxAmount;
    private decimal _lineTotal;
    private string? _notes;

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));

    public int ProductId
    {
        get => _productId;
        set
        {
            if (_productId != value)
            {
                _productId = value;
                OnPropertyChanged(nameof(ProductId));
            }
        }
    }

    public string? ProductName
    {
        get => _productName;
        set
        {
            if (_productName != value)
            {
                _productName = value;
                OnPropertyChanged(nameof(ProductName));
            }
        }
    }

    public int ProductUnitId
    {
        get => _productUnitId;
        set
        {
            if (_productUnitId != value)
            {
                _productUnitId = value;
                OnPropertyChanged(nameof(ProductUnitId));
            }
        }
    }

    public string? UnitName
    {
        get => _unitName;
        set
        {
            if (_unitName != value)
            {
                _unitName = value;
                OnPropertyChanged(nameof(UnitName));
            }
        }
    }

    public decimal Quantity
    {
        get => _quantity;
        set
        {
            if (_quantity != value)
            {
                _quantity = value;
                OnPropertyChanged(nameof(Quantity));
                RecalculateLineTotal();
            }
        }
    }

    public decimal UnitPrice
    {
        get => _unitPrice;
        set
        {
            if (_unitPrice != value)
            {
                _unitPrice = value;
                OnPropertyChanged(nameof(UnitPrice));
                RecalculateLineTotal();
            }
        }
    }

    public decimal DiscountAmount
    {
        get => _discountAmount;
        set
        {
            if (_discountAmount != value)
            {
                _discountAmount = value;
                OnPropertyChanged(nameof(DiscountAmount));
                RecalculateLineTotal();
            }
        }
    }

    public decimal TaxAmount
    {
        get => _taxAmount;
        set
        {
            if (_taxAmount != value)
            {
                _taxAmount = value;
                OnPropertyChanged(nameof(TaxAmount));
            }
        }
    }

    public decimal LineTotal
    {
        get => _lineTotal;
        set
        {
            if (_lineTotal != value)
            {
                _lineTotal = value;
                OnPropertyChanged(nameof(LineTotal));
            }
        }
    }

    public string? Notes
    {
        get => _notes;
        set
        {
            if (_notes != value)
            {
                _notes = value;
                OnPropertyChanged(nameof(Notes));
            }
        }
    }

    public void RecalculateLineTotal()
    {
        LineTotal = (Quantity * UnitPrice) - DiscountAmount;
    }
}
