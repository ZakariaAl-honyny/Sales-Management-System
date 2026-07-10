using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Windows.Input;
using SalesSystem.Contracts.Requests;
using SalesSystem.DesktopPWF.Messaging.Messages;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Services.App.Toast;

namespace SalesSystem.DesktopPWF.ViewModels.Settings;

/// <summary>
/// ViewModel for managing database backups — create, list, and restore.
/// Also handles backup configuration settings (BackupPath, Schedule, Retention, Update URL).
/// </summary>
public class BackupViewModel : ViewModelBase
{
    private readonly IBackupApiService _backupService;
    private readonly ISettingsApiService _settingsApiService;
    private readonly IDialogService _dialogService;
    private readonly IToastNotificationService _toastService;
    private readonly IEventBus _eventBus;

    private ObservableCollection<BackupFileItem> _backupFiles = new();
    private BackupFileItem? _selectedBackupFile;

    private string _backupPathFromSettings = string.Empty;
    private string _backupScheduleTime = "02:00";
    private int _backupRetentionDays = 30;
    private string _updateServerUrl = string.Empty;

    public BackupViewModel(
        IBackupApiService backupService,
        ISettingsApiService settingsApiService,
        IDialogService dialogService,
        IToastNotificationService toastService,
        IEventBus eventBus)
    {
        _backupService = backupService;
        _settingsApiService = settingsApiService;
        _dialogService = dialogService;
        _toastService = toastService;
        _eventBus = eventBus;
        SetDialogService(_dialogService);

        CreateBackupCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(CreateBackupOperationAsync)));
        RestoreBackupCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(RestoreBackupOperationAsync)));
        SaveBackupSettingsCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(SaveBackupSettingsOperationAsync)));
        BrowseBackupPathCommand = new RelayCommand(_ => BrowseBackupPath());

        _ = ExecuteAsync(LoadBackupsOperationAsync);
        _ = ExecuteAsync(LoadBackupSettingsOperationAsync);
    }

    #region Properties

    public ObservableCollection<BackupFileItem> BackupFiles
    {
        get => _backupFiles;
        set => SetProperty(ref _backupFiles, value);
    }

    public BackupFileItem? SelectedBackupFile
    {
        get => _selectedBackupFile;
        set => SetProperty(ref _selectedBackupFile, value);
    }

    public string BackupPathFromSettings
    {
        get => _backupPathFromSettings;
        set => SetProperty(ref _backupPathFromSettings, value);
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

    public AsyncRelayCommand CreateBackupCommand { get; }
    public AsyncRelayCommand RestoreBackupCommand { get; }
    public AsyncRelayCommand SaveBackupSettingsCommand { get; }
    public ICommand BrowseBackupPathCommand { get; }

    #endregion

    #region Operations

    private async Task LoadBackupSettingsOperationAsync()
    {
        StatusMessage = string.Empty;

        var result = await _settingsApiService.GetSettingsAsync();
        if (result.IsSuccess && result.Value != null)
        {
            var s = result.Value;
            BackupPathFromSettings = s.BackupPath ?? string.Empty;
            BackupScheduleTime = s.BackupScheduleTime ?? "02:00";
            BackupRetentionDays = s.BackupRetentionDays;
            UpdateServerUrl = s.UpdateServerUrl ?? string.Empty;
        }
        else
        {
            LogSystemError($"Failed to load backup settings: {result.Error}", "BackupViewModel.LoadBackupSettingsAsync");
            StatusMessage = "فشل في تحميل إعدادات النسخ الاحتياطي";
        }
    }

    private async Task SaveBackupSettingsOperationAsync()
    {
        StatusMessage = string.Empty;

        // Load current settings first to preserve non-backup fields
        var currentResult = await _settingsApiService.GetSettingsAsync();
        if (!currentResult.IsSuccess || currentResult.Value == null)
        {
            StatusMessage = HandleFailure(currentResult.Error ?? "فشل في تحميل الإعدادات الحالية",
                "BackupViewModel.SaveBackupSettingsAsync");
            return;
        }

        var current = currentResult.Value;

        var request = new UpdateSettingsRequest(
            current.StoreName,
            current.Address,
            current.Phone,
            current.Email,
            current.LogoPath,
            current.DefaultTaxRate,
            current.IsTaxEnabled,
            current.TaxNumber,
            current.EnableStockAlerts,
            current.AllowNegativeStock,
            current.InvoicePrefix,
            BackupPath: BackupPathFromSettings,
            BackupScheduleTime: BackupScheduleTime,
            BackupRetentionDays: BackupRetentionDays,
            UpdateServerUrl: UpdateServerUrl,
            SignatureUrl: current.SignaturePath);

        var result = await _settingsApiService.UpdateSettingsAsync(request);
        if (result.IsSuccess)
        {
            _settingsApiService.RefreshCache();
            _eventBus.Publish(new StoreSettingsChangedMessage());

            StatusMessage = "✅ تم حفظ إعدادات النسخ الاحتياطي بنجاح";
            _toastService.ShowSuccess("تم حفظ إعدادات النسخ الاحتياطي");
        }
        else
        {
            StatusMessage = HandleFailure(result.Error ?? "فشل في حفظ إعدادات النسخ الاحتياطي",
                "BackupViewModel.SaveBackupSettingsAsync");
            await _dialogService.ShowErrorAsync("خطأ في حفظ إعدادات النسخ", StatusMessage);
        }
    }

    private void BrowseBackupPath()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "اختيار مجلد النسخ الاحتياطي",
            Multiselect = false,
            CheckFileExists = false,
            ValidateNames = false,
            FileName = "حدد المجلد"
        };

        if (dialog.ShowDialog() == true)
        {
            var dir = System.IO.Path.GetDirectoryName(dialog.FileName);
            if (!string.IsNullOrEmpty(dir))
            {
                BackupPathFromSettings = dir;
            }
        }
    }

    private async Task LoadBackupsOperationAsync()
    {
        StatusMessage = string.Empty;

        var result = await _backupService.GetBackupListAsync();
        if (result.IsSuccess && result.Value != null)
        {
            var items = result.Value
                .Select(f => BackupFileItem.FromFileName(f))
                .OrderByDescending(i => i.CreatedAtSort)
                .ToList();

            BackupFiles = new ObservableCollection<BackupFileItem>(items);
        }
        else
        {
            StatusMessage = HandleFailure(result.Error ?? "فشل في تحميل قائمة النسخ الاحتياطية", "BackupViewModel.LoadBackupsAsync");
        }
    }

    private async Task CreateBackupOperationAsync()
    {
        StatusMessage = "جارٍ إنشاء نسخة احتياطية...";

        var result = await _backupService.CreateBackupAsync();
        if (result.IsSuccess)
        {
            StatusMessage = "✅ تم إنشاء النسخة الاحتياطية بنجاح";
            _toastService.ShowSuccess("تم إنشاء النسخة الاحتياطية بنجاح");
            await LoadBackupsOperationAsync();
        }
        else
        {
            StatusMessage = result.Error ?? "فشل في إنشاء النسخة الاحتياطية";
            await _dialogService.ShowErrorAsync("خطأ في النسخ الاحتياطي", StatusMessage);
        }
    }

    private async Task RestoreBackupOperationAsync()
    {
        if (SelectedBackupFile == null)
        {
            await _dialogService.ShowWarningAsync(
                "تنبيه",
                "الرجاء تحديد نسخة احتياطية من القائمة أولاً.");
            return;
        }

        var confirm = await _dialogService.ShowConfirmationAsync(
            "تأكيد استعادة النسخة الاحتياطية",
            $"⚠️ تنبيه: استعادة النسخة الاحتياطية '{SelectedBackupFile.FileName}' سيؤدي إلى استبدال قاعدة البيانات الحالية بالكامل وإغلاق جميع الاتصالات النشطة.\n\nهل تريد الاستمرار؟");

        if (!confirm) return;

        StatusMessage = "جارٍ استعادة النسخة الاحتياطية... قد يستغرق هذا وقتاً.";

        var result = await _backupService.RestoreBackupAsync(SelectedBackupFile.FileName);
        if (result.IsSuccess)
        {
            StatusMessage = "✅ تم استعادة قاعدة البيانات بنجاح. سيتم إغلاق النظام لإعادة التحميل.";
            await _dialogService.ShowSuccessAsync("نجاح الاستعادة", StatusMessage);

            _eventBus.Publish(new ApplicationShutdownMessage());
        }
        else
        {
            StatusMessage = result.Error ?? "فشل في استعادة النسخة الاحتياطية";
            await _dialogService.ShowErrorAsync("خطأ في الاستعادة", StatusMessage);
        }
    }

    #endregion
}

