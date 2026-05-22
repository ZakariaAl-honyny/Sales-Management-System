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

        SaveCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(SaveOperationAsync, ex => ShowSaveError(ex))));
        CancelCommand = new RelayCommand(Cancel);
    }

    public WarehouseEditorViewModel(WarehouseDto warehouse)
        : this(App.GetService<IWarehouseApiService>(), App.GetService<IEventBus>(), App.GetService<IDialogService>())
    {
        _warehouseId = warehouse.Id;
        _name = warehouse.Name;
        _location = warehouse.Location ?? string.Empty;
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
        set => SetProperty(ref _name, value);
    }

    public string Location
    {
        get => _location;
        set => SetProperty(ref _location, value);
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

    private bool _hasNameError;
    public bool HasNameError
    {
        get => _hasNameError;
        set
        {
            if (SetProperty(ref _hasNameError, value))
                OnPropertyChanged(nameof(NameError));
        }
    }

    public string? NameError => HasNameError ? "الاسم مطلوب" : null;
    #endregion

    #region Commands
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    #endregion

    #region Methods
    private bool Validate()
    {
        HasNameError = string.IsNullOrWhiteSpace(Name);
        return !HasNameError;
    }

    private void ShowSaveError(Exception ex)
    {
        ErrorMessage = HandleException(ex, "WarehouseEditorViewModel.SaveAsync", "[WarehouseEditorViewModel.SaveAsync] Failed to save warehouse.");
        _ = _dialogService.ShowErrorAsync("خطأ", ErrorMessage!);
    }

    private async Task SaveOperationAsync()
    {
        if (!Validate())
        {
            var errors = new List<string>();
            if (HasNameError) errors.Add("• " + NameError);

            await _dialogService.ShowValidationErrorsAsync("بيانات غير مكتملة", errors);
            RequestFocusFirstInvalidField();
            return;
        }

        ErrorMessage = null;

        Result<WarehouseDto> result;

        if (IsEditMode)
        {
            var updateRequest = new UpdateWarehouseRequest(
                Name,
                string.IsNullOrWhiteSpace(Location) ? null : Location,
                IsDefault,
                IsActive);

            result = await _warehouseService.UpdateAsync(_warehouseId, updateRequest);
        }
        else
        {
            var createRequest = new CreateWarehouseRequest(
                Name,
                string.IsNullOrWhiteSpace(Location) ? null : Location,
                IsDefault);

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
            await _dialogService.ShowErrorAsync("خطأ", ErrorMessage!);
        }
    }

    private void Cancel()
    {
        RequestClose();
    }
    #endregion
}
