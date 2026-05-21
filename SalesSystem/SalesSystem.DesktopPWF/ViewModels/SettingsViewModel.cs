using SalesSystem.DesktopPWF.Messaging.Messages;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using Serilog;
using System.Collections.ObjectModel;
using System.Windows;

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

    public SettingsViewModel()
    {
        _settingsService = App.GetService<ISettingsApiService>();
        _backupService = App.GetService<IBackupApiService>();
        _dialogService = App.GetService<IDialogService>();

        LoadCommand = new AsyncRelayCommand(async _ => await LoadSettingsAsync());
        SaveCommand = new AsyncRelayCommand(async _ => await SaveSettingsAsync());
        CreateBackupCommand = new AsyncRelayCommand(async _ => await CreateBackupAsync());
        RestoreBackupCommand = new AsyncRelayCommand(async _ => await RestoreBackupAsync(), _ => !string.IsNullOrEmpty(SelectedBackup));

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

    #region Commands
    public AsyncRelayCommand LoadCommand { get; }
    public AsyncRelayCommand SaveCommand { get; }
    public AsyncRelayCommand CreateBackupCommand { get; }
    public AsyncRelayCommand RestoreBackupCommand { get; }
    #endregion

    #region Logic
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
