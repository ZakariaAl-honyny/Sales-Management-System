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
using SalesSystem.DesktopPWF.Models;

namespace SalesSystem.DesktopPWF.ViewModels.Payments;

/// <summary>
/// ViewModel for Supplier Payment Editor.
/// Enhanced with multi-invoice allocation grid for settling unpaid purchase invoices.
/// </summary>
public class SupplierPaymentEditorViewModel : ViewModelBase
{
    private readonly ISupplierPaymentApiService _paymentService;
    private readonly ISupplierApiService _supplierService;
    private readonly IPurchaseInvoiceApiService _invoiceService;
    private readonly ISupplierPaymentApplicationApiService _applicationService;
    private readonly IEventBus _eventBus;
    private readonly IPaymentPrinter _paymentPrinter;
    private readonly ISettingsApiService _settingsService;
    private readonly IDialogService _dialogService;
    private readonly int? _paymentId;
    private readonly bool _isReadOnly;

    private int _selectedSupplierId;
    private DateTime _paymentDate = DateTime.Today;
    private decimal _amount;
    private PaymentMethod _paymentMethod = PaymentMethod.Cash;
    private string _notes = string.Empty;
    private string _errorMessage = string.Empty;
    private decimal _totalAllocated;

    private ObservableCollection<SupplierDto> _suppliers = new();

    public SupplierPaymentEditorViewModel(
        ISupplierPaymentApiService paymentService,
        ISupplierApiService supplierService,
        IPurchaseInvoiceApiService invoiceService,
        ISupplierPaymentApplicationApiService applicationService,
        IEventBus eventBus,
        IDialogService dialogService,
        int? paymentId = null,
        bool isReadOnly = false)
    {
        _paymentId = paymentId;
        _isReadOnly = isReadOnly;
        _paymentService = paymentService;
        _supplierService = supplierService;
        _invoiceService = invoiceService;
        _applicationService = applicationService;
        _eventBus = eventBus;
        _paymentPrinter = App.GetService<IPaymentPrinter>();
        _settingsService = App.GetService<ISettingsApiService>();
        _dialogService = dialogService;
        SetDialogService(dialogService);

        SaveCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(SaveOperationAsync, "جاري حفظ السداد...")));
        CancelCommand = new RelayCommand(OnCancel);
        PrintCommand = new AsyncRelayCommand(OnPrint, () => _paymentId.HasValue);
        SearchSupplierCommand = new RelayCommand(SearchSupplier, () => !_isReadOnly);
        LoadUnpaidInvoicesCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(LoadUnpaidInvoicesAsync, "جاري تحميل الفواتير...")));

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
            App.GetService<IPurchaseInvoiceApiService>(),
            App.GetService<ISupplierPaymentApplicationApiService>(),
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
        set
        {
            if (SetProperty(ref _selectedSupplierId, value))
            {
                ClearErrors(nameof(SelectedSupplierId));
            }
        }
    }

    public DateTime PaymentDate
    {
        get => _paymentDate;
        set => SetProperty(ref _paymentDate, value);
    }

    public decimal Amount
    {
        get => _amount;
        set
        {
            if (SetProperty(ref _amount, value))
            {
                OnPropertyChanged(nameof(RemainingToAllocate));
            }
        }
    }

    public PaymentMethod PaymentMethod
    {
        get => _paymentMethod;
        set
        {
            SetProperty(ref _paymentMethod, value);
        }
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
        new PaymentMethodOption(PaymentMethod.BankTransfer, "تحويل بنكي"),
        new PaymentMethodOption(PaymentMethod.CreditCard, "بطاقة ائتمان")
    };

    /// <summary>
    /// Unpaid purchase invoices for the selected supplier, available for allocation
    /// </summary>
    public ObservableCollection<UnpaidPurchaseInvoiceLine> UnpaidInvoices { get; } = new();

    /// <summary>
    /// Total amount allocated across all invoices
    /// </summary>
    public decimal TotalAllocated
    {
        get => _totalAllocated;
        private set => SetProperty(ref _totalAllocated, value);
    }

    /// <summary>
    /// Remaining payment amount after deducting allocations
    /// </summary>
    public decimal RemainingToAllocate => Amount - TotalAllocated;

    /// <summary>
    /// True when at least one invoice has allocation
    /// </summary>
    public bool HasAllocations => TotalAllocated > 0;

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand PrintCommand { get; }
    public ICommand SearchSupplierCommand { get; }
    public ICommand LoadUnpaidInvoicesCommand { get; }

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
    /// Loads unpaid purchase invoices for the selected supplier.
    /// </summary>
    public async Task LoadUnpaidInvoicesAsync()
    {
        if (SelectedSupplierId <= 0)
        {
            UnpaidInvoices.Clear();
            TotalAllocated = 0;
            return;
        }

        var result = await _invoiceService.GetAllAsync(supplierId: SelectedSupplierId, pageSize: 200);
        if (!result.IsSuccess || result.Value == null) return;

        var unpaid = result.Value
            .Where(inv => inv.Status == 2 && inv.NetTotal > inv.PaidAmount) // Posted + outstanding
            .Select(inv => new UnpaidPurchaseInvoiceLine
            {
                InvoiceId = inv.Id,
                InvoiceNo = inv.InvoiceNo,
                InvoiceDate = inv.InvoiceDate.ToString("yyyy-MM-dd"),
                TotalAmount = inv.NetTotal,
                PaidAmount = inv.PaidAmount,
                AllocatedAmount = 0
            })
            .OrderByDescending(i => i.InvoiceNo)
            .ToList();

        UnpaidInvoices.Clear();
        foreach (var item in unpaid)
        {
            item.PropertyChanged += OnAllocationItemChanged;
            UnpaidInvoices.Add(item);
        }

        RecalculateAllocations();
    }

    private void OnAllocationItemChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(UnpaidInvoiceAllocationItem.AllocatedAmount))
        {
            RecalculateAllocations();
        }
    }

    private void RecalculateAllocations()
    {
        TotalAllocated = UnpaidInvoices.Sum(i => i.AllocatedAmount);
        OnPropertyChanged(nameof(RemainingToAllocate));
        OnPropertyChanged(nameof(HasAllocations));
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
        if (RemainingToAllocate < 0)
            AddError(nameof(Amount), "المبلغ المخصص أكبر من قيمة سند الدفع");

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
                CashBoxId: null,
                Notes: Notes);
            var result = await _paymentService.CreateAsync(request);

            if (result.IsSuccess)
            {
                var paymentId = result.Value!.Id;

                // Create allocations for each invoice with an allocated amount
                foreach (var allocation in UnpaidInvoices.Where(a => a.AllocatedAmount > 0))
                {
                    var appRequest = new CreateSupplierPaymentApplicationRequest(
                        paymentId,
                        allocation.InvoiceId,
                        allocation.AllocatedAmount);

                    await _applicationService.CreateAsync(appRequest);
                }

                _eventBus.Publish(new SupplierPaymentChangedMessage(paymentId));
                RequestClose();
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "حدث خطأ غير معروف", "SupplierPaymentEditorViewModel.SaveAsync", "[SupplierPaymentEditorViewModel.SaveAsync] Failed to save supplier payment.");
                await _dialogService.ShowErrorAsync("خطأ في حفظ السداد", ErrorMessage);
            }
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

    public override void Cleanup()
    {
        foreach (var item in UnpaidInvoices)
        {
            item.PropertyChanged -= OnAllocationItemChanged;
        }
        UnpaidInvoices.Clear();
        base.Cleanup();
    }
}
