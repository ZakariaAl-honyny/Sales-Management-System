using SalesSystem.DesktopPWF.Messaging.Messages;
using SalesSystem.DesktopPWF.Services.App.Toast;
using System.Windows.Input;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Models;
using System.Collections.ObjectModel;

namespace SalesSystem.DesktopPWF.ViewModels.CustomerReceipt;

/// <summary>
/// ViewModel for Customer Receipt Editor (Create/Edit)
/// Enhanced with multi-invoice allocation grid for settling unpaid sales invoices.
/// </summary>
public class CustomerReceiptEditorViewModel : ViewModelBase
{
    private readonly ICustomerReceiptApiService _receiptService;
    private readonly ISalesInvoiceApiService _invoiceService;
    private readonly IEventBus _eventBus;
    private readonly IDialogService _dialogService;

    private int? _receiptId;
    private int _receiptNo;
    private int _customerId;
    private string? _customerName;
    private int _cashBoxId;
    private string? _cashBoxName;
    private int _currencyId;
    private decimal _amount;
    private DateTime _receiptDate = DateTime.Today;
    private string _notes = string.Empty;
    private byte _status = 1; // Draft
    private bool _isEditMode;
    private string? _errorMessage;
    private decimal _totalAllocated;

    public CustomerReceiptEditorViewModel()
        : this(App.GetService<ICustomerReceiptApiService>(), App.GetService<ISalesInvoiceApiService>(), App.GetService<IEventBus>(), App.GetService<IDialogService>())
    {
    }

