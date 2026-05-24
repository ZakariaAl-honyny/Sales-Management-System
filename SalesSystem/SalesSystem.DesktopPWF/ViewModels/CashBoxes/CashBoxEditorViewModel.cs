using SalesSystem.Contracts.Requests;
using SalesSystem.DesktopPWF.Messaging.Messages;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Services.App.Toast;
using System.Windows.Input;

namespace SalesSystem.DesktopPWF.ViewModels.CashBoxes;

public class CashBoxEditorViewModel : ViewModelBase
{
    private readonly ICashBoxApiService _cashBoxService;
    private readonly IDialogService _dialogService;
    private readonly IEventBus _eventBus;
    private readonly IToastNotificationService _toastService;

    private string _boxName = string.Empty;
    private decimal _openingBalance;
    private bool _isEditMode;
    private string? _errorMessage;

    public CashBoxEditorViewModel()
        : this(
            App.GetService<ICashBoxApiService>(),
            App.GetService<IDialogService>(),
            App.GetService<IEventBus>(),
            App.GetService<IToastNotificationService>())
    {
    }

    public CashBoxEditorViewModel(
        ICashBoxApiService cashBoxService,
        IDialogService dialogService,
        IEventBus eventBus,
        IToastNotificationService? toastService = null)
    {
        _cashBoxService = cashBoxService ?? throw new ArgumentNullException(nameof(cashBoxService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _toastService = toastService ?? App.GetService<IToastNotificationService>();
        SetDialogService(_dialogService);

        InitializeCommands();
    }

    private void InitializeCommands()
    {
        SaveCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(SaveOperationAsync, "جاري حفظ الصندوق...")));
        CancelCommand = new RelayCommand(Cancel);
    }

    #region Properties

    public string BoxName
    {
        get => _boxName;
        set
        {
            if (SetProperty(ref _boxName, value))
            {
                ValidateField(() => !string.IsNullOrWhiteSpace(value), nameof(BoxName), "اسم الصندوق مطلوب");
            }
        }
    }

    public decimal OpeningBalance
    {
        get => _openingBalance;
        set
        {
            if (SetProperty(ref _openingBalance, value))
            {
                ValidateField(() => value >= 0, nameof(OpeningBalance), "الرصيد الافتتاحي لا يمكن أن يكون سالباً");
            }
        }
    }

    public bool IsEditMode
    {
        get => _isEditMode;
        private set => SetProperty(ref _isEditMode, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    #endregion

    #region Commands

    public ICommand SaveCommand { get; private set; } = null!;
    public ICommand CancelCommand { get; private set; } = null!;

    #endregion

    #region Methods

    public void LoadForEdit(string boxName, decimal currentBalance)
    {
        BoxName = boxName;
        OpeningBalance = currentBalance;
        IsEditMode = true;
    }

    private async Task<bool> ValidateAsync()
    {
        ClearAllErrors();

        if (string.IsNullOrWhiteSpace(BoxName))
            AddError(nameof(BoxName), "اسم الصندوق مطلوب");

        if (OpeningBalance < 0)
            AddError(nameof(OpeningBalance), "الرصيد الافتتاحي لا يمكن أن يكون سالباً");

        return await ValidateAllAsync();
    }

    private async Task SaveOperationAsync()
    {
        if (!await ValidateAsync()) return;

        var request = new CreateCashBoxRequest(
            BoxName.Trim(),
            OpeningBalance,
            null,
            null,
            null);

        var result = await _cashBoxService.CreateAsync(request);

        if (result.IsSuccess)
        {
            var id = result.Value?.Id ?? 0;
            _eventBus.Publish(new CashBoxChangedMessage(id));
            _toastService.ShowSuccess("تم إضافة الصندوق بنجاح");
            RequestClose();
        }
        else
        {
            var error = HandleFailure(result.Error ?? "فشل في إضافة الصندوق", "CashBoxEditorViewModel.SaveOperationAsync", "[CashBoxEditorViewModel.SaveOperationAsync] Failed to create cash box.");
            ErrorMessage = error;
            await _dialogService.ShowErrorAsync("خطأ في حفظ الصندوق", error);
        }
    }

    private void Cancel()
    {
        RequestClose();
    }

    #endregion
}
