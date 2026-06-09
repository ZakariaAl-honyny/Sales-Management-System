using SalesSystem.DesktopPWF.Messaging.Messages;
using System.Collections.ObjectModel;
using System.Windows.Input;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Enums;
using SalesSystem.Contracts.Requests;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Services.App.Toast;

namespace SalesSystem.DesktopPWF.ViewModels.Purchases;

// PurchaseOrderStatus values: Draft=1, Approved=2, PartiallyReceived=3, Received=4, Cancelled=5
internal static class PoStatus
{
    public const byte Draft = 1;
    public const byte Approved = 2;
    public const byte PartiallyReceived = 3;
    public const byte Received = 4;
    public const byte Cancelled = 5;
}

public class PurchaseOrderEditorViewModel : ViewModelBase
{
    private readonly IPurchaseOrderApiService _orderService;
    private readonly IEventBus _eventBus;
    private readonly ISupplierApiService _supplierService;
    private readonly IWarehouseApiService _warehouseService;
    private readonly IProductApiService _productService;
    private readonly IDialogService _dialogService;
    private readonly ISoundService _soundService;
    private readonly IToastNotificationService _toastService;

    private int? _orderId;
    private int _orderNo;
    private int _selectedWarehouseId;
    private int? _selectedSupplierId;
    private DateTime _orderDate = DateTime.Today;
    private DateOnly? _expectedDate;
    private string? _notes;
    private bool _isEditMode;
    private string? _errorMessage;
    private byte _status = PoStatus.Draft;
    public bool IsReadOnly { get; private set; }

    private ObservableCollection<PurchaseOrderLineViewModel> _items = new();
    private ObservableCollection<SupplierDto> _suppliers = new();
    private ObservableCollection<WarehouseDto> _warehouses = new();
    private ObservableCollection<ProductDto> _products = new();

    private int? _selectedCurrencyId;
    private decimal _exchangeRate = 1.0m;

    public PurchaseOrderEditorViewModel(
        IPurchaseOrderApiService orderService,
        IEventBus eventBus,
        ISupplierApiService supplierService,
        IWarehouseApiService warehouseService,
        IProductApiService productService,
        IDialogService dialogService,
        ISoundService soundService,
        IToastNotificationService toastService,
        int? orderId = null,
        bool isReadOnly = false)
    {
        _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _supplierService = supplierService ?? throw new ArgumentNullException(nameof(supplierService));
        _warehouseService = warehouseService ?? throw new ArgumentNullException(nameof(warehouseService));
        _productService = productService ?? throw new ArgumentNullException(nameof(productService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        SetDialogService(dialogService);
        _soundService = soundService;
        _toastService = toastService ?? throw new ArgumentNullException(nameof(toastService));
        _orderId = orderId;
        _isEditMode = orderId.HasValue;
        IsReadOnly = isReadOnly;

        SaveCommand = new AsyncRelayCommand(SaveAsync);
        PostCommand = new AsyncRelayCommand(PostAsync);
        CancelOrderCommand = new AsyncRelayCommand(CancelOrderAsync);
        CancelCommand = new RelayCommand(Cancel);
        AddLineCommand = new RelayCommand(AddLine);
        RemoveLineCommand = new RelayCommand(RemoveLine);
        SearchSupplierCommand = new RelayCommand(SearchSupplier);
        SearchProductCommand = new RelayCommand(SearchProduct);

        _ = InitializeAsync();
    }

    public PurchaseOrderEditorViewModel(int? orderId = null, bool isReadOnly = false)
        : this(
            App.GetService<IPurchaseOrderApiService>(),
            App.GetService<IEventBus>(),
            App.GetService<ISupplierApiService>(),
            App.GetService<IWarehouseApiService>(),
            App.GetService<IProductApiService>(),
            App.GetService<IDialogService>(),
            App.GetService<ISoundService>(),
            App.GetService<IToastNotificationService>(),
            orderId,
            isReadOnly)
    {
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
            await LoadOrderAsync();
        }
        else
        {
            if (Warehouses.Any())
            {
                var defaultWarehouse = Warehouses.FirstOrDefault(w => w.IsDefault) ?? Warehouses.First();
                SelectedWarehouseId = defaultWarehouse.Id;
            }
            AddLine();
        }
    }

    #region Properties
    public int? OrderId => _orderId;

    public int OrderNo
    {
        get => _orderNo;
        set => SetProperty(ref _orderNo, value);
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

    public ObservableCollection<PurchaseOrderLineViewModel> Items
    {
        get => _items;
        set => SetProperty(ref _items, value);
    }

    public int SelectedWarehouseId
    {
        get => _selectedWarehouseId;
        set => SetProperty(ref _selectedWarehouseId, value);
    }

    public int? SelectedSupplierId
    {
        get => _selectedSupplierId;
        set => SetProperty(ref _selectedSupplierId, value);
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

    public byte Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

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

    public int? SelectedCurrencyId
    {
        get => _selectedCurrencyId;
        set => SetProperty(ref _selectedCurrencyId, value);
    }

    public decimal ExchangeRate
    {
        get => _exchangeRate;
        set => SetProperty(ref _exchangeRate, value);
    }

    public List<EnumDisplayItem> SaleModeOptions { get; } = new()
    {
        new EnumDisplayItem { Value = (byte)SaleMode.Retail, Display = "تجزئة" },
        new EnumDisplayItem { Value = (byte)SaleMode.Wholesale, Display = "جملة" }
    };
    #endregion

    #region Commands
    public ICommand SaveCommand { get; }
    public ICommand PostCommand { get; }
    public ICommand CancelOrderCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand AddLineCommand { get; }
    public ICommand RemoveLineCommand { get; }
    public ICommand SearchSupplierCommand { get; }
    public ICommand SearchProductCommand { get; }
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
        });
    }

