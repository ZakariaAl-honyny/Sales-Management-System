using SalesSystem.DesktopPWF.Messaging.Messages;
using SalesSystem.DesktopPWF.Services.App.Toast;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Helpers;

namespace SalesSystem.DesktopPWF.ViewModels.InventoryOperations;

/// <summary>
/// ViewModel for Inventory Operation Editor
/// Supports three operation types: صرف مخزني (1), توريد مخزني (2), تسوية مخزنية (3)
/// </summary>
public class InventoryOperationEditorViewModel : ViewModelBase
{
    private readonly IInventoryOperationApiService _operationService;
    private readonly IWarehouseApiService _warehouseService;
    private readonly IProductApiService _productService;
    private readonly IEventBus _eventBus;
    private readonly IDialogService _dialogService;
    private readonly IToastNotificationService _toastService;
    private readonly ISoundService _soundService;
    private readonly IInventoryApiService _inventoryService;

    private readonly byte _operationType;
    private readonly int? _operationId;
    private readonly bool _isReadOnly;

    private int _warehouseId;
    private DateTime _operationDate = DateTime.Today;
    private string _referenceNo = string.Empty;
    private string _notes = string.Empty;
    private byte? _adjustmentType;
    private string _errorMessage = string.Empty;
    private string _searchText = string.Empty;

    private ObservableCollection<WarehouseDto> _warehouses = new();
    private ObservableCollection<ProductDto> _products = new();
    private ObservableCollection<OperationItemViewModel> _items = new();

    /// <summary>
    /// Parameterless constructor for design-time / XAML support.
    /// </summary>
    public InventoryOperationEditorViewModel() : this(1)
    {
    }

    /// <summary>
    /// Constructor with operation type.
    /// </summary>
    /// <param name="operationType">1=StockIssue, 2=StockReceipt, 3=Adjustment</param>
    /// <param name="operationId">Optional existing operation ID for edit/view</param>
    /// <param name="isReadOnly">True for view-only mode</param>
    public InventoryOperationEditorViewModel(
        byte operationType,
        int? operationId = null,
        bool isReadOnly = false)
        : this(
            App.GetService<IInventoryOperationApiService>(),
            App.GetService<IWarehouseApiService>(),
            App.GetService<IProductApiService>(),
            App.GetService<IEventBus>(),
            App.GetService<IDialogService>(),
            App.GetService<IToastNotificationService>(),
            App.GetService<ISoundService>(),
            App.GetService<IInventoryApiService>(),
            operationType,
            operationId,
            isReadOnly)
    {
    }

    public InventoryOperationEditorViewModel(
        IInventoryOperationApiService operationService,
        IWarehouseApiService warehouseService,
        IProductApiService productService,
        IEventBus eventBus,
        IDialogService dialogService,
        IToastNotificationService toastService,
        ISoundService soundService,
        IInventoryApiService inventoryService,
        byte operationType,
        int? operationId = null,
        bool isReadOnly = false)
    {
        _operationService = operationService ?? throw new ArgumentNullException(nameof(operationService));
        _warehouseService = warehouseService ?? throw new ArgumentNullException(nameof(warehouseService));
        _productService = productService ?? throw new ArgumentNullException(nameof(productService));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _toastService = toastService ?? throw new ArgumentNullException(nameof(toastService));
        _soundService = soundService ?? throw new ArgumentNullException(nameof(soundService));
        _inventoryService = inventoryService ?? throw new ArgumentNullException(nameof(inventoryService));
        _operationType = operationType;
        _operationId = operationId;
        _isReadOnly = isReadOnly;

        SetDialogService(dialogService);

        AddItemCommand = new RelayCommand(_ => OnAddItem());
        RemoveItemCommand = new RelayCommand(p => OnRemoveItem(p as OperationItemViewModel));
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        CancelCommand = new RelayCommand(_ => OnCancel());
        PostCommand = new AsyncRelayCommand(PostAsync);
        SearchProductCommand = new RelayCommand(SearchProduct);
        QuickAddProductCommand = new AsyncRelayCommand(QuickAddProductAsync);

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            await LoadWarehousesAsync();
            await LoadProductsAsync();
            if (_operationId.HasValue)
            {
                await LoadOperationAsync();
            }
            else
            {
                // Add empty row for new operations
                OnAddItem();
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Error in {Method}", nameof(InitializeAsync));
            await _dialogService.ShowErrorAsync("خطأ", "حدث خطأ أثناء تحميل البيانات");
        }
    }

