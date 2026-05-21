using SalesSystem.DesktopPWF.Messaging.Messages;
using System.Windows;
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

    private int _warehouseId;
    private string _code = string.Empty;
    private string _name = string.Empty;
    private string _location = string.Empty;
    private bool _isDefault;
    private bool _isActive = true;
    private bool _isLoading;
    private bool _isEditMode;
    private string? _errorMessage;


    public WarehouseEditorViewModel()
        : this(App.GetService<IWarehouseApiService>(), App.GetService<IEventBus>())
    {
    }

    public WarehouseEditorViewModel(IWarehouseApiService warehouseService, IEventBus eventBus)
    {
        _warehouseService = warehouseService;
        _eventBus = eventBus;

        SaveCommand = new AsyncRelayCommand(SaveAsync, () => CanSave);
        CancelCommand = new RelayCommand(Cancel);
    }

    public WarehouseEditorViewModel(WarehouseDto warehouse)
        : this(App.GetService<IWarehouseApiService>(), App.GetService<IEventBus>())
    {
        _warehouseId = warehouse.Id;
        _code = warehouse.Code ?? string.Empty;
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

    public string Code
    {
        get => _code;
        set => SetProperty(ref _code, value);
    }

    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value))
            {
                OnPropertyChanged(nameof(CanSave));
                (SaveCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            }
        }
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

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    // Validation
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
    public bool CanSave => !HasErrors && !string.IsNullOrWhiteSpace(Name);
    #endregion

    #region Commands
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    #endregion

    #region Methods
    private bool Validate()
    {
        HasNameError = string.IsNullOrWhiteSpace(Name);
        OnPropertyChanged(nameof(HasErrors));
        OnPropertyChanged(nameof(CanSave));
        (SaveCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        return !HasNameError;
    }

    private async Task SaveAsync()
    {
        if (!Validate())
        {
            var errors = new List<string>();
            if (HasNameError) errors.Add("• " + NameError);

            string errorMsg = "يرجى إكمال البيانات الإلزامية التالية:\n\n" + string.Join("\n", errors);
            System.Windows.MessageBox.Show(errorMsg, "بيانات غير مكتملة", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            Result<WarehouseDto> result;

            if (IsEditMode)
            {
                var updateRequest = new UpdateWarehouseRequest(
                    Name,
                    string.IsNullOrWhiteSpace(Code) ? null : Code,
                    string.IsNullOrWhiteSpace(Location) ? null : Location,
                    IsDefault,
                    IsActive);

                result = await _warehouseService.UpdateAsync(_warehouseId, updateRequest);
            }
            else
            {
                var createRequest = new CreateWarehouseRequest(
                    Name,
                    string.IsNullOrWhiteSpace(Code) ? null : Code,
                    string.IsNullOrWhiteSpace(Location) ? null : Location,
                    IsDefault);

                result = await _warehouseService.CreateAsync(createRequest);
            }

            if (result.IsSuccess && result.Value != null)
            {
                // Publish event to notify other modules
                _eventBus.Publish(new WarehouseChangedMessage(result.Value.Id));

                System.Windows.MessageBox.Show(
                    IsEditMode ? "تم تحديث المستودع بنجاح" : "تم إضافة المستودع بنجاح",
                    "نجاح",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                RequestClose();
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشل في حفظ المستودع", "WarehouseEditorViewModel.SaveAsync", "[WarehouseEditorViewModel.SaveAsync] Failed to save warehouse.");
                System.Windows.MessageBox.Show(ErrorMessage, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = HandleException(ex, "WarehouseEditorViewModel.SaveAsync", "[WarehouseEditorViewModel.SaveAsync] Failed to save warehouse.");
            System.Windows.MessageBox.Show(ErrorMessage, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void Cancel()
    {
        RequestClose();
    }
    #endregion
}