/// <summary>
/// Represents a single backup file in the list view.
/// </summary>
public class BackupFileItem
{
    public string FileName { get; set; } = string.Empty;
    public string DisplaySize { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    public string CreatedAtSort { get; set; } = string.Empty;

    /// <summary>
    /// Parses a backup filename (e.g. "SalesSystem_20260523_235900.bak")
    /// and returns a display-friendly BackupFileItem.
    /// </summary>
    public static BackupFileItem FromFileName(string fileName)
    {
        var item = new BackupFileItem
        {
            FileName = fileName,
            DisplaySize = "—",
            CreatedAt = string.Empty,
            CreatedAtSort = fileName
        };

        // Parse format: SalesSystem_yyyyMMdd_HHmmss.bak or similar date-stamped names
        var match = Regex.Match(fileName,
            @"(\d{4})(\d{2})(\d{2})_(\d{2})(\d{2})(\d{2})",
            RegexOptions.None,
            TimeSpan.FromSeconds(1));

        if (match.Success)
        {
            var year = match.Groups[1].Value;
            var month = match.Groups[2].Value;
            var day = match.Groups[3].Value;
            var hour = match.Groups[4].Value;
            var minute = match.Groups[5].Value;
            var second = match.Groups[6].Value;

            item.CreatedAt = $"{day}/{month}/{year} {hour}:{minute}";
            item.CreatedAtSort = $"{year}{month}{day}{hour}{minute}{second}";
        }

        return item;
    }
}
