using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Windows.Input;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Services.App.Toast;

namespace SalesSystem.DesktopPWF.ViewModels.Settings;

/// <summary>
/// ViewModel for managing database backups — create, list, and restore.
/// </summary>
public class BackupViewModel : ViewModelBase
{
    private readonly IBackupApiService _backupService;
    private readonly IDialogService _dialogService;
    private readonly IToastNotificationService _toastService;

    private ObservableCollection<BackupFileItem> _backupFiles = new();
    private BackupFileItem? _selectedBackupFile;
    private string _backupPath = string.Empty;

    public BackupViewModel(
        IBackupApiService backupService,
        IDialogService dialogService,
        IToastNotificationService toastService)
    {
        _backupService = backupService;
        _dialogService = dialogService;
        _toastService = toastService;
        SetDialogService(_dialogService);

        BackupPath = "يمكن تعديل مسار النسخ من الإعدادات";

        CreateBackupCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(CreateBackupOperationAsync)));
        RestoreBackupCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(RestoreBackupOperationAsync)));

        _ = ExecuteAsync(LoadBackupsOperationAsync);
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

    public string BackupPath
    {
        get => _backupPath;
        set => SetProperty(ref _backupPath, value);
    }

    #endregion

    #region Commands

    public AsyncRelayCommand CreateBackupCommand { get; }
    public AsyncRelayCommand RestoreBackupCommand { get; }

    #endregion

    #region Operations

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

            System.Windows.Application.Current.Shutdown();
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
