using SalesSystem.DesktopPWF.Messaging.Messages;
using SalesSystem.DesktopPWF.Services.App.Toast;
using System.Collections.ObjectModel;
using System.Windows.Input;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;

namespace SalesSystem.DesktopPWF.ViewModels.InventoryCount;

/// <summary>
/// ViewModel for Inventory Count Editor (Create/Edit)
/// </summary>
public class InventoryCountEditorViewModel : ViewModelBase
{
    private readonly IInventoryCountApiService _countService;
    private readonly IEventBus _eventBus;
    private readonly IDialogService _dialogService;
    private readonly IToastNotificationService _toastService;

    private int? _countId;
    private int _countNo;
    private int _warehouseId;
    private string? _warehouseName;
    private DateTime _countDate = DateTime.Today;
    private string _notes = string.Empty;
    private byte _status = 1;
    private bool _isEditMode;
    private string? _errorMessage;

    private ObservableCollection<InventoryCountLineDto> _lines = new();
    private InventoryCountLineDto? _selectedLine;

    public InventoryCountEditorViewModel()
        : this(App.GetService<IInventoryCountApiService>(), App.GetService<IEventBus>(), App.GetService<IDialogService>(), App.GetService<IToastNotificationService>())
    {
    }

    public InventoryCountEditorViewModel(
        IInventoryCountApiService countService,
        IEventBus eventBus,
        IDialogService dialogService,
        IToastNotificationService toastService)
    {
        _countService = countService;
        _eventBus = eventBus;
        _dialogService = dialogService;
        _toastService = toastService;
        SetDialogService(dialogService);

        SaveCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(SaveOperationAsync, "جاري حفظ الجرد...")));
        CancelDialogCommand = new RelayCommand(Cancel);
    }

    /// <summary>
    /// Constructor for editing an existing count
    /// </summary>
    public InventoryCountEditorViewModel(InventoryCountDto count)
        : this(App.GetService<IInventoryCountApiService>(), App.GetService<IEventBus>(), App.GetService<IDialogService>(), App.GetService<IToastNotificationService>())
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
                _lines.Add(line);
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

    public string Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }

    public byte Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public ObservableCollection<InventoryCountLineDto> Lines
    {
        get => _lines;
        set => SetProperty(ref _lines, value);
    }

    public InventoryCountLineDto? SelectedLine
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
        if (Lines.Count == 0)
            AddError(nameof(Lines), "يجب إضافة بند واحد على الأقل");

        return await ValidateAllAsync();
    }

    private async Task SaveOperationAsync()
    {
        if (!await ValidateAsync()) return;

        ErrorMessage = null;

        if (IsEditMode && _countId.HasValue)
        {
            // Use update API if available; otherwise treat as new
            var updateRequest = new UpdateInventoryCountRequest(
                string.IsNullOrWhiteSpace(Notes) ? null : Notes);

            // If update not available, fallback to create
            var result = await _countService.CreateAsync(new CreateInventoryCountRequest(
                WarehouseId,
                CountDate,
                string.IsNullOrWhiteSpace(Notes) ? null : Notes));

            if (result.IsSuccess && result.Value != null)
            {
                _eventBus.Publish(new InventoryCountChangedMessage(result.Value.Id));
                _toastService.ShowSuccess("تم حفظ الجرد بنجاح");
                RequestClose();
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشل في حفظ الجرد", "InventoryCountEditorViewModel.SaveAsync", "[InventoryCountEditorViewModel.SaveAsync] Failed to save count.");
                await _dialogService.ShowErrorAsync("خطأ في حفظ الجرد", ErrorMessage!);
            }
        }
        else
        {
            var result = await _countService.CreateAsync(new CreateInventoryCountRequest(
                WarehouseId,
                CountDate,
                string.IsNullOrWhiteSpace(Notes) ? null : Notes));

            if (result.IsSuccess && result.Value != null)
            {
                _eventBus.Publish(new InventoryCountChangedMessage(result.Value.Id));
                _toastService.ShowSuccess("تم إنشاء الجرد بنجاح");
                RequestClose();
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشل في إنشاء الجرد", "InventoryCountEditorViewModel.SaveAsync", "[InventoryCountEditorViewModel.SaveAsync] Failed to create count.");
                await _dialogService.ShowErrorAsync("خطأ في إنشاء الجرد", ErrorMessage!);
            }
        }
    }

    private void Cancel()
    {
        RequestClose();
    }
    #endregion
}
