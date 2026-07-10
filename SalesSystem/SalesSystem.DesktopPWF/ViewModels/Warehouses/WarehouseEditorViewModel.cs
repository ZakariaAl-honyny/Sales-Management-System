using System.Collections.Generic;
using SalesSystem.DesktopPWF.Messaging.Messages;
using System.Windows.Input;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Services.App.Toast;

namespace SalesSystem.DesktopPWF.ViewModels;

/// <summary>
/// ViewModel for Warehouse Editor Dialog
/// </summary>
public class WarehouseEditorViewModel : ViewModelBase
{
    private readonly IWarehouseApiService _warehouseService;
    private readonly IEventBus _eventBus;
    private readonly IDialogService _dialogService;
    private readonly IToastNotificationService _toastService;

    private int _warehouseId;
    private string _name = string.Empty;
    private string? _phone;
    private string? _address;
    private string? _notes;
    private bool _isActive = true;
    private bool _isEditMode;
    private string? _errorMessage;


    public WarehouseEditorViewModel()
        : this(App.GetService<IWarehouseApiService>(), App.GetService<IEventBus>(), App.GetService<IDialogService>(), App.GetService<IToastNotificationService>())
    {
    }

    public WarehouseEditorViewModel(IWarehouseApiService warehouseService, IEventBus eventBus, IDialogService dialogService, IToastNotificationService? toastService = null)
    {
        _warehouseService = warehouseService;
        _eventBus = eventBus;
        _dialogService = dialogService;
        _toastService = toastService ?? App.GetService<IToastNotificationService>();
        SetDialogService(dialogService);

        SaveCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(SaveOperationAsync, ex => ShowSaveError(ex))));
        CancelCommand = new RelayCommand(Cancel);
    }

    public WarehouseEditorViewModel(WarehouseDto warehouse)
        : this(App.GetService<IWarehouseApiService>(), App.GetService<IEventBus>(), App.GetService<IDialogService>(), App.GetService<IToastNotificationService>())
    {
        _warehouseId = warehouse.Id;
        _name = warehouse.Name;
        _phone = warehouse.Phone;
        _address = warehouse.Address;
        _notes = warehouse.Notes;
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

        if (!string.IsNullOrEmpty(Phone) && Phone.Length > 20)
            AddError(nameof(Phone), "رقم الهاتف لا يتجاوز 20 حرفاً");

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
                Phone: string.IsNullOrWhiteSpace(Phone) ? null : Phone,
                Address: string.IsNullOrWhiteSpace(Address) ? null : Address,
                Notes: string.IsNullOrWhiteSpace(Notes) ? null : Notes,
                IsActive: IsActive);

            result = await _warehouseService.UpdateAsync(_warehouseId, updateRequest);
        }
        else
        {
            var createRequest = new CreateWarehouseRequest(
                Name,
                Phone: string.IsNullOrWhiteSpace(Phone) ? null : Phone,
                Address: string.IsNullOrWhiteSpace(Address) ? null : Address,
                Notes: string.IsNullOrWhiteSpace(Notes) ? null : Notes);

            result = await _warehouseService.CreateAsync(createRequest);
        }

        if (result.IsSuccess && result.Value != null)
        {
            _eventBus.Publish(new WarehouseChangedMessage(result.Value.Id));

            _toastService.ShowSuccess(IsEditMode ? "تم تحديث المستودع بنجاح" : "تم إضافة المستودع بنجاح");

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
