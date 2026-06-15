using SalesSystem.Contracts.Requests;
using SalesSystem.DesktopPWF.Messaging.Messages;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Services.App.Toast;
using System.Windows.Input;

namespace SalesSystem.DesktopPWF.ViewModels.CashBoxes;

/// <summary>
/// Editor ViewModel for creating and updating cash boxes.
/// Schema §4.3: lightweight register with Name, BranchId, AccountId, Description.
/// Balance tracked on linked Account, not here.
/// </summary>
public class CashBoxEditorViewModel : ViewModelBase
{
    private readonly ICashBoxApiService _cashBoxService;
    private readonly IDialogService _dialogService;
    private readonly IEventBus _eventBus;
    private readonly IToastNotificationService _toastService;

    private int? _editingId;
    private string _name = string.Empty;
    private int? _accountId;
    private bool _isAccountAutoCreated = true;
    private string? _accountInfo;
    private short _branchId;
    private string? _description;
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
        SaveCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(SaveOperationAsync, "جاري حفظ الصندوق...")));
        CancelCommand = new RelayCommand(Cancel);
    }

    #region Properties

    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value))
            {
                ValidateField(() => !string.IsNullOrWhiteSpace(value), nameof(Name), "اسم الصندوق مطلوب");
            }
        }
    }

    public int? AccountId
    {
        get => _accountId;
        set => SetProperty(ref _accountId, value);
    }

    public bool IsAccountAutoCreated
    {
        get => _isAccountAutoCreated;
        set => SetProperty(ref _isAccountAutoCreated, value);
    }

    public string? AccountInfo
    {
        get => _accountInfo;
        set => SetProperty(ref _accountInfo, value);
    }

    public short BranchId
    {
        get => _branchId;
        set => SetProperty(ref _branchId, value);
    }

    public string? Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
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

    /// <summary>
    /// Loads cash box data into the editor for existing record editing.
    /// </summary>
    public void LoadForEdit(
        int id,
        string name,
        int? accountId,
        string? accountName,
        short branchId,
        string? description)
    {
        _editingId = id;
        Name = name;
        AccountId = accountId;
        IsAccountAutoCreated = accountId == null;
        AccountInfo = accountName ?? "سيتم إنشاء حساب تلقائي";
        BranchId = branchId;
        Description = description;
        IsEditMode = true;
    }

    private async Task<bool> ValidateAsync()
    {
        ClearAllErrors();

        if (string.IsNullOrWhiteSpace(Name))
            AddError(nameof(Name), "اسم الصندوق مطلوب");

        return await ValidateAllAsync();
    }

    private async Task SaveOperationAsync()
    {
        if (!await ValidateAsync()) return;

        if (_editingId.HasValue)
        {
            await UpdateCashBoxAsync();
        }
        else
        {
            await CreateCashBoxAsync();
        }
    }

    private async Task CreateCashBoxAsync()
    {
        // Determine AccountId: null = auto-create
        int? requestAccountId = IsAccountAutoCreated ? null : AccountId;

        var request = new CreateCashBoxRequest(
            Name.Trim(),
            requestAccountId,
            BranchId,
            string.IsNullOrWhiteSpace(Description) ? null : Description.Trim());

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
            var error = HandleFailure(result.Error ?? "فشل في إضافة الصندوق",
                "CashBoxEditorViewModel.SaveOperationAsync",
                "[CashBoxEditorViewModel.SaveOperationAsync] Failed to create cash box.");
            ErrorMessage = error;
            await _dialogService.ShowErrorAsync("خطأ في حفظ الصندوق", error);
        }
    }

    private async Task UpdateCashBoxAsync()
    {
        if (!_editingId.HasValue) return;

        int id = _editingId.Value;

        var request = new UpdateCashBoxRequest(
            Name.Trim(),
            BranchId,
            string.IsNullOrWhiteSpace(Description) ? null : Description.Trim());

        var result = await _cashBoxService.UpdateAsync(id, request);

        if (result.IsSuccess)
        {
            _eventBus.Publish(new CashBoxChangedMessage(id));
            _toastService.ShowSuccess("تم تحديث بيانات الصندوق بنجاح");
            RequestClose();
        }
        else
        {
            var error = HandleFailure(result.Error ?? "فشل في تحديث بيانات الصندوق",
                "CashBoxEditorViewModel.SaveOperationAsync",
                "[CashBoxEditorViewModel.SaveOperationAsync] Failed to update cash box.");
            ErrorMessage = error;
            await _dialogService.ShowErrorAsync("خطأ في تحديث الصندوق", error);
        }
    }

    private void Cancel()
    {
        RequestClose();
    }

    public override void Cleanup()
    {
        _editingId = null;
        IsEditMode = false;
        ErrorMessage = null;
        base.Cleanup();
    }

    #endregion
}
