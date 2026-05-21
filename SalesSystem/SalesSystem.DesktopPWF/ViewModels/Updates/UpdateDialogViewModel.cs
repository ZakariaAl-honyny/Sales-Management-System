using SalesSystem.Application.Updates.Models;
using SalesSystem.DesktopPWF.Services.App;

namespace SalesSystem.DesktopPWF.ViewModels.Updates;

public class UpdateDialogViewModel : ViewModelBase, IDisposable
{
    private readonly IUpdaterService _updaterService;
    private readonly UpdateInfo _updateInfo;
    private CancellationTokenSource? _downloadCts;

    public string SystemName { get; } = "نظام إدارة المبيعات";
    public string CurrentVersion { get; }
    public string LatestVersion { get; }
    public string ReleaseDate { get; }
    public bool IsCriticalUpdate { get; }

    public string HeaderText =>
        $"يتوفر إصدار جديد من {SystemName}";

    public string SubHeaderText =>
        $"الإصدار {LatestVersion} متاح الآن  •  لديك الإصدار {CurrentVersion}";

    public List<string> ChangelogItems { get; }

    private double _downloadProgress;
    public double DownloadProgress
    {
        get => _downloadProgress;
        set => SetProperty(ref _downloadProgress, value);
    }

    private string _downloadStatusText = string.Empty;
    public string DownloadStatusText
    {
        get => _downloadStatusText;
        set => SetProperty(ref _downloadStatusText, value);
    }

    private bool _isDownloading;
    public bool IsDownloading
    {
        get => _isDownloading;
        set
        {
            if (SetProperty(ref _isDownloading, value))
            {
                OnPropertyChanged(nameof(IsNotDownloading));
                OnPropertyChanged(nameof(DownloadButtonText));
            }
        }
    }

    public bool IsNotDownloading => !IsDownloading;

    public string DownloadButtonText =>
        IsDownloading ? "جارٍ التحميل..." : "تحميل وتثبيت";

    public bool CanSkip => !IsCriticalUpdate;

    public UpdateDialogAction Result { get; private set; }
        = UpdateDialogAction.RemindLater;

    public AsyncRelayCommand DownloadAndInstallCommand { get; }
    public RelayCommand RemindLaterCommand { get; }
    public RelayCommand SkipVersionCommand { get; }
    public RelayCommand CancelDownloadCommand { get; }

    public Action? CloseDialog { get; set; }

    public UpdateDialogViewModel(IUpdaterService updaterService, UpdateInfo updateInfo)
    {
        _updaterService = updaterService;
        _updateInfo = updateInfo;

        CurrentVersion = updaterService.GetCurrentVersion().Value;
        LatestVersion = updateInfo.LatestVersion;
        ReleaseDate = updateInfo.ReleaseDate;
        IsCriticalUpdate = updateInfo.IsCritical;
        ChangelogItems = updateInfo.Changelog;

        DownloadAndInstallCommand = new AsyncRelayCommand(
            DownloadAndInstallAsync,
            () => !IsDownloading);

        RemindLaterCommand = new RelayCommand(
            RemindLater,
            () => CanSkip && !IsDownloading);

        SkipVersionCommand = new RelayCommand(
            SkipThisVersion,
            () => CanSkip && !IsDownloading);

        CancelDownloadCommand = new RelayCommand(
            CancelDownload,
            () => IsDownloading);
    }

    private async Task DownloadAndInstallAsync()
    {
        IsDownloading = true;
        DownloadStatusText = "جارٍ تجهيز التحميل...";
        DownloadProgress = 0;
        _downloadCts = new CancellationTokenSource();

        var progressReporter = new Progress<DownloadProgress>(p =>
        {
            DownloadProgress = p.Percentage;
            DownloadStatusText = FormatDownloadStatus(p);
        });

        try
        {
            var downloadResult = await _updaterService.DownloadUpdateAsync(
                _updateInfo.DownloadUrl,
                _updateInfo.ChecksumSHA256,
                progressReporter,
                _downloadCts.Token);

            if (!downloadResult.IsSuccess || string.IsNullOrEmpty(downloadResult.Value))
            {
                if (!_downloadCts.Token.IsCancellationRequested)
                {
                    DownloadStatusText = $"فشل التحميل: {downloadResult.Error ?? "تحقق من اتصال الإنترنت والمحاولة مرة أخرى."}";
                }
                IsDownloading = false;
                return;
            }

            var installerPath = downloadResult.Value;

            DownloadStatusText = "اكتمل التحميل. جارٍ تشغيل المثبّت...";
            await Task.Delay(800);

            Result = UpdateDialogAction.InstallNow;
            var launchResult = await _updaterService.LaunchInstallerAndExitAsync(installerPath);
            if (launchResult.IsSuccess && launchResult.Value)
            {
                // Caller should exit — close the dialog
                CloseDialog?.Invoke();
            }
        }
        catch (Exception ex)
        {
            DownloadStatusText = $"خطأ: {ex.Message}";
            IsDownloading = false;
        }
    }

    private void RemindLater()
    {
        Result = UpdateDialogAction.RemindLater;
        CloseDialog?.Invoke();
    }

    private void SkipThisVersion()
    {
        _updaterService.SkipVersion(_updateInfo.LatestVersion);
        Result = UpdateDialogAction.SkipVersion;
        CloseDialog?.Invoke();
    }

    private void CancelDownload()
    {
        _downloadCts?.Cancel();
        IsDownloading = false;
        DownloadProgress = 0;
        DownloadStatusText = "تم إلغاء التحميل.";
    }

    private string FormatDownloadStatus(DownloadProgress p)
    {
        if (p.TotalBytes <= 0)
            return $"جارٍ التحميل... {p.BytesReceived / 1024.0 / 1024.0:N1} MB";

        var receivedMb = p.BytesReceived / 1024.0 / 1024.0;
        var totalMb = p.TotalBytes / 1024.0 / 1024.0;
        var speed = p.SpeedKbps >= 1024
            ? $"{p.SpeedKbps / 1024.0:N1} MB/s"
            : $"{p.SpeedKbps:N0} KB/s";

        return $"{receivedMb:N1} / {totalMb:N1} MB  •  {speed}  •  {p.Percentage:N0}%";
    }

    public void Dispose()
    {
        _downloadCts?.Cancel();
        _downloadCts?.Dispose();
    }
}

public enum UpdateDialogAction
{
    InstallNow,
    RemindLater,
    SkipVersion
}