    #region Properties

    public byte OperationType => _operationType;

    public int WarehouseId
    {
        get => _warehouseId;
        set => SetProperty(ref _warehouseId, value);
    }

    public DateTime OperationDate
    {
        get => _operationDate;
        set => SetProperty(ref _operationDate, value);
    }

    public string ReferenceNo
    {
        get => _referenceNo;
        set => SetProperty(ref _referenceNo, value);
    }

    public string Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }

    public byte? AdjustmentType
    {
        get => _adjustmentType;
        set => SetProperty(ref _adjustmentType, value);
    }

    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public bool IsReadOnly => _isReadOnly;
    public bool IsEdit => _operationId.HasValue;
    public bool IsAdjustmentType => _operationType == 3;
    public bool IsStockIssue => _operationType == 1;
    public bool IsStockReceipt => _operationType == 2;

    /// <summary>
    /// Window title based on operation type and mode.
    /// </summary>
    public string WindowTitle
    {
        get
        {
            if (_isReadOnly)
                return _operationType switch { 1 => "عرض صرف مخزني", 2 => "عرض توريد مخزني", 3 => "عرض تسوية مخزنية", _ => "عرض عملية مخزنية" };
            if (_operationId.HasValue)
                return _operationType switch { 1 => "تعديل صرف مخزني", 2 => "تعديل توريد مخزني", 3 => "تعديل تسوية مخزنية", _ => "تعديل عملية مخزنية" };
            return _operationType switch { 1 => "صرف مخزني جديد", 2 => "توريد مخزني جديد", 3 => "تسوية مخزنية جديدة", _ => "عملية مخزنية جديدة" };
        }
    }

    /// <summary>
    /// Subtitle shown below the header.
    /// </summary>
    public string Subtitle => _operationType switch
    {
        1 => "صرف أصناف من المخزن لأغراض مختلفة",
        2 => "إضافة أصناف إلى المخزن",
        3 => "تسوية الفروق المخزنية",
        _ => ""
    };

    /// <summary>
    /// Header icon.
    /// </summary>
    public string HeaderIcon => _operationType switch
    {
        1 => "⬇️",
        2 => "⬆️",
        3 => "⚖️",
        _ => "📦"
    };

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

    public ObservableCollection<OperationItemViewModel> Items
    {
        get => _items;
        set => SetProperty(ref _items, value);
    }

    /// <summary>
    /// Adjustment type options for ComboBox (only visible for Adjustment type).
    /// </summary>
    public List<KeyValuePair<byte?, string>> AdjustmentTypeList { get; } = new()
    {
        new KeyValuePair<byte?, string>(1, "فائض (زيادة في المخزون)"),
        new KeyValuePair<byte?, string>(2, "عجز (نقص في المخزون)"),
    };

    /// <summary>
    /// Stock issue reason options for ComboBox (only visible for Issue type).
    /// </summary>
    public List<KeyValuePair<byte?, string>> StockIssueReasonList { get; } = new()
    {
        new KeyValuePair<byte?, string>(1, "تالف"),
        new KeyValuePair<byte?, string>(2, "استخدام داخلي"),
        new KeyValuePair<byte?, string>(3, "عينة مجانية"),
        new KeyValuePair<byte?, string>(4, "أخرى"),
    };

    #endregion

    #region Commands

    public ICommand AddItemCommand { get; }
    public ICommand RemoveItemCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand PostCommand { get; }
    public ICommand SearchProductCommand { get; }
    public ICommand QuickAddProductCommand { get; }

    #endregion

    #region Methods

    private async Task LoadWarehousesAsync()
    {
        try
        {
            var result = await _warehouseService.GetAllAsync();
            if (result.IsSuccess && result.Value != null)
            {
                Warehouses = new ObservableCollection<WarehouseDto>(result.Value);
            }
        }
        catch (Exception ex)
        {
            HandleException(ex, "InventoryOperationEditorViewModel.LoadWarehousesAsync",
                "[InventoryOperationEditorViewModel.LoadWarehousesAsync] Failed to load warehouses.");
        }
    }

    private async Task LoadProductsAsync()
    {
        try
        {
            var result = await _productService.GetAllAsync();
            if (result.IsSuccess && result.Value != null)
            {
                Products = new ObservableCollection<ProductDto>(result.Value);
            }
        }
        catch (Exception ex)
        {
            HandleException(ex, "InventoryOperationEditorViewModel.LoadProductsAsync",
                "[InventoryOperationEditorViewModel.LoadProductsAsync] Failed to load products.");
        }
    }

    private async Task LoadOperationAsync()
    {
        if (!_operationId.HasValue) return;

        await ExecuteAsync(async () =>
        {
            ErrorMessage = string.Empty;
            var result = await _operationService.GetByIdAsync(_operationId.Value);
            if (result.IsSuccess && result.Value != null)
            {
                var op = result.Value;
                WarehouseId = op.WarehouseId;
                OperationDate = op.OperationDate;
                ReferenceNo = op.ReferenceNo ?? string.Empty;
                Notes = op.Notes ?? string.Empty;
                AdjustmentType = op.AdjustmentType;

                Items.Clear();
                foreach (var item in op.Items)
                {
                    Items.Add(new OperationItemViewModel(_soundService, _inventoryService, WarehouseId)
                    {
                        Products = this.Products,
                        ProductId = item.ProductId,
                        ProductName = item.ProductName,
                        Quantity = item.Quantity,
                        UnitCost = item.UnitCost,
                        StockIssueReason = item.StockIssueReason,
                        Notes = item.Notes
                    });
                }
            }
            else
            {
                ErrorMessage = result.Error ?? "حدث خطأ غير معروف";
                await _dialogService.ShowErrorAsync("خطأ", ErrorMessage);
            }
        });
    }

    private void OnAddItem()
    {
        var item = new OperationItemViewModel(_soundService, _inventoryService, WarehouseId)
        {
            Products = this.Products
        };
        Items.Add(item);
    }

    private void OnRemoveItem(OperationItemViewModel? item)
    {
        if (item != null)
        {
            Items.Remove(item);
        }
    }

    private async Task QuickAddProductAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText)) return;

        var searchText = SearchText.Trim();
        SearchText = string.Empty; // Clear immediately for responsiveness

        // Find product by barcode or name locally first
        var product = Products.FirstOrDefault(p =>
            p.Name.Equals(searchText, StringComparison.OrdinalIgnoreCase) ||
            (p.Barcode?.Equals(searchText, StringComparison.OrdinalIgnoreCase) ?? false));

        if (product == null)
        {
            // Fallback to API if not found locally
            var apiResult = await _productService.GetByBarcodeAsync(searchText);
            if (apiResult.IsSuccess && apiResult.Value != null)
            {
                product = apiResult.Value;
            }
        }

        if (product != null)
        {
            if (WarehouseId <= 0)
            {
                await _dialogService.ShowWarningAsync("تنبيه", "يجب اختيار المستودع أولاً");
                return;
            }

            var existingItem = Items.FirstOrDefault(i => i.ProductId == product.Id);
            if (existingItem != null)
            {
                existingItem.Quantity += 1;
            }
            else
            {
                Items.Add(new OperationItemViewModel(_soundService, _inventoryService, WarehouseId)
                {
                    Products = this.Products,
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Quantity = 1,
                    UnitCost = _operationType == 2 ? product.PurchasePrice : null,
                    StockIssueReason = _operationType == 1 ? (byte?)4 : null // Default "أخرى" for issue
                });
            }
            _soundService.PlaySuccess();
        }
        else
        {
            _soundService.PlayError();
            await _dialogService.ShowWarningAsync("غير موجود", "لم يتم العثور على المنتج. يرجى التحقق من الاسم أو الباركود.");
        }
    }

    private void SearchProduct(object? parameter)
    {
        var vm = new ViewModels.Products.ProductSelectionViewModel(WarehouseId);

        vm.OnProductSelected += async (product) =>
        {
            if (WarehouseId <= 0)
            {
                await _dialogService.ShowWarningAsync("تنبيه", "يجب اختيار المستودع أولاً");
                return;
            }

            var existingItem = Items.FirstOrDefault(i => i.ProductId == product.Id);
            if (existingItem != null)
            {
                existingItem.Quantity += 1;
            }
            else
            {
                var emptyLine = Items.FirstOrDefault(i => i.ProductId == 0);
                if (emptyLine != null) Items.Remove(emptyLine);

                Items.Add(new OperationItemViewModel(_soundService, _inventoryService, WarehouseId)
                {
                    Products = this.Products,
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Quantity = 1,
                    UnitCost = _operationType == 2 ? product.PurchasePrice : null,
                    StockIssueReason = _operationType == 1 ? (byte?)4 : null
                });
            }
            _soundService.PlaySuccess();

            System.Windows.Application.Current.Dispatcher.Invoke(() => vm.CloseDialog());
        };

        _dialogService.ShowDialog(vm);

        // Ensure there is an empty line if needed
        if (Items.All(i => i.ProductId != 0))
        {
            OnAddItem();
        }
    }

    private async Task<bool> ValidateAsync()
    {
        ClearAllErrors();

        if (WarehouseId <= 0)
            AddError(nameof(WarehouseId), "المستودع مطلوب");

        if (Items.Count == 0 || Items.All(i => i.ProductId == 0))
            AddError(nameof(Items), "يجب إضافة صنف واحد على الأقل");

        if (Items.Any(i => i.ProductId == 0))
            AddError(nameof(Items), "يرجى اختيار منتج لكل صنف مضاف");

        if (Items.Any(i => i.Quantity <= 0))
            AddError(nameof(Items), "يرجى إدخال كمية صحيحة (أكبر من 0) لكل صنف");

        if (_operationType == 3 && !AdjustmentType.HasValue)
            AddError(nameof(AdjustmentType), "نوع التسوية مطلوب للتسويات المخزنية");

        return await ValidateAllAsync();
    }

    private async Task SaveAsync()
    {
        if (!await ValidateAsync()) return;

        await ExecuteAsync(async () =>
        {
            ErrorMessage = string.Empty;

            var items = Items
                .Where(i => i.ProductId > 0 && i.Quantity > 0)
                .Select(i => new CreateInventoryOperationItemRequest(
                    i.ProductId,
                    i.Quantity,
                    _operationType == 2 ? i.UnitCost : null, // UnitCost only for receipt
                    _operationType == 1 ? i.StockIssueReason : null, // Reason only for issue
                    string.IsNullOrWhiteSpace(i.Notes) ? null : i.Notes
                ))
                .ToList();

            var request = new CreateInventoryOperationRequest(
                WarehouseId,
                _operationType,
                _operationType == 3 ? AdjustmentType : null,
                OperationDate,
                string.IsNullOrWhiteSpace(ReferenceNo) ? null : ReferenceNo,
                string.IsNullOrWhiteSpace(Notes) ? null : Notes,
                items);

            Result<InventoryOperationDto> result;

            result = await _operationService.CreateAsync(request);

            if (result.IsSuccess)
            {
                var opId = result.Value!.Id;
                _eventBus.Publish(new InventoryOperationChangedMessage(opId));
                _toastService.ShowSuccess("تم حفظ العملية المخزنية بنجاح");
                RequestClose();
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "حدث خطأ غير معروف",
                    "InventoryOperationEditorViewModel.SaveAsync",
                    "[InventoryOperationEditorViewModel.SaveAsync] Failed to save inventory operation.");
                await _dialogService.ShowErrorAsync("خطأ في الحفظ", ErrorMessage);
            }
        });
    }

    private async Task PostAsync()
    {
        if (!await ValidateAsync()) return;

        var confirmed = await _dialogService.ShowConfirmationAsync("تأكيد الترحيل",
            "هل أنت متأكد من ترحيل هذه العملية المخزنية؟\n\n" +
            "• سيتم تحديث المخزون فوراً.\n" +
            "• لا يمكن التراجع عن الترحيل.");

        if (!confirmed) return;

        await ExecuteAsync(async () =>
        {
            ErrorMessage = string.Empty;

            // First save as draft
            var items = Items
                .Where(i => i.ProductId > 0 && i.Quantity > 0)
                .Select(i => new CreateInventoryOperationItemRequest(
                    i.ProductId,
                    i.Quantity,
                    _operationType == 2 ? i.UnitCost : null,
                    _operationType == 1 ? i.StockIssueReason : null,
                    string.IsNullOrWhiteSpace(i.Notes) ? null : i.Notes
                ))
                .ToList();

            var request = new CreateInventoryOperationRequest(
                WarehouseId,
                _operationType,
                _operationType == 3 ? AdjustmentType : null,
                OperationDate,
                string.IsNullOrWhiteSpace(ReferenceNo) ? null : ReferenceNo,
                string.IsNullOrWhiteSpace(Notes) ? null : Notes,
                items);

            var createResult = await _operationService.CreateAsync(request);

            if (!createResult.IsSuccess || createResult.Value == null)
            {
                ErrorMessage = HandleFailure(createResult.Error ?? "فشل في إنشاء العملية",
                    "InventoryOperationEditorViewModel.PostAsync",
                    "[InventoryOperationEditorViewModel.PostAsync] Failed to create operation.");
                await _dialogService.ShowErrorAsync("خطأ", ErrorMessage);
                return;
            }

            // Then post immediately
            var postResult = await _operationService.PostAsync(createResult.Value.Id);

            if (postResult.IsSuccess)
            {
                _eventBus.Publish(new InventoryOperationChangedMessage(postResult.Value!.Id));
                _toastService.ShowSuccess("تم ترحيل العملية المخزنية بنجاح");
                RequestClose();
            }
            else
            {
                ErrorMessage = HandleFailure(postResult.Error ?? "فشل في الترحيل",
                    "InventoryOperationEditorViewModel.PostAsync",
                    "[InventoryOperationEditorViewModel.PostAsync] Failed to post operation.");
                await _dialogService.ShowErrorAsync("خطأ في الترحيل", ErrorMessage);
            }
        });
    }

    private void OnCancel()
    {
        RequestClose();
    }

    #endregion
}

