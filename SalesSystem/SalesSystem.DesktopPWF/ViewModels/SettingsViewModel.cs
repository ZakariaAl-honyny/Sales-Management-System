using SalesSystem.DesktopPWF.Messaging.Messages;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
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
    private readonly IPrintApiService _printService;

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

    private int _costingMethod = 1; // Default WeightedAverage
    private string _thermalPrinterName = string.Empty;
    private string _a4PrinterName = string.Empty;
    private string _logoPath = string.Empty;
    private string _storeTaxNumber = string.Empty;
    private decimal _printTaxRate;
    private string _receiptHeader = string.Empty;
    private string _receiptFooter = string.Empty;
    private int _escPosCodePage = 22;
    private bool _autoPrintOnPost;
    private string _backupPath = string.Empty;
    private string _backupScheduleTime = "02:00";
    private int _backupRetentionDays = 30;
    private string _updateServerUrl = string.Empty;

    public SettingsViewModel()
    {
        _settingsService = App.GetService<ISettingsApiService>();
        _backupService = App.GetService<IBackupApiService>();
        _printService = App.GetService<IPrintApiService>();
        SetDialogService(App.GetService<IDialogService>());

        LoadCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(LoadSettingsOperationAsync, ex => StatusMessage = HandleException(ex, "SettingsViewModel.LoadSettingsAsync", "[SettingsViewModel.LoadSettingsAsync] Failed to load system settings."))));
        SaveCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(SaveSettingsOperationAsync, ex => StatusMessage = HandleException(ex, "SettingsViewModel.SaveSettingsAsync", "[SettingsViewModel.SaveSettingsAsync] Unexpected error during save."))));
        CreateBackupCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(CreateBackupOperationAsync, ex => StatusMessage = HandleException(ex, "SettingsViewModel.CreateBackupAsync", "[SettingsViewModel.CreateBackupAsync] Unexpected error."))));
        RestoreBackupCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(RestoreBackupOperationAsync, ex => StatusMessage = HandleException(ex, "SettingsViewModel.RestoreBackupAsync", "[SettingsViewModel.RestoreBackupAsync] Unexpected error."))), () => !string.IsNullOrEmpty(SelectedBackup));
        BrowseLogoCommand = new RelayCommand(_ => BrowseLogo());
        TestPrintCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(TestPrintOperationAsync, ex => StatusMessage = HandleException(ex, "SettingsViewModel.TestPrintAsync", "[SettingsViewModel.TestPrintAsync] Test print failed."))));
        BrowseBackupPathCommand = new RelayCommand(_ => BrowseBackupPath());

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

    #region Costing Method Properties
    public int CostingMethod
    {
        get => _costingMethod;
        set => SetProperty(ref _costingMethod, value);
    }

    public bool IsWeightedAverageSelected
    {
        get => _costingMethod == 1;
        set { if (value) CostingMethod = 1; }
    }

    public bool IsLastPriceSelected
    {
        get => _costingMethod == 2;
        set { if (value) CostingMethod = 2; }
    }

    public bool IsSupplierPriceSelected
    {
        get => _costingMethod == 3;
        set { if (value) CostingMethod = 3; }
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

    public string ReceiptHeader
    {
        get => _receiptHeader;
        set
        {
            if (SetProperty(ref _receiptHeader, value))
            {
                ValidateField(() => value?.Length <= 200, nameof(ReceiptHeader), "رأس الإيصال يجب ألا يتجاوز 200 حرف");
            }
        }
    }

    public string ReceiptFooter
    {
        get => _receiptFooter;
        set
        {
            if (SetProperty(ref _receiptFooter, value))
            {
                ValidateField(() => value?.Length <= 200, nameof(ReceiptFooter), "تذييل الإيصال يجب ألا يتجاوز 200 حرف");
            }
        }
    }

    public int EscPosCodePage
    {
        get => _escPosCodePage;
        set
        {
            if (SetProperty(ref _escPosCodePage, value))
            {
                ValidateField(() => value >= 0 && value <= 255, nameof(EscPosCodePage), "كود صفحة ESC/POS يجب أن يكون بين 0 و 255");
            }
        }
    }

    public bool AutoPrintOnPost
    {
        get => _autoPrintOnPost;
        set => SetProperty(ref _autoPrintOnPost, value);
    }

    public List<string> InstalledPrinters { get; } =
        System.Drawing.Printing.PrinterSettings.InstalledPrinters
            .Cast<string>()
            .OrderBy(p => p)
            .ToList();
    #endregion

    #region Backup & Update Properties
    public string BackupPath
    {
        get => _backupPath;
        set => SetProperty(ref _backupPath, value);
    }

    public string BackupScheduleTime
    {
        get => _backupScheduleTime;
        set => SetProperty(ref _backupScheduleTime, value);
    }

    public int BackupRetentionDays
    {
        get => _backupRetentionDays;
        set => SetProperty(ref _backupRetentionDays, value);
    }

    public string UpdateServerUrl
    {
        get => _updateServerUrl;
        set => SetProperty(ref _updateServerUrl, value);
    }
    #endregion

    #region Commands
    public AsyncRelayCommand LoadCommand { get; }
    public AsyncRelayCommand SaveCommand { get; }
    public AsyncRelayCommand CreateBackupCommand { get; }
    public AsyncRelayCommand RestoreBackupCommand { get; }
    public ICommand BrowseLogoCommand { get; }
    public ICommand TestPrintCommand { get; }
    public ICommand BrowseBackupPathCommand { get; }
    #endregion

    #region Logic

    private void BrowseLogo()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "اختيار شعار المتجر",
            Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp",
            Multiselect = false
        };

        if (dialog.ShowDialog() == true)
        {
            LogoPath = dialog.FileName;
            StatusMessage = "✅ تم اختيار الشعار";
        }
    }

    private void BrowseBackupPath()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "اختيار مجلد النسخ الاحتياطي",
            Multiselect = false
        };

        if (dialog.ShowDialog() == true)
        {
            BackupPath = dialog.FolderName;
            StatusMessage = "✅ تم اختيار مجلد النسخ الاحتياطي";
        }
    }

    private async Task TestPrintOperationAsync()
    {
        StatusMessage = string.Empty;

        var result = await _printService.TestPrintAsync();
        if (result.IsSuccess)
        {
            StatusMessage = "✅ تمت طباعة الاختبار بنجاح";
        }
        else
        {
            LogSystemError($"Print test failed: {result.Error}", "SettingsViewModel.TestPrintOperationAsync");
            StatusMessage = "فشلت طباعة الاختبار";
            await DialogService!.ShowErrorAsync("خطأ في الطباعة", "فشل اختبار الطباعة. يرجى التحقق من إعدادات الطابعة والمحاولة مرة أخرى.");
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
            CostingMethod = s.CostingMethod;
            BackupPath = s.BackupPath ?? string.Empty;
            BackupScheduleTime = s.BackupScheduleTime ?? "02:00";
            BackupRetentionDays = s.BackupRetentionDays;
            UpdateServerUrl = s.UpdateServerUrl ?? string.Empty;
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
                ReceiptHeader = p.ReceiptHeader ?? string.Empty;
                ReceiptFooter = p.ReceiptFooter ?? string.Empty;
                EscPosCodePage = p.EscPosCodePage;
                AutoPrintOnPost = p.AutoPrintOnPost;
            }
        }
        catch (Exception ex)
        {
            LogSystemError("Failed to load print settings", "SettingsViewModel.LoadSettingsOperationAsync", ex);
        }
    }

    private async Task SaveSettingsOperationAsync()
    {
        if (string.IsNullOrWhiteSpace(CompanyName))
        {
            var errors = new List<string>
            {
                "• اسم المنشأة مطلوب"
            };
            StatusMessage = "يرجى إدخال اسم المنشأة";
            string errorMsg = "يرجى إكمال البيانات الإلزامية التالية:\n\n" + string.Join("\n", errors);
            await DialogService!.ShowWarningAsync("بيانات غير مكتملة", errorMsg);
            return;
        }

        if (DefaultTaxRate < 0 || DefaultTaxRate > 100)
        {
            StatusMessage = "نسبة الضريبة يجب أن تكون بين 0 و 100";
            await DialogService!.ShowWarningAsync("خطأ في البيانات", StatusMessage);
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
            InvoicePrefix,
            CostingMethod,
            BackupPath,
            BackupScheduleTime,
            BackupRetentionDays,
            UpdateServerUrl
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
                PrintTaxRate,
                AutoPrintOnPost,
                ReceiptHeader,
                ReceiptFooter,
                EscPosCodePage);
            var printResult = await _settingsService.UpdatePrintSettingsAsync(printRequest);
            if (!printResult.IsSuccess)
            {
                StatusMessage = HandleFailure(printResult.Error ?? "فشل في حفظ إعدادات الطباعة",
                    "SettingsViewModel.SaveSettingsAsync");
                await DialogService!.ShowErrorAsync("خطأ في حفظ إعدادات الطباعة", StatusMessage);
                return;
            }

            StatusMessage = "✅ تم حفظ الإعدادات بنجاح";
            _ = Task.Delay(3000).ContinueWith(_ => StatusMessage = string.Empty);
        }
        else
        {
            StatusMessage = HandleFailure(result.Error ?? "فشل في حفظ الإعدادات", "SettingsViewModel.SaveSettingsAsync", "[SettingsViewModel.SaveSettingsAsync] Failed to update system settings.");
            await DialogService!.ShowErrorAsync("خطأ في الحفظ", StatusMessage);
        }
    }

    private async Task CreateBackupOperationAsync()
    {
        StatusMessage = "جاري إنشاء نسخة احتياطية...";

        var result = await _backupService.CreateBackupAsync();
        if (result.IsSuccess)
        {
            StatusMessage = "✅ تم إنشاء النسخة الاحتياطية بنجاح";
            await RefreshBackupListAsync();
        }
        else
        {
            StatusMessage = result.Error ?? "فشل في إنشاء النسخة الاحتياطية";
            await DialogService!.ShowErrorAsync("خطأ في النسخ الاحتياطي", StatusMessage);
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

        var confirm = await DialogService!.ShowConfirmationAsync("تأكيد استعادة النسخة الاحتياطية", $"⚠️ تنبيه: استعادة النسخة الاحتياطية '{SelectedBackup}' سيؤدي إلى استبدال قاعدة البيانات الحالية تماماً وإغلاق جميع الاتصالات النشطة.\n\nهل تريد الاستمرار؟");

        if (!confirm) return;

        StatusMessage = "جاري استعادة النسخة الاحتياطية... قد يستغرق هذا وقتاً.";

        var result = await _backupService.RestoreBackupAsync(SelectedBackup);
        if (result.IsSuccess)
        {
            StatusMessage = "✅ تم استعادة قاعدة البيانات بنجاح. سيتم إغلاق النظام لإعادة التحميل.";
            await DialogService!.ShowSuccessAsync("نجاح الاستعادة", StatusMessage);

            System.Windows.Application.Current.Shutdown();
        }
        else
        {
            StatusMessage = result.Error ?? "فشل في استعادة النسخة الاحتياطية";
            await DialogService!.ShowErrorAsync("خطأ في الاستعادة", StatusMessage);
        }
    }
    #endregion
}
