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
/// ViewModel for Supplier Payment Editor
/// </summary>
public class SupplierPaymentEditorViewModel : ViewModelBase
{
    private readonly ISupplierPaymentApiService _paymentService;
    private readonly ISupplierApiService _supplierService;
    private readonly IEventBus _eventBus;
    private readonly IPaymentPrinter _paymentPrinter;
    private readonly ISettingsApiService _settingsService;
    private readonly IDialogService _dialogService;

    private readonly int? _paymentId;
    private readonly bool _isReadOnly;

    private int _selectedSupplierId;
    private DateTime _paymentDate = DateTime.Today;
    private decimal _amount;
    private PaymentType _paymentType = PaymentType.Cash;
    private string _notes = string.Empty;
    private string _errorMessage = string.Empty;

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

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public bool IsReadOnly => _isReadOnly;
    public bool IsEdit => _paymentId.HasValue;

    public string WindowTitle => _isReadOnly ? "عرض سداد المورد" :
                                 _paymentId.HasValue ? "تعديل سداد المورد" : "إضافة سداد مورد جديد";

    public ObservableCollection<PaymentTypeOption> PaymentTypeOptions { get; } = new()
    {
        new PaymentTypeOption(PaymentType.Cash, "نقدي"),
        new PaymentTypeOption(PaymentType.Credit, "آجل"),
        new PaymentTypeOption(PaymentType.Mixed, "مختلط")
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
            LogSystemError($"Failed to load supplier payment {_paymentId}", "SupplierPaymentEditorViewModel.LoadPaymentAsync", ex);
            ErrorMessage = "حدث خطأ غير متوقع أثناء تحميل بيانات السداد";
            await _dialogService.ShowErrorAsync("خطأ في تحميل البيانات", ErrorMessage);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void Cancel()
    {
        RequestClose();
    }

    private async Task SaveOperationAsync()
    {
        var errors = new List<string>();
        if (SelectedSupplierId == 0) errors.Add("• المورد مطلوب");
        if (Amount <= 0) errors.Add("• يجب إدخال المبلغ بشكل صحيح (أكبر من 0)");

        if (errors.Any())
        {
            await _dialogService.ShowValidationErrorsAsync("بيانات غير مكتملة", errors);
            RequestFocusFirstInvalidField();
            return;
        }

        ErrorMessage = string.Empty;

        Result<SupplierPaymentDto> result;

        if (_paymentId.HasValue)
        {
            var request = new UpdateSupplierPaymentRequest(
                SupplierId: SelectedSupplierId,
                Amount: Amount,
                PaymentMethod: PaymentType,
                PaymentDate: PaymentDate,
                PurchaseInvoiceId: null,
                Notes: Notes);
            result = await _paymentService.UpdateAsync(_paymentId.Value, request);
        }
        else
        {
            var request = new CreateSupplierPaymentRequest(
                SupplierId: SelectedSupplierId,
                Amount: Amount,
                PaymentMethod: PaymentType,
                PaymentDate: PaymentDate,
                PurchaseInvoiceId: null,
                Notes: Notes);
            result = await _paymentService.CreateAsync(request);
        }

        if (result.IsSuccess)
        {
            _eventBus.Publish(new SupplierPaymentChangedMessage(result.Value!.Id));
            RequestClose();
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "حدث خطأ غير معروف", "SupplierPaymentEditorViewModel.SaveAsync", "[SupplierPaymentEditorViewModel.SaveAsync] Failed to save supplier payment.");
            await _dialogService.ShowErrorAsync("خطأ في حفظ السداد", ErrorMessage);
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
