using System.Collections.ObjectModel;
using System.Windows.Input;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;
using SalesSystem.DesktopPWF.Messaging.Messages;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Services.App.Toast;
using SalesSystem.DesktopPWF.ViewModels.Products;

namespace SalesSystem.DesktopPWF.ViewModels.InventoryCount;

/// <summary>
/// ViewModel for Inventory Count Editor (Create/Edit)
/// Follows the same pattern as InventoryTransactionEditorViewModel.
/// Supports new count creation and editing of existing draft counts.
/// </summary>
public class InventoryCountEditorViewModel : ViewModelBase
{
    private readonly IInventoryCountApiService _countApiService;
    private readonly IWarehouseApiService _warehouseService;
    private readonly IInventoryApiService _inventoryService;
    private readonly IDialogService _dialogService;
    private readonly IEventBus _eventBus;
    private readonly IToastNotificationService _toast;

    private int? _countId;
    private int _countNo;
    private int _warehouseId;
    private string? _warehouseName;
    private DateTime _countDate = DateTime.Today;
    private string? _notes;
    private string? _errorMessage;
    private byte _status = 1; // Draft
    private bool _isEditMode;

    private ObservableCollection<InventoryCountLineItem> _lines = new();
    private InventoryCountLineItem? _selectedLine;
    private ObservableCollection<WarehouseDto> _warehouses = new();

    /// <summary>
    /// Parameterized constructor for DI (new count creation).
    /// </summary>
    public InventoryCountEditorViewModel(
        IInventoryCountApiService countApiService,
        IWarehouseApiService warehouseService,
        IInventoryApiService inventoryService,
        IDialogService dialogService,
        IEventBus eventBus,
        IToastNotificationService toast)
    {
        _countApiService = countApiService ?? throw new ArgumentNullException(nameof(countApiService));
        _warehouseService = warehouseService ?? throw new ArgumentNullException(nameof(warehouseService));
        _inventoryService = inventoryService ?? throw new ArgumentNullException(nameof(inventoryService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _toast = toast ?? throw new ArgumentNullException(nameof(toast));

        SetDialogService(dialogService);

        SaveCommand = new AsyncRelayCommand(SaveAsync);
        PostCommand = new AsyncRelayCommand(PostAsync);
        CancelCommand = new RelayCommand(_ => RequestClose());
        AddLineCommand = new AsyncRelayCommand(AddLineAsync);
        RemoveLineCommand = new RelayCommand(p => RemoveLine(p as InventoryCountLineItem));

        _ = LoadAsync();
    }

    /// <summary>
    /// Parameterless constructor for XAML designer / service locator.
    /// </summary>
    public InventoryCountEditorViewModel()
        : this(
            App.GetService<IInventoryCountApiService>(),
            App.GetService<IWarehouseApiService>(),
            App.GetService<IInventoryApiService>(),
            App.GetService<IDialogService>(),
            App.GetService<IEventBus>(),
            App.GetService<IToastNotificationService>())
    {
    }

    /// <summary>
    /// Constructor for editing an existing count.
    /// Loads the count data including lines from the DTO.
    /// </summary>
    public InventoryCountEditorViewModel(InventoryCountDto count)
        : this(
            App.GetService<IInventoryCountApiService>(),
            App.GetService<IWarehouseApiService>(),
            App.GetService<IInventoryApiService>(),
            App.GetService<IDialogService>(),
            App.GetService<IEventBus>(),
            App.GetService<IToastNotificationService>())
    {
        _countId = count.Id;
        _countNo = count.CountNo;
        _warehouseId = count.WarehouseId;
        _warehouseName = count.WarehouseName;
        _countDate = count.CountDate;
        _notes = count.Notes ?? string.Empty;
        _status = count.Status;
        _isEditMode = true;

        if (count.Lines != null)
        {
            foreach (var line in count.Lines)
            {
                var lineItem = new InventoryCountLineItem
                {
                    Id = line.Id,
                    ProductId = line.ProductId,
                    ProductName = line.ProductName,
                    ProductUnitId = line.ProductUnitId,
                    ProductUnitName = line.ProductUnitName,
                    SystemQuantity = line.SystemQuantity,
                    IsCounted = line.ActualQuantity > 0
                };
                lineItem.SetInitialActualQuantity(line.ActualQuantity);
                _lines.Add(lineItem);
            }
        }
    }

    #region Properties

    public string Title => IsEditMode ? "تعديل الجرد" : "جرد جديد";

    public bool IsEditMode
    {
        get => _isEditMode;
        set => SetProperty(ref _isEditMode, value);
    }

    public int? CountId => _countId;

    public int CountNo
    {
        get => _countNo;
        set => SetProperty(ref _countNo, value);
    }

    public int WarehouseId
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

    public DateTime CountDate
    {
        get => _countDate;
        set => SetProperty(ref _countDate, value);
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

    public ObservableCollection<InventoryCountLineItem> Lines
    {
        get => _lines;
        set => SetProperty(ref _lines, value);
    }

    public InventoryCountLineItem? SelectedLine
    {
        get => _selectedLine;
        set => SetProperty(ref _selectedLine, value);
    }

    public bool HasLines => Lines.Count > 0;

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
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل المستودعات", "InventoryCountEditorViewModel.LoadWarehousesAsync");
        }
    }

    private async Task AddLineAsync()
    {
        var vm = new ProductSelectionViewModel(WarehouseId);
        vm.OnProductSelected += product =>
        {
            var existing = Lines.FirstOrDefault(l => l.ProductId == product.Id);
            if (existing != null)
            {
                _toast.ShowInfo("المنتج موجود مسبقاً في قائمة الجرد");
            }
            else
            {
                Lines.Add(new InventoryCountLineItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    ProductUnitId = product.DefaultPurchaseUnitId ?? 0, // 0 = service layer auto-determines
                    ProductUnitName = "حبة",
                    SystemQuantity = 0,
                    ActualQuantity = 0,
                    IsCounted = false
                });
                OnPropertyChanged(nameof(HasLines));
            }
            vm.CloseDialog();
        };

        _dialogService.ShowDialog(vm);
    }

