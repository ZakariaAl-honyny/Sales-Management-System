using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.DesktopPWF.Messaging.Messages;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Services.App.Toast;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace SalesSystem.DesktopPWF.ViewModels.Sales;

/// <summary>
/// ViewModel for creating and editing Sales Quotations.
/// Supports customer/warehouse selection, item line management, and totals computation.
/// </summary>
public class SalesQuotationEditorViewModel : ViewModelBase
{
    private readonly ISalesQuotationApiService _quotationService;
    private readonly ICustomerApiService _customerService;
    private readonly IWarehouseApiService _warehouseService;
    private readonly IProductApiService _productService;
    private readonly IDialogService _dialogService;
    private readonly IEventBus _eventBus;
    private readonly IToastNotificationService _toastService;

    private int? _quotationId;
    private string _quotationNo = string.Empty;
    private int? _selectedCustomerId;
    private int _selectedWarehouseId;
    private DateTime _quotationDate = DateTime.Today;
    private DateTime? _expiryDate;
    private string? _notes;
    private string? _errorMessage;
    private bool _isEditMode;
    private decimal _subTotal;
    private decimal _discountAmount;
    private decimal _taxAmount;
    private decimal _totalAmount;

    private ObservableCollection<CustomerDto> _customers = new();
    private ObservableCollection<WarehouseDto> _warehouses = new();
    private ObservableCollection<ProductDto> _products = new();
    private ObservableCollection<SalesQuotationItemLineViewModel> _items = new();