/// <summary>
/// ViewModel for a single inventory operation item row
/// </summary>
public class OperationItemViewModel : ViewModelBase
{
    private readonly ISoundService? _soundService;
    private readonly IInventoryApiService? _inventoryService;
    private int _warehouseId;
    private int _productId;
    private string _productName = string.Empty;
    private decimal _quantity;
    private decimal? _unitCost;
    private byte? _stockIssueReason;
    private string? _notes;

    public OperationItemViewModel(ISoundService? soundService = null, IInventoryApiService? inventoryService = null, int warehouseId = 0)
    {
        _soundService = soundService;
        _inventoryService = inventoryService;
        _warehouseId = warehouseId;
    }

    public int ProductId
    {
        get => _productId;
        set
        {
            if (_productId == value) return;
            _productId = value;
            OnPropertyChanged();

            // Auto-fill product name from the Products lookup
            if (Products?.Any(p => p.Id == value) == true)
            {
                var product = Products!.First(p => p.Id == value);
                ProductName = product.Name;

                // Set default UnitCost from PurchasePrice for receipt
                if (UnitCost == null || UnitCost == 0)
                {
                    UnitCost = product.PurchasePrice;
                }
            }
        }
    }

    public string ProductName
    {
        get => _productName;
        set => SetProperty(ref _productName, value);
    }

    public decimal Quantity
    {
        get => _quantity;
        set
        {
            if (SetProperty(ref _quantity, value))
            {
                _soundService?.PlaySuccess();
            }
        }
    }

    public decimal? UnitCost
    {
        get => _unitCost;
        set => SetProperty(ref _unitCost, value);
    }

    public byte? StockIssueReason
    {
        get => _stockIssueReason;
        set => SetProperty(ref _stockIssueReason, value);
    }

    public string? Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }

    public ObservableCollection<ProductDto>? Products { get; set; }
}
