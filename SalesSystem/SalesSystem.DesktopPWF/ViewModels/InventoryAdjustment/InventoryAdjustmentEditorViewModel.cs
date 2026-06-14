using SalesSystem.DesktopPWF.Messaging.Messages;
using SalesSystem.DesktopPWF.Services.App.Toast;
using System.Collections.ObjectModel;
using System.Windows.Input;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;

namespace SalesSystem.DesktopPWF.ViewModels.InventoryAdjustment;

/// <summary>
/// ViewModel for Inventory Adjustment Editor (Create/Edit)
/// </summary>
public class InventoryAdjustmentEditorViewModel : ViewModelBase
{
    private readonly IInventoryAdjustmentApiService _adjustmentService;
    private readonly IEventBus _eventBus;
    private readonly IDialogService _dialogService;
    private readonly IToastNotificationService _toastService;

    private int? _adjustmentId;
    private int _adjustmentNo;
    private int _warehouseId;
    private string? _warehouseName;
    private byte _adjustmentType = 1; // Addition
    private DateTime _adjustmentDate = DateTime.Today;
    private int _accountId;
    private string? _accountName;
    private byte _status = 1;
    private bool _isEditMode;
    private string? _errorMessage;

    private ObservableCollection<InventoryAdjustmentLineDto> _lines = new();
    private InventoryAdjustmentLineDto? _selectedLine;

    public InventoryAdjustmentEditorViewModel()
        : this(App.GetService<IInventoryAdjustmentApiService>(), App.GetService<IEventBus>(), App.GetService<IDialogService>(), App.GetService<IToastNotificationService>())
    {
    }

    public InventoryAdjustmentEditorViewModel(
        IInventoryAdjustmentApiService adjustmentService,
        IEventBus eventBus,
        IDialogService dialogService,
        IToastNotificationService toastService)
    {
        _adjustmentService = adjustmentService;
        _eventBus = eventBus;
        _dialogService = dialogService;
        _toastService = toastService;
        SetDialogService(dialogService);

        SaveCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(SaveOperationAsync, "جاري حفظ التسوية...")));
        CancelDialogCommand = new RelayCommand(Cancel);
    }

    /// <summary>
    /// Constructor for editing an existing adjustment
    /// </summary>
    public InventoryAdjustmentEditorViewModel(InventoryAdjustmentDto adjustment)
        : this(App.GetService<IInventoryAdjustmentApiService>(), App.GetService<IEventBus>(), App.GetService<IDialogService>(), App.GetService<IToastNotificationService>())
    {
        _adjustmentId = adjustment.Id;
        _adjustmentNo = adjustment.AdjustmentNo;
        _warehouseId = adjustment.WarehouseId;
        _warehouseName = adjustment.WarehouseName;
        _adjustmentType = adjustment.AdjustmentType;
        _adjustmentDate = adjustment.AdjustmentDate;
        _accountId = adjustment.AccountId;
        _accountName = adjustment.AccountName;
        _status = adjustment.Status;
        _isEditMode = true;

        if (adjustment.Lines != null)
        {
            foreach (var line in adjustment.Lines)
            {
                _lines.Add(line);
            }
        }
    }

    #region Properties
    public string Title => IsEditMode ? "تعديل تسوية مخزون" : "تسوية مخزون جديدة";

    public bool IsEditMode
    {
        get => _isEditMode;
        set => SetProperty(ref _isEditMode, value);
    }

    public int AdjustmentNo
    {
        get => _adjustmentNo;
        set => SetProperty(ref _adjustmentNo, value);
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

    public byte Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public ObservableCollection<InventoryAdjustmentLineDto> Lines
    {
        get => _lines;
        set => SetProperty(ref _lines, value);
    }

    public InventoryAdjustmentLineDto? SelectedLine
    {
        get => _selectedLine;
        set => SetProperty(ref _selectedLine, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }
    #endregion

    #region Commands
    public ICommand SaveCommand { get; }
    public ICommand CancelDialogCommand { get; }
    #endregion

    #region Methods
    private async Task<bool> ValidateAsync()
    {
        ClearAllErrors();

        if (WarehouseId <= 0)
            AddError(nameof(WarehouseId), "المستودع مطلوب");
        if (AccountId <= 0)
            AddError(nameof(AccountId), "الحساب المحاسبي مطلوب");
        if (AdjustmentType < 1 || AdjustmentType > 3)
            AddError(nameof(AdjustmentType), "نوع التسوية غير صالح (1=إضافة, 2=خصم, 3=تصحيح)");
        if (Lines.Count == 0)
            AddError(nameof(Lines), "يجب إضافة بند واحد على الأقل");

        return await ValidateAllAsync();
    }

    private async Task SaveOperationAsync()
    {
        if (!await ValidateAsync()) return;

        ErrorMessage = null;

        var request = new CreateInventoryAdjustmentRequest(
            WarehouseId,
            AdjustmentDate,
            AdjustmentType,
            AccountId);

        var result = await _adjustmentService.CreateAsync(request);

        if (result.IsSuccess && result.Value != null)
        {
            _eventBus.Publish(new InventoryAdjustmentChangedMessage(result.Value.Id));
            _toastService.ShowSuccess("تم إنشاء التسوية بنجاح");
            RequestClose();
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في حفظ التسوية", "InventoryAdjustmentEditorViewModel.SaveAsync", "[InventoryAdjustmentEditorViewModel.SaveAsync] Failed to save adjustment.");
            await _dialogService.ShowErrorAsync("خطأ في حفظ التسوية", ErrorMessage!);
        }
    }

    private void Cancel()
    {
        RequestClose();
    }
    #endregion
}
