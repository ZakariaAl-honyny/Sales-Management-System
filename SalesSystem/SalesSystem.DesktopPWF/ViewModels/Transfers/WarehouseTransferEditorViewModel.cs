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

namespace SalesSystem.DesktopPWF.ViewModels.Transfers;

/// <summary>
/// ViewModel for creating, editing, and posting warehouse transfers.
/// Follows the InventoryTransactionEditorViewModel pattern exactly.
/// </summary>
public class WarehouseTransferEditorViewModel : ViewModelBase
{
    private readonly IWarehouseTransferApiService _transferService;
    private readonly IWarehouseApiService _warehouseService;
    private readonly IDialogService _dialogService;
    private readonly IEventBus _eventBus;
    private readonly IToastNotificationService _toast;

    private int? _transferId;
    private bool _isReadOnly;
    private short _sourceWarehouseId;
    private short _destinationWarehouseId;
    private DateTime _transferDate = DateTime.Today;
    private string? _notes;
    private string? _errorMessage;
    private byte _status = 1; // Draft

    private ObservableCollection<WarehouseDto> _sourceWarehouses = new();
    private ObservableCollection<WarehouseDto> _destinationWarehouses = new();
    private ObservableCollection<WarehouseTransferLineItem> _lines = new();

    /// <summary>
    /// Primary constructor — DI resolved. Used by App.GetService for creating new transfers.
    /// </summary>
    public WarehouseTransferEditorViewModel(
        IWarehouseTransferApiService transferService,
        IWarehouseApiService warehouseService,
        IDialogService dialogService,
        IEventBus eventBus,
        IToastNotificationService toast)
        : this(transferService, warehouseService, dialogService, eventBus, toast, null, false)
    {
    }

    /// <summary>
    /// Full constructor with all services plus optional transferId and isReadOnly.
    /// </summary>
    public WarehouseTransferEditorViewModel(
        IWarehouseTransferApiService transferService,
        IWarehouseApiService warehouseService,
        IDialogService dialogService,
        IEventBus eventBus,
        IToastNotificationService toast,
        int? transferId = null,
        bool isReadOnly = false)
    {
        _transferService = transferService;
        _warehouseService = warehouseService;
        _dialogService = dialogService;
        _eventBus = eventBus;
        _toast = toast;
        _transferId = transferId;
        _isReadOnly = isReadOnly;

        SetDialogService(dialogService);

        SaveCommand = new AsyncRelayCommand(SaveAsync);
        PostCommand = new AsyncRelayCommand(PostAsync);
        CancelCommand = new RelayCommand(_ => RequestClose());
        AddLineCommand = new AsyncRelayCommand(AddLineAsync);
        RemoveLineCommand = new RelayCommand(p => RemoveLine(p as WarehouseTransferLineItem));

        _ = LoadAsync();
    }

    /// <summary>
    /// Parameterless constructor for XAML designer / service locator support.
    /// </summary>
    public WarehouseTransferEditorViewModel()
        : this(
            App.GetService<IWarehouseTransferApiService>(),
            App.GetService<IWarehouseApiService>(),
            App.GetService<IDialogService>(),
            App.GetService<IEventBus>(),
            App.GetService<IToastNotificationService>(),
            null,
            false)
    {
    }

    /// <summary>
    /// Convenience constructor for editing an existing transfer.
    /// Used by e.g. WarehouseTransfersListViewModel.OnEdit().
    /// </summary>
    public WarehouseTransferEditorViewModel(int transferId)
        : this(
            App.GetService<IWarehouseTransferApiService>(),
            App.GetService<IWarehouseApiService>(),
            App.GetService<IDialogService>(),
            App.GetService<IEventBus>(),
            App.GetService<IToastNotificationService>(),
            transferId,
            false)
    {
    }

    /// <summary>
    /// Convenience constructor for view-only mode.
    /// Used by e.g. WarehouseTransfersListViewModel.OnView().
    /// </summary>
    public WarehouseTransferEditorViewModel(int transferId, bool isReadOnly)
        : this(
            App.GetService<IWarehouseTransferApiService>(),
            App.GetService<IWarehouseApiService>(),
            App.GetService<IDialogService>(),
            App.GetService<IEventBus>(),
            App.GetService<IToastNotificationService>(),
            transferId,
            isReadOnly)
    {
    }

    // ── Properties ────────────────────────────────────────────────────────────────

    public int? TransferId => _transferId;

    public bool IsReadOnly => _isReadOnly;
    public bool IsEdit => _transferId.HasValue;

    public string Title => _isReadOnly
        ? "عرض نقل مخزون"
        : _transferId.HasValue
            ? "تعديل نقل مخزون"
            : "نقل مخزون جديد";

    public short SourceWarehouseId
    {
        get => _sourceWarehouseId;
        set => SetProperty(ref _sourceWarehouseId, value);
    }

