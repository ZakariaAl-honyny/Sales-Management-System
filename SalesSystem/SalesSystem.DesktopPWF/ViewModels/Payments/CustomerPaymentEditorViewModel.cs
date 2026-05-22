using SalesSystem.DesktopPWF.Messaging.Messages;
using System.Collections.ObjectModel;
using System.Windows.Input;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Enums;
using SalesSystem.Contracts.Requests;
using SalesSystem.DesktopPWF.Helpers;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Helpers;

namespace SalesSystem.DesktopPWF.ViewModels.Payments;

/// <summary>
/// ViewModel for Customer Payment Editor
/// </summary>
public class CustomerPaymentEditorViewModel : ViewModelBase
{
    private readonly ICustomerPaymentApiService _paymentService;
    private readonly ICustomerApiService _customerService;
    private readonly IEventBus _eventBus;
    private readonly IPaymentPrinter _paymentPrinter;
    private readonly ISettingsApiService _settingsService;
    private readonly IDialogService _dialogService;

    private readonly int? _paymentId;
    private readonly bool _isReadOnly;

    private int _selectedCustomerId;
    private DateTime _paymentDate = DateTime.Today;
    private decimal _amount;
    private PaymentType _paymentType = PaymentType.Cash;
    private string _notes = string.Empty;
    private bool _isLoading;
    private string _errorMessage = string.Empty;

    private ObservableCollection<CustomerDto> _customers = new();

    public CustomerPaymentEditorViewModel(
        ICustomerPaymentApiService paymentService,
        ICustomerApiService customerService,
        IEventBus eventBus,
        IDialogService dialogService,
        int? paymentId = null,
        bool isReadOnly = false)
    {
        _paymentId = paymentId;
        _isReadOnly = isReadOnly;
        _paymentService = paymentService;
        _customerService = customerService;
        _eventBus = eventBus;
        _paymentPrinter = App.GetService<IPaymentPrinter>();
        _settingsService = App.GetService<ISettingsApiService>();
        _dialogService = dialogService;

        SaveCommand = new AsyncRelayCommand(SaveAsync, () => !IsLoading && !_isReadOnly);
        CancelCommand = new RelayCommand(OnCancel);
        PrintCommand = new AsyncRelayCommand(OnPrint, () => _paymentId.HasValue);
        SearchCustomerCommand = new RelayCommand(SearchCustomer, () => !_isReadOnly);

        InitializeAsync();
    }

    private async void InitializeAsync()
    {
        await LoadCustomersAsync();
        if (_paymentId.HasValue)
        {
            await LoadPaymentAsync();
        }
    }

    public CustomerPaymentEditorViewModel(int? paymentId = null, bool isReadOnly = false)
        : this(
            App.GetService<ICustomerPaymentApiService>(),
            App.GetService<ICustomerApiService>(),
            App.GetService<IEventBus>(),
            App.GetService<IDialogService>(),
            paymentId,
            isReadOnly)
    {
    }

    public ObservableCollection<CustomerDto> Customers
    {
        get => _customers;
        set => SetProperty(ref _customers, value);
    }

    public int SelectedCustomerId
    {
        get => _selectedCustomerId;
        set => SetProperty(ref _selectedCustomerId, value);
    }

    public DateTime PaymentDate
    {
        get => _paymentDate;
        set => SetProperty(ref _paymentDate, value);
    }

    public decimal Amount
    {
        get => _amount;
        set => SetProperty(ref _amount, value);
    }

    public PaymentType PaymentType
    {
        get => _paymentType;
        set => SetProperty(ref _paymentType, value);
    }

    public string Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public bool IsReadOnly => _isReadOnly;
    public bool IsEdit => _paymentId.HasValue;

    public string WindowTitle => _isReadOnly ? "عرض سداد العميل" :
                                 _paymentId.HasValue ? "تعديل سداد العميل" : "إضافة سداد عميل جديد";

    public ObservableCollection<PaymentTypeOption> PaymentTypeOptions { get; } = new()
    {
        new PaymentTypeOption(PaymentType.Cash, "نقدي"),
        new PaymentTypeOption(PaymentType.Credit, "آجل"),
        new PaymentTypeOption(PaymentType.Mixed, "مختلط")
    };

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand PrintCommand { get; }
    public ICommand SearchCustomerCommand { get; }


    private void SearchCustomer()
    {
        var dialogService = App.GetService<IDialogService>();
        var vm = new Customers.CustomerSelectionViewModel();
        if (dialogService.ShowDialog(vm) && vm.SelectedCustomer != null)
        {
            SelectedCustomerId = vm.SelectedCustomer.Id;
        }
    }

