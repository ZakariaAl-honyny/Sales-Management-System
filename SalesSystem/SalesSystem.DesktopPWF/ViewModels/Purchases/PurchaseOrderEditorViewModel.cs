using SalesSystem.Contracts.DTOs;
using SalesSystem.DesktopPWF.Messaging.Messages;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Services.App.Toast;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace SalesSystem.DesktopPWF.ViewModels.Purchases;

/// <summary>
/// ViewModel for creating and editing Purchase Orders.
/// Supports supplier/warehouse selection, multi-currency, and item line management.
/// </summary>
public class PurchaseOrderEditorViewModel : ViewModelBase
{
    private readonly IPurchaseOrderApiService _orderService;
    private readonly ISupplierApiService _supplierService;
    private readonly IWarehouseApiService _warehouseService;
    private readonly IProductApiService _productService;
    private readonly ICurrencyApiService _currencyService;
    private readonly IDialogService _dialogService;
    private readonly IEventBus _eventBus;
    private readonly IToastNotificationService _toastService;

    private int? _orderId;
    private int? _orderNo;
    private int _selectedSupplierId;
    private int _selectedWarehouseId;
    private int? _selectedCurrencyId;
    private decimal? _exchangeRate;
    private bool _isForeignCurrency;
    private DateTime _orderDate = DateTime.Today;
    private DateOnly? _expectedDate;
    private string? _notes;
    private string? _errorMessage;
    private bool _isEditMode;
    private decimal _subTotal;
    private decimal _totalAmount;

    private ObservableCollection<SupplierDto> _suppliers = new();
    private ObservableCollection<WarehouseDto> _warehouses = new();
    private ObservableCollection<ProductDto> _products = new();
    private ObservableCollection<CurrencyDto> _currencies = new();
    private ObservableCollection<PurchaseOrderItemLineViewModel> _items = new();

