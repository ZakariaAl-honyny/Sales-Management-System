using SalesSystem.DesktopPWF.Messaging.Messages;
using System.Windows;
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

    private int _supplierId;
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


    public SupplierEditorViewModel()
        : this(App.GetService<ISupplierApiService>(), App.GetService<IEventBus>())
    {
    }

    public SupplierEditorViewModel(ISupplierApiService supplierService, IEventBus eventBus)
    {
        _supplierService = supplierService;
        _eventBus = eventBus;

        SaveCommand = new AsyncRelayCommand(SaveAsync);
        CancelCommand = new RelayCommand(Cancel);
    }

    public SupplierEditorViewModel(SupplierDto supplier)
        : this(supplier, App.GetService<ISupplierApiService>(), App.GetService<IEventBus>())
    {
    }

    public SupplierEditorViewModel(SupplierDto supplier, ISupplierApiService supplierService, IEventBus eventBus)
        : this(supplierService, eventBus)
    {
        _supplierId = supplier.Id;
        _code = supplier.Code ?? string.Empty;
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
        HasOpeningBalanceError = OpeningBalance < 0;
        return !HasNameError && !HasOpeningBalanceError;
    }

    private async Task SaveAsync()
    {
        if (!Validate())
        {
            var errors = new List<string>();
            if (HasNameError) errors.Add("• " + NameError);
            if (HasOpeningBalanceError) errors.Add("• " + OpeningBalanceError);
            
            string errorMsg = "يرجى إكمال البيانات الإلزامية التالية:\n\n" + string.Join("\n", errors);
            System.Windows.MessageBox.Show(errorMsg, "بيانات غير مكتملة", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            Result<SupplierDto> result;

            if (IsEditMode)
            {
                var updateRequest = new UpdateSupplierRequest(
                    Name,
                    string.IsNullOrWhiteSpace(Code) ? null : Code,
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
                    string.IsNullOrWhiteSpace(Code) ? null : Code,
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

                System.Windows.MessageBox.Show(
                    IsEditMode ? "تم تحديث المورد بنجاح" : "تم إضافة المورد بنجاح",
                    "نجاح",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                RequestClose();
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشل في حفظ المورد", "SupplierEditorViewModel.SaveAsync", "[SupplierEditorViewModel.SaveAsync] Failed to save supplier.");
                System.Windows.MessageBox.Show(ErrorMessage, "خطأ في الحفظ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = HandleException(ex, "SupplierEditorViewModel.SaveAsync", "[SupplierEditorViewModel.SaveAsync] Failed to save supplier.");
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
