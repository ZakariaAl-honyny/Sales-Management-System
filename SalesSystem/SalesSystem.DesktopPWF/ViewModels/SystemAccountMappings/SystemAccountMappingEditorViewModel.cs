using System.Windows.Input;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;

namespace SalesSystem.DesktopPWF.ViewModels.SystemAccountMappings;

/// <summary>
/// Editor ViewModel for editing a system account mapping.
/// Allows changing the linked account for each mapping key.
/// RULE-059: Save button always enabled — validates on click with warning dialog.
/// </summary>
public class SystemAccountMappingEditorViewModel : ViewModelBase
{
    private readonly ISystemAccountMappingApiService _mappingApi;
    private readonly IDialogService _dialogService;
    private readonly SystemAccountMappingDto _dto;

    /// <summary>
    /// Raised when the mapping is saved successfully.
    /// </summary>
    public event Action? OnSaved;

    public SystemAccountMappingEditorViewModel(
        ISystemAccountMappingApiService mappingApi,
        IDialogService dialogService,
        SystemAccountMappingDto existing)
    {
        _mappingApi = mappingApi ?? throw new ArgumentNullException(nameof(mappingApi));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _dto = existing ?? throw new ArgumentNullException(nameof(existing));
        SetDialogService(dialogService);

        _accountId = existing.AccountId;
        _accountName = existing.AccountName ?? string.Empty;
        _accountCode = existing.AccountCode ?? string.Empty;

        SaveCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(SaveOperationAsync)));
        CancelCommand = new RelayCommand(RequestClose);
    }

    // ═══════════════════════════════════════════════════════════════
    // Commands
    // ═══════════════════════════════════════════════════════════════

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    // ═══════════════════════════════════════════════════════════════
    // Properties
    // ═══════════════════════════════════════════════════════════════

    public string MappingKeyDisplay => _dto.MappingKeyName ?? _dto.MappingKey.ToString();

    private int _accountId;
    public int AccountId
    {
        get => _accountId;
        set => SetProperty(ref _accountId, value);
    }

    private string _accountName = string.Empty;
    public string AccountName
    {
        get => _accountName;
        set => SetProperty(ref _accountName, value);
    }

    private string _accountCode = string.Empty;
    public string AccountCode
    {
        get => _accountCode;
        set => SetProperty(ref _accountCode, value);
    }

    private string? _errorMessage;
    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    // ═══════════════════════════════════════════════════════════════
    // Validation
    // ═══════════════════════════════════════════════════════════════

    private bool Validate()
    {
        ClearAllErrors();

        if (AccountId <= 0)
            AddError(nameof(AccountId), "يرجى اختيار حساب محاسبي");

        return !HasErrors;
    }

    // ═══════════════════════════════════════════════════════════════
    // Operations
    // ═══════════════════════════════════════════════════════════════

    private async Task SaveOperationAsync()
    {
        ErrorMessage = null;

        if (!Validate())
        {
            await ValidateAllAsync();
            return;
        }

        var request = new UpdateSystemAccountMappingRequest(AccountId);

        var result = await _mappingApi.UpdateAsync(_dto.Id, request);
        if (result.IsSuccess)
        {
            await _dialogService.ShowSuccessAsync("تم", "تم تحديث حساب النظام بنجاح");
            OnSaved?.Invoke();
            RequestClose();
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحديث حساب النظام", "SystemAccountMappingEditorViewModel.Save");
            await _dialogService.ShowErrorAsync("خطأ في تحديث الربط المحاسبي", ErrorMessage!);
        }
    }
}