    public short DestinationWarehouseId
    {
        get => _destinationWarehouseId;
        set => SetProperty(ref _destinationWarehouseId, value);
    }

    public DateTime TransferDate
    {
        get => _transferDate;
        set => SetProperty(ref _transferDate, value);
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

    /// <summary>True when there is at least one line item.</summary>
    public bool HasLines => _lines.Count > 0;

    public ObservableCollection<WarehouseDto> SourceWarehouses
    {
        get => _sourceWarehouses;
        set => SetProperty(ref _sourceWarehouses, value);
    }

    public ObservableCollection<WarehouseDto> DestinationWarehouses
    {
        get => _destinationWarehouses;
        set => SetProperty(ref _destinationWarehouses, value);
    }

    public ObservableCollection<WarehouseTransferLineItem> Lines
    {
        get => _lines;
        set
        {
            if (SetProperty(ref _lines, value))
                OnPropertyChanged(nameof(HasLines));
        }
    }

    // ── Commands ──────────────────────────────────────────────────────────────────

    public ICommand SaveCommand { get; }
    public ICommand PostCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand AddLineCommand { get; }
    public ICommand RemoveLineCommand { get; }

    // ── Initialization ────────────────────────────────────────────────────────────

    private async Task LoadAsync()
    {
        await ExecuteAsync(async () =>
        {
            await LoadWarehousesAsync();
            if (_transferId.HasValue)
            {
                await LoadTransferAsync();
            }
        });
    }

    private async Task LoadWarehousesAsync()
    {
        ErrorMessage = null;
        var result = await _warehouseService.GetAllAsync();
        if (result.IsSuccess && result.Value != null)
        {
            var warehouses = result.Value;
            SourceWarehouses = new ObservableCollection<WarehouseDto>(warehouses);
            DestinationWarehouses = new ObservableCollection<WarehouseDto>(warehouses);
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل المستودعات",
                "WarehouseTransferEditorViewModel.LoadWarehousesAsync");
        }
    }

