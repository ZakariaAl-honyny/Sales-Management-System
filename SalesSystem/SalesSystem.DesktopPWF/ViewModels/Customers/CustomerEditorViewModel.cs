using SalesSystem.DesktopPWF.Messaging.Messages;
using System.Windows.Input;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;

namespace SalesSystem.DesktopPWF.ViewModels.Customers;

/// <summary>
/// ViewModel for Customer Editor Dialog
/// </summary>
public class CustomerEditorViewModel : ViewModelBase
{
    private readonly ICustomerApiService _customerService;
    private readonly IEventBus _eventBus;
    private readonly IDialogService _dialogService;

    private int _customerId;
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


    public CustomerEditorViewModel()
        : this(App.GetService<ICustomerApiService>(), App.GetService<IEventBus>(), App.GetService<IDialogService>())
    {
    }

    public CustomerEditorViewModel(ICustomerApiService customerService, IEventBus eventBus, IDialogService dialogService)
    {
        _customerService = customerService;
        _eventBus = eventBus;
        _dialogService = dialogService;
        SetDialogService(dialogService);

        SaveCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(SaveOperationAsync, "جاري حفظ العميل...")));
        CancelCommand = new RelayCommand(Cancel);
    }

    public CustomerEditorViewModel(CustomerDto customer)
        : this(customer, App.GetService<ICustomerApiService>(), App.GetService<IEventBus>(), App.GetService<IDialogService>())
    {
    }

    public CustomerEditorViewModel(CustomerDto customer, ICustomerApiService customerService, IEventBus eventBus, IDialogService dialogService)
        : this(customerService, eventBus, dialogService)
    {
        _customerId = customer.Id;
        _name = customer.Name;
        _phone = customer.Phone ?? string.Empty;
        _email = customer.Email ?? string.Empty;
        _address = customer.Address ?? string.Empty;
        _taxNumber = customer.TaxNumber ?? string.Empty;
        _creditLimit = customer.CreditLimit;
        _openingBalance = customer.OpeningBalance;
        _isActive = customer.IsActive;
        _isEditMode = true;
    }

    #region Properties
    public string Title => IsEditMode ? "تعديل عميل" : "إضافة عميل جديد";

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
                    AddError(nameof(Name), "اسم العميل مطلوب");
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
        set
        {
            if (SetProperty(ref _creditLimit, value))
            {
                if (value < 0)
                    AddError(nameof(CreditLimit), "الحد الائتماني يجب أن يكون أكبر من أو يساوي صفر");
                else
                    ClearErrors(nameof(CreditLimit));
            }
        }
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
            AddError(nameof(Name), "اسم العميل مطلوب");
        if (CreditLimit < 0)
            AddError(nameof(CreditLimit), "الحد الائتماني يجب أن يكون أكبر من أو يساوي صفر");
        if (OpeningBalance < 0)
            AddError(nameof(OpeningBalance), "الرصيد الافتتاحي يجب أن يكون أكبر من أو يساوي صفر");

        return await ValidateAllAsync();
    }

    private async Task SaveOperationAsync()
    {
        if (!await ValidateAsync()) return;

        ErrorMessage = null;

        Result<CustomerDto> result;

        if (IsEditMode)
        {
            var updateRequest = new UpdateCustomerRequest(
                Name,
                string.IsNullOrWhiteSpace(Phone) ? null : Phone,
                string.IsNullOrWhiteSpace(Email) ? null : Email,
                string.IsNullOrWhiteSpace(Address) ? null : Address,
                string.IsNullOrWhiteSpace(TaxNumber) ? null : TaxNumber,
                CreditLimit,
                IsActive);

            result = await _customerService.UpdateAsync(_customerId, updateRequest);
        }
        else
        {
            var createRequest = new CreateCustomerRequest(
                Name,
                string.IsNullOrWhiteSpace(Phone) ? null : Phone,
                string.IsNullOrWhiteSpace(Email) ? null : Email,
                string.IsNullOrWhiteSpace(Address) ? null : Address,
                string.IsNullOrWhiteSpace(TaxNumber) ? null : TaxNumber,
                OpeningBalance,
                CreditLimit);

            result = await _customerService.CreateAsync(createRequest);
        }

        if (result.IsSuccess && result.Value != null)
        {
            // Publish event to notify other modules
            _eventBus.Publish(new CustomerChangedMessage(result.Value.Id));

            await _dialogService.ShowSuccessAsync("نجاح", IsEditMode ? "تم تحديث العميل بنجاح" : "تم إضافة العميل بنجاح");

            RequestClose();
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في حفظ العميل", "CustomerEditorViewModel.SaveAsync", "[CustomerEditorViewModel.SaveAsync] Failed to save customer.");
            await _dialogService.ShowErrorAsync("خطأ في حفظ العميل", ErrorMessage!);
        }
    }

    private void Cancel()
    {
        RequestClose();
    }
    #endregion
}