    private async Task LoadCustomersAsync()
    {
        try
        {
            var result = await _customerService.GetAllAsync();
            if (result.IsSuccess)
            {
                Customers = new ObservableCollection<CustomerDto>(result.Value ?? new List<CustomerDto>());
            }
        }
        catch
        {
            // Ignore
        }
    }

    private async Task LoadPaymentAsync()
    {
        if (!_paymentId.HasValue) return;

        try
        {
            IsLoading = true;
            var result = await _paymentService.GetByIdAsync(_paymentId.Value);
            if (result.IsSuccess)
            {
                var payment = result.Value!;
                SelectedCustomerId = payment.CustomerId;
                PaymentDate = payment.PaymentDate;
                Amount = payment.Amount;
                PaymentType = (PaymentType)payment.PaymentMethod;
                Notes = payment.Notes ?? string.Empty;
            }
            else
            {
                ErrorMessage = result.Error ?? "حدث خطأ غير معروف";
            }
        }
        catch (Exception ex)
        {
            LogSystemError($"Failed to load customer payment {_paymentId}", "CustomerPaymentEditorViewModel.LoadPaymentAsync", ex);
            ErrorMessage = "حدث خطأ غير متوقع أثناء تحميل بيانات السداد";
            await _dialogService.ShowErrorAsync("خطأ في تحميل البيانات", ErrorMessage);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task SaveAsync()
    {
        var errors = new List<string>();
        if (SelectedCustomerId == 0) errors.Add("• العميل مطلوب");
        if (Amount <= 0) errors.Add("• يجب إدخال المبلغ بشكل صحيح (أكبر من 0)");

        if (errors.Any())
        {
            await _dialogService.ShowValidationErrorsAsync("بيانات غير مكتملة", errors);
            RequestFocusFirstInvalidField();
            return;
        }

        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            Result<CustomerPaymentDto> result;

            if (_paymentId.HasValue)
            {
                var request = new UpdateCustomerPaymentRequest(
                    CustomerId: SelectedCustomerId,
                    Amount: Amount,
                    PaymentMethod: PaymentType,
                    PaymentDate: PaymentDate,
                    SalesInvoiceId: null,
                    Notes: Notes);
                result = await _paymentService.UpdateAsync(_paymentId.Value, request);
            }
            else
            {
                var request = new CreateCustomerPaymentRequest(
                    CustomerId: SelectedCustomerId,
                    Amount: Amount,
                    PaymentMethod: PaymentType,
                    PaymentDate: PaymentDate,
                    SalesInvoiceId: null,
                    Notes: Notes);
                result = await _paymentService.CreateAsync(request);
            }

            if (result.IsSuccess)
            {
                _eventBus.Publish(new CustomerPaymentChangedMessage(result.Value!.Id));
                RequestClose();
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "حدث خطأ غير معروف", "CustomerPaymentEditorViewModel.SaveAsync", "[CustomerPaymentEditorViewModel.SaveAsync] Failed to save customer payment.");
                await _dialogService.ShowErrorAsync("خطأ في حفظ السداد", ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = HandleException(ex, "CustomerPaymentEditorViewModel.SaveAsync", "[CustomerPaymentEditorViewModel.SaveAsync] Failed to save customer payment.");
            await _dialogService.ShowErrorAsync("خطأ في حفظ السداد", ErrorMessage);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void OnCancel()
    {
        RequestClose();
    }

    private async Task OnPrint()
    {
        if (!_paymentId.HasValue) return;

        IsLoading = true;
        try
        {
            var settingsResult = await _settingsService.GetSettingsAsync();
            if (!settingsResult.IsSuccess || settingsResult.Value == null) return;

            var paymentResult = await _paymentService.GetByIdAsync(_paymentId.Value);
            if (!paymentResult.IsSuccess || paymentResult.Value == null) return;

            _paymentPrinter.PrintPreview(paymentResult.Value.ToPrintDto(), settingsResult.Value.ToPrintDto());
        }
        catch (Exception ex)
        {
            LogSystemError($"Failed to print customer payment {_paymentId}", "CustomerPaymentEditorViewModel.OnPrint", ex);
            ErrorMessage = "حدث خطأ غير متوقع أثناء الطباعة";
        }
        finally
        {
            IsLoading = false;
        }
    }
}

/// <summary>
/// Payment type display option
/// </summary>
public record PaymentTypeOption(PaymentType Value, string Display);