    public CustomerReceiptEditorViewModel(ICustomerReceiptApiService receiptService, ISalesInvoiceApiService invoiceService, IEventBus eventBus, IDialogService dialogService)
    {
        _receiptService = receiptService;
        _invoiceService = invoiceService;
        _eventBus = eventBus;
        _dialogService = dialogService;
        SetDialogService(dialogService);

        SaveCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(SaveOperationAsync, "جاري حفظ سند القبض...")));
        CancelDialogCommand = new RelayCommand(Cancel);
        LoadUnpaidInvoicesCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(LoadUnpaidInvoicesAsync, "جاري تحميل الفواتير...")));
    }

    /// <summary>
    /// Constructor for editing an existing receipt
    /// </summary>
    public CustomerReceiptEditorViewModel(CustomerReceiptDto receipt)
        : this(App.GetService<ICustomerReceiptApiService>(), App.GetService<ISalesInvoiceApiService>(), App.GetService<IEventBus>(), App.GetService<IDialogService>())
    {
        _receiptId = receipt.Id;
        _receiptNo = receipt.ReceiptNo;
        _customerId = receipt.CustomerId;
        _customerName = receipt.CustomerName;
        _cashBoxId = receipt.CashBoxId;
        _cashBoxName = receipt.CashBoxName;
        _currencyId = receipt.CurrencyId;
        _amount = receipt.Amount;
        _receiptDate = receipt.ReceiptDate;
        _notes = receipt.Notes ?? string.Empty;
        _status = receipt.Status;
        _isEditMode = true;
    }

    #region Properties
    public string Title => IsEditMode ? "تعديل سند قبض" : "سند قبض جديد";

    public bool IsEditMode
    {
        get => _isEditMode;
        set => SetProperty(ref _isEditMode, value);
    }

    public int ReceiptNo
    {
        get => _receiptNo;
        set => SetProperty(ref _receiptNo, value);
    }

    public int CustomerId
    {
        get => _customerId;
        set
        {
            if (SetProperty(ref _customerId, value))
            {
                if (value <= 0)
                    AddError(nameof(CustomerId), "العميل مطلوب");
                else
                    ClearErrors(nameof(CustomerId));
            }
        }
    }

    public string? CustomerName
    {
        get => _customerName;
        set => SetProperty(ref _customerName, value);
    }

    public int CashBoxId
    {
        get => _cashBoxId;
        set
        {
            if (SetProperty(ref _cashBoxId, value))
            {
                if (value <= 0)
                    AddError(nameof(CashBoxId), "الصندوق مطلوب");
                else
                    ClearErrors(nameof(CashBoxId));
            }
        }
    }

    public string? CashBoxName
    {
        get => _cashBoxName;
        set => SetProperty(ref _cashBoxName, value);
    }

    public int CurrencyId
    {
        get => _currencyId;
        set
        {
            if (SetProperty(ref _currencyId, value))
            {
                if (value <= 0)
                    AddError(nameof(CurrencyId), "العملة مطلوبة");
                else
                    ClearErrors(nameof(CurrencyId));
            }
        }
    }

    public decimal Amount
    {
        get => _amount;
        set
        {
            if (SetProperty(ref _amount, value))
            {
                if (value <= 0)
                    AddError(nameof(Amount), "المبلغ يجب أن يكون أكبر من صفر");
                OnPropertyChanged(nameof(RemainingToAllocate));
            }
        }
    }

    public DateTime ReceiptDate
    {
        get => _receiptDate;
        set => SetProperty(ref _receiptDate, value);
    }

    public string Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }

    public byte Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    /// <summary>
    /// Unpaid invoices for the selected customer, available for allocation
    /// </summary>
    public ObservableCollection<UnpaidInvoiceAllocationItem> UnpaidInvoices { get; } = new();

    /// <summary>
    /// Total amount allocated across all invoices — updated when allocation amounts change
    /// </summary>
    public decimal TotalAllocated
    {
        get => _totalAllocated;
        private set => SetProperty(ref _totalAllocated, value);
    }

    /// <summary>
    /// Remaining receipt amount after deducting allocations
    /// </summary>
    public decimal RemainingToAllocate => Amount - TotalAllocated;

    /// <summary>
    /// True when at least one invoice has been selected for allocation
    /// </summary>
    public bool HasAllocations => TotalAllocated > 0;
    #endregion

    #region Commands
    public ICommand SaveCommand { get; }
    public ICommand CancelDialogCommand { get; }
    public ICommand LoadUnpaidInvoicesCommand { get; }
    #endregion

    #region Methods
    private async Task<bool> ValidateAsync()
    {
        ClearAllErrors();

        if (CustomerId <= 0)
            AddError(nameof(CustomerId), "العميل مطلوب");
        if (CashBoxId <= 0)
            AddError(nameof(CashBoxId), "الصندوق مطلوب");
        if (CurrencyId <= 0)
            AddError(nameof(CurrencyId), "العملة مطلوبة");
        if (Amount <= 0)
            AddError(nameof(Amount), "المبلغ يجب أن يكون أكبر من صفر");
        if (RemainingToAllocate < 0)
            AddError(nameof(Amount), "المبلغ المخصص أكبر من قيمة سند القبض");

        return await ValidateAllAsync();
    }

    /// <summary>
    /// Loads unpaid invoices for the selected customer.
    /// Called when CustomerId changes (from external selection).
    /// </summary>
    public async Task LoadUnpaidInvoicesAsync()
    {
        if (CustomerId <= 0)
        {
            UnpaidInvoices.Clear();
            TotalAllocated = 0;
            return;
        }

        var result = await _invoiceService.GetAllAsync(customerId: CustomerId, pageSize: 200);
        if (!result.IsSuccess || result.Value == null) return;

        var unpaid = result.Value
            .Where(inv => inv.Status == 2 && inv.DueAmount > 0) // Posted + has outstanding balance
            .Select(inv => new UnpaidInvoiceAllocationItem
            {
                InvoiceId = inv.Id,
                InvoiceNo = inv.InvoiceNo,
                InvoiceDate = inv.InvoiceDate.ToString("yyyy-MM-dd"),
                TotalAmount = inv.TotalAmount,
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

    private async Task SaveOperationAsync()
    {
        if (!await ValidateAsync()) return;

        ErrorMessage = null;

        var request = new CreateCustomerReceiptRequest(
            CustomerId,
            CashBoxId,
            CurrencyId,
            Amount,
            string.IsNullOrWhiteSpace(Notes) ? null : Notes);

        Result<CustomerReceiptDto> result;

        if (IsEditMode && _receiptId.HasValue)
        {
            // Receipts are created and then posted — no separate update API.
            // For editing unsaved drafts, we'd need to cancel + recreate.
            result = await _receiptService.CreateAsync(request);
        }
        else
        {
            result = await _receiptService.CreateAsync(request);
        }

        if (result.IsSuccess && result.Value != null)
        {
            var receiptId = result.Value.Id;

            // Create allocations for each invoice with an allocated amount
            foreach (var allocation in UnpaidInvoices.Where(a => a.AllocatedAmount > 0))
            {
                var appRequest = new AddReceiptApplicationRequest(
                    receiptId,
                    allocation.InvoiceId,
                    allocation.AllocatedAmount);

                await _receiptService.AddApplicationAsync(receiptId, appRequest);
            }

            _eventBus.Publish(new CustomerReceiptChangedMessage(receiptId));

            await _dialogService.ShowSuccessAsync("نجاح", IsEditMode ? "تم تحديث سند القبض بنجاح" : "تم إضافة سند القبض بنجاح");

            RequestClose();
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في حفظ سند القبض", "CustomerReceiptEditorViewModel.SaveAsync", "[CustomerReceiptEditorViewModel.SaveAsync] Failed to save receipt.");
            await _dialogService.ShowErrorAsync("خطأ في حفظ سند القبض", ErrorMessage!);
        }
    }

    private void Cancel()
    {
        RequestClose();
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
    #endregion
}
