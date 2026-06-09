using SalesSystem.DesktopPWF.Messaging.Messages;
using System.Collections.ObjectModel;
using System.Windows.Input;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Enums;
using SalesSystem.Contracts.Requests;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Helpers;

namespace SalesSystem.DesktopPWF.ViewModels.Payments;

/// <summary>
/// ViewModel for Supplier Payment Editor (Phase 29 — Cheque support).
/// </summary>
public class SupplierPaymentEditorViewModel : ViewModelBase
{
    private readonly ISupplierPaymentApiService _paymentService;
    private readonly ISupplierApiService _supplierService;
    private readonly IEventBus _eventBus;
    private readonly IPaymentPrinter _paymentPrinter;
    private readonly ISettingsApiService _settingsService;
    private readonly IDialogService _dialogService;
    private readonly IChequeApiService _chequeService;

    private readonly int? _paymentId;
    private readonly bool _isReadOnly;

    private int _selectedSupplierId;
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

    private ObservableCollection<SupplierDto> _suppliers = new();

    public SupplierPaymentEditorViewModel(
        ISupplierPaymentApiService paymentService,
        ISupplierApiService supplierService,
        IEventBus eventBus,
        IDialogService dialogService,
        int? paymentId = null,
        bool isReadOnly = false)
    {
        _paymentId = paymentId;
        _isReadOnly = isReadOnly;
        _paymentService = paymentService;
        _supplierService = supplierService;
        _eventBus = eventBus;
        _paymentPrinter = App.GetService<IPaymentPrinter>();
        _settingsService = App.GetService<ISettingsApiService>();
        _dialogService = dialogService;
        _chequeService = App.GetService<IChequeApiService>();
        SetDialogService(dialogService);

        SaveCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(SaveOperationAsync, "جاري حفظ السداد...")));
        CancelCommand = new RelayCommand(OnCancel);
        PrintCommand = new AsyncRelayCommand(OnPrint, () => _paymentId.HasValue);
        SearchSupplierCommand = new RelayCommand(SearchSupplier, () => !_isReadOnly);

        InitializeAsync();
    }

    private async void InitializeAsync()
    {
        try
        {
            await LoadSuppliersAsync();
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

    public SupplierPaymentEditorViewModel(int? paymentId = null, bool isReadOnly = false)
        : this(
            App.GetService<ISupplierPaymentApiService>(),
            App.GetService<ISupplierApiService>(),
            App.GetService<IEventBus>(),
            App.GetService<IDialogService>(),
            paymentId,
            isReadOnly)
    {
    }

    public ObservableCollection<SupplierDto> Suppliers
    {
        get => _suppliers;
        set => SetProperty(ref _suppliers, value);
    }

    public int SelectedSupplierId
    {
        get => _selectedSupplierId;
        set => SetProperty(ref _selectedSupplierId, value);
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

    public string WindowTitle => _isReadOnly ? "عرض سداد المورد" :
                                 _paymentId.HasValue ? "تعديل سداد المورد" : "إضافة سداد مورد جديد";

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
    public ICommand SearchSupplierCommand { get; }


    private void SearchSupplier()
    {
        var dialogService = App.GetService<IDialogService>();
        var vm = new Suppliers.SupplierSelectionViewModel();
        if (dialogService.ShowDialog(vm) && vm.SelectedSupplier != null)
        {
            SelectedSupplierId = vm.SelectedSupplier.Id;
        }
    }

    private async Task LoadSuppliersAsync()
    {
        try
        {
            var result = await _supplierService.GetAllAsync();
            if (result.IsSuccess)
            {
                Suppliers = new ObservableCollection<SupplierDto>(result.Value ?? new List<SupplierDto>());
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
                SelectedSupplierId = payment.SupplierId;
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
            LogSystemError($"Failed to load supplier payment {_paymentId}", "SupplierPaymentEditorViewModel.LoadPaymentAsync", ex);
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

        if (SelectedSupplierId == 0)
            AddError(nameof(SelectedSupplierId), "المورد مطلوب");
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
            var request = new UpdateSupplierPaymentRequest(
                SupplierId: SelectedSupplierId,
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
                    await CreateChequeRecordAsync(result.Value!.Id, forCustomer: false);
                }

                _eventBus.Publish(new SupplierPaymentChangedMessage(result.Value!.Id));
                RequestClose();
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "حدث خطأ غير معروف", "SupplierPaymentEditorViewModel.SaveAsync", "[SupplierPaymentEditorViewModel.SaveAsync] Failed to save supplier payment.");
                await _dialogService.ShowErrorAsync("خطأ في حفظ السداد", ErrorMessage);
            }
        }
        else
        {
            // Create new payment
            var request = new CreateSupplierPaymentRequest(
                SupplierId: SelectedSupplierId,
                Amount: Amount,
                PaymentMethod: PaymentMethod,
                PaymentDate: PaymentDate,
                PurchaseInvoiceId: null,
                Notes: Notes);
            var result = await _paymentService.CreateAsync(request);

            if (result.IsSuccess)
            {
                // Create cheque if payment method is Cheque
                if (PaymentMethod == PaymentMethod.Cheque)
                {
                    await CreateChequeRecordAsync(result.Value!.Id, forCustomer: false);
                }

                _eventBus.Publish(new SupplierPaymentChangedMessage(result.Value!.Id));
                RequestClose();
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "حدث خطأ غير معروف", "SupplierPaymentEditorViewModel.SaveAsync", "[SupplierPaymentEditorViewModel.SaveAsync] Failed to save supplier payment.");
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
            LogSystemError($"Failed to print supplier payment {_paymentId}", "SupplierPaymentEditorViewModel.OnPrint", ex);
            ErrorMessage = "حدث خطأ غير متوقع أثناء الطباعة";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
