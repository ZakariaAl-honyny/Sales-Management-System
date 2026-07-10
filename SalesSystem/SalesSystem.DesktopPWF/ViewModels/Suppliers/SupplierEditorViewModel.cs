using SalesSystem.DesktopPWF.Messaging.Messages;
using System.Windows.Input;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;

namespace SalesSystem.DesktopPWF.ViewModels.Suppliers;

/// <summary>
/// ViewModel for Supplier Editor Dialog
/// </summary>
public class SupplierEditorViewModel : ViewModelBase
{
    private readonly ISupplierApiService _supplierService;
    private readonly IEventBus _eventBus;
    private readonly IDialogService _dialogService;
    private readonly IScreenWindowService _screenWindowService;

    private int _supplierId;
    private string _name = string.Empty;
    private string _phone = string.Empty;
    private string _email = string.Empty;
    private string _address = string.Empty;
    private string _taxNumber = string.Empty;
    private string _notes = string.Empty;
    private decimal _creditLimit;
    private bool _isActive = true;
    private bool _isEditMode;
    private string? _errorMessage;

    public SupplierEditorViewModel()
        : this(App.GetService<ISupplierApiService>(), App.GetService<IEventBus>(), App.GetService<IDialogService>(), App.GetService<IScreenWindowService>())
    {
    }

    public SupplierEditorViewModel(ISupplierApiService supplierService, IEventBus eventBus, IDialogService dialogService, IScreenWindowService screenWindowService)
    {
        _supplierService = supplierService;
        _eventBus = eventBus;
        _dialogService = dialogService;
        _screenWindowService = screenWindowService;
        SetDialogService(dialogService);

        SaveCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(SaveOperationAsync, "جاري حفظ المورد...")));
        CancelCommand = new RelayCommand(Cancel);
        ManageContactsCommand = new RelayCommand(OpenContacts);
    }

    public SupplierEditorViewModel(SupplierDto supplier)
        : this(supplier, App.GetService<ISupplierApiService>(), App.GetService<IEventBus>(), App.GetService<IDialogService>(), App.GetService<IScreenWindowService>())
    {
    }

    public SupplierEditorViewModel(SupplierDto supplier, ISupplierApiService supplierService, IEventBus eventBus, IDialogService dialogService, IScreenWindowService screenWindowService)
        : this(supplierService, eventBus, dialogService, screenWindowService)
    {
        _supplierId = supplier.Id;
        _name = supplier.Name;
        _phone = supplier.Phone ?? string.Empty;
        _email = supplier.Email ?? string.Empty;
        _address = supplier.Address ?? string.Empty;
        _taxNumber = supplier.TaxNumber ?? string.Empty;
        _notes = supplier.Notes ?? string.Empty;
        _creditLimit = supplier.CreditLimit;
        _isActive = supplier.IsActive;
        _isEditMode = true;
    }

    #region Properties
    public string Title => IsEditMode ? "تعديل مورد" : "إضافة مورد جديد";

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
                    AddError(nameof(Name), "اسم المورد مطلوب");
                else
                    ClearErrors(nameof(Name));
            }
        }
    }

    public string Phone
    {
        get => _phone;
        set => SetProperty(ref _phone, value);
    }

    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public string Address
    {
        get => _address;
        set => SetProperty(ref _address, value);
    }

    public string TaxNumber
    {
        get => _taxNumber;
        set => SetProperty(ref _taxNumber, value);
    }

    public string Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }

    public decimal CreditLimit
    {
        get => _creditLimit;
        set => SetProperty(ref _creditLimit, value);
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
    public ICommand ManageContactsCommand { get; }
    #endregion

    #region Methods
    private async Task<bool> ValidateAsync()
    {
        ClearAllErrors();

        if (string.IsNullOrWhiteSpace(Name))
            AddError(nameof(Name), "اسم المورد مطلوب");

        return await ValidateAllAsync();
    }

    private async Task SaveOperationAsync()
    {
        if (!await ValidateAsync())
        {
            return;
        }

        ErrorMessage = null;

        Result<SupplierDto> result;

        if (IsEditMode)
        {
            var updateRequest = new UpdateSupplierRequest(
                Name,
                string.IsNullOrWhiteSpace(Phone) ? null : Phone,
                string.IsNullOrWhiteSpace(Email) ? null : Email,
                string.IsNullOrWhiteSpace(Address) ? null : Address,
                string.IsNullOrWhiteSpace(TaxNumber) ? null : TaxNumber,
                string.IsNullOrWhiteSpace(Notes) ? null : Notes,
                CreditLimit,
                CategoryId: null,
                IsActive: IsActive);

            result = await _supplierService.UpdateAsync(_supplierId, updateRequest);
        }
        else
        {
            var createRequest = new CreateSupplierRequest(
                Name,
                string.IsNullOrWhiteSpace(Phone) ? null : Phone,
                string.IsNullOrWhiteSpace(Email) ? null : Email,
                string.IsNullOrWhiteSpace(Address) ? null : Address,
                string.IsNullOrWhiteSpace(TaxNumber) ? null : TaxNumber,
                string.IsNullOrWhiteSpace(Notes) ? null : Notes,
                CreditLimit);

            result = await _supplierService.CreateAsync(createRequest);
        }

        if (result.IsSuccess && result.Value != null)
        {
            // Publish event to notify other modules
            _eventBus.Publish(new SupplierChangedMessage(result.Value.Id));

            await _dialogService.ShowSuccessAsync("نجاح", IsEditMode ? "تم تحديث المورد بنجاح" : "تم إضافة المورد بنجاح");

            RequestClose();
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في حفظ المورد", "SupplierEditorViewModel.SaveAsync", "[SupplierEditorViewModel.SaveAsync] Failed to save supplier.");
            await _dialogService.ShowErrorAsync("خطأ في حفظ المورد", ErrorMessage!);
        }
    }

    private void OpenContacts()
    {
        if (_supplierId <= 0) return;

        var contactsVm = App.GetService<SupplierContactListViewModel>();
        contactsVm.LoadContacts(_supplierId, Name);

        _screenWindowService.OpenScreen(contactsVm, new ScreenWindowOptions
        {
            Title = $"جهات اتصال المورد: {Name}",
            Width = 800,
            Height = 500
        });
    }

    private void Cancel()
    {
        RequestClose();
    }
    #endregion
}
