using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Services.App.Toast;

namespace SalesSystem.DesktopPWF.ViewModels.CashBoxes;

/// <summary>
/// LEGACY: DailyClosureViewModel was built around DailyClosureDto / CashTransaction
/// which were removed in the 65-table schema migration (ReceiptVoucher/PaymentVoucher).
/// Kept as a stub for future rewrite. No runtime functionality.
/// </summary>
public class DailyClosureViewModel : ViewModelBase
{
    private readonly ICashBoxApiService _cashBoxService;
    private readonly IDialogService _dialogService;
    private readonly IToastNotificationService _toastService;

    private int _cashBoxId;
    private string _cashBoxName = string.Empty;
    private string? _errorMessage;
    private bool _isEmpty;

    public DailyClosureViewModel()
        : this(
            App.GetService<ICashBoxApiService>(),
            App.GetService<IDialogService>(),
            App.GetService<IToastNotificationService>())
    {
    }

    public DailyClosureViewModel(
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
