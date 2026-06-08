using System.Collections.Generic;
using SalesSystem.DesktopPWF.Messaging.Messages;
using System.Windows.Input;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;

namespace SalesSystem.DesktopPWF.ViewModels;

/// <summary>
/// ViewModel for Warehouse Editor Dialog
/// </summary>
public class WarehouseEditorViewModel : ViewModelBase
{
    private readonly IWarehouseApiService _warehouseService;
    private readonly IEventBus _eventBus;
    private readonly IDialogService _dialogService;

    private int _warehouseId;
    private string _name = string.Empty;
    private string _location = string.Empty;
    private byte _type = 1;
    private string? _phone;
    private string? _address;
    private string? _managerName;
    private string? _notes;
    private bool _isDefault;
    private bool _isActive = true;
    private bool _isEditMode;
    private string? _errorMessage;


    public WarehouseEditorViewModel()
        : this(App.GetService<IWarehouseApiService>(), App.GetService<IEventBus>(), App.GetService<IDialogService>())
    {
    }

    public WarehouseEditorViewModel(IWarehouseApiService warehouseService, IEventBus eventBus, IDialogService dialogService)
    {
        _warehouseService = warehouseService;
        _eventBus = eventBus;
        _dialogService = dialogService;
        SetDialogService(dialogService);

        SaveCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(SaveOperationAsync, ex => ShowSaveError(ex))));
        CancelCommand = new RelayCommand(Cancel);
    }

    public WarehouseEditorViewModel(WarehouseDto warehouse)
        : this(App.GetService<IWarehouseApiService>(), App.GetService<IEventBus>(), App.GetService<IDialogService>())
    {
        _warehouseId = warehouse.Id;
        _name = warehouse.Name;
        _location = warehouse.Location ?? string.Empty;
        _type = warehouse.Type;
        _phone = warehouse.Phone;
        _address = warehouse.Address;
        _managerName = warehouse.ManagerName;
        _notes = warehouse.Notes;
        _isDefault = warehouse.IsDefault;
        _isActive = warehouse.IsActive;
        _isEditMode = true;
    }

    #region Properties
    public string Title => IsEditMode ? "تعديل مستودع" : "إضافة مستودع جديد";

    public bool IsEditMode
    {
        get => _isEditMode;
        set => SetProperty(ref _isEditMode, value);
    }

    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value))
            {
                if (string.IsNullOrWhiteSpace(value))
                    AddError(nameof(Name), "اسم المستودع مطلوب");
                else
                    ClearErrors(nameof(Name));
            }
        }
    }

    public string Location
    {
        get => _location;
        set => SetProperty(ref _location, value);
    }

    /// <summary>
    /// Warehouse type list for the ComboBox.
    /// Key = byte (1-4), Value = Arabic display name.
    /// </summary>
    public List<KeyValuePair<byte, string>> TypeList { get; } = new()
    {
        new KeyValuePair<byte, string>(1, "رئيسي"),
        new KeyValuePair<byte, string>(2, "فرعي"),
        new KeyValuePair<byte, string>(3, "صالة عرض"),
        new KeyValuePair<byte, string>(4, "تالف"),
    };

    public byte SelectedType
    {
        get => _type;
        set
        {
            if (SetProperty(ref _type, value))
            {
                if (value < 1 || value > 4)
                    AddError(nameof(SelectedType), "نوع المستودع يجب أن يكون بين 1 و 4");
                else
                    ClearErrors(nameof(SelectedType));
            }
        }
    }

    public string? Phone
    {
        get => _phone;
        set
        {
            if (SetProperty(ref _phone, value))
            {
                if (!string.IsNullOrEmpty(value) && value.Length > 20)
                    AddError(nameof(Phone), "رقم الهاتف لا يتجاوز 20 حرفاً");
                else
                    ClearErrors(nameof(Phone));
            }
        }
    }

    public string? Address
    {
        get => _address;
        set => SetProperty(ref _address, value);
    }

    public string? ManagerName
    {
        get => _managerName;
        set
        {
            if (SetProperty(ref _managerName, value))
            {
                if (!string.IsNullOrEmpty(value) && value.Length > 100)
                    AddError(nameof(ManagerName), "اسم المدير لا يتجاوز 100 حرف");
                else
                    ClearErrors(nameof(ManagerName));
            }
        }
    }

    public string? Notes
    {
        get => _notes;
        set
        {
            if (SetProperty(ref _notes, value))
            {
                if (!string.IsNullOrEmpty(value) && value.Length > 500)
                    AddError(nameof(Notes), "الملاحظات لا تتجاوز 500 حرف");
                else
                    ClearErrors(nameof(Notes));
            }
        }
    }

    public bool IsDefault
    {
        get => _isDefault;
        set => SetProperty(ref _isDefault, value);
    }

    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    #endregion

    #region Commands
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    #endregion

    #region Methods
    private async Task<bool> ValidateAsync()
    {
        ClearAllErrors();

        if (string.IsNullOrWhiteSpace(Name))
            AddError(nameof(Name), "اسم المستودع مطلوب");

        if (SelectedType < 1 || SelectedType > 4)
            AddError(nameof(SelectedType), "نوع المستودع مطلوب ويجب أن يكون بين 1 و 4");

        if (!string.IsNullOrEmpty(Phone) && Phone.Length > 20)
            AddError(nameof(Phone), "رقم الهاتف لا يتجاوز 20 حرفاً");

        if (!string.IsNullOrEmpty(ManagerName) && ManagerName.Length > 100)
            AddError(nameof(ManagerName), "اسم المدير لا يتجاوز 100 حرف");

        if (!string.IsNullOrEmpty(Notes) && Notes.Length > 500)
            AddError(nameof(Notes), "الملاحظات لا تتجاوز 500 حرف");

        return await ValidateAllAsync();
    }

    private void ShowSaveError(Exception ex)
    {
        ErrorMessage = HandleException(ex, "WarehouseEditorViewModel.SaveAsync", "[WarehouseEditorViewModel.SaveAsync] Failed to save warehouse.");
        _ = _dialogService.ShowErrorAsync("خطأ في حفظ المستودع", ErrorMessage!);
    }

    private async Task SaveOperationAsync()
    {
        if (!await ValidateAsync())
        {
            return;
        }

        ErrorMessage = null;

        Result<WarehouseDto> result;

        if (IsEditMode)
        {
            var updateRequest = new UpdateWarehouseRequest(
                Name,
                Type: SelectedType,
                Location: string.IsNullOrWhiteSpace(Location) ? null : Location,
                Phone: string.IsNullOrWhiteSpace(Phone) ? null : Phone,
                Address: string.IsNullOrWhiteSpace(Address) ? null : Address,
                ManagerName: string.IsNullOrWhiteSpace(ManagerName) ? null : ManagerName,
                IsDefault: IsDefault,
                IsActive: IsActive,
                Notes: string.IsNullOrWhiteSpace(Notes) ? null : Notes);

            result = await _warehouseService.UpdateAsync(_warehouseId, updateRequest);
        }
        else
        {
            var createRequest = new CreateWarehouseRequest(
                Name,
                Type: SelectedType,
                Location: string.IsNullOrWhiteSpace(Location) ? null : Location,
                Phone: string.IsNullOrWhiteSpace(Phone) ? null : Phone,
                Address: string.IsNullOrWhiteSpace(Address) ? null : Address,
                ManagerName: string.IsNullOrWhiteSpace(ManagerName) ? null : ManagerName,
                IsDefault: IsDefault,
                Notes: string.IsNullOrWhiteSpace(Notes) ? null : Notes);

            result = await _warehouseService.CreateAsync(createRequest);
        }

        if (result.IsSuccess && result.Value != null)
        {
            _eventBus.Publish(new WarehouseChangedMessage(result.Value.Id));

            await _dialogService.ShowSuccessAsync("نجاح", IsEditMode ? "تم تحديث المستودع بنجاح" : "تم إضافة المستودع بنجاح");

            RequestClose();
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في حفظ المستودع", "WarehouseEditorViewModel.SaveAsync", "[WarehouseEditorViewModel.SaveAsync] Failed to save warehouse.");
            await _dialogService.ShowErrorAsync("خطأ في حفظ المستودع", ErrorMessage!);
        }
    }

    private void Cancel()
    {
        RequestClose();
    }
    #endregion
}
