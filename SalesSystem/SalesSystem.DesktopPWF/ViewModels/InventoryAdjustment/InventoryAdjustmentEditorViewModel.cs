using System.Collections.ObjectModel;
using System.Windows.Input;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Responses;
using SalesSystem.DesktopPWF.Messaging.Messages;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Services.App.Toast;
using SalesSystem.DesktopPWF.ViewModels.Products;

namespace SalesSystem.DesktopPWF.ViewModels.InventoryAdjustment;

/// <summary>
/// ViewModel for Inventory Adjustment Editor (Create/Edit/Post).
/// Patterns: InventoryTransactionEditorViewModel (working reference).
/// </summary>
public class InventoryAdjustmentEditorViewModel : ViewModelBase
{
    private readonly IInventoryAdjustmentApiService _adjustmentService;
    private readonly IWarehouseApiService _warehouseService;
    private readonly IDialogService _dialogService;
    private readonly IEventBus _eventBus;
    private readonly IToastNotificationService _toast;

    private int? _adjustmentId;
    private int _adjustmentNo;
    private short _warehouseId;
    private string? _warehouseName;
    private byte _adjustmentType = 1; // Addition
    private DateTime _adjustmentDate = DateTime.Today;
    private int _accountId;
    private string? _accountName;
    private string? _notes;
    private string? _errorMessage;
    private byte _status = 1; // Draft

    private ObservableCollection<WarehouseDto> _warehouses = new();
    private ObservableCollection<InventoryAdjustmentLineItem> _lines = new();