    private async Task LoadOrderAsync()
    {
        if (!_orderId.HasValue) return;

        await ExecuteAsync(async () =>
        {
            ErrorMessage = null;
            var result = await _orderService.GetByIdAsync(_orderId.Value);
            if (result.IsSuccess && result.Value != null)
            {
                var order = result.Value;
                OrderNo = order.OrderNo;
                SelectedWarehouseId = order.WarehouseId;
                SelectedSupplierId = order.SupplierId;
                OrderDate = order.OrderDate;
                ExpectedDate = order.ExpectedDate;
                Notes = order.Notes;
                Status = order.Status;
                SelectedCurrencyId = order.CurrencyId;
                ExchangeRate = order.ExchangeRate ?? 1.0m;

                if (order.Status != PoStatus.Draft)
                {
                    IsReadOnly = true;
                    OnPropertyChanged(nameof(IsReadOnly));
                }

                Items.Clear();
                foreach (var item in order.Items)
                {
                    var lineVm = new PurchaseOrderLineViewModel(Products);
                    lineVm.ProductId = item.ProductId;
                    lineVm.Quantity = item.Quantity;
                    lineVm.UnitCost = item.UnitCost;
                    lineVm.SelectedProduct = Products.FirstOrDefault(p => p.Id == item.ProductId);
                    lineVm.ProductUnitId = item.ProductUnitId;
                    lineVm.Notes = item.Notes;
                    lineVm.ReceivedQuantity = item.ReceivedQuantity;

                    lineVm.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(PurchaseOrderLineViewModel.LineTotal))
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
                ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل أمر الشراء", "PurchaseOrderEditorViewModel.LoadOrderAsync", $"[PurchaseOrderEditorViewModel.LoadOrderAsync] Failed to load PO ID {_orderId}.");
            }
        });
    }

