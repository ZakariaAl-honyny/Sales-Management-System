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
/// ViewModel for Customer Payment Editor (Phase 29 — Cheque support).
/// </summary>
public class CustomerPaymentEditorViewModel : ViewModelBase
{
    private readonly ICustomerPaymentApiService _paymentService;
    private readonly ICustomerApiService _customerService;
    private readonly IEventBus _eventBus;
    private readonly IPaymentPrinter _paymentPrinter;
    private readonly ISettingsApiService _settingsService;
    private readonly IDialogService _dialogService;
    private readonly IChequeApiService _chequeService;

    private readonly int? _paymentId;
    private readonly bool _isReadOnly;

    private int _selectedCustomerId;
    private DateTime _paymentDate = DateTime.Today;
    private decimal _amount;
    private PaymentMethod _paymentMethod = PaymentMethod.Cash;
    private string _notes = string.Empty;
    private string _errorMessage = string.Empty;

    // Cheque-specific fields
    private string _chequeNumber = string.Empty;
    private string _chequeBankName = string.Empty;
    private DateTime? _chequeIssueDate = DateTime.Today;
    private DateTime? _chequeMaturityDate = DateTime.Today.AddDays(30);

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
        _chequeService = App.GetService<IChequeApiService>();
        SetDialogService(dialogService);

        SaveCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(SaveOperationAsync, "جاري حفظ السداد...")));
        CancelCommand = new RelayCommand(OnCancel);
        PrintCommand = new AsyncRelayCommand(OnPrint, () => _paymentId.HasValue);
        SearchCustomerCommand = new RelayCommand(SearchCustomer, () => !_isReadOnly);

        InitializeAsync();
    }

    private async void InitializeAsync()
    {
        try
        {
            await LoadCustomersAsync();
            if (_paymentId.HasValue)
            {
                await LoadPaymentAsync();
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Error in {Method}", nameof(InitializeAsync));
            await _dialogService.ShowErrorAsync("خطأ", "حدث خطأ أثناء تحميل بيانات السداد");
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

    public PaymentMethod PaymentMethod
    {
        get => _paymentMethod;
        set
        {
            if (SetProperty(ref _paymentMethod, value))
            {
                OnPropertyChanged(nameof(ShowChequeFields));
            }
        }
    }

    public bool ShowChequeFields => PaymentMethod == PaymentMethod.Cheque;

    public string ChequeNumber
    {
        get => _chequeNumber;
        set => SetProperty(ref _chequeNumber, value);
    }

    public string ChequeBankName
    {
        get => _chequeBankName;
        set => SetProperty(ref _chequeBankName, value);
    }

    public DateTime? ChequeIssueDate
    {
        get => _chequeIssueDate;
        set => SetProperty(ref _chequeIssueDate, value);
    }

    public DateTime? ChequeMaturityDate
    {
        get => _chequeMaturityDate;
        set => SetProperty(ref _chequeMaturityDate, value);
    }

    public string Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
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

    public ObservableCollection<PaymentMethodOption> PaymentMethodOptions { get; } = new()
    {
        new PaymentMethodOption(PaymentMethod.Cash, "نقدي"),
        new PaymentMethodOption(PaymentMethod.Cheque, "شيك"),
        new PaymentMethodOption(PaymentMethod.BankTransfer, "تحويل بنكي"),
        new PaymentMethodOption(PaymentMethod.CreditCard, "بطاقة ائتمان")
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
            IsBusy = true;
            var result = await _paymentService.GetByIdAsync(_paymentId.Value);
            if (result.IsSuccess)
            {
                var payment = result.Value!;
                SelectedCustomerId = payment.CustomerId;
                PaymentDate = payment.PaymentDate;
                Amount = payment.Amount;
                PaymentMethod = (PaymentMethod)payment.PaymentMethod;
                Notes = payment.Notes ?? string.Empty;

                // Load associated cheque if payment method is Cheque
                if (PaymentMethod == PaymentMethod.Cheque)
                {
                    await LoadChequeDataAsync(_paymentId.Value);
                }
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
            IsBusy = false;
        }
    }

    /// <summary>
    /// Loads cheque data linked to this payment.
    /// </summary>
    private async Task LoadChequeDataAsync(int paymentId)
    {
        try
        {
            var chequesResult = await _chequeService.GetAllAsync(paymentId: paymentId);
            if (chequesResult.IsSuccess && chequesResult.Value != null && chequesResult.Value.Count > 0)
            {
                var cheque = chequesResult.Value[0];
                ChequeNumber = cheque.ChequeNumber;
                ChequeBankName = cheque.BankName;
                ChequeIssueDate = cheque.IssueDate;
                ChequeMaturityDate = cheque.MaturityDate;
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "Failed to load cheque data for payment {PaymentId}", paymentId);
        }
    }

    /// <summary>
    /// Validates all fields before saving. Uses INotifyDataErrorInfo pattern.
    /// </summary>
    private async Task<bool> ValidateAsync()
    {
        ClearAllErrors();

        if (SelectedCustomerId == 0)
            AddError(nameof(SelectedCustomerId), "العميل مطلوب");
        if (Amount <= 0)
            AddError(nameof(Amount), "يجب إدخال المبلغ بشكل صحيح (أكبر من 0)");

        // Cheque validation
        if (PaymentMethod == PaymentMethod.Cheque)
        {
            if (string.IsNullOrWhiteSpace(ChequeNumber))
                AddError(nameof(ChequeNumber), "رقم الشيك مطلوب");
            if (string.IsNullOrWhiteSpace(ChequeBankName))
                AddError(nameof(ChequeBankName), "اسم البنك مطلوب");
            if (ChequeIssueDate == null)
                AddError(nameof(ChequeIssueDate), "تاريخ إصدار الشيك مطلوب");
            if (ChequeMaturityDate == null)
                AddError(nameof(ChequeMaturityDate), "تاريخ استحقاق الشيك مطلوب");
            if (ChequeMaturityDate.HasValue && ChequeIssueDate.HasValue && ChequeMaturityDate < ChequeIssueDate)
                AddError(nameof(ChequeMaturityDate), "تاريخ الاستحقاق يجب أن يكون بعد تاريخ الإصدار");
        }

        return await ValidateAllAsync();
    }

    private async Task SaveOperationAsync()
    {
        if (!await ValidateAsync()) return;

        ErrorMessage = string.Empty;

        if (_paymentId.HasValue)
        {
            // Update existing payment
            var request = new UpdateCustomerPaymentRequest(
                CustomerId: SelectedCustomerId,
                Amount: Amount,
                PaymentMethod: PaymentMethod,
                PaymentDate: PaymentDate,
                Notes: Notes);
            var result = await _paymentService.UpdateAsync(_paymentId.Value, request);

            if (result.IsSuccess)
            {
                // Create or update cheque if payment method is Cheque
                if (PaymentMethod == PaymentMethod.Cheque)
                {
                    await CreateOrUpdateChequeAsync(result.Value!.Id, forCustomer: true);
                }

                _eventBus.Publish(new CustomerPaymentChangedMessage(result.Value!.Id));
                RequestClose();
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "حدث خطأ غير معروف", "CustomerPaymentEditorViewModel.SaveAsync", "[CustomerPaymentEditorViewModel.SaveAsync] Failed to save customer payment.");
                await _dialogService.ShowErrorAsync("خطأ في حفظ السداد", ErrorMessage);
            }
        }
        else
        {
            // Create new payment
            var request = new CreateCustomerPaymentRequest(
                CustomerId: SelectedCustomerId,
                Amount: Amount,
                PaymentMethod: PaymentMethod,
                PaymentDate: PaymentDate,
                SalesInvoiceId: null,
                Notes: Notes);
            var result = await _paymentService.CreateAsync(request);

            if (result.IsSuccess)
            {
                // Create cheque if payment method is Cheque
                if (PaymentMethod == PaymentMethod.Cheque)
                {
                    await CreateChequeRecordAsync(result.Value!.Id, forCustomer: true);
                }

                _eventBus.Publish(new CustomerPaymentChangedMessage(result.Value!.Id));
                RequestClose();
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "حدث خطأ غير معروف", "CustomerPaymentEditorViewModel.SaveAsync", "[CustomerPaymentEditorViewModel.SaveAsync] Failed to save customer payment.");
                await _dialogService.ShowErrorAsync("خطأ في حفظ السداد", ErrorMessage);
            }
        }
    }

    /// <summary>
    /// Creates a cheque record linked to a payment.
    /// </summary>
    private async Task CreateChequeRecordAsync(int paymentId, bool forCustomer)
    {
        try
        {
            var chequeRequest = new CreateChequeRequest(
                ChequeNumber: ChequeNumber,
                BankName: ChequeBankName,
                IssueDate: ChequeIssueDate ?? DateTime.Now,
                MaturityDate: ChequeMaturityDate ?? DateTime.Now.AddDays(30),
                Amount: Amount,
                CustomerPaymentId: forCustomer ? paymentId : null,
                SupplierPaymentId: !forCustomer ? paymentId : null,
                Notes: Notes);

            var chequeResult = await _chequeService.CreateAsync(chequeRequest);
            if (!chequeResult.IsSuccess)
            {
                Serilog.Log.Warning("Cheque creation logged but payment already saved for PaymentId={PaymentId}: {Error}",
                    paymentId, chequeResult.Error);
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Failed to create cheque record for payment {PaymentId}", paymentId);
        }
    }

    /// <summary>
    /// Creates or updates a cheque record (for edit mode).
    /// For simplicity, creates a new cheque linked to the payment.
    /// </summary>
    private async Task CreateOrUpdateChequeAsync(int paymentId, bool forCustomer)
    {
        // For existing payments with cheque method, create a new cheque record
        await CreateChequeRecordAsync(paymentId, forCustomer);
    }

    private void OnCancel()
    {
        RequestClose();
    }

    private async Task OnPrint()
    {
        if (!_paymentId.HasValue) return;

        IsBusy = true;
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
            IsBusy = false;
        }
    }
}

/// <summary>
/// Payment method display option
/// </summary>
public record PaymentMethodOption(PaymentMethod Value, string Display);