    private async Task LoadTransferAsync()
    {
        if (!_transferId.HasValue) return;

        ErrorMessage = null;
        var result = await _transferService.GetByIdAsync(_transferId.Value);
        if (result.IsSuccess && result.Value != null)
        {
            var transfer = result.Value;
            SourceWarehouseId = transfer.FromWarehouseId;
            DestinationWarehouseId = transfer.ToWarehouseId;
            TransferDate = transfer.TransferDate;
            Notes = transfer.Notes;
            Status = transfer.Status;

            Lines.Clear();
            foreach (var line in transfer.Lines)
            {
                Lines.Add(new WarehouseTransferLineItem
                {
                    ProductId = line.ProductId,
                    ProductName = line.ProductName ?? string.Empty,
                    BatchId = line.BatchId,
                    BatchNo = line.BatchNo,
                    Quantity = line.Quantity,
                    UnitCost = line.UnitCost
                });
            }
            OnPropertyChanged(nameof(HasLines));
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل بيانات التحويل",
                "WarehouseTransferEditorViewModel.LoadTransferAsync");
        }
    }

    // ── Line Management ───────────────────────────────────────────────────────────

    private async Task AddLineAsync()
    {
        var vm = new ProductSelectionViewModel(0); // No stock display needed for transfer
        vm.OnProductSelected += product =>
        {
            var existing = Lines.FirstOrDefault(l => l.ProductId == product.Id);
            if (existing != null)
            {
                existing.Quantity += 1;
            }
            else
            {
                Lines.Add(new WarehouseTransferLineItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    ProductUnitId = 0, // 0 = service auto
                    ProductUnitName = "حبة",
                    BatchId = 0,
                    Quantity = 1,
                    UnitCost = 0m
                });
            }
            OnPropertyChanged(nameof(HasLines));
            vm.CloseDialog();
        };

        _dialogService.ShowDialog(vm);
    }

    private void RemoveLine(WarehouseTransferLineItem? line)
    {
        if (line != null)
        {
            Lines.Remove(line);
            OnPropertyChanged(nameof(HasLines));
        }
    }

    // ── Save ──────────────────────────────────────────────────────────────────────

    private async Task SaveAsync()
    {
        if (!await ValidateAsync()) return;

        await ExecuteAsync(async () =>
        {
            ErrorMessage = null;

            var request = new CreateWarehouseTransferRequest(
                0,
                SourceWarehouseId,
                DestinationWarehouseId,
                TransferDate,
                Notes,
                Lines.Select(l => new CreateWarehouseTransferLineRequest(
                    l.ProductId,
                    l.ProductUnitId,
                    l.Quantity,
                    l.UnitCost,
                    null)).ToList());

            Result<WarehouseTransferDto> result;
            if (_transferId.HasValue)
            {
                result = await _transferService.UpdateAsync(_transferId.Value, request);
            }
            else
            {
                result = await _transferService.CreateAsync(request);
            }

            if (result.IsSuccess)
            {
                _transferId = result.Value!.Id;
                Status = result.Value.Status;
                _eventBus.Publish(new WarehouseTransferChangedMessage(_transferId.Value));
                OnPropertyChanged(nameof(Title));
                _toast.ShowSuccess("تم حفظ المسودة بنجاح");
                RequestClose();
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "حدث خطأ أثناء الحفظ",
                    "WarehouseTransferEditorViewModel.SaveAsync");
                await _dialogService.ShowErrorAsync("خطأ في الحفظ", ErrorMessage);
            }
        });
    }

    // ── Post ──────────────────────────────────────────────────────────────────────

    private async Task PostAsync()
    {
        if (!await ValidateAsync()) return;

        await ExecuteAsync(async () =>
        {
            ErrorMessage = null;

            // Save first if draft (no ID yet)
            if (!_transferId.HasValue)
            {
                var createRequest = new CreateWarehouseTransferRequest(
                    0,
                    SourceWarehouseId,
                    DestinationWarehouseId,
                    TransferDate,
                    Notes,
                    Lines.Select(l => new CreateWarehouseTransferLineRequest(
                        l.ProductId,
                        l.ProductUnitId,
                        l.Quantity,
                        l.UnitCost,
                        null)).ToList());

                var createResult = await _transferService.CreateAsync(createRequest);
                if (!createResult.IsSuccess)
                {
                    ErrorMessage = HandleFailure(createResult.Error ?? "حدث خطأ أثناء الحفظ",
                        "WarehouseTransferEditorViewModel.PostAsync");
                    await _dialogService.ShowErrorAsync("خطأ في الترحيل", ErrorMessage);
                    return;
                }
                _transferId = createResult.Value!.Id;
            }

            var postResult = await _transferService.PostAsync(_transferId.Value);
            if (postResult.IsSuccess)
            {
                _eventBus.Publish(new WarehouseTransferChangedMessage(_transferId.Value));
                _toast.ShowSuccess("تم الترحيل بنجاح");
                RequestClose();
            }
            else
            {
                ErrorMessage = HandleFailure(postResult.Error ?? "حدث خطأ أثناء الترحيل",
                    "WarehouseTransferEditorViewModel.PostAsync");
                await _dialogService.ShowErrorAsync("خطأ في الترحيل", ErrorMessage);
            }
        });
    }

    // ── Validation ────────────────────────────────────────────────────────────────

    private async Task<bool> ValidateAsync()
    {
        ClearAllErrors();

        if (SourceWarehouseId <= 0)
            AddError(nameof(SourceWarehouseId), "يجب اختيار المستودع المصدر");

        if (DestinationWarehouseId <= 0)
            AddError(nameof(DestinationWarehouseId), "يجب اختيار المستودع الهدف");

        if (SourceWarehouseId > 0 && DestinationWarehouseId > 0 && SourceWarehouseId == DestinationWarehouseId)
            AddError(nameof(DestinationWarehouseId), "لا يمكن التحويل إلى نفس المستودع");

        if (Lines.Count == 0)
            AddError(nameof(Lines), "يجب إضافة صنف واحد على الأقل");

        var invalidProductLines = Lines.Where(l => l.ProductId <= 0).ToList();
        if (invalidProductLines.Any())
            AddError(nameof(Lines), $"يجب اختيار منتج لكل صنف (الأصناف غير الصالحة: {invalidProductLines.Count})");

        var invalidQtyLines = Lines.Where(l => l.Quantity <= 0).ToList();
        if (invalidQtyLines.Any())
            AddError(nameof(Lines), $"الكمية يجب أن تكون أكبر من صفر (الأصناف غير الصحيحة: {invalidQtyLines.Count})");

        var negativeCostLines = Lines.Where(l => l.UnitCost < 0).ToList();
        if (negativeCostLines.Any())
            AddError(nameof(Lines), "تكلفة الوحدة لا يمكن أن تكون سالبة");

        return await ValidateAllAsync();
    }

    // ── Cleanup ───────────────────────────────────────────────────────────────────

    public override void Cleanup()
    {
        base.Cleanup();
    }
}

/// <summary>
/// Represents a single line item in a warehouse transfer.
/// Simple ViewModel with computed TotalCost.
/// </summary>
public class WarehouseTransferLineItem : ViewModelBase
{
    private int _productId;
    private string? _productName;
    private int _productUnitId;
    private string? _productUnitName;
    private int _batchId;
    private int? _batchNo;
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

    public int BatchId
    {
        get => _batchId;
        set => SetProperty(ref _batchId, value);
    }

    public int? BatchNo
    {
        get => _batchNo;
        set => SetProperty(ref _batchNo, value);
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
