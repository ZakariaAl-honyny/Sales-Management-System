using SalesSystem.DesktopPWF.Messaging.Messages;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using Serilog;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace SalesSystem.DesktopPWF.ViewModels;

/// <summary>
/// Settings ViewModel - handles system settings management
/// </summary>
public class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsApiService _settingsService;
    private readonly IBackupApiService _backupService;
    private readonly IDialogService _dialogService;

    private string _companyName = string.Empty;
    private string? _taxNumber;
    private string? _phone;
    private string? _email;
    private string? _address;
    private decimal _defaultTaxRate;
    private string _invoicePrefix = "INV";
    private bool _enableStockAlerts = true;
    private bool _allowNegativeStock;
    private bool _autoUpdatePrices;
    private ObservableCollection<string> _backups = new();
    private string? _selectedBackup;

    private string _thermalPrinterName = string.Empty;
    private string _a4PrinterName = string.Empty;
    private string _logoPath = string.Empty;
    private string _storeTaxNumber = string.Empty;
    private decimal _printTaxRate;

    public SettingsViewModel()
    {
        _settingsService = App.GetService<ISettingsApiService>();
        _backupService = App.GetService<IBackupApiService>();
        _dialogService = App.GetService<IDialogService>();

        LoadCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(LoadSettingsOperationAsync, ex => StatusMessage = HandleException(ex, "SettingsViewModel.LoadSettingsAsync", "[SettingsViewModel.LoadSettingsAsync] Failed to load system settings."))));
        SaveCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(SaveSettingsOperationAsync, ex => StatusMessage = HandleException(ex, "SettingsViewModel.SaveSettingsAsync", "[SettingsViewModel.SaveSettingsAsync] Unexpected error during save."))));
        CreateBackupCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(CreateBackupOperationAsync, ex => StatusMessage = HandleException(ex, "SettingsViewModel.CreateBackupAsync", "[SettingsViewModel.CreateBackupAsync] Unexpected error."))));
        RestoreBackupCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(RestoreBackupOperationAsync, ex => StatusMessage = HandleException(ex, "SettingsViewModel.RestoreBackupAsync", "[SettingsViewModel.RestoreBackupAsync] Unexpected error."))), () => !string.IsNullOrEmpty(SelectedBackup));
        BrowseLogoCommand = new RelayCommand(_ => BrowseLogo());
        TestPrintCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(TestPrintOperationAsync, ex => StatusMessage = HandleException(ex, "SettingsViewModel.TestPrintAsync", "[SettingsViewModel.TestPrintAsync] Test print failed."))));

        _ = ExecuteAsync(LoadSettingsOperationAsync);
        _ = RefreshBackupListAsync();
    }

    #region Properties
    public string CompanyName
    {
        get => _companyName;
        set => SetProperty(ref _companyName, value);
    }

    public string? TaxNumber
    {
        get => _taxNumber;
        set => SetProperty(ref _taxNumber, value);
    }

    public string? Phone
    {
        get => _phone;
        set => SetProperty(ref _phone, value);
    }

    public string? Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public string? Address
    {
        get => _address;
        set => SetProperty(ref _address, value);
    }

    public decimal DefaultTaxRate
    {
        get => _defaultTaxRate;
        set => SetProperty(ref _defaultTaxRate, value);
    }

    public string InvoicePrefix
    {
        get => _invoicePrefix;
        set => SetProperty(ref _invoicePrefix, value);
    }

    public bool EnableStockAlerts
    {
        get => _enableStockAlerts;
        set => SetProperty(ref _enableStockAlerts, value);
    }

    public bool AllowNegativeStock
    {
        get => _allowNegativeStock;
        set => SetProperty(ref _allowNegativeStock, value);
    }

    public bool AutoUpdatePrices
    {
        get => _autoUpdatePrices;
        set => SetProperty(ref _autoUpdatePrices, value);
    }

    public ObservableCollection<string> Backups
    {
        get => _backups;
        set => SetProperty(ref _backups, value);
    }

    public string? SelectedBackup
    {
        get => _selectedBackup;
        set
        {
            if (SetProperty(ref _selectedBackup, value))
            {
                RestoreBackupCommand.RaiseCanExecuteChanged();
            }
        }
    }
    #endregion

    #region Print Properties
    public string ThermalPrinterName
    {
        get => _thermalPrinterName;
        set => SetProperty(ref _thermalPrinterName, value);
    }

    public string A4PrinterName
    {
        get => _a4PrinterName;
        set => SetProperty(ref _a4PrinterName, value);
    }

    public string LogoPath
    {
        get => _logoPath;
        set => SetProperty(ref _logoPath, value);
    }

    public string StoreTaxNumber
    {
        get => _storeTaxNumber;
        set => SetProperty(ref _storeTaxNumber, value);
    }

    public decimal PrintTaxRate
    {
        get => _printTaxRate;
        set => SetProperty(ref _printTaxRate, value);
    }

    public List<string> InstalledPrinters { get; } =
        System.Drawing.Printing.PrinterSettings.InstalledPrinters
            .Cast<string>()
            .OrderBy(p => p)
            .ToList();
    #endregion

    #region Commands
    public AsyncRelayCommand LoadCommand { get; }
    public AsyncRelayCommand SaveCommand { get; }
    public AsyncRelayCommand CreateBackupCommand { get; }
    public AsyncRelayCommand RestoreBackupCommand { get; }
    public ICommand BrowseLogoCommand { get; }
    public ICommand TestPrintCommand { get; }
    #endregion

    #region Logic

    private void BrowseLogo()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "ط§ط®طھط± ط´ط¹ط§ط± ط§ظ„ظ…طھط¬ط±",
            Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp",
            Multiselect = false
        };

        if (dialog.ShowDialog() == true)
        {
            LogoPath = dialog.FileName;
            StatusMessage = "âœ… طھظ… ط§ط®طھظٹط§ط± ط§ظ„ط´ط¹ط§ط±";
        }
    }

    private async Task TestPrintOperationAsync()
    {
        StatusMessage = string.Empty;

        var httpClient = App.GetService<System.Net.Http.HttpClient>();
        var response = await httpClient.PostAsync("api/v1/print/test", null);
        if (response.IsSuccessStatusCode)
        {
            StatusMessage = "âœ… طھظ…طھ ط·ط¨ط§ط¹ط© ط§ظ„ط§ط®طھط¨ط§ط± ط¨ظ†ط¬ط§ط­";
        }
        else
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            Serilog.Log.Warning("Print test failed. Status: {StatusCode}, Body: {Body}", response.StatusCode, errorBody);
            StatusMessage = "â‌Œ ظپط´ظ„طھ ط·ط¨ط§ط¹ط© ط§ظ„ط§ط®طھط¨ط§ط±";
            await _dialogService.ShowErrorAsync("ط®ط·ط£ ظپظٹ ط§ظ„ط·ط¨ط§ط¹ط©", "ظپط´ظ„ ط§ط®طھط¨ط§ط± ط§ظ„ط·ط¨ط§ط¹ط©. ظٹط±ط¬ظ‰ ط§ظ„طھط­ظ‚ظ‚ ظ…ظ† ط¥ط¹ط¯ط§ط¯ط§طھ ط§ظ„ط·ط§ط¨ط¹ط© ظˆط§ظ„ظ…ط­ط§ظˆظ„ط© ظ…ط±ط© ط£ط®ط±ظ‰.");
        }
    }

    private async Task LoadSettingsOperationAsync()
    {
        StatusMessage = string.Empty;

        var result = await _settingsService.GetSettingsAsync();
        if (result.IsSuccess && result.Value != null)
        {
            var s = result.Value;
            CompanyName = s.StoreName;
            Phone = s.Phone;
            Address = s.Address;
            TaxNumber = s.TaxNumber;
            Email = s.Email;
            DefaultTaxRate = s.DefaultTaxRate;
            EnableStockAlerts = s.EnableStockAlerts;
            AllowNegativeStock = s.AllowNegativeStock;
            AutoUpdatePrices = s.AutoUpdatePrices;
            InvoicePrefix = s.InvoicePrefix;
        }

        try
        {
            var printResult = await _settingsService.GetPrintSettingsAsync();
            if (printResult.IsSuccess && printResult.Value != null)
            {
                var p = printResult.Value;
                ThermalPrinterName = p.ThermalPrinterName;
                A4PrinterName = p.A4PrinterName;
                LogoPath = p.LogoPath;
                StoreTaxNumber = p.StoreTaxNumber;
                PrintTaxRate = p.TaxRate;
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "Failed to load print settings");
        }
    }

    private async Task SaveSettingsOperationAsync()
    {
        if (string.IsNullOrWhiteSpace(CompanyName))
        {
            var errors = new List<string>
            {
                "â€¢ ط§ط³ظ… ط§ظ„ظ…ظ†ط´ط£ط© ظ…ط·ظ„ظˆط¨"
            };
            StatusMessage = "ظٹط±ط¬ظ‰ ط¥ط¯ط®ط§ظ„ ط§ط³ظ… ط§ظ„ظ…ظ†ط´ط£ط©";
            string errorMsg = "ظٹط±ط¬ظ‰ ط¥ظƒظ…ط§ظ„ ط§ظ„ط¨ظٹط§ظ†ط§طھ ط§ظ„ط¥ظ„ط²ط§ظ…ظٹط© ط§ظ„طھط§ظ„ظٹط©:\n\n" + string.Join("\n", errors);
            await _dialogService.ShowWarningAsync("ط¨ظٹط§ظ†ط§طھ ط؛ظٹط± ظ…ظƒطھظ…ظ„ط©", errorMsg);
            return;
        }

        if (DefaultTaxRate < 0 || DefaultTaxRate > 100)
        {
            StatusMessage = "ظ†ط³ط¨ط© ط§ظ„ط¶ط±ظٹط¨ط© ظٹط¬ط¨ ط£ظ† طھظƒظˆظ† ط¨ظٹظ† 0 ظˆ 100";
            await _dialogService.ShowWarningAsync("ط®ط·ط£ ظپظٹ ط§ظ„ط¨ظٹط§ظ†ط§طھ", StatusMessage);
            return;
        }

        StatusMessage = string.Empty;

        var request = new UpdateSettingsRequest(
            CompanyName,
            Address,
            Phone,
            Email,
            null,
            "SAR",
            DefaultTaxRate,
            DefaultTaxRate > 0,
            TaxNumber,
            EnableStockAlerts,
            AllowNegativeStock,
            AutoUpdatePrices,
            InvoicePrefix
        );

        var result = await _settingsService.UpdateSettingsAsync(request);
        if (result.IsSuccess)
        {
            _settingsService.RefreshCache();

            var printRequest = new UpdatePrintSettingsRequest(
                ThermalPrinterName,
                A4PrinterName,
                LogoPath,
                StoreTaxNumber,
                PrintTaxRate);
            await _settingsService.UpdatePrintSettingsAsync(printRequest);

            StatusMessage = "âœ… طھظ… ط­ظپط¸ ط§ظ„ط¥ط¹ط¯ط§ط¯ط§طھ ط¨ظ†ط¬ط§ط­";
            _ = Task.Delay(3000).ContinueWith(_ => StatusMessage = string.Empty);
        }
        else
        {
            StatusMessage = HandleFailure(result.Error ?? "ظپط´ظ„ ظپظٹ ط­ظپط¸ ط§ظ„ط¥ط¹ط¯ط§ط¯ط§طھ", "SettingsViewModel.SaveSettingsAsync", "[SettingsViewModel.SaveSettingsAsync] Failed to update system settings.");
            await _dialogService.ShowErrorAsync("ط®ط·ط£ ظپظٹ ط§ظ„ط­ظپط¸", StatusMessage);
        }
    }

    private async Task CreateBackupOperationAsync()
    {
        StatusMessage = "ط¬ط§ط±ظٹ ط¥ظ†ط´ط§ط، ظ†ط³ط®ط© ط§ط­طھظٹط§ط·ظٹط©...";

        var result = await _backupService.CreateBackupAsync();
        if (result.IsSuccess)
        {
            StatusMessage = "âœ… طھظ… ط¥ظ†ط´ط§ط، ط§ظ„ظ†ط³ط®ط© ط§ظ„ط§ط­طھظٹط§ط·ظٹط© ط¨ظ†ط¬ط§ط­";
            await RefreshBackupListAsync();
        }
        else
        {
            StatusMessage = result.Error ?? "ظپط´ظ„ ظپظٹ ط¥ظ†ط´ط§ط، ط§ظ„ظ†ط³ط®ط© ط§ظ„ط§ط­طھظٹط§ط·ظٹط©";
            await _dialogService.ShowErrorAsync("ط®ط·ط£ ظپظٹ ط§ظ„ظ†ط³ط® ط§ظ„ط§ط­طھظٹط§ط·ظٹ", StatusMessage);
        }
    }

    private async Task RefreshBackupListAsync()
    {
        try
        {
            var result = await _backupService.GetBackupListAsync();
            if (result.IsSuccess && result.Value != null)
            {
                Backups = new ObservableCollection<string>(result.Value);
            }
        }
        catch (Exception ex)
        {
            HandleException(ex, "SettingsViewModel.RefreshBackupListAsync", "[SettingsViewModel.RefreshBackupListAsync] Failed to refresh backup list.");
        }
    }

    private async Task RestoreBackupOperationAsync()
    {
        if (string.IsNullOrEmpty(SelectedBackup)) return;

        var confirm = await _dialogService.ShowConfirmationAsync("طھط£ظƒظٹط¯ ط§ط³طھط¹ط§ط¯ط© ط§ظ„ظ†ط³ط®ط© ط§ظ„ط§ط­طھظٹط§ط·ظٹط©", $"âڑ ï¸ڈ طھظ†ط¨ظٹظ‡: ط§ط³طھط¹ط§ط¯ط© ط§ظ„ظ†ط³ط®ط© ط§ظ„ط§ط­طھظٹط§ط·ظٹط© '{SelectedBackup}' ط³ظٹط¤ط¯ظٹ ط¥ظ„ظ‰ ط§ط³طھط¨ط¯ط§ظ„ ظ‚ط§ط¹ط¯ط© ط§ظ„ط¨ظٹط§ظ†ط§طھ ط§ظ„ط­ط§ظ„ظٹط© طھظ…ط§ظ…ط§ظ‹ ظˆط¥ط؛ظ„ط§ظ‚ ط¬ظ…ظٹط¹ ط§ظ„ط§طھطµط§ظ„ط§طھ ط§ظ„ظ†ط´ط·ط©.\n\nظ‡ظ„ طھط±ظٹط¯ ط§ظ„ط§ط³طھظ…ط±ط§ط±طں");

        if (!confirm) return;

        StatusMessage = "ط¬ط§ط±ظٹ ط§ط³طھط¹ط§ط¯ط© ط§ظ„ظ†ط³ط®ط© ط§ظ„ط§ط­طھظٹط§ط·ظٹط©... ظ‚ط¯ ظٹط³طھط؛ط±ظ‚ ظ‡ط°ط§ ظˆظ‚طھط§ظ‹.";

        var result = await _backupService.RestoreBackupAsync(SelectedBackup);
        if (result.IsSuccess)
        {
            StatusMessage = "âœ… طھظ… ط§ط³طھط¹ط§ط¯ط© ظ‚ط§ط¹ط¯ط© ط§ظ„ط¨ظٹط§ظ†ط§طھ ط¨ظ†ط¬ط§ط­. ط³ظٹطھظ… ط¥ط؛ظ„ط§ظ‚ ط§ظ„ظ†ط¸ط§ظ… ظ„ط¥ط¹ط§ط¯ط© ط§ظ„طھط­ظ…ظٹظ„.";
            await _dialogService.ShowSuccessAsync("ظ†ط¬ط§ط­ ط§ظ„ط§ط³طھط¹ط§ط¯ط©", StatusMessage);

            System.Windows.Application.Current.Shutdown();
        }
        else
        {
            StatusMessage = result.Error ?? "ظپط´ظ„ ظپظٹ ط§ط³طھط¹ط§ط¯ط© ط§ظ„ظ†ط³ط®ط© ط§ظ„ط§ط­طھظٹط§ط·ظٹط©";
            await _dialogService.ShowErrorAsync("ط®ط·ط£ ظپظٹ ط§ظ„ط§ط³طھط¹ط§ط¯ط©", StatusMessage);
        }
    }
    #endregion
}
