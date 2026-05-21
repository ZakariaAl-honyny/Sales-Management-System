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
    private bool _isLoading;
    private string _statusMessage = string.Empty;
    private ObservableCollection<string> _backups = new();
    private string? _selectedBackup;

    // Print settings
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

        LoadCommand = new AsyncRelayCommand(async _ => await LoadSettingsAsync());
        SaveCommand = new AsyncRelayCommand(async _ => await SaveSettingsAsync());
        CreateBackupCommand = new AsyncRelayCommand(async _ => await CreateBackupAsync());
        RestoreBackupCommand = new AsyncRelayCommand(async _ => await RestoreBackupAsync(), _ => !string.IsNullOrEmpty(SelectedBackup));
        BrowseLogoCommand = new RelayCommand(_ => BrowseLogo());
        TestPrintCommand = new AsyncRelayCommand(async _ => await TestPrintAsync());

        _ = LoadSettingsAsync();
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

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
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
            Title = "اختر شعار المتجر",
            Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp",
            Multiselect = false
        };

        if (dialog.ShowDialog() == true)
        {
            LogoPath = dialog.FileName;
            StatusMessage = "✅ تم اختيار الشعار";
        }
    }

    private async Task TestPrintAsync()
    {
        IsLoading = true;
        StatusMessage = string.Empty;
        try
        {
            // Use the existing HttpClient — call the API test endpoint
            var httpClient = App.GetService<System.Net.Http.HttpClient>();
            var response = await httpClient.PostAsync("api/v1/print/test", null);
            if (response.IsSuccessStatusCode)
            {
                StatusMessage = "✅ تمت طباعة الاختبار بنجاح";
            }
            else
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                StatusMessage = "❌ فشلت طباعة الاختبار";
                await _dialogService.ShowErrorAsync("خطأ في الطباعة", errorBody);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = HandleException(ex, "SettingsViewModel.TestPrintAsync",
                "[SettingsViewModel.TestPrintAsync] Test print failed.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadSettingsAsync()
    {
        if (IsLoading) return;
        IsLoading = true;
        StatusMessage = string.Empty;

        try
        {
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

            // Load print settings
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
        catch (Exception ex)
        {
            StatusMessage = HandleException(ex, "SettingsViewModel.LoadSettingsAsync", "[SettingsViewModel.LoadSettingsAsync] Failed to load system settings.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task SaveSettingsAsync()
    {
        if (IsLoading) return;

        if (string.IsNullOrWhiteSpace(CompanyName))
        {
            var errors = new List<string>
            {
                "• اسم المنشأة مطلوب"
            };
            StatusMessage = "يرجى إدخال اسم المنشأة";
            string errorMsg = "يرجى إكمال البيانات الإلزامية التالية:\n\n" + string.Join("\n", errors);
            await _dialogService.ShowWarningAsync("بيانات غير مكتملة", errorMsg);
            return;
        }

        if (DefaultTaxRate < 0 || DefaultTaxRate > 100)
        {
            StatusMessage = "نسبة الضريبة يجب أن تكون بين 0 و 100";
            await _dialogService.ShowWarningAsync("خطأ في البيانات", StatusMessage);
            return;
        }

        IsLoading = true;
        StatusMessage = string.Empty;

        try
        {
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

                // Save print settings
                var printRequest = new UpdatePrintSettingsRequest(
                    ThermalPrinterName,
                    A4PrinterName,
                    LogoPath,
                    StoreTaxNumber,
                    PrintTaxRate);
                await _settingsService.UpdatePrintSettingsAsync(printRequest);

                StatusMessage = "✅ تم حفظ الإعدادات بنجاح";
                _ = Task.Delay(3000).ContinueWith(_ => StatusMessage = string.Empty);
            }
            else
            {
                StatusMessage = HandleFailure(result.Error ?? "فشل في حفظ الإعدادات", "SettingsViewModel.SaveSettingsAsync", "[SettingsViewModel.SaveSettingsAsync] Failed to update system settings.");
                await _dialogService.ShowErrorAsync("خطأ في الحفظ", StatusMessage);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = HandleException(ex, "SettingsViewModel.SaveSettingsAsync", "[SettingsViewModel.SaveSettingsAsync] Unexpected error during save.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task CreateBackupAsync()
    {
        if (IsLoading) return;
        IsLoading = true;
        StatusMessage = "جاري إنشاء نسخة احتياطية...";

        try
        {
            var result = await _backupService.CreateBackupAsync();
            if (result.IsSuccess)
            {
                StatusMessage = "✅ تم إنشاء النسخة الاحتياطية بنجاح";
                await RefreshBackupListAsync();
            }
            else
            {
                StatusMessage = result.Error ?? "فشل في إنشاء النسخة الاحتياطية";
                await _dialogService.ShowErrorAsync("خطأ في النسخ الاحتياطي", StatusMessage);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = HandleException(ex, "SettingsViewModel.CreateBackupAsync", "[SettingsViewModel.CreateBackupAsync] Unexpected error.");
        }
        finally
        {
            IsLoading = false;
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

    private async Task RestoreBackupAsync()
    {
        if (string.IsNullOrEmpty(SelectedBackup) || IsLoading) return;

        var confirm = await _dialogService.ShowConfirmationAsync("تأكيد استعادة النسخة الاحتياطية", $"⚠️ تنبيه: استعادة النسخة الاحتياطية '{SelectedBackup}' سيؤدي إلى استبدال قاعدة البيانات الحالية تماماً وإغلاق جميع الاتصالات النشطة.\n\nهل تريد الاستمرار؟");

        if (!confirm) return;

        IsLoading = true;
        StatusMessage = "جاري استعادة النسخة الاحتياطية... قد يستغرق هذا وقتاً.";

        try
        {
            var result = await _backupService.RestoreBackupAsync(SelectedBackup);
            if (result.IsSuccess)
            {
                StatusMessage = "✅ تم استعادة قاعدة البيانات بنجاح. سيتم إغلاق النظام لإعادة التحميل.";
                await _dialogService.ShowSuccessAsync("نجاح الاستعادة", StatusMessage);

                System.Windows.Application.Current.Shutdown();
            }
            else
            {
                StatusMessage = result.Error ?? "فشل في استعادة النسخة الاحتياطية";
                await _dialogService.ShowErrorAsync("خطأ في الاستعادة", StatusMessage);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = HandleException(ex, "SettingsViewModel.RestoreBackupAsync", "[SettingsViewModel.RestoreBackupAsync] Unexpected error.");
        }
        finally
        {
            IsLoading = false;
        }
    }
    #endregion
}
