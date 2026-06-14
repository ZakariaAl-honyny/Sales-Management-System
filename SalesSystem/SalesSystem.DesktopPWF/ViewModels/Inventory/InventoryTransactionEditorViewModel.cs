using System.Collections.ObjectModel;
using System.Windows.Input;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.DesktopPWF.Messaging.Messages;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Services.App.Toast;
using SalesSystem.DesktopPWF.ViewModels.Products;

namespace SalesSystem.DesktopPWF.ViewModels.Inventory;

public class InventoryTransactionEditorViewModel : ViewModelBase
{
    private readonly IInventoryApiService _inventoryApiService;
    private readonly IWarehouseApiService _warehouseService;
    private readonly IDialogService _dialogService;
    private readonly IEventBus _eventBus;
    private readonly IT‌oastNotificationService _toast;
    private byte _transactionType;

    private int? _transactionId;
    private int _currentTransactionNo;
    private short _warehouseId;
    private DateTime _transactionDate = DateTime.Today;
    private string? _notes;
    private string? _errorMessage;
    private byte _status = 1; // Draft

    private ObservableCollection<WarehouseDto> _warehouses = new();
    private ObservableCollection<InventoryTransactionLineItem> _lines = new();

    public InventoryTransactionEditorViewModel(
        IInventoryApiService inventoryApiService,
        IWarehouseApiService warehouseService,
        IDialogService dialogService,
        IEventBus eventBus,
        ITo‌astNotificationService toast,
        byte transactionType)
    {
        _inventoryApiService = inventoryApiService;
        _warehouseService = warehouseService;
        _dialogService = dialogService;
        _eventBus = eventBus;
        _toast = toast;
        _transactionType = transactionType;

        SetDialogService(dialogService);

        SaveCommand = new AsyncRelayCommand(SaveAsync);
        PostCommand = new AsyncRelayCommand(PostAsync);
        CancelCommand = new RelayCommand(_ => RequestClose());
        AddLineCommand = new AsyncRelayCommand(AddLineAsync);
        RemoveLineCommand = new RelayCommand(p => RemoveLine(p as InventoryTransactionLineItem));

        _ = LoadAsync();
    }

    public InventoryTransactionEditorViewModel()
        : this(
            App.GetService<IInventoryApiService>(),
            App.GetService<IWarehouseApiService>(),
            App.GetService<IDialogService>(),
            App.GetService<IEventBus>(),
            App.GetService<ITo‌astNotificationService>(),
            11)
    {
    }

    public string Title => TransactionType switch
    {
        9 => "تالف وهالك",
        11 => "صرف مخزني",
        12 => "توريد مخزني",
        _ => "حركة مخزنية"
    };

    public string TransactionTypeDisplay => TransactionType switch
    {
        9 => "تلف",
        11 => "صرف داخلي",
        12 => "استلام داخلي",
        _ => "غير معروف"
    };

    public byte TransactionType => _transactionType;

    public int? TransactionId => _transactionId;

    public void SetTransactionType(byte type) => _transactionType = type;

    public int CurrentTransactionNo
    {
        get => _currentTransactionNo;
        set => SetProperty(ref _currentTransactionNo, value);
    }

    public short WarehouseId
    {
        get => _warehouseId;
        set => SetProperty(ref _warehouseId, value);
    }

    public DateTime TransactionDate
    {
        get => _transactionDate;
        set => SetProperty(ref _transactionDate, value);
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

    public ObservableCollection<InventoryTransactionLineItem> Lines
    {
        get => _lines;
        set => SetProperty(ref _lines, value);
    }

    public ICommand SaveCommand { get; }
    public ICommand PostCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand AddLineCommand { get; }
    public ICommand RemoveLineCommand { get; }

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
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل المستودعات", "InventoryTransactionEditorViewModel.LoadWarehousesAsync");
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
                Lines.Add(new InventoryTransactionLineItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    ProductUnitId = 1,
                    ProductUnitName = "حبة",
                    Quantity = 1,
                    UnitCost = 0m
                });
            }
            vm.CloseDialog();
        };

        _dialogService.ShowDialog(vm);
    }

    private void RemoveLine(InventoryTransactionLineItem? line)
    {
        if (line != null)
            Lines.Remove(line);
    }

    private async Task SaveAsync()
    {
        if (!await ValidateAsync()) return;

        await ExecuteAsync(async () =>
        {
            ErrorMessage = null;

            var request = new CreateInventoryTransactionRequest(
                0,
                _transactionType,
                WarehouseId,
                TransactionDate,
                ReferenceType: null,
                ReferenceId: null,
                Notes,
                Lines.Select(l => new CreateInventoryTransactionLineRequest(
                    l.ProductId,
                    l.ProductUnitId,
                    l.Quantity,
                    l.UnitCost,
                    null)).ToList());

            var result = await _inventoryApiService.CreateInventoryTransactionAsync(request);
            if (result.IsSuccess)
            {
                _transactionId = result.Value!.Id;
                _eventBus.Publish(new InventoryTransactionChangedMessage(_transactionId.Value));
                _toast.ShowSuccess("تم حفظ المسودة بنجاح");
                RequestClose();
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "حدث خطأ أثناء الحفظ", "InventoryTransactionEditorViewModel.SaveAsync");
                await _dialogService.ShowErrorAsync("خطأ في الحفظ", ErrorMessage);
            }
        });
    }

    private async Task PostAsync()
    {
        if (!await ValidateAsync()) return;

        await ExecuteAsync(async () =>
        {
            ErrorMessage = null;

            // Save first if needed
            if (!_transactionId.HasValue)
            {
                var createRequest = new CreateInventoryTransactionRequest(
                    0,
                    _transactionType,
                    WarehouseId,
                    TransactionDate,
                    ReferenceType: null,
                    ReferenceId: null,
                    Notes,
                    Lines.Select(l => new CreateInventoryTransactionLineRequest(
                        l.ProductId,
                        l.ProductUnitId,
                        l.Quantity,
                        l.UnitCost,
                        null)).ToList());

                var createResult = await _inventoryApiService.CreateInventoryTransactionAsync(createRequest);
                if (!createResult.IsSuccess)
                {
                    ErrorMessage = HandleFailure(createResult.Error ?? "حدث خطأ أثناء الحفظ", "InventoryTransactionEditorViewModel.PostAsync");
                    await _dialogService.ShowErrorAsync("خطأ في الترحيل", ErrorMessage);
                    return;
                }
                _transactionId = createResult.Value!.Id;
            }

            var postResult = await _inventoryApiService.PostInventoryTransactionAsync(_transactionId.Value);
            if (postResult.IsSuccess)
            {
                _eventBus.Publish(new InventoryTransactionChangedMessage(_transactionId.Value));
                _toast.ShowSuccess("تم الترحيل بنجاح");
                RequestClose();
            }
            else
            {
                ErrorMessage = HandleFailure(postResult.Error ?? "حدث خطأ أثناء الترحيل", "InventoryTransactionEditorViewModel.PostAsync");
                await _dialogService.ShowErrorAsync("خطأ في الترحيل", ErrorMessage);
            }
        });
    }

    private async Task<bool> ValidateAsync()
    {
        ClearAllErrors();

        if (WarehouseId <= 0)
            AddError(nameof(WarehouseId), "يجب اختيار مستودع");

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

    public override void Cleanup()
    {
        base.Cleanup();
    }
}

public class InventoryTransactionLineItem : ViewModelBase
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

    public decimal TotalCost => Quantity * UnitCost;
}