    /// <summary>
    /// DI constructor — all services injected
    /// </summary>
    public InventoryAdjustmentEditorViewModel(
        IInventoryAdjustmentApiService adjustmentService,
        IWarehouseApiService warehouseService,
        IDialogService dialogService,
        IEventBus eventBus,
        IToastNotificationService toast)
    {
        _adjustmentService = adjustmentService ?? throw new ArgumentNullException(nameof(adjustmentService));
        _warehouseService = warehouseService ?? throw new ArgumentNullException(nameof(warehouseService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _toast = toast ?? throw new ArgumentNullException(nameof(toast));

        SetDialogService(dialogService);

        SaveCommand = new AsyncRelayCommand(SaveAsync);
        PostCommand = new AsyncRelayCommand(PostAsync);
        CancelCommand = new RelayCommand(_ => RequestClose());
        AddLineCommand = new AsyncRelayCommand(AddLineAsync);
        RemoveLineCommand = new RelayCommand(p => RemoveLine(p as InventoryAdjustmentLineItem));

        _ = LoadAsync();
    }

    /// <summary>
    /// Parameterless constructor — resolves services from DI container
    /// </summary>
    public InventoryAdjustmentEditorViewModel()
        : this(
            App.GetService<IInventoryAdjustmentApiService>(),
            App.GetService<IWarehouseApiService>(),
            App.GetService<IDialogService>(),
            App.GetService<IEventBus>(),
            App.GetService<IToastNotificationService>())
    {
    }

    /// <summary>
    /// Constructor for editing an existing adjustment (loads DTO data)
    /// </summary>
    public InventoryAdjustmentEditorViewModel(InventoryAdjustmentDto adjustment)
        : this(
            App.GetService<IInventoryAdjustmentApiService>(),
            App.GetService<IWarehouseApiService>(),
            App.GetService<IDialogService>(),
            App.GetService<IEventBus>(),
            App.GetService<IToastNotificationService>())
    {
        _adjustmentId = adjustment.Id;
        _adjustmentNo = adjustment.AdjustmentNo;
        _warehouseId = (short)adjustment.WarehouseId;
        _warehouseName = adjustment.WarehouseName;
        _adjustmentType = adjustment.AdjustmentType;
        _adjustmentDate = adjustment.AdjustmentDate;
        _accountId = adjustment.AccountId;
        _accountName = adjustment.AccountName;
        _notes = null; // Notes not in DTO yet; kept for future
        _status = adjustment.Status;

        if (adjustment.Lines != null)
        {
            foreach (var line in adjustment.Lines)
            {
                _lines.Add(new InventoryAdjustmentLineItem
                {
                    ProductId = line.ProductId,
                    ProductName = line.ProductName,
                    ProductUnitId = line.ProductUnitId,
                    ProductUnitName = line.ProductUnitName,
                    Quantity = line.Quantity,
                    UnitCost = line.UnitCost
                });
            }
        }
    }

    #region Properties

    public int? AdjustmentId => _adjustmentId;

    public string Title => _adjustmentId.HasValue ? "تعديل تسوية مخزون" : "تسوية مخزون جديدة";

    public int AdjustmentNo
    {
        get => _adjustmentNo;
        set => SetProperty(ref _adjustmentNo, value);
    }

    public short WarehouseId
    {
        get => _warehouseId;
        set
        {
            if (SetProperty(ref _warehouseId, value))
            {
                if (value <= 0)
                    AddError(nameof(WarehouseId), "المستودع مطلوب");
                else
                    ClearErrors(nameof(WarehouseId));
            }
        }
    }

    public string? WarehouseName
    {
        get => _warehouseName;
        set => SetProperty(ref _warehouseName, value);
    }

    public byte AdjustmentType
    {
        get => _adjustmentType;
        set
        {
            if (SetProperty(ref _adjustmentType, value))
            {
                if (value < 1 || value > 3)
                    AddError(nameof(AdjustmentType), "نوع التسوية غير صالح");
                else
                    ClearErrors(nameof(AdjustmentType));

                OnPropertyChanged(nameof(AdjustmentTypeDisplay));
                OnPropertyChanged(nameof(IsAdditionSelected));
                OnPropertyChanged(nameof(IsDeductionSelected));
                OnPropertyChanged(nameof(IsCorrectionSelected));
            }
        }
    }

    public string AdjustmentTypeDisplay => AdjustmentType switch
    {
        1 => "إضافة",
        2 => "خصم",
        3 => "تصحيح",
        _ => "غير معروف"
    };

    // Convenience booleans for XAML radio button IsChecked binding
    public bool IsAdditionSelected
    {
        get => _adjustmentType == 1;
        set { if (value) AdjustmentType = 1; }
    }

    public bool IsDeductionSelected
    {
        get => _adjustmentType == 2;
        set { if (value) AdjustmentType = 2; }
    }

    public bool IsCorrectionSelected
    {
        get => _adjustmentType == 3;
        set { if (value) AdjustmentType = 3; }
    }

    public DateTime AdjustmentDate
    {
        get => _adjustmentDate;
        set => SetProperty(ref _adjustmentDate, value);
    }

    public int AccountId
    {
        get => _accountId;
        set
        {
            if (SetProperty(ref _accountId, value))
            {
                if (value <= 0)
                    AddError(nameof(AccountId), "الحساب المحاسبي مطلوب");
                else
                    ClearErrors(nameof(AccountId));
            }
        }
    }

    public string? AccountName
    {
        get => _accountName;
        set => SetProperty(ref _accountName, value);
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

    public ObservableCollection<WarehouseDto> Warehouses
    {
        get => _warehouses;
        set => SetProperty(ref _warehouses, value);
    }

    public ObservableCollection<InventoryAdjustmentLineItem> Lines
    {
        get => _lines;
        set => SetProperty(ref _lines, value);
    }

    public bool HasLines => _lines.Count > 0;

    #endregion

    #region Commands

    public ICommand SaveCommand { get; }
    public ICommand PostCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand AddLineCommand { get; }
    public ICommand RemoveLineCommand { get; }

    #endregion

    #region Methods

    private async Task LoadAsync()
    {
        await ExecuteAsync(LoadWarehousesAsync);
    }

    private async Task LoadWarehousesAsync()
    {
        ErrorMessage = null;
        var result = await _warehouseService.GetAllAsync();
        if (result.IsSuccess && result.Value != null)
        {
            Warehouses = new ObservableCollection<WarehouseDto>(result.Value);
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل المستودعات",
                "InventoryAdjustmentEditorViewModel.LoadWarehousesAsync");
        }
    }

    private async Task AddLineAsync()
    {
        var vm = new ProductSelectionViewModel(0);
        vm.OnProductSelected += product =>
        {
            var existing = Lines.FirstOrDefault(l => l.ProductId == product.Id);
            if (existing != null)
            {
                existing.Quantity += 1;
            }
            else
            {
                Lines.Add(new InventoryAdjustmentLineItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    ProductUnitId = product.DefaultPurchaseUnitId ?? 0, // Use purchase unit for inventory ops; 0 = service auto
                    ProductUnitName = "حبة",
                    Quantity = 1,
                    UnitCost = 0m
                });
            }
            OnPropertyChanged(nameof(HasLines));
            vm.CloseDialog();
        };

        _dialogService.ShowDialog(vm);
    }

    private void RemoveLine(InventoryAdjustmentLineItem? line)
    {
        if (line != null)
        {
            Lines.Remove(line);
            OnPropertyChanged(nameof(HasLines));
        }
    }

    private async Task<bool> ValidateAsync()
    {
        ClearAllErrors();

        if (WarehouseId <= 0)
            AddError(nameof(WarehouseId), "يجب اختيار مستودع");

        if (AccountId <= 0)
            AddError(nameof(AccountId), "الحساب المحاسبي مطلوب");

        if (AdjustmentType < 1 || AdjustmentType > 3)
            AddError(nameof(AdjustmentType), "نوع التسوية غير صالح (1=إضافة, 2=خصم, 3=تصحيح)");

        if (Lines.Count == 0)
            AddError(nameof(Lines), "يجب إضافة صنف واحد على الأقل");

        var invalidLines = Lines.Where(l => l.Quantity <= 0).ToList();
        if (invalidLines.Any())
            AddError(nameof(Lines), $"الكمية يجب أن تكون أكبر من صفر (الأصناف غير الصحيحة: {invalidLines.Count})");

        var negativeCostLines = Lines.Where(l => l.UnitCost < 0).ToList();
        if (negativeCostLines.Any())
            AddError(nameof(Lines), "تكلفة الوحدة لا يمكن أن تكون سالبة");

        return await ValidateAllAsync();
    }

    /// <summary>
    /// Saves the adjustment as a draft via API, publishes message, and closes.
    /// </summary>
    private async Task SaveAsync()
    {
        if (!await ValidateAsync()) return;

        await ExecuteAsync(async () =>
        {
            ErrorMessage = null;

            var request = new CreateInventoryAdjustmentRequest(
                WarehouseId,
                AdjustmentDate,
                AdjustmentType,
                AccountId);

            var result = await _adjustmentService.CreateAsync(request);
            if (result.IsSuccess && result.Value != null)
            {
                _adjustmentId = result.Value.Id;
                _eventBus.Publish(new InventoryAdjustmentChangedMessage(result.Value.Id));
                _toast.ShowSuccess("تم إنشاء التسوية بنجاح");
                RequestClose();
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "حدث خطأ أثناء الحفظ",
                    "InventoryAdjustmentEditorViewModel.SaveAsync");
                await _dialogService.ShowErrorAsync("خطأ في الحفظ", ErrorMessage);
            }
        });
    }

