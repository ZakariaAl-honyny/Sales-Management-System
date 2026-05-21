using SalesSystem.DesktopPWF.Messaging.Messages;
using System.Windows;
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

    private int _customerId;
    private string _code = string.Empty;
    private string _name = string.Empty;
    private string _phone = string.Empty;
    private string _email = string.Empty;
    private string _address = string.Empty;
    private string _taxNumber = string.Empty;
    private decimal _creditLimit;
    private decimal _openingBalance;
    private string _notes = string.Empty;
    private bool _isActive = true;
    private bool _isLoading;
    private bool _isEditMode;
    private string? _errorMessage;


    public CustomerEditorViewModel()
        : this(App.GetService<ICustomerApiService>(), App.GetService<IEventBus>())
    {
    }

    public CustomerEditorViewModel(ICustomerApiService customerService, IEventBus eventBus)
    {
        _customerService = customerService;
        _eventBus = eventBus;

        SaveCommand = new AsyncRelayCommand(SaveAsync);
        CancelCommand = new RelayCommand(Cancel);
    }

    public CustomerEditorViewModel(CustomerDto customer)
        : this(customer, App.GetService<ICustomerApiService>(), App.GetService<IEventBus>())
    {
    }

    public CustomerEditorViewModel(CustomerDto customer, ICustomerApiService customerService, IEventBus eventBus)
        : this(customerService, eventBus)
    {
        _customerId = customer.Id;
        _code = customer.Code ?? string.Empty;
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

    public string Code
    {
        get => _code;
        set => SetProperty(ref _code, value);
    }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
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
        set => SetProperty(ref _openingBalance, value);
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

    private bool _hasCreditLimitError;
    public bool HasCreditLimitError
    {
        get => _hasCreditLimitError;
        set
        {
            if (SetProperty(ref _hasCreditLimitError, value))
                OnPropertyChanged(nameof(CreditLimitError));
        }
    }

    public string? CreditLimitError => HasCreditLimitError ? "الحد الائتماني يجب أن يكون أكبر من أو يساوي صفر" : null;

    private bool _hasOpeningBalanceError;
    public bool HasOpeningBalanceError
    {
        get => _hasOpeningBalanceError;
        set
        {
            if (SetProperty(ref _hasOpeningBalanceError, value))
                OnPropertyChanged(nameof(OpeningBalanceError));
        }
    }

    public string? OpeningBalanceError => HasOpeningBalanceError ? "الرصيد الافتتاحي يجب أن يكون أكبر من أو يساوي صفر" : null;
    #endregion

    #region Commands
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    #endregion

    #region Methods
    private bool Validate()
    {
        HasNameError = string.IsNullOrWhiteSpace(Name);
        HasCreditLimitError = CreditLimit < 0;
        HasOpeningBalanceError = OpeningBalance < 0;

        return !HasNameError && !HasCreditLimitError && !HasOpeningBalanceError;
    }

    private async Task SaveAsync()
    {
        if (!Validate())
        {
            var errors = new List<string>();
            if (HasNameError) errors.Add("• " + NameError);
            if (HasCreditLimitError) errors.Add("• " + CreditLimitError);
            if (HasOpeningBalanceError) errors.Add("• " + OpeningBalanceError);
            
            string errorMsg = "يرجى إكمال البيانات الإلزامية التالية:\n\n" + string.Join("\n", errors);
            System.Windows.MessageBox.Show(errorMsg, "بيانات غير مكتملة", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            Result<CustomerDto> result;

            if (IsEditMode)
            {
                var updateRequest = new UpdateCustomerRequest(
                    Name,
                    string.IsNullOrWhiteSpace(Code) ? null : Code,
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
                    string.IsNullOrWhiteSpace(Code) ? null : Code,
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

                System.Windows.MessageBox.Show(
                    IsEditMode ? "تم تحديث العميل بنجاح" : "تم إضافة العميل بنجاح",
                    "نجاح",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                RequestClose();
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشل في حفظ العميل", "CustomerEditorViewModel.SaveAsync", "[CustomerEditorViewModel.SaveAsync] Failed to save customer.");
                System.Windows.MessageBox.Show(ErrorMessage, "خطأ في الحفظ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = HandleException(ex, "CustomerEditorViewModel.SaveAsync", "[CustomerEditorViewModel.SaveAsync] Failed to save customer.");
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
