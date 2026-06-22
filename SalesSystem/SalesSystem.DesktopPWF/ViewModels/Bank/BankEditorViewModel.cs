using System.Windows.Input;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Services.App.Toast;

namespace SalesSystem.DesktopPWF.ViewModels.Bank;

/// <summary>
/// Editor ViewModel for creating and updating banks.
/// Schema §4.4 Banks: Name, AccountId (int), AccountNumber, IBAN.
/// </summary>
public class BankEditorViewModel : ViewModelBase
{
    private readonly IBankApiService _bankService;
    private readonly IDialogService _dialogService;
    private readonly IToastNotificationService _toastService;

    private int _bankId;
    private int? _accountId;
    private string _name = string.Empty;
    private string? _accountNumber;
    private string? _iban;
    private bool _isActive = true;
    private bool _isEditMode;
    private string? _errorMessage;

    public BankEditorViewModel()
    {
        _bankService = App.GetService<IBankApiService>();
        _dialogService = App.GetService<IDialogService>();
        _toastService = App.GetService<IToastNotificationService>();
        SetDialogService(_dialogService);

        SaveCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(SaveOperationAsync, "جاري حفظ البنك...")));
        CancelCommand = new RelayCommand(Cancel);
    }

    #region Properties

    public string Title => IsEditMode ? "تعديل بنك" : "إضافة بنك جديد";

    public bool IsEditMode
    {
        get => _isEditMode;
        set => SetProperty(ref _isEditMode, value);
    }

    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value))
            {
                if (string.IsNullOrWhiteSpace(value))
                    AddError(nameof(Name), "اسم البنك مطلوب");
                else
                    ClearErrors(nameof(Name));
            }
        }
    }

    public int? AccountId
    {
        get => _accountId;
        set => SetProperty(ref _accountId, value);
    }

    public string? AccountNumber
    {
        get => _accountNumber;
        set => SetProperty(ref _accountNumber, value);
    }

    public string? Iban
    {
        get => _iban;
        set => SetProperty(ref _iban, value);
    }

    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
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

    public void LoadBank(BankDto bank)
    {
        _bankId = bank.Id;
        _accountId = bank.AccountId;
        _name = bank.Name;
        _accountNumber = bank.AccountNumber;
        _iban = bank.Iban;
        _isActive = bank.IsActive;
        _isEditMode = true;
    }

    private async Task<bool> ValidateAsync()
    {
        ClearAllErrors();

        if (string.IsNullOrWhiteSpace(Name))
            AddError(nameof(Name), "اسم البنك مطلوب");

        return await ValidateAllAsync();
    }

    private async Task SaveOperationAsync()
    {
        if (!await ValidateAsync()) return;

        ErrorMessage = null;

        if (IsEditMode)
        {
            var request = new UpdateBankRequest(
                Name: Name,
                AccountNumber: string.IsNullOrWhiteSpace(AccountNumber) ? null : AccountNumber.Trim(),
                Iban: string.IsNullOrWhiteSpace(Iban) ? null : Iban.Trim());
            var result = await _bankService.UpdateAsync(_bankId, request);

            if (result.IsSuccess)
            {
                _toastService.ShowSuccess("تم تحديث البنك بنجاح");
                RequestClose();
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشل في تحديث البنك", "BankEditorViewModel.SaveAsync");
                await _dialogService.ShowErrorAsync("خطأ في تحديث البنك", ErrorMessage!);
            }
        }
        else
        {
            var request = new CreateBankRequest(
                AccountId: _accountId,
                Name: Name,
                AccountNumber: string.IsNullOrWhiteSpace(AccountNumber) ? null : AccountNumber.Trim(),
                Iban: string.IsNullOrWhiteSpace(Iban) ? null : Iban.Trim());
            var result = await _bankService.CreateAsync(request);

            if (result.IsSuccess)
            {
                _toastService.ShowSuccess("تم إضافة البنك بنجاح");
                RequestClose();
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشل في إضافة البنك", "BankEditorViewModel.SaveAsync");
                await _dialogService.ShowErrorAsync("خطأ في إضافة البنك", ErrorMessage!);
            }
        }
    }

    private void Cancel()
    {
        RequestClose();
    }

    #endregion
}