    private async Task SaveAsync()
    {
        if (!await ValidateOrder()) return;

        await ExecuteAsync(async () =>
        {
            ErrorMessage = null;
            var request = BuildRequest();

            Result<PurchaseOrderDto> result;
            if (_isEditMode)
            {
                result = await _orderService.UpdateAsync(_orderId!.Value, request);
            }
            else
            {
                result = await _orderService.CreateAsync(request);
            }

            if (result.IsSuccess && result.Value != null)
            {
                _orderId = result.Value.Id;
                _status = result.Value.Status;
                _isEditMode = true;

                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(IsEditMode));
                OnPropertyChanged(nameof(IsReadOnly));

                _toastService.ShowSuccess("تم حفظ أمر الشراء بنجاح");
                _eventBus.Publish(new PurchaseOrderChangedMessage(_orderId.Value));
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشل في حفظ أمر الشراء", "PurchaseOrderEditorViewModel.SaveAsync", "[PurchaseOrderEditorViewModel.SaveAsync] Failed to save purchase order.");
                await _dialogService.ShowErrorAsync("خطأ في الحفظ", ErrorMessage!);
            }
        });
    }

    private async Task PostAsync()
    {
        if (!_isEditMode || _orderId == null)
        {
            await SaveAsync();
            if (_orderId == null) return;
        }

        if (!await _dialogService.ShowConfirmationAsync("تأكيد الاعتماد", "هل أنت متأكد من اعتماد أمر الشراء هذا؟")) return;

        await ExecuteAsync(async () =>
        {
            ErrorMessage = null;
            var postResult = await _orderService.PostAsync(_orderId!.Value);
            if (postResult.IsSuccess)
            {
                _toastService.ShowSuccess("تم اعتماد أمر الشراء بنجاح");
                _eventBus.Publish(new PurchaseOrderChangedMessage(_orderId.Value));
                RequestClose();
            }
            else
            {
                ErrorMessage = HandleFailure(postResult.Error ?? "فشل في اعتماد أمر الشراء", "PurchaseOrderEditorViewModel.PostAsync", $"[PurchaseOrderEditorViewModel.PostAsync] Failed to post PO ID {_orderId}.");
                await _dialogService.ShowErrorAsync("خطأ في الاعتماد", ErrorMessage!);
            }
        });
    }

    private async Task CancelOrderAsync()
    {
        if (!_orderId.HasValue) return;

        if (!await _dialogService.ShowConfirmationAsync("تأكيد الإلغاء", "هل أنت متأكد من إلغاء أمر الشراء هذا؟")) return;

        await ExecuteAsync(async () =>
        {
            ErrorMessage = null;
            var cancelResult = await _orderService.CancelAsync(_orderId!.Value);
            if (cancelResult.IsSuccess)
            {
                _toastService.ShowSuccess("تم إلغاء أمر الشراء بنجاح");
                _eventBus.Publish(new PurchaseOrderChangedMessage(_orderId.Value));
                RequestClose();
            }
            else
            {
                ErrorMessage = HandleFailure(cancelResult.Error ?? "فشل في إلغاء أمر الشراء", "PurchaseOrderEditorViewModel.CancelOrderAsync", $"[PurchaseOrderEditorViewModel.CancelOrderAsync] Failed to cancel PO ID {_orderId}.");
                await _dialogService.ShowErrorAsync("خطأ في الإلغاء", ErrorMessage!);
            }
        });
    }

    private void Cancel()
    {
        RequestClose();
    }

    private void AddLine()
    {
        var line = new PurchaseOrderLineViewModel(Products);
        line.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(PurchaseOrderLineViewModel.LineTotal))
            {
                RecalculateTotals();
            }
        };
        Items.Add(line);
        RecalculateTotals();
    }

    private void RemoveLine(object? parameter)
    {
        if (parameter is PurchaseOrderLineViewModel line)
        {
            Items.Remove(line);
            RecalculateTotals();
        }
    }

    private async Task<bool> ValidateOrder()
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
            await _dialogService.ShowWarningAsync("بيانات غير مكتملة", "يرجى إكمال البيانات الإلزامية التالية:\n\n" + string.Join("\n", errors));
            return false;
        }

        return true;
    }

    private CreatePurchaseOrderRequest BuildRequest()
    {
        var items = Items
            .Where(i => i.SelectedProduct != null && i.Quantity > 0)
            .Select(i => new CreatePurchaseOrderItemRequest(
                i.ProductId > 0 ? i.ProductId : (i.SelectedProduct?.Id ?? 0),
                i.ProductUnitId > 0 ? i.ProductUnitId : 0,
                i.Quantity,
                i.UnitCost,
                i.Notes))
            .ToList();

        return new CreatePurchaseOrderRequest(
            SelectedSupplierId ?? 0,
            SelectedWarehouseId,
            OrderNo > 0 ? OrderNo : null,
            OrderDate,
            ExpectedDate,
            SelectedCurrencyId,
            ExchangeRate != 1.0m ? ExchangeRate : null,
            Notes,
            items);
    }

    private void RecalculateTotals()
    {
        SubTotal = Items.Sum(i => i.LineTotal);
        TotalAmount = SubTotal;

        OnPropertyChanged(nameof(TotalAmount));
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

    private void SearchProduct()
    {
        var vm = new ViewModels.Products.ProductSelectionViewModel(SelectedWarehouseId);
        bool picked = false;

        vm.OnProductSelected += (product) =>
        {
            if (picked) return;
            picked = true;

            var existingLine = Items.FirstOrDefault(i => i.ProductId == product.Id);
            if (existingLine != null)
            {
                existingLine.Quantity += 1;
                _soundService.PlaySuccess();
            }
            else
            {
                var line = new PurchaseOrderLineViewModel(Products)
                {
                    SelectedProduct = product,
                    Quantity = 1,
                    UnitCost = product.PurchasePrice
                };
                line.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(PurchaseOrderLineViewModel.LineTotal))
                        RecalculateTotals();
                };
                var lastLine = Items.LastOrDefault();
                if (lastLine != null && lastLine.SelectedProduct == null)
                    Items.Remove(lastLine);
                Items.Add(line);
                _soundService.PlaySuccess();
            }

            RecalculateTotals();
            System.Windows.Application.Current.Dispatcher.Invoke(() => vm.CloseDialog());
        };

        _dialogService.ShowDialog(vm);
    }
    #endregion
}