    public SalesQuotationEditorViewModel(
        ISalesQuotationApiService quotationService,
        ICustomerApiService customerService,
        IWarehouseApiService warehouseService,
        IProductApiService productService,
        IDialogService dialogService,
        IEventBus eventBus,
        IToastNotificationService toastService,
        int? quotationId = null)
    {
        _quotationService = quotationService ?? throw new ArgumentNullException(nameof(quotationService));
        _customerService = customerService ?? throw new ArgumentNullException(nameof(customerService));
        _warehouseService = warehouseService ?? throw new ArgumentNullException(nameof(warehouseService));
        _productService = productService ?? throw new ArgumentNullException(nameof(productService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _toastService = toastService ?? throw new ArgumentNullException(nameof(toastService));
        SetDialogService(dialogService);

        _quotationId = quotationId;
        _isEditMode = quotationId.HasValue;

        SaveCommand = new AsyncRelayCommand(SaveAsync);
        CancelCommand = new RelayCommand(() => RequestClose());
        AddItemCommand = new RelayCommand(AddItem);
        RemoveItemCommand = new RelayCommand(RemoveItem);

        _ = InitializeAsync();
    }

    /// <summary>
    /// Parameterless constructor for designer/Service Locator pattern
    /// </summary>
    public SalesQuotationEditorViewModel(int? quotationId = null)
        : this(
            App.GetService<ISalesQuotationApiService>(),
            App.GetService<ICustomerApiService>(),
            App.GetService<IWarehouseApiService>(),
            App.GetService<IProductApiService>(),
            App.GetService<IDialogService>(),
            App.GetService<IEventBus>(),
            App.GetService<IToastNotificationService>(),
            quotationId)
    {
    }

    private async Task InitializeAsync()
    {
        await ExecuteAsync(InitializeOperationAsync, "جاري تحميل البيانات...");
    }

    private async Task InitializeOperationAsync()
    {
        await LoadReferenceDataAsync();

        if (_isEditMode && _quotationId.HasValue)
        {
            await LoadQuotationAsync();
        }
        else
        {
            if (Warehouses.Any())
            {
                var defaultWarehouse = Warehouses.FirstOrDefault(w => w.IsDefault) ?? Warehouses.First();
                SelectedWarehouseId = defaultWarehouse.Id;
            }
            AddItem();
        }
    }

    #region Properties

    public int? QuotationId => _quotationId;

    public bool IsEditMode => _isEditMode;

    public string QuotationNo
    {
        get => _quotationNo;
        set => SetProperty(ref _quotationNo, value);
    }

    public int? SelectedCustomerId
    {
        get => _selectedCustomerId;
        set
        {
            if (SetProperty(ref _selectedCustomerId, value))
                ClearErrors(nameof(SelectedCustomerId));
        }
    }

    public int SelectedWarehouseId
    {
        get => _selectedWarehouseId;
        set
        {
            if (SetProperty(ref _selectedWarehouseId, value))
                ClearErrors(nameof(SelectedWarehouseId));
        }
    }

    public DateTime QuotationDate
    {
        get => _quotationDate;
        set => SetProperty(ref _quotationDate, value);
    }

    public DateTime? ExpiryDate
    {
        get => _expiryDate;
        set => SetProperty(ref _expiryDate, value);
    }

    public decimal DiscountAmount
    {
        get => _discountAmount;
        set
        {
            if (SetProperty(ref _discountAmount, value))
            {
                ValidateDiscount();
                RecalculateTotals();
            }
        }
    }

    public string? Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
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

    public ObservableCollection<ProductDto> Products
    {
        get => _products;
        set => SetProperty(ref _products, value);
    }

    public ObservableCollection<SalesQuotationItemLineViewModel> Items
    {
        get => _items;
        set => SetProperty(ref _items, value);
    }

    public decimal SubTotal
    {
        get => _subTotal;
        private set => SetProperty(ref _subTotal, value);
    }

    public decimal TaxAmount
    {
        get => _taxAmount;
        private set => SetProperty(ref _taxAmount, value);
    }

    public decimal TotalAmount
    {
        get => _totalAmount;
        private set => SetProperty(ref _totalAmount, value);
    }

    #endregion

    #region Commands

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand AddItemCommand { get; }
    public ICommand RemoveItemCommand { get; }

    #endregion

    #region Methods

    private async Task LoadReferenceDataAsync()
    {
        ErrorMessage = null;

        var customersResult = await _customerService.GetAllAsync();
        if (customersResult.IsSuccess && customersResult.Value != null)
            Customers = new ObservableCollection<CustomerDto>(customersResult.Value);

        var warehousesResult = await _warehouseService.GetAllAsync();
        if (warehousesResult.IsSuccess && warehousesResult.Value != null)
            Warehouses = new ObservableCollection<WarehouseDto>(warehousesResult.Value);

        var productsResult = await _productService.GetAllAsync();
        if (productsResult.IsSuccess && productsResult.Value != null)
        {
            Products.Clear();
            foreach (var p in productsResult.Value)
                Products.Add(p);
        }
    }

    private async Task LoadQuotationAsync()
    {
        if (!_quotationId.HasValue) return;

        ErrorMessage = null;
        var result = await _quotationService.GetByIdAsync(_quotationId.Value);
        if (result.IsSuccess && result.Value != null)
        {
            var q = result.Value;
            QuotationNo = q.QuotationNo;
            SelectedCustomerId = q.CustomerId;
            SelectedWarehouseId = q.WarehouseId;
            QuotationDate = q.QuotationDate;
            ExpiryDate = q.ExpiryDate;
            DiscountAmount = q.DiscountAmount;
            Notes = q.Notes;

            Items.Clear();
            foreach (var item in q.Items)
            {
                var lineVm = new SalesQuotationItemLineViewModel(Products)
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    DiscountAmount = item.DiscountAmount,
                    Notes = item.Notes
                };
                lineVm.SelectedProduct = Products.FirstOrDefault(p => p.Id == item.ProductId);
                lineVm.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(SalesQuotationItemLineViewModel.LineTotal))
                        RecalculateTotals();
                };
                Items.Add(lineVm);
            }
            RecalculateTotals();
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل عرض السعر",
                "SalesQuotationEditorViewModel.LoadQuotationAsync");
        }
    }

    private async Task<bool> ValidateAsync()
    {
        ClearAllErrors();

        if (SelectedWarehouseId <= 0)
            AddError(nameof(SelectedWarehouseId), "يجب اختيار المستودع");

        if (!Items.Any(i => i.SelectedProduct != null && i.Quantity > 0))
            AddError(nameof(Items), "يجب إضافة صنف واحد على الأقل");

        if (DiscountAmount < 0)
            AddError(nameof(DiscountAmount), "قيمة الخصم لا يمكن أن تكون سالبة");

        return await ValidateAllAsync();
    }

    private void ValidateDiscount()
    {
        ClearErrors(nameof(DiscountAmount));
        if (DiscountAmount < 0)
            AddError(nameof(DiscountAmount), "قيمة الخصم لا يمكن أن تكون سالبة");
    }

    private async Task SaveAsync()
    {
        if (!await ValidateAsync()) return;

        await ExecuteAsync(async () =>
        {
            ErrorMessage = null;
            var requestItems = Items
                .Where(i => i.SelectedProduct != null && i.Quantity > 0)
                .Select(i => new CreateSalesQuotationItemRequest(
                    i.SelectedProduct!.Id,
                    i.Quantity,
                    i.UnitPrice,
                    i.DiscountAmount,
                    Mode: 1,
                    i.Notes))
                .ToList();

            if (_isEditMode && _quotationId.HasValue)
            {
                var updateRequest = new UpdateSalesQuotationRequest(
                    SelectedCustomerId,
                    SelectedWarehouseId,
                    QuotationDate,
                    ExpiryDate,
                    DiscountAmount,
                    Notes,
                    CurrencyId: null,
                    ExchangeRate: null,
                    requestItems);

                var result = await _quotationService.UpdateAsync(_quotationId.Value, updateRequest);
                if (result.IsSuccess)
                {
                    _toastService.ShowSuccess("تم تحديث عرض السعر بنجاح");
                    _eventBus.Publish(new SalesQuotationChangedMessage(_quotationId.Value));
                    RequestClose();
                }
                else
                {
                    ErrorMessage = HandleFailure(result.Error ?? "فشل في تحديث عرض السعر",
                        "SalesQuotationEditorViewModel.SaveAsync");
                    await _dialogService.ShowErrorAsync("خطأ في حفظ عرض السعر", ErrorMessage!);
                }
            }
            else
            {
                var createRequest = new CreateSalesQuotationRequest(
                    SelectedCustomerId,
                    SelectedWarehouseId,
                    QuotationDate,
                    ExpiryDate,
                    DiscountAmount,
                    Notes,
                    CurrencyId: null,
                    ExchangeRate: null,
                    requestItems);

                var result = await _quotationService.CreateAsync(createRequest);
                if (result.IsSuccess && result.Value != null)
                {
                    _quotationId = result.Value.Id;
                    _isEditMode = true;
                    _toastService.ShowSuccess("تم إنشاء عرض السعر بنجاح");
                    _eventBus.Publish(new SalesQuotationChangedMessage(_quotationId.Value));
                    RequestClose();
                }
                else
                {
                    ErrorMessage = HandleFailure(result.Error ?? "فشل في إنشاء عرض السعر",
                        "SalesQuotationEditorViewModel.SaveAsync");
                    await _dialogService.ShowErrorAsync("خطأ في حفظ عرض السعر", ErrorMessage!);
                }
            }
        });
    }

    private void AddItem()
    {
        var line = new SalesQuotationItemLineViewModel(Products);
        line.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(SalesQuotationItemLineViewModel.LineTotal))
                RecalculateTotals();
        };
        Items.Add(line);
        RecalculateTotals();
    }

    private void RemoveItem(object? parameter)
    {
        if (parameter is SalesQuotationItemLineViewModel line)
        {
            Items.Remove(line);
            RecalculateTotals();
        }
    }

    private void RecalculateTotals()
    {
        SubTotal = Items.Sum(i => i.LineTotal);
        TotalAmount = SubTotal - DiscountAmount;
        if (TotalAmount < 0) TotalAmount = 0;
    }

    #endregion
}