    private void RemoveLine(InventoryCountLineItem? line)
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
            AddError(nameof(WarehouseId), "المستودع مطلوب");

        if (Lines.Count == 0)
            AddError(nameof(Lines), "يجب إضافة صنف واحد على الأقل");

        var uncountedLines = Lines.Where(l => !l.IsCounted).ToList();
        if (uncountedLines.Any())
            AddError(nameof(Lines), $"يوجد {uncountedLines.Count} صنف لم يتم إدخال الكمية الفعلية له");

        var negativeLines = Lines.Where(l => l.ActualQuantity < 0).ToList();
        if (negativeLines.Any())
            AddError(nameof(Lines), "الكمية الفعلية لا يمكن أن تكون سالبة");

        return await ValidateAllAsync();
    }

    private async Task SaveAsync()
    {
        if (!await ValidateAsync()) return;

        await ExecuteAsync(async () =>
        {
            ErrorMessage = null;

            if (IsEditMode && _countId.HasValue)
            {
                // In edit mode, add any new lines via API
                var newLines = Lines.Where(l => l.Id == 0).ToList();
                foreach (var line in newLines)
                {
                    var lineRequest = new AddInventoryCountLineRequest(
                        _countId.Value,
                        line.ProductId,
                        line.ProductUnitId,
                        line.SystemQuantity,
                        line.ActualQuantity);

                    var lineResult = await _countApiService.AddLineAsync(_countId.Value, lineRequest);
                    if (!lineResult.IsSuccess)
                    {
                        ErrorMessage = HandleFailure(lineResult.Error ?? "فشل في إضافة البند", "InventoryCountEditorViewModel.SaveAsync");
                        await _dialogService.ShowErrorAsync("خطأ في حفظ البند", ErrorMessage!);
                        return;
                    }
                }

                // Update existing lines — update each line if its ActualQuantity changed
                // (AddLineAsync with same ProductId should overwrite)
                var existingLines = Lines.Where(l => l.Id > 0 && l.IsModified).ToList();
                foreach (var line in existingLines)
                {
                    var lineRequest = new AddInventoryCountLineRequest(
                        _countId.Value,
                        line.ProductId,
                        line.ProductUnitId,
                        line.SystemQuantity,
                        line.ActualQuantity);

                    var lineResult = await _countApiService.AddLineAsync(_countId.Value, lineRequest);
                    if (!lineResult.IsSuccess)
                    {
                        ErrorMessage = HandleFailure(lineResult.Error ?? "فشل في تحديث البند", "InventoryCountEditorViewModel.SaveAsync");
                        await _dialogService.ShowErrorAsync("خطأ في تحديث البند", ErrorMessage!);
                        return;
                    }
                }

                _eventBus.Publish(new InventoryCountChangedMessage(_countId.Value));
                _toast.ShowSuccess("تم حفظ الجرد بنجاح");
                RequestClose();
            }
            else
            {
                // Create the count header first
                var createResult = await _countApiService.CreateAsync(new CreateInventoryCountRequest(
                    WarehouseId,
                    CountDate,
                    string.IsNullOrWhiteSpace(Notes) ? null : Notes));

                if (!createResult.IsSuccess || createResult.Value == null)
                {
                    ErrorMessage = HandleFailure(createResult.Error ?? "فشل في إنشاء الجرد", "InventoryCountEditorViewModel.SaveAsync");
                    await _dialogService.ShowErrorAsync("خطأ في إنشاء الجرد", ErrorMessage!);
                    return;
                }

                var createdCount = createResult.Value;
                _countId = createdCount.Id;
                _countNo = createdCount.CountNo;

                // Add each line via API
                foreach (var line in Lines)
                {
                    var lineRequest = new AddInventoryCountLineRequest(
                        _countId.Value,
                        line.ProductId,
                        line.ProductUnitId,
                        line.SystemQuantity,
                        line.ActualQuantity);

                    var lineResult = await _countApiService.AddLineAsync(_countId.Value, lineRequest);
                    if (!lineResult.IsSuccess)
                    {
                        ErrorMessage = HandleFailure(lineResult.Error ?? "فشل في إضافة البند", "InventoryCountEditorViewModel.SaveAsync");
                        await _dialogService.ShowErrorAsync("خطأ في حفظ البند", ErrorMessage!);
                        return;
                    }
                }

                _eventBus.Publish(new InventoryCountChangedMessage(_countId.Value));
                _toast.ShowSuccess("تم إنشاء الجرد بنجاح");
                RequestClose();
            }
        });
    }

    private async Task PostAsync()
    {
        if (!await ValidateAsync()) return;

        await ExecuteAsync(async () =>
        {
            ErrorMessage = null;

            // Save first if it's a new count (no ID yet)
            if (!_countId.HasValue)
            {
                var createResult = await _countApiService.CreateAsync(new CreateInventoryCountRequest(
                    WarehouseId,
                    CountDate,
                    string.IsNullOrWhiteSpace(Notes) ? null : Notes));

                if (!createResult.IsSuccess || createResult.Value == null)
                {
                    ErrorMessage = HandleFailure(createResult.Error ?? "فشل في إنشاء الجرد", "InventoryCountEditorViewModel.PostAsync");
                    await _dialogService.ShowErrorAsync("خطأ في الترحيل", ErrorMessage!);
                    return;
                }

                var createdCount = createResult.Value;
                _countId = createdCount.Id;
                _countNo = createdCount.CountNo;

                // Add each line
                foreach (var line in Lines)
                {
                    var lineRequest = new AddInventoryCountLineRequest(
                        _countId.Value,
                        line.ProductId,
                        line.ProductUnitId,
                        line.SystemQuantity,
                        line.ActualQuantity);

                    var lineResult = await _countApiService.AddLineAsync(_countId.Value, lineRequest);
                    if (!lineResult.IsSuccess)
                    {
                        ErrorMessage = HandleFailure(lineResult.Error ?? "فشل في إضافة البند", "InventoryCountEditorViewModel.PostAsync");
                        await _dialogService.ShowErrorAsync("خطأ في الترحيل", ErrorMessage!);
                        return;
                    }
                }
            }
            else
            {
                // Existing count — save any pending changes first
                var newLines = Lines.Where(l => l.Id == 0).ToList();
                foreach (var line in newLines)
                {
                    var lineRequest = new AddInventoryCountLineRequest(
                        _countId.Value,
                        line.ProductId,
                        line.ProductUnitId,
                        line.SystemQuantity,
                        line.ActualQuantity);

                    var lineResult = await _countApiService.AddLineAsync(_countId.Value, lineRequest);
                    if (!lineResult.IsSuccess)
                    {
                        ErrorMessage = HandleFailure(lineResult.Error ?? "فشل في إضافة البند", "InventoryCountEditorViewModel.PostAsync");
                        await _dialogService.ShowErrorAsync("خطأ في الترحيل", ErrorMessage!);
                        return;
                    }
                }
            }

            // Now post the count
            var postResult = await _countApiService.PostAsync(_countId.Value);
            if (postResult.IsSuccess)
            {
                _eventBus.Publish(new InventoryCountChangedMessage(_countId.Value));
                _toast.ShowSuccess("تم ترحيل الجرد بنجاح");
                RequestClose();
            }
            else
            {
                ErrorMessage = HandleFailure(postResult.Error ?? "فشل في ترحيل الجرد", "InventoryCountEditorViewModel.PostAsync");
                await _dialogService.ShowErrorAsync("خطأ في الترحيل", ErrorMessage!);
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
/// Represents a single line item in the inventory count.
/// Editable properties with computed Difference and change tracking.
/// </summary>
public class InventoryCountLineItem : ViewModelBase
{
    private int _id;
    private int _productId;
    private string? _productName;
    private int _productUnitId;
    private string? _productUnitName;
    private decimal _systemQuantity;
    private decimal _actualQuantity;
    private bool _isCounted;
    private decimal _savedActualQuantity;
    private bool _isModified;

    public int Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

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

    /// <summary>
    /// System quantity (current stock in the warehouse) — readonly from API.
    /// </summary>
    public decimal SystemQuantity
    {
        get => _systemQuantity;
        set => SetProperty(ref _systemQuantity, value);
    }

    /// <summary>
    /// Actual counted quantity — editable by the user.
    /// Setting this value automatically updates IsCounted, Difference, and IsModified.
    /// </summary>
    public decimal ActualQuantity
    {
        get => _actualQuantity;
        set
        {
            if (SetProperty(ref _actualQuantity, value))
            {
                IsCounted = _isCounted || value > 0; // Once counted, stays counted
                IsModified = Math.Abs(value - _savedActualQuantity) > 0.0001m;
                OnPropertyChanged(nameof(Difference));
                OnPropertyChanged(nameof(DifferenceDisplay));
            }
        }
    }

    /// <summary>
    /// Whether this item has been counted (ActualQuantity entered).
    /// </summary>
    public bool IsCounted
    {
        get => _isCounted;
        set
        {
            if (SetProperty(ref _isCounted, value))
                OnPropertyChanged(nameof(IsCountedDisplay));
        }
    }

    /// <summary>
    /// Computed difference between actual and system quantity.
    /// Positive = surplus, Negative = shortage, Zero = match.
    /// </summary>
    public decimal Difference => ActualQuantity - SystemQuantity;

    /// <summary>
    /// Formatted difference for display with direction indicator.
    /// </summary>
    public string DifferenceDisplay => Difference switch
    {
        > 0 => $"▲ {Difference:N3} (زيادة)",
        < 0 => $"▼ {Math.Abs(Difference):N3} (عجز)",
        _ => "— (مطابق)"
    };

    /// <summary>
    /// Display text for counted status.
    /// </summary>
    public string IsCountedDisplay => IsCounted ? "تم الجرد" : "لم يتم";

    /// <summary>
    /// Whether the ActualQuantity has changed from the originally loaded value.
    /// </summary>
    public bool IsModified
    {
        get => _isModified;
        private set => SetProperty(ref _isModified, value);
    }

    /// <summary>
    /// Call this after loading from DTO to mark the initial ActualQuantity as "saved".
    /// </summary>
    public void MarkAsSaved()
    {
        _savedActualQuantity = _actualQuantity;
        IsModified = false;
    }

    /// <summary>
    /// Sets the initial ActualQuantity and marks as saved.
    /// </summary>
    public void SetInitialActualQuantity(decimal quantity)
    {
        _actualQuantity = quantity;
        _savedActualQuantity = quantity;
        _isCounted = quantity > 0;
        OnPropertyChanged(nameof(ActualQuantity));
        OnPropertyChanged(nameof(Difference));
        OnPropertyChanged(nameof(DifferenceDisplay));
        OnPropertyChanged(nameof(IsCountedDisplay));
    }
}