    public PurchaseOrderEditorViewModel(
        IPurchaseOrderApiService orderService,
        ISupplierApiService supplierService,
        IWarehouseApiService warehouseService,
        IProductApiService productService,
        ICurrencyApiService currencyService,
        IDialogService dialogService,
        IEventBus eventBus,
        IToastNotificationService toastService,
        int? orderId = null)
    {
        _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
        _supplierService = supplierService ?? throw new ArgumentNullException(nameof(supplierService));
        _warehouseService = warehouseService ?? throw new ArgumentNullException(nameof(warehouseService));
        _productService = productService ?? throw new ArgumentNullException(nameof(productService));
        _currencyService = currencyService ?? throw new ArgumentNullException(nameof(currencyService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _toastService = toastService ?? throw new ArgumentNullException(nameof(toastService));
        SetDialogService(dialogService);

        _orderId = orderId;
        _isEditMode = orderId.HasValue;

        SaveCommand = new AsyncRelayCommand(SaveAsync);
        CancelCommand = new RelayCommand(() => RequestClose());
        AddItemCommand = new RelayCommand(AddItem);
        RemoveItemCommand = new RelayCommand(RemoveItem);

        _ = InitializeAsync();
    }

    /// <summary>
    /// Parameterless constructor for designer/Service Locator pattern
    /// </summary>
    public PurchaseOrderEditorViewModel(int? orderId = null)
        : this(
            App.GetService<IPurchaseOrderApiService>(),
            App.GetService<ISupplierApiService>(),
            App.GetService<IWarehouseApiService>(),
            App.GetService<IProductApiService>(),
            App.GetService<ICurrencyApiService>(),
            App.GetService<IDialogService>(),
            App.GetService<IEventBus>(),
            App.GetService<IToastNotificationService>(),
            orderId)
    {
    }

    private async Task InitializeAsync()
    {
        await ExecuteAsync(InitializeOperationAsync, "جاري تحميل البيانات...");
    }

    private async Task InitializeOperationAsync()
    {
        await LoadReferenceDataAsync();

        if (_isEditMode && _orderId.HasValue)
        {
            await LoadOrderAsync();
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

    public int? OrderId => _orderId;

    public bool IsEditMode => _isEditMode;

    public int? OrderNo
    {
        get => _orderNo;
        set => SetProperty(ref _orderNo, value);
    }

    public int SelectedSupplierId
    {
        get => _selectedSupplierId;
        set
        {
            if (SetProperty(ref _selectedSupplierId, value))
                ClearErrors(nameof(SelectedSupplierId));
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

    public DateTime OrderDate
    {
        get => _orderDate;
        set => SetProperty(ref _orderDate, value);
    }

    public DateOnly? ExpectedDate
    {
        get => _expectedDate;
        set => SetProperty(ref _expectedDate, value);
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

    public ObservableCollection<CurrencyDto> Currencies
    {
        get => _currencies;
        set => SetProperty(ref _currencies, value);
    }

    public ObservableCollection<PurchaseOrderItemLineViewModel> Items
    {
        get => _items;
        set => SetProperty(ref _items, value);
    }

    public decimal SubTotal
    {
        get => _subTotal;
        private set => SetProperty(ref _subTotal, value);
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

        var suppliersResult = await _supplierService.GetAllAsync();
        if (suppliersResult.IsSuccess && suppliersResult.Value != null)
            Suppliers = new ObservableCollection<SupplierDto>(suppliersResult.Value);

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

        var currenciesResult = await _currencyService.GetAllAsync();
        if (currenciesResult.IsSuccess && currenciesResult.Value != null)
            Currencies = new ObservableCollection<CurrencyDto>(currenciesResult.Value);
    }

    private async Task LoadOrderAsync()
    {
        if (!_orderId.HasValue) return;

        ErrorMessage = null;
        var result = await _orderService.GetByIdAsync(_orderId.Value);
        if (result.IsSuccess && result.Value != null)
        {
            var order = result.Value;
            OrderNo = order.OrderNo;
            SelectedSupplierId = order.SupplierId;
            SelectedWarehouseId = order.WarehouseId;
            SelectedCurrencyId = order.CurrencyId;
            ExchangeRate = order.ExchangeRate;
            OrderDate = order.OrderDate;
            ExpectedDate = order.ExpectedDate;
            Notes = order.Notes;

            Items.Clear();
            foreach (var item in order.Items)
            {
                var lineVm = new PurchaseOrderItemLineViewModel(Products)
                {
                    ProductId = item.ProductId,
                    ProductUnitId = item.ProductUnitId,
                    Quantity = item.Quantity,
                    UnitCost = item.UnitCost,
                    Notes = item.Notes
                };
                lineVm.SelectedProduct = Products.FirstOrDefault(p => p.Id == item.ProductId);
                lineVm.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(PurchaseOrderItemLineViewModel.LineTotal))
                        RecalculateTotals();
                };
                Items.Add(lineVm);
            }
            RecalculateTotals();
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل أمر الشراء",
                "PurchaseOrderEditorViewModel.LoadOrderAsync");
        }
    }

    private async Task<bool> ValidateAsync()
    {
        ClearAllErrors();

        if (SelectedSupplierId <= 0)
            AddError(nameof(SelectedSupplierId), "يجب اختيار المورد");

        if (SelectedWarehouseId <= 0)
            AddError(nameof(SelectedWarehouseId), "يجب اختيار المستودع");

        if (!Items.Any(i => i.SelectedProduct != null && i.Quantity > 0))
            AddError(nameof(Items), "يجب إضافة صنف واحد على الأقل");

        return await ValidateAllAsync();
    }

    private async Task SaveAsync()
    {
        if (!await ValidateAsync()) return;

        await ExecuteAsync(async () =>
        {
            ErrorMessage = null;
            var requestItems = Items
                .Where(i => i.SelectedProduct != null && i.Quantity > 0)
                .Select(i => new CreatePurchaseOrderItemRequest(
                    i.SelectedProduct!.Id,
                    i.ProductUnitId > 0 ? i.ProductUnitId : 1,
                    i.Quantity,
                    i.UnitCost,
                    i.Notes))
                .ToList();

            if (_isEditMode && _orderId.HasValue)
            {
                var updateRequest = new UpdatePurchaseOrderRequest(
                    SelectedSupplierId,
                    SelectedWarehouseId,
                    OrderDate,
                    ExpectedDate,
                    SelectedCurrencyId,
                    ExchangeRate,
                    Notes,
                    requestItems);

                var result = await _orderService.UpdateAsync(_orderId.Value, updateRequest);
                if (result.IsSuccess)
                {
                    _toastService.ShowSuccess("تم تحديث أمر الشراء بنجاح");
                    _eventBus.Publish(new PurchaseOrderChangedMessage(_orderId.Value));
                    RequestClose();
                }
                else
                {
                    ErrorMessage = HandleFailure(result.Error ?? "فشل في تحديث أمر الشراء",
                        "PurchaseOrderEditorViewModel.SaveAsync");
                    await _dialogService.ShowErrorAsync("خطأ في حفظ أمر الشراء", ErrorMessage!);
                }
            }
            else
            {
                var createRequest = new CreatePurchaseOrderRequest(
                    SelectedSupplierId,
                    SelectedWarehouseId,
                    OrderNo,
                    OrderDate,
                    ExpectedDate,
                    SelectedCurrencyId,
                    ExchangeRate,
                    Notes,
                    requestItems);

                var result = await _orderService.CreateAsync(createRequest);
                if (result.IsSuccess && result.Value != null)
                {
                    _orderId = result.Value.Id;
                    _isEditMode = true;
                    _toastService.ShowSuccess("تم إنشاء أمر الشراء بنجاح");
                    _eventBus.Publish(new PurchaseOrderChangedMessage(_orderId.Value));
                    RequestClose();
                }
                else
                {
                    ErrorMessage = HandleFailure(result.Error ?? "فشل في إنشاء أمر الشراء",
                        "PurchaseOrderEditorViewModel.SaveAsync");
                    await _dialogService.ShowErrorAsync("خطأ في حفظ أمر الشراء", ErrorMessage!);
                }
            }
        });
    }

    private void AddItem()
    {
        var line = new PurchaseOrderItemLineViewModel(Products);
        line.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(PurchaseOrderItemLineViewModel.LineTotal))
                RecalculateTotals();
        };
        Items.Add(line);
        RecalculateTotals();
    }

    private void RemoveItem(object? parameter)
    {
        if (parameter is PurchaseOrderItemLineViewModel line)
        {
            Items.Remove(line);
            RecalculateTotals();
        }
    }

    private void RecalculateTotals()
    {
        SubTotal = Items.Sum(i => i.LineTotal);
        TotalAmount = SubTotal;
    }

    private int GetBaseCurrencyId()
    {
        var baseCurrency = Currencies.FirstOrDefault(c => c.IsBaseCurrency);
        return baseCurrency?.Id ?? 0;
    }

    #endregion
}