    /// <summary>
    /// Posts the adjustment (saves first if it is a new draft, then posts).
    /// Follows the same pattern as InventoryTransactionEditorViewModel.PostAsync.
    /// </summary>
    private async Task PostAsync()
    {
        if (!await ValidateAsync()) return;

        await ExecuteAsync(async () =>
        {
            ErrorMessage = null;

            // Save first if needed (no ID yet)
            if (!_adjustmentId.HasValue)
            {
                var createRequest = new CreateInventoryAdjustmentRequest(
                    WarehouseId,
                    AdjustmentDate,
                    AdjustmentType,
                    AccountId);

                var createResult = await _adjustmentService.CreateAsync(createRequest);
                if (!createResult.IsSuccess || createResult.Value == null)
                {
                    ErrorMessage = HandleFailure(createResult.Error ?? "حدث خطأ أثناء الحفظ",
                        "InventoryAdjustmentEditorViewModel.PostAsync");
                    await _dialogService.ShowErrorAsync("خطأ في الترحيل", ErrorMessage);
                    return;
                }
                _adjustmentId = createResult.Value.Id;
            }

            // Post the adjustment
            var postResult = await _adjustmentService.PostAsync(_adjustmentId.Value);
            if (postResult.IsSuccess)
            {
                _eventBus.Publish(new InventoryAdjustmentChangedMessage(_adjustmentId.Value));
                _toast.ShowSuccess("تم الترحيل بنجاح");
                RequestClose();
            }
            else
            {
                ErrorMessage = HandleFailure(postResult.Error ?? "حدث خطأ أثناء الترحيل",
                    "InventoryAdjustmentEditorViewModel.PostAsync");
                await _dialogService.ShowErrorAsync("خطأ في الترحيل", ErrorMessage);
            }
        });
    }

    public override void Cleanup()
    {
        base.Cleanup();
    }

    #endregion
}

/// <summary>
/// Editable line item ViewModel for Inventory Adjustment lines.
/// Follows the same pattern as InventoryTransactionLineItem.
/// </summary>
public class InventoryAdjustmentLineItem : ViewModelBase
{
    private int _productId;
    private string? _productName;
    private int _productUnitId;
    private string? _productUnitName;
    private decimal _quantity;
    private decimal _unitCost;

    public int ProductId
    {
        get => _productId;
        set => SetProperty(ref _productId, value);
    }

    public string? ProductName
    {
        get => _productName;
        set => SetProperty(ref _productName, value);
    }

    public int ProductUnitId
    {
        get => _productUnitId;
        set => SetProperty(ref _productUnitId, value);
    }

    public string? ProductUnitName
    {
        get => _productUnitName;
        set => SetProperty(ref _productUnitName, value);
    }

    public decimal Quantity
    {
        get => _quantity;
        set
        {
            if (SetProperty(ref _quantity, value))
                OnPropertyChanged(nameof(TotalCost));
        }
    }

    public decimal UnitCost
    {
        get => _unitCost;
        set
        {
            if (SetProperty(ref _unitCost, value))
                OnPropertyChanged(nameof(TotalCost));
        }
    }

    /// <summary>Computed total = Quantity × UnitCost</summary>
    public decimal TotalCost => Quantity * UnitCost;
}