public class PurchaseOrderLineViewModel : ViewModelBase
{
    private int _productId;
    private ProductDto? _selectedProduct;
    private decimal _quantity = 1;
    private decimal _receivedQuantity;
    private decimal _unitCost;
    private int _productUnitId;
    private string? _notes;
    private byte _mode = (byte)SaleMode.Retail;

    public ObservableCollection<ProductDto> AvailableProducts { get; }

    public PurchaseOrderLineViewModel(ObservableCollection<ProductDto> products)
    {
        AvailableProducts = products;
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
                if (UnitCost == 0)
                {
                    UnitCost = value.PurchasePrice;
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
            }
        }
    }

    public decimal ReceivedQuantity
    {
        get => _receivedQuantity;
        set => SetProperty(ref _receivedQuantity, value);
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
            }
        }
    }

    public int ProductUnitId
    {
        get => _productUnitId;
        set => SetProperty(ref _productUnitId, value);
    }

    public decimal LineTotal => Quantity * UnitCost;

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

    public string? Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }

    private void ValidateQuantity()
    {
        ClearErrors(nameof(Quantity));
        if (Quantity <= 0)
            AddError(nameof(Quantity), "الكمية يجب أن تكون أكبر من صفر");
        else if (Quantity > 999999)
            AddError(nameof(Quantity), "الكمية كبيرة جداً");
    }

    private void ValidateUnitCost()
    {
        ClearErrors(nameof(UnitCost));
        if (UnitCost < 0)
            AddError(nameof(UnitCost), "التكلفة لا يمكن أن تكون سالبة");
    }
}

