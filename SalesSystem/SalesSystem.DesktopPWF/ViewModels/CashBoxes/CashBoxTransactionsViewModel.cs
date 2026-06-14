using SalesSystem.Contracts.Responses;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Services.App.Toast;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace SalesSystem.DesktopPWF.ViewModels.CashBoxes;

/// <summary>
/// LEGACY: CashBoxTransactionsViewModel was built around CashTransactionDto / DailyClosure
/// which were removed in the 65-table schema migration (ReceiptVoucher/PaymentVoucher).
/// Kept as a stub for future rewrite. No runtime functionality.
/// </summary>
public class CashBoxTransactionsViewModel : ViewModelBase
{
    private readonly ICashBoxApiService _cashBoxService;
    private readonly IDialogService _dialogService;
    private readonly IToastNotificationService _toastService;

    private string? _errorMessage;
    private bool _isEmpty;
    private int _cashBoxId;
    private string _cashBoxName = string.Empty;

    public CashBoxTransactionsViewModel()
        : this(
            App.GetService<ICashBoxApiService>(),
            App.GetService<IDialogService>(),
            App.GetService<IToastNotificationService>())
    {
    }

    public CashBoxTransactionsViewModel(
        ICashBoxApiService cashBoxService,
        IDialogService dialogService,
        IToastNotificationService? toastService = null)
    {
        _cashBoxService = cashBoxService ?? throw new ArgumentNullException(nameof(cashBoxService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _toastService = toastService ?? App.GetService<IToastNotificationService>();

        SetDialogService(dialogService);
    }

    #region Properties

    public int CashBoxId
    {
        get => _cashBoxId;
        set => SetProperty(ref _cashBoxId, value);
    }

    public string CashBoxName
    {
        get => _cashBoxName;
        set => SetProperty(ref _cashBoxName, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public bool IsEmpty
    {
        get => _isEmpty;
        private set => SetProperty(ref _isEmpty, value);
    }

    #endregion
}
