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

    private int _supplierId;
    private string _name = string.Empty;
    private string _phone = string.Empty;
    private string _email = string.Empty;
    private string _address = string.Empty;
    private string _taxNumber = string.Empty;
    private decimal _creditLimit;
    private decimal _openingBalance;
    private string _notes = string.Empty;
    private bool _isActive = true;
    private bool _isEditMode;
    private string? _errorMessage;


    public SupplierEditorViewModel()
        : this(App.GetService<ISupplierApiService>(), App.GetService<IEventBus>(), App.GetService<IDialogService>())
    {
    }

    public SupplierEditorViewModel(ISupplierApiService supplierService, IEventBus eventBus, IDialogService dialogService)
    {
        _supplierService = supplierService;
        _eventBus = eventBus;
        _dialogService = dialogService;
        SetDialogService(dialogService);

        SaveCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(SaveOperationAsync, "جاري حفظ المورد...")));
        CancelCommand = new RelayCommand(Cancel);
    }

    public SupplierEditorViewModel(SupplierDto supplier)
        : this(supplier, App.GetService<ISupplierApiService>(), App.GetService<IEventBus>(), App.GetService<IDialogService>())
    {
    }

    public SupplierEditorViewModel(SupplierDto supplier, ISupplierApiService supplierService, IEventBus eventBus, IDialogService dialogService)
        : this(supplierService, eventBus, dialogService)
    {
        _supplierId = supplier.Id;
        _name = supplier.Name;
        _phone = supplier.Phone ?? string.Empty;
        _email = supplier.Email ?? string.Empty;
        _address = supplier.Address ?? string.Empty;
        _taxNumber = supplier.TaxNumber ?? string.Empty;
        _creditLimit = supplier.CreditLimit;
        _openingBalance = supplier.OpeningBalance;
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

    public decimal CreditLimit
    {
        get => _creditLimit;
        set => SetProperty(ref _creditLimit, value);
    }

    public decimal OpeningBalance
    {
        get => _openingBalance;
        set
        {
            if (SetProperty(ref _openingBalance, value))
            {
                if (value < 0)
                    AddError(nameof(OpeningBalance), "الرصيد الافتتاحي يجب أن يكون أكبر من أو يساوي صفر");
                else
                    ClearErrors(nameof(OpeningBalance));
            }
        }
    }

    public string Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
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
            AddError(nameof(Name), "اسم المورد مطلوب");
        if (OpeningBalance < 0)
            AddError(nameof(OpeningBalance), "الرصيد الافتتاحي يجب أن يكون أكبر من أو يساوي صفر");

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
                CreditLimit,
                IsActive);

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
                OpeningBalance,
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

    private void Cancel()
    {
        RequestClose();
    }
    #endregion
}
