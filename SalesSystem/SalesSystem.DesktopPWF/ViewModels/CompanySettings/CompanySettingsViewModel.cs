using System.Windows.Input;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;

namespace SalesSystem.DesktopPWF.ViewModels.CompanySettings;

/// <summary>
/// ViewModel for editing company-wide settings (name, contact, logo, default currency).
/// Singleton row (Id=1) enforced at database level.
/// RULE-059: Save button always enabled — validates on click with warning dialog.
/// </summary>
public class CompanySettingsViewModel : ViewModelBase
{
    private readonly ICompanySettingsApiService _settingsApi;
    private readonly IDialogService _dialogService;
    private readonly ICurrencyApiService _currencyApi;

    public CompanySettingsViewModel()
        : this(
            App.GetService<ICompanySettingsApiService>(),
            App.GetService<IDialogService>(),
            App.GetService<ICurrencyApiService>())
    {
    }

    public CompanySettingsViewModel(
        ICompanySettingsApiService settingsApi,
        IDialogService dialogService,
        ICurrencyApiService currencyApi)
    {
        _settingsApi = settingsApi ?? throw new ArgumentNullException(nameof(settingsApi));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _currencyApi = currencyApi ?? throw new ArgumentNullException(nameof(currencyApi));
        SetDialogService(dialogService);

        LoadCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(LoadOperationAsync)));
        SaveCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(SaveOperationAsync)));
        CloseCommand = new RelayCommand(RequestClose);

        _ = LoadAsync();
    }

    // ═══════════════════════════════════════════════════════════════
    // Commands
    // ═══════════════════════════════════════════════════════════════

    public ICommand LoadCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CloseCommand { get; }

    // ═══════════════════════════════════════════════════════════════
    // Properties
    // ═══════════════════════════════════════════════════════════════

    private string _companyName = string.Empty;
    public string CompanyName
    {
        get => _companyName;
        set => SetProperty(ref _companyName, value);
    }

    private string? _phone;
    public string? Phone
    {
        get => _phone;
        set => SetProperty(ref _phone, value);
    }

    private string? _email;
    public string? Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    private string? _address;
    public string? Address
    {
        get => _address;
        set => SetProperty(ref _address, value);
    }

    private string? _taxNumber;
    public string? TaxNumber
    {
        get => _taxNumber;
        set => SetProperty(ref _taxNumber, value);
    }

    private string? _logoPath;
    public string? LogoPath
    {
        get => _logoPath;
        set => SetProperty(ref _logoPath, value);
    }

    private int _defaultCurrencyId;
    public int DefaultCurrencyId
    {
        get => _defaultCurrencyId;
        set => SetProperty(ref _defaultCurrencyId, value);
    }

    private string? _currencyName;
    public string? CurrencyName
    {
        get => _currencyName;
        set => SetProperty(ref _currencyName, value);
    }

    private List<CurrencyDto> _currencies = new();
    public List<CurrencyDto> Currencies
    {
        get => _currencies;
        set => SetProperty(ref _currencies, value);
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

        if (string.IsNullOrWhiteSpace(CompanyName))
            AddError(nameof(CompanyName), "اسم الشركة مطلوب");
        if (CompanyName?.Trim().Length > 200)
            AddError(nameof(CompanyName), "اسم الشركة لا يمكن أن يتجاوز 200 حرف");
        if (DefaultCurrencyId <= 0)
            AddError(nameof(DefaultCurrencyId), "العملة الافتراضية مطلوبة");
        if (Phone?.Length > 30)
            AddError(nameof(Phone), "رقم الهاتف لا يمكن أن يتجاوز 30 حرفاً");
        if (Email?.Length > 100)
            AddError(nameof(Email), "البريد الإلكتروني لا يمكن أن يتجاوز 100 حرف");
        if (Address?.Length > 300)
            AddError(nameof(Address), "العنوان لا يمكن أن يتجاوز 300 حرف");
        if (TaxNumber?.Length > 50)
            AddError(nameof(TaxNumber), "الرقم الضريبي لا يمكن أن يتجاوز 50 حرفاً");

        return !HasErrors;
    }

    // ═══════════════════════════════════════════════════════════════
    // Operations
    // ═══════════════════════════════════════════════════════════════

    private async Task LoadAsync()
    {
        await ExecuteAsync(LoadOperationAsync);
    }

    private async Task LoadOperationAsync()
    {
        ErrorMessage = null;

        // Load currencies for dropdown
        var currenciesResult = await _currencyApi.GetAllAsync(true);
        if (currenciesResult.IsSuccess && currenciesResult.Value != null)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Currencies = currenciesResult.Value;
            });
        }

        var result = await _settingsApi.GetAsync();
        if (result.IsSuccess && result.Value != null)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                CompanyName = result.Value.CompanyName;
                Phone = result.Value.Phone;
                Email = result.Value.Email;
                Address = result.Value.Address;
                TaxNumber = result.Value.TaxNumber;
                LogoPath = result.Value.LogoPath;
                DefaultCurrencyId = result.Value.DefaultCurrencyId;
                CurrencyName = result.Value.CurrencyName;
            });
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل إعدادات الشركة", "CompanySettingsViewModel.Load");
        }
    }

    private async Task SaveOperationAsync()
    {
        ErrorMessage = null;

        if (!Validate())
        {
            await ValidateAllAsync();
            return;
        }

        var request = new UpdateCompanySettingsRequest(
            CompanyName.Trim(),
            (short)DefaultCurrencyId,
            Phone?.Trim(),
            Email?.Trim(),
            Address?.Trim(),
            TaxNumber?.Trim(),
            LogoPath?.Trim());

        var result = await _settingsApi.UpdateAsync(request);
        if (result.IsSuccess)
        {
            await _dialogService.ShowSuccessAsync("تم", "تم حفظ إعدادات الشركة بنجاح");
            RequestClose();
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل حفظ إعدادات الشركة", "CompanySettingsViewModel.Save");
            LogSystemError($"Failed to save company settings: {result.Error}", "CompanySettingsViewModel.Save");
        }
    }
}
