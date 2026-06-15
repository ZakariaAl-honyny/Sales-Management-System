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
    private readonly IScreenWindowService _screenWindowService;

    private int _customerId;
    private string _name = string.Empty;
    private string _phone = string.Empty;
    private string _email = string.Empty;
    private string _address = string.Empty;
    private string _taxNumber = string.Empty;
    private decimal _creditLimit;
    private string _notes = string.Empty;
    private bool _isActive = true;
    private bool _isEditMode;
    private string? _errorMessage;

    public CustomerEditorViewModel()
        : this(App.GetService<ICustomerApiService>(), App.GetService<IEventBus>(), App.GetService<IDialogService>(), App.GetService<IScreenWindowService>())
    {
    }

    public CustomerEditorViewModel(ICustomerApiService customerService, IEventBus eventBus, IDialogService dialogService, IScreenWindowService screenWindowService)
    {
        _customerService = customerService;
        _eventBus = eventBus;
        _dialogService = dialogService;
        _screenWindowService = screenWindowService;
        SetDialogService(dialogService);

        SaveCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(SaveOperationAsync, "جاري حفظ العميل...")));
        CancelCommand = new RelayCommand(Cancel);
        ManageContactsCommand = new RelayCommand(OpenContacts);
    }

    public CustomerEditorViewModel(CustomerDto customer)
        : this(customer, App.GetService<ICustomerApiService>(), App.GetService<IEventBus>(), App.GetService<IDialogService>(), App.GetService<IScreenWindowService>())
    {
    }

    public CustomerEditorViewModel(CustomerDto customer, ICustomerApiService customerService, IEventBus eventBus, IDialogService dialogService, IScreenWindowService screenWindowService)
        : this(customerService, eventBus, dialogService, screenWindowService)
    {
        _customerId = customer.Id;
        _name = customer.Name;
        _phone = customer.Phone ?? string.Empty;
        _email = customer.Email ?? string.Empty;
        _address = customer.Address ?? string.Empty;
        _taxNumber = customer.TaxNumber ?? string.Empty;
        _creditLimit = customer.CreditLimit;
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
        set
        {
            if (SetProperty(ref _phone, value))
            {
                if (!string.IsNullOrWhiteSpace(value) && !System.Text.RegularExpressions.Regex.IsMatch(value.Trim(), @"^05\d{8}$"))
                    AddError(nameof(Phone), "رقم الجوال يجب أن يبدأ بـ 05 ويتكون من 10 أرقام");
                else
                    ClearErrors(nameof(Phone));
            }
        }
    }

    public string Email
    {
        get => _email;
        set
        {
            if (SetProperty(ref _email, value))
            {
                if (!string.IsNullOrWhiteSpace(value) && !value.Contains('@'))
                    AddError(nameof(Email), "البريد الإلكتروني يجب أن يحتوي على @");
                else
                    ClearErrors(nameof(Email));
            }
        }
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
    public ICommand ManageContactsCommand { get; }
    #endregion

    #region Methods
    private async Task<bool> ValidateAsync()
    {
        ClearAllErrors();

        if (string.IsNullOrWhiteSpace(Name))
            AddError(nameof(Name), "اسم العميل مطلوب");
        if (CreditLimit < 0)
            AddError(nameof(CreditLimit), "الحد الائتماني يجب أن يكون أكبر من أو يساوي صفر");

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

    private void OpenContacts()
    {
        if (_customerId <= 0) return;

        var contactsVm = App.GetService<CustomerContactListViewModel>();
        contactsVm.LoadContacts(_customerId, Name);

        _screenWindowService.OpenScreen(contactsVm, new ScreenWindowOptions
        {
            Title = $"جهات اتصال العميل: {Name}",
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
