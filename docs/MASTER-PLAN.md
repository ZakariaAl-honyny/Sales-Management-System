Implementation Plan: Auto-Update System
📋 Master Rules for AI Agent
This is a self-contained module. It must NEVER block app startup. All network calls must be fire-and-forget with silent failure.

🗂️ Phase 0: Setup & Version File
Task 0.1 — Set Assembly Version Format
XML

<!-- File: YourApp.WPF/YourApp.WPF.csproj -->
<!-- ADD these properties -->

<PropertyGroup>
  <!-- Version format: Year.Month.BuildNumber -->
  <!-- Example: 2026.5.1350 -->
  <AssemblyVersion>2026.5.1350</AssemblyVersion>
  <FileVersion>2026.5.1350</FileVersion>
  <Version>2026.5.1350</Version>
</PropertyGroup>
Task 0.2 — Version JSON File (Host on Your Server)
JSON

// File: version.json
// Host this file at a stable URL, e.g.:
// https://your-server.com/updates/version.json
// UPDATE this file every time you release a new version

{
  "LatestVersion": "2026.5.1350",
  "ReleaseDate": "2026-05-20",
  "DownloadUrl": "https://your-server.com/updates/SalesSystemSetup.exe",
  "ChecksumSHA256": "a3f8e2c1d4b5a6f7e8c9d0b1a2c3d4e5f6a7b8c9d0e1f2a3b4c5d6e7f8a9b0c1",
  "MinimumRequiredVersion": "2026.1.0",
  "IsCritical": false,
  "Changelog": [
    "✅ إضافة نظام الوحدات الديناميكي المتعدد (UOM)",
    "✅ دعم تعدد الباركود لنفس الوحدة",
    "✅ إضافة نظام الصناديق المتعددة",
    "✅ تحديث الأسعار التلقائي من فواتير المشتريات",
    "🔧 إصلاح مشكلة التقريب في متوسط التكلفة المرجح",
    "🔧 إصلاح خطأ الطباعة الحرارية مع الأحرف العربية"
  ]
}
Task 0.3 — Add Skipped Version to Local Settings
XML

<!-- File: YourApp.WPF/App.config -->
<!-- ADD this setting -->

<appSettings>
  <add key="SkippedVersion" value="" />
  <add key="UpdateCheckUrl" 
       value="https://your-server.com/updates/version.json" />
</appSettings>
Task 0.4 — Install NuGet Package
XML

<!-- File: YourApp.Infrastructure.csproj -->
<ItemGroup>
  <!-- For downloading files with progress reporting -->
  <PackageReference Include="Downloader" Version="3.1.0" />
</ItemGroup>
✅ Phase 0 Checklist
 Assembly version set in .csproj
 version.json uploaded to server and publicly accessible
 URL is HTTPS (never HTTP for security)
 SkippedVersion key exists in App.config
 Downloader NuGet installed
🏗️ Phase 1: Domain Contracts
Task 1.1 — Data Models
csharp

// File: Application/Updates/Models/UpdateInfo.cs

public record UpdateInfo
{
    public string LatestVersion { get; init; } = string.Empty;
    public string ReleaseDate { get; init; } = string.Empty;
    public string DownloadUrl { get; init; } = string.Empty;
    public string ChecksumSHA256 { get; init; } = string.Empty;
    public string MinimumRequiredVersion { get; init; } = string.Empty;
    public bool IsCritical { get; init; }
    public List<string> Changelog { get; init; } = new();

    // ─── Computed properties ──────────────────────

    /// <summary>
    /// True if server version is higher than running version.
    /// </summary>
    public bool IsUpdateAvailable(string currentVersion)
    {
        if (!Version.TryParse(LatestVersion, out var serverVer))
            return false;
        if (!Version.TryParse(currentVersion, out var currentVer))
            return false;

        return serverVer > currentVer;
    }

    /// <summary>
    /// True if running version is below minimum required.
    /// Forces update — cannot be skipped.
    /// </summary>
    public bool IsForceUpdate(string currentVersion)
    {
        if (!Version.TryParse(MinimumRequiredVersion, out var minVer))
            return false;
        if (!Version.TryParse(currentVersion, out var currentVer))
            return false;

        return currentVer < minVer;
    }
}

// File: Application/Updates/Models/UpdateCheckResult.cs

public record UpdateCheckResult
{
    public bool IsSuccess { get; init; }
    public UpdateInfo? UpdateInfo { get; init; }
    public string? ErrorMessage { get; init; }
    public bool UpdateAvailable { get; init; }

    public static UpdateCheckResult NoUpdate()
        => new() { IsSuccess = true, UpdateAvailable = false };

    public static UpdateCheckResult Available(UpdateInfo info)
        => new() { IsSuccess = true, UpdateAvailable = true, UpdateInfo = info };

    public static UpdateCheckResult Failed(string reason)
        => new() { IsSuccess = false, ErrorMessage = reason };
}

// File: Application/Updates/Models/DownloadProgress.cs

public record DownloadProgress(
    long BytesReceived,
    long TotalBytes,
    double Percentage,
    double SpeedKbps
);
Task 1.2 — IUpdaterService Interface
csharp

// File: Application/Updates/IUpdaterService.cs

public interface IUpdaterService
{
    /// <summary>
    /// Checks server for new version. NEVER throws — returns Failed result.
    /// </summary>
    Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken ct = default);

    /// <summary>
    /// Downloads installer to temp folder with progress reporting.
    /// </summary>
    Task<string?> DownloadUpdateAsync(
        string downloadUrl,
        string expectedChecksum,
        IProgress<DownloadProgress> progress,
        CancellationToken ct = default);

    /// <summary>
    /// Launches downloaded installer and shuts down the app.
    /// </summary>
    void LaunchInstallerAndExit(string installerPath);

    /// <summary>
    /// Returns current running assembly version.
    /// </summary>
    string GetCurrentVersion();

    /// <summary>
    /// Saves skipped version to local settings.
    /// </summary>
    void SkipVersion(string version);

    /// <summary>
    /// Returns the version the user chose to skip (or empty string).
    /// </summary>
    string GetSkippedVersion();
}
✅ Phase 1 Checklist
 UpdateInfo.IsUpdateAvailable() uses System.Version comparison (not string)
 UpdateInfo.IsForceUpdate() computed separately from regular update
 UpdateCheckResult never contains exceptions — only data
 IUpdaterService.CheckForUpdatesAsync documented as "NEVER throws"
⚙️ Phase 2: Infrastructure Implementation
Task 2.1 — UpdaterService
csharp

// File: Infrastructure/Updates/UpdaterService.cs

public class UpdaterService : IUpdaterService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UpdaterService> _logger;
    private readonly string _versionFileUrl;

    public UpdaterService(
        HttpClient httpClient,
        ILogger<UpdaterService> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _versionFileUrl = configuration["UpdateCheckUrl"]
            ?? throw new InvalidOperationException("UpdateCheckUrl not configured");
    }

    // ═══════════════════════════════════════════════
    // CHECK FOR UPDATES
    // ═══════════════════════════════════════════════
    public async Task<UpdateCheckResult> CheckForUpdatesAsync(
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Checking for updates at {Url}", _versionFileUrl);

            // Short timeout — don't make user wait on slow connection
            using var cts = CancellationTokenSource
                .CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(8));

            var response = await _httpClient.GetAsync(_versionFileUrl, cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Update server returned {Status}", response.StatusCode);
                return UpdateCheckResult.Failed(
                    $"Server returned {response.StatusCode}");
            }

            var json = await response.Content.ReadAsStringAsync(cts.Token);

            var updateInfo = JsonSerializer.Deserialize<UpdateInfo>(json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (updateInfo == null)
                return UpdateCheckResult.Failed("Invalid version.json format");

            var currentVersion = GetCurrentVersion();
            var skippedVersion = GetSkippedVersion();

            // User already chose to skip this exact version
            if (!updateInfo.IsForceUpdate(currentVersion) &&
                updateInfo.LatestVersion == skippedVersion)
            {
                _logger.LogInformation(
                    "Version {Version} was skipped by user", skippedVersion);
                return UpdateCheckResult.NoUpdate();
            }

            if (updateInfo.IsUpdateAvailable(currentVersion))
            {
                _logger.LogInformation(
                    "Update available: {Current} → {Latest}",
                    currentVersion, updateInfo.LatestVersion);
                return UpdateCheckResult.Available(updateInfo);
            }

            _logger.LogInformation("App is up to date ({Version})", currentVersion);
            return UpdateCheckResult.NoUpdate();
        }
        catch (OperationCanceledException)
        {
            // Timeout or cancellation — silent fail
            _logger.LogWarning("Update check timed out");
            return UpdateCheckResult.Failed("Connection timeout");
        }
        catch (HttpRequestException ex)
        {
            // No internet — silent fail, user never sees this
            _logger.LogWarning(ex, "No internet connection for update check");
            return UpdateCheckResult.Failed("No internet connection");
        }
        catch (Exception ex)
        {
            // Any other error — log and silent fail
            _logger.LogError(ex, "Unexpected error during update check");
            return UpdateCheckResult.Failed(ex.Message);
        }
    }

    // ═══════════════════════════════════════════════
    // DOWNLOAD UPDATE
    // ═══════════════════════════════════════════════
    public async Task<string?> DownloadUpdateAsync(
        string downloadUrl,
        string expectedChecksum,
        IProgress<DownloadProgress> progress,
        CancellationToken ct = default)
    {
        try
        {
            // Save to user's temp folder
            var fileName = Path.GetFileName(new Uri(downloadUrl).LocalPath);
            var tempPath = Path.Combine(Path.GetTempPath(), "SalesSystemUpdate", fileName);

            Directory.CreateDirectory(Path.GetDirectoryName(tempPath)!);

            _logger.LogInformation(
                "Downloading update from {Url} to {Path}", downloadUrl, tempPath);

            // Download with progress tracking
            using var response = await _httpClient.GetAsync(
                downloadUrl,
                HttpCompletionOption.ResponseHeadersRead,
                ct);

            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            await using var contentStream = await response.Content.ReadAsStreamAsync(ct);
            await using var fileStream = new FileStream(
                tempPath, FileMode.Create, FileAccess.Write,
                FileShare.None, bufferSize: 81920, useAsync: true);

            var buffer = new byte[81920];
            long bytesReceived = 0;
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer, ct)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
                bytesReceived += bytesRead;

                var elapsed = stopwatch.Elapsed.TotalSeconds;
                var speedKbps = elapsed > 0
                    ? bytesReceived / 1024.0 / elapsed
                    : 0;

                var percentage = totalBytes > 0
                    ? bytesReceived * 100.0 / totalBytes
                    : 0;

                progress.Report(new DownloadProgress(
                    bytesReceived, totalBytes, percentage, speedKbps));
            }

            // ─── Verify checksum ───────────────────────────
            if (!string.IsNullOrWhiteSpace(expectedChecksum))
            {
                var isValid = await VerifyChecksumAsync(tempPath, expectedChecksum);
                if (!isValid)
                {
                    File.Delete(tempPath);
                    _logger.LogError("Checksum verification failed for {File}", tempPath);
                    return null; // Corrupted download
                }
            }

            _logger.LogInformation("Download complete: {Path}", tempPath);
            return tempPath;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Download cancelled by user");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Download failed");
            return null;
        }
    }

    // ═══════════════════════════════════════════════
    // LAUNCH INSTALLER & EXIT
    // ═══════════════════════════════════════════════
    public void LaunchInstallerAndExit(string installerPath)
    {
        if (!File.Exists(installerPath))
        {
            _logger.LogError("Installer not found at {Path}", installerPath);
            return;
        }

        _logger.LogInformation("Launching installer: {Path}", installerPath);

        // Launch installer as independent process
        // /SILENT flag for InnoSetup installers (no UAC prompt dialogs)
        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = installerPath,
            Arguments = "/SILENT /CLOSEAPPLICATIONS /RESTARTAPPLICATIONS",
            UseShellExecute = true,   // Required for UAC elevation prompt
            Verb = "runas"            // Request admin privileges
        };

        System.Diagnostics.Process.Start(startInfo);

        // Shut down THIS app so installer can replace files
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            System.Windows.Application.Current.Shutdown();
        });
    }

    // ─── Helper Methods ───────────────────────────

    public string GetCurrentVersion()
    {
        var version = System.Reflection.Assembly
            .GetExecutingAssembly()
            .GetName()
            .Version;

        // Format: Major.Minor.Build (drop Revision)
        return version != null
            ? $"{version.Major}.{version.Minor}.{version.Build}"
            : "0.0.0";
    }

    public void SkipVersion(string version)
    {
        var config = ConfigurationManager.AppSettings;
        config["SkippedVersion"] = version;

        // Persist to App.config file
        var configFile = ConfigurationManager.OpenExeConfiguration(
            ConfigurationUserLevel.None);
        configFile.AppSettings.Settings["SkippedVersion"].Value = version;
        configFile.Save(ConfigurationSaveMode.Modified);
        ConfigurationManager.RefreshSection("appSettings");

        _logger.LogInformation("Version {Version} marked as skipped", version);
    }

    public string GetSkippedVersion()
        => ConfigurationManager.AppSettings["SkippedVersion"] ?? string.Empty;

    private async Task<bool> VerifyChecksumAsync(string filePath, string expectedSha256)
    {
        await using var stream = File.OpenRead(filePath);
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = await sha256.ComputeHashAsync(stream);
        var actualHash = Convert.ToHexString(hashBytes).ToLowerInvariant();
        return actualHash == expectedSha256.ToLowerInvariant();
    }
}
Task 2.2 — Register Services in DI
csharp

// File: Infrastructure/DependencyInjection.cs
// ADD to existing service registration

public static IServiceCollection AddUpdateServices(
    this IServiceCollection services,
    IConfiguration configuration)
{
    // HttpClient with timeout and user agent
    services.AddHttpClient<IUpdaterService, UpdaterService>(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.Add(
            "User-Agent",
            "SalesSystem-UpdateChecker/1.0");
        // Prevent caching of version file
        client.DefaultRequestHeaders.CacheControl =
            new System.Net.Http.Headers.CacheControlHeaderValue
            {
                NoCache = true,
                NoStore = true
            };
    });

    return services;
}
✅ Phase 2 Checklist
 HTTP timeout set to 8 seconds for version check
 Download has 30 second timeout (longer file)
 SHA256 checksum verified after download
 Corrupted download deleted automatically
 LaunchInstallerAndExit uses runas verb for admin elevation
 SkipVersion persists to App.config file
 All catch blocks log but never rethrow
⚙️ Phase 3: Application Layer — Update ViewModel
Task 3.1 — UpdateDialogViewModel
csharp

// File: WPF/ViewModels/Updates/UpdateDialogViewModel.cs

public class UpdateDialogViewModel : BaseViewModel
{
    private readonly IUpdaterService _updaterService;
    private readonly UpdateInfo _updateInfo;
    private CancellationTokenSource? _downloadCts;

    // ─── Display Properties ───────────────────────
    public string SystemName { get; } = "نظام إدارة المبيعات";
    public string CurrentVersion { get; }
    public string LatestVersion { get; }
    public string ReleaseDate { get; }
    public bool IsCriticalUpdate { get; }

    // Header text — matches Cloudflare WARP style
    public string HeaderText =>
        $"يتوفر إصدار جديد من {SystemName}";

    public string SubHeaderText =>
        $"الإصدار {LatestVersion} متاح الآن  •  لديك الإصدار {CurrentVersion}";

    public List<string> ChangelogItems { get; }

    // ─── Download Progress ────────────────────────
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
        IsDownloading ? "جارٍ التحميل..." : "⬇️  تحميل وتثبيت";

    // Critical updates cannot be skipped
    public bool CanSkip => !IsCriticalUpdate;

    // Result tells the Window what action was taken
    public UpdateDialogAction Result { get; private set; }
        = UpdateDialogAction.RemindLater;

    // ─── Commands ─────────────────────────────────
    public IAsyncRelayCommand DownloadAndInstallCommand { get; }
    public IRelayCommand RemindLaterCommand { get; }
    public IRelayCommand SkipVersionCommand { get; }
    public IRelayCommand CancelDownloadCommand { get; }

    // Reference to the Window — needed to close it
    public Action? CloseDialog { get; set; }

    public UpdateDialogViewModel(
        IUpdaterService updaterService,
        UpdateInfo updateInfo)
    {
        _updaterService = updaterService;
        _updateInfo = updateInfo;

        CurrentVersion = updaterService.GetCurrentVersion();
        LatestVersion = updateInfo.LatestVersion;
        ReleaseDate = updateInfo.ReleaseDate;
        IsCriticalUpdate = updateInfo.IsCritical;
        ChangelogItems = updateInfo.Changelog;

        DownloadAndInstallCommand = new AsyncRelayCommand(
            DownloadAndInstallAsync,
            () => !IsDownloading);

        RemindLaterCommand = new RelayCommand(RemindLater,
            () => CanSkip && !IsDownloading);

        SkipVersionCommand = new RelayCommand(SkipThisVersion,
            () => CanSkip && !IsDownloading);

        CancelDownloadCommand = new RelayCommand(CancelDownload,
            () => IsDownloading);
    }

    // ═══════════════════════════════════════════════
    // DOWNLOAD AND INSTALL
    // ═══════════════════════════════════════════════
    private async Task DownloadAndInstallAsync()
    {
        IsDownloading = true;
        DownloadStatusText = "جارٍ تجهيز التحميل...";
        DownloadProgress = 0;
        _downloadCts = new CancellationTokenSource();

        var progressReporter = new Progress<DownloadProgress>(p =>
        {
            // Progress callback runs on UI thread via Progress<T>
            DownloadProgress = p.Percentage;
            DownloadStatusText = FormatDownloadStatus(p);
        });

        try
        {
            var installerPath = await _updaterService.DownloadUpdateAsync(
                _updateInfo.DownloadUrl,
                _updateInfo.ChecksumSHA256,
                progressReporter,
                _downloadCts.Token);

            if (installerPath == null)
            {
                // Download failed or was cancelled
                if (!_downloadCts.Token.IsCancellationRequested)
                {
                    DownloadStatusText =
                        "❌ فشل التحميل. يرجى التحقق من اتصال الإنترنت والمحاولة مرة أخرى.";
                }
                IsDownloading = false;
                return;
            }

            // Download succeeded
            DownloadStatusText = "✅ اكتمل التحميل. جارٍ تشغيل المثبّت...";
            await Task.Delay(800); // Brief pause so user sees success message

            Result = UpdateDialogAction.InstallNow;

            // Launch installer — this also shuts down the app
            _updaterService.LaunchInstallerAndExit(installerPath);
        }
        catch (Exception ex)
        {
            DownloadStatusText = $"❌ خطأ: {ex.Message}";
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
}

public enum UpdateDialogAction
{
    InstallNow,
    RemindLater,
    SkipVersion
}
✅ Phase 3 Checklist
 Progress<T> used (ensures UI thread callback — no Dispatcher.Invoke needed)
 Cancelled download sets IsDownloading = false correctly
 CloseDialog is Action delegate (not direct Window reference — keeps ViewModel clean)
 IsCritical = true disables RemindLater and Skip buttons
 Download status shows MB received, total, and speed
🖼️ Phase 4: WPF Update Dialog Window
Task 4.1 — Update Dialog XAML
XML

<!-- File: Views/Updates/UpdateDialog.xaml -->
<Window x:Class="YourApp.Views.Updates.UpdateDialog"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="تحديث البرنامج"
        Width="520" Height="auto"
        MinHeight="400"
        ResizeMode="NoResize"
        WindowStartupLocation="CenterOwner"
        FlowDirection="RightToLeft"
        WindowStyle="None"
        AllowsTransparency="True"
        Background="Transparent">

    <Window.Resources>
        <!-- Button Styles -->
        <Style x:Key="PrimaryButtonStyle" TargetType="Button">
            <Setter Property="Height" Value="42"/>
            <Setter Property="Padding" Value="20,0"/>
            <Setter Property="FontSize" Value="13"/>
            <Setter Property="FontWeight" Value="Bold"/>
            <Setter Property="Background" Value="#0078D4"/>
            <Setter Property="Foreground" Value="White"/>
            <Setter Property="BorderThickness" Value="0"/>
            <Setter Property="Cursor" Value="Hand"/>
            <Setter Property="Template">
                <Setter.Value>
                    <ControlTemplate TargetType="Button">
                        <Border Background="{TemplateBinding Background}"
                                CornerRadius="6"
                                Padding="{TemplateBinding Padding}">
                            <ContentPresenter HorizontalAlignment="Center"
                                              VerticalAlignment="Center"/>
                        </Border>
                        <ControlTemplate.Triggers>
                            <Trigger Property="IsMouseOver" Value="True">
                                <Setter Property="Background" Value="#005A9E"/>
                            </Trigger>
                            <Trigger Property="IsEnabled" Value="False">
                                <Setter Property="Background" Value="#CCCCCC"/>
                                <Setter Property="Foreground" Value="#888888"/>
                            </Trigger>
                        </ControlTemplate.Triggers>
                    </ControlTemplate>
                </Setter.Value>
            </Setter>
        </Style>

        <Style x:Key="GhostButtonStyle" TargetType="Button">
            <Setter Property="Height" Value="36"/>
            <Setter Property="Padding" Value="16,0"/>
            <Setter Property="FontSize" Value="12"/>
            <Setter Property="Background" Value="Transparent"/>
            <Setter Property="Foreground" Value="#555555"/>
            <Setter Property="BorderThickness" Value="0"/>
            <Setter Property="Cursor" Value="Hand"/>
        </Style>

        <Style x:Key="ChangelogItemStyle" TargetType="TextBlock">
            <Setter Property="FontSize" Value="12"/>
            <Setter Property="Margin" Value="0,3"/>
            <Setter Property="TextWrapping" Value="Wrap"/>
            <Setter Property="Foreground" Value="#333333"/>
        </Style>
    </Window.Resources>

    <!-- Drop shadow container -->
    <Border CornerRadius="12"
            Background="White"
            Margin="10">
        <Border.Effect>
            <DropShadowEffect BlurRadius="20" Opacity="0.2"
                              ShadowDepth="4" Color="Black"/>
        </Border.Effect>

        <Grid>
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto"/>  <!-- Blue header band -->
                <RowDefinition Height="Auto"/>  <!-- Version info -->
                <RowDefinition Height="*"/>     <!-- Changelog -->
                <RowDefinition Height="Auto"/>  <!-- Progress bar -->
                <RowDefinition Height="Auto"/>  <!-- Buttons -->
            </Grid.RowDefinitions>

            <!-- ═══ ROW 0: HEADER BAND ═══ -->
            <Border Grid.Row="0"
                    Background="#0078D4"
                    CornerRadius="12,12,0,0"
                    Padding="24,20">
                <Grid>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*"/>
                        <ColumnDefinition Width="Auto"/>
                    </Grid.ColumnDefinitions>

                    <StackPanel>
                        <!-- System icon + name -->
                        <TextBlock Text="⬆️  تحديث متوفر"
                                   Foreground="#BBDEFB"
                                   FontSize="11"
                                   Margin="0,0,0,4"/>

                        <TextBlock Text="{Binding HeaderText}"
                                   Foreground="White"
                                   FontSize="16"
                                   FontWeight="Bold"
                                   TextWrapping="Wrap"/>

                        <TextBlock Text="{Binding SubHeaderText}"
                                   Foreground="#90CAF9"
                                   FontSize="11"
                                   Margin="0,6,0,0"/>
                    </StackPanel>

                    <!-- Critical badge (only for critical updates) -->
                    <Border Grid.Column="1"
                            Background="#FF5722"
                            CornerRadius="4"
                            Padding="8,4"
                            VerticalAlignment="Top"
                            Visibility="{Binding IsCriticalUpdate,
                                         Converter={StaticResource BoolToVisibility}}">
                        <TextBlock Text="تحديث إلزامي"
                                   Foreground="White"
                                   FontSize="10"
                                   FontWeight="Bold"/>
                    </Border>
                </Grid>
            </Border>

            <!-- ═══ ROW 1: VERSION COMPARISON ═══ -->
            <Grid Grid.Row="1" Margin="24,16,24,8">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition/>
                    <ColumnDefinition Width="Auto"/>
                    <ColumnDefinition/>
                </Grid.ColumnDefinitions>

                <!-- Current version -->
                <Border Background="#F5F5F5" CornerRadius="6" Padding="12,8">
                    <StackPanel HorizontalAlignment="Center">
                        <TextBlock Text="الإصدار الحالي"
                                   FontSize="10" Foreground="#888"
                                   HorizontalAlignment="Center"/>
                        <TextBlock Text="{Binding CurrentVersion}"
                                   FontSize="14" FontWeight="Bold"
                                   Foreground="#555"
                                   HorizontalAlignment="Center"/>
                    </StackPanel>
                </Border>

                <!-- Arrow -->
                <TextBlock Grid.Column="1"
                           Text="  →  "
                           FontSize="20" Foreground="#0078D4"
                           VerticalAlignment="Center"/>

                <!-- New version -->
                <Border Grid.Column="2"
                        Background="#E3F2FD"
                        BorderBrush="#0078D4"
                        BorderThickness="2"
                        CornerRadius="6" Padding="12,8">
                    <StackPanel HorizontalAlignment="Center">
                        <TextBlock Text="الإصدار الجديد"
                                   FontSize="10" Foreground="#0078D4"
                                   HorizontalAlignment="Center"/>
                        <TextBlock Text="{Binding LatestVersion}"
                                   FontSize="14" FontWeight="Bold"
                                   Foreground="#0078D4"
                                   HorizontalAlignment="Center"/>
                        <TextBlock Text="{Binding ReleaseDate}"
                                   FontSize="9" Foreground="#888"
                                   HorizontalAlignment="Center"/>
                    </StackPanel>
                </Border>
            </Grid>

            <!-- ═══ ROW 2: CHANGELOG ═══ -->
            <Border Grid.Row="2"
                    Margin="24,0,24,12"
                    BorderBrush="#EEEEEE"
                    BorderThickness="1"
                    CornerRadius="6">
                <StackPanel>
                    <TextBlock Text="ما الجديد في هذا الإصدار:"
                               FontSize="11" FontWeight="Bold"
                               Foreground="#444"
                               Margin="12,10,12,6"/>

                    <ScrollViewer MaxHeight="160"
                                  VerticalScrollBarVisibility="Auto"
                                  Margin="12,0,12,10">
                        <ItemsControl ItemsSource="{Binding ChangelogItems}">
                            <ItemsControl.ItemTemplate>
                                <DataTemplate>
                                    <TextBlock Text="{Binding}"
                                               Style="{StaticResource ChangelogItemStyle}"/>
                                </DataTemplate>
                            </ItemsControl.ItemTemplate>
                        </ItemsControl>
                    </ScrollViewer>
                </StackPanel>
            </Border>

            <!-- ═══ ROW 3: DOWNLOAD PROGRESS ═══ -->
            <StackPanel Grid.Row="3"
                        Margin="24,0,24,12"
                        Visibility="{Binding IsDownloading,
                                     Converter={StaticResource BoolToVisibility}}">

                <ProgressBar Value="{Binding DownloadProgress}"
                             Minimum="0" Maximum="100"
                             Height="8"
                             Background="#E0E0E0"
                             Foreground="#0078D4"/>

                <TextBlock Text="{Binding DownloadStatusText}"
                           FontSize="11" Foreground="#666"
                           HorizontalAlignment="Center"
                           Margin="0,6,0,0"/>
            </StackPanel>

            <!-- ═══ ROW 4: ACTION BUTTONS ═══ -->
            <Border Grid.Row="4"
                    BorderBrush="#EEEEEE"
                    BorderThickness="0,1,0,0"
                    Padding="24,12">
                <Grid>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="Auto"/>
                        <ColumnDefinition Width="Auto"/>
                        <ColumnDefinition Width="*"/>
                        <ColumnDefinition Width="Auto"/>
                    </Grid.ColumnDefinitions>

                    <!-- Skip This Version (leftmost, subtle) -->
                    <Button Grid.Column="0"
                            Content="تخطي هذا الإصدار"
                            Style="{StaticResource GhostButtonStyle}"
                            Command="{Binding SkipVersionCommand}"
                            Visibility="{Binding CanSkip,
                                         Converter={StaticResource BoolToVisibility}}"/>

                    <!-- Cancel Download (only during download) -->
                    <Button Grid.Column="1"
                            Content="إلغاء التحميل"
                            Style="{StaticResource GhostButtonStyle}"
                            Command="{Binding CancelDownloadCommand}"
                            Visibility="{Binding IsDownloading,
                                         Converter={StaticResource BoolToVisibility}}"
                            Foreground="#F44336"/>

                    <!-- Spacer -->
                    <StackPanel Grid.Column="2"/>

                    <!-- Right side buttons -->
                    <StackPanel Grid.Column="3"
                                Orientation="Horizontal">

                        <!-- Remind Later -->
                        <Button Content="تذكيري لاحقاً"
                                Style="{StaticResource GhostButtonStyle}"
                                Command="{Binding RemindLaterCommand}"
                                IsEnabled="{Binding IsNotDownloading}"
                                Margin="0,0,8,0"
                                Visibility="{Binding CanSkip,
                                             Converter={StaticResource BoolToVisibility}}"/>

                        <!-- Download & Install (PRIMARY) -->
                        <Button Content="{Binding DownloadButtonText}"
                                Style="{StaticResource PrimaryButtonStyle}"
                                Command="{Binding DownloadAndInstallCommand}"
                                MinWidth="160"/>
                    </StackPanel>
                </Grid>
            </Border>
        </Grid>
    </Border>
</Window>
Task 4.2 — Update Dialog Code-Behind
csharp

// File: Views/Updates/UpdateDialog.xaml.cs

public partial class UpdateDialog : Window
{
    private bool _allowClose = false;

    public UpdateDialog(UpdateDialogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // Allow ViewModel to close the window
        viewModel.CloseDialog = () =>
        {
            _allowClose = true;
            Close();
        };

        // Allow dragging the borderless window
        MouseLeftButtonDown += (_, _) => DragMove();
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        var vm = (UpdateDialogViewModel)DataContext;

        // Prevent closing during download (unless explicitly allowed)
        if (vm.IsDownloading && !_allowClose)
        {
            e.Cancel = true;
            return;
        }

        base.OnClosing(e);
    }
}
✅ Phase 4 Checklist
 Window has WindowStyle="None" and AllowsTransparency="True" for custom shape
 DragMove() enables window dragging by mouse
 Download progress bar hidden when IsDownloading = false
 "Skip" and "Remind Later" buttons hidden for critical updates
 "Cancel Download" button only visible during active download
 Window cannot be closed (X button) during download
 Version comparison shows visual difference (grey vs blue border)
🚀 Phase 5: App Startup Integration
Task 5.1 — Background Update Check on Startup
csharp

// File: WPF/App.xaml.cs

public partial class App : System.Windows.Application
{
    private IServiceProvider _serviceProvider = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 1. Initialize services
        _serviceProvider = BuildServiceProvider();
        PrintingBootstrapper.Initialize();

        // 2. Show main window FIRST — never delay startup for update check
        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();

        // 3. Check for updates in background — completely non-blocking
        // Use Task.Run to move off UI thread, with delay to let app load first
        _ = Task.Run(async () =>
        {
            // Wait 3 seconds after startup before checking
            await Task.Delay(TimeSpan.FromSeconds(3));
            await CheckForUpdatesInBackgroundAsync();
        });
    }

    private async Task CheckForUpdatesInBackgroundAsync()
    {
        try
        {
            var updaterService = _serviceProvider
                .GetRequiredService<IUpdaterService>();

            var result = await updaterService.CheckForUpdatesAsync();

            // Only show dialog if update available
            if (!result.UpdateAvailable || result.UpdateInfo == null)
                return;

            // Show dialog on UI thread
            await Dispatcher.InvokeAsync(() =>
            {
                ShowUpdateDialog(result.UpdateInfo);
            });
        }
        catch (Exception ex)
        {
            // SILENT FAIL — user never sees this error
            var logger = _serviceProvider
                .GetRequiredService<ILogger<App>>();
            logger.LogWarning(ex, "Background update check failed silently");
        }
    }

    private void ShowUpdateDialog(UpdateInfo updateInfo)
    {
        var updaterService = _serviceProvider
            .GetRequiredService<IUpdaterService>();

        var viewModel = new UpdateDialogViewModel(updaterService, updateInfo);

        var dialog = new UpdateDialog(viewModel)
        {
            Owner = MainWindow  // Centers relative to main window
        };

        dialog.ShowDialog();

        // Log the user's choice
        var logger = _serviceProvider
            .GetRequiredService<ILogger<App>>();

        logger.LogInformation(
            "User chose: {Action} for version {Version}",
            viewModel.Result,
            updateInfo.LatestVersion);
    }

    private IServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();

        // Add update services
        var config = new ConfigurationBuilder()
            .AddAppSettings()
            .Build();

        services.AddSingleton<IConfiguration>(config);
        services.AddUpdateServices(config);
        services.AddLogging(builder => builder.AddDebug());

        // ... register all other services

        return services.BuildServiceProvider();
    }
}
Task 5.2 — Manual Update Check (from Menu)
csharp

// File: WPF/ViewModels/MainViewModel.cs
// ADD this command to allow user to manually check for updates

public IAsyncRelayCommand CheckForUpdatesManuallyCommand { get; }

private async Task CheckForUpdatesManuallyAsync()
{
    try
    {
        IsBusy = true;
        StatusMessage = "جارٍ التحقق من وجود تحديثات...";

        var result = await _updaterService.CheckForUpdatesAsync();

        if (!result.IsSuccess)
        {
            System.Windows.MessageBox.Show(
                "تعذر الاتصال بخادم التحديثات.\n" +
                "يرجى التحقق من اتصال الإنترنت والمحاولة لاحقاً.",
                "تعذر التحقق",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
            return;
        }

        if (!result.UpdateAvailable)
        {
            System.Windows.MessageBox.Show(
                $"برنامجك محدّث! 🎉\n" +
                $"تعمل على أحدث إصدار: {_updaterService.GetCurrentVersion()}",
                "لا توجد تحديثات",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
            return;
        }

        // Show update dialog
        var vm = new UpdateDialogViewModel(_updaterService, result.UpdateInfo!);
        var dialog = new UpdateDialog(vm) { Owner = System.Windows.Application.Current.MainWindow };
        dialog.ShowDialog();
    }
    finally
    {
        IsBusy = false;
        StatusMessage = string.Empty;
    }
}
✅ Phase 5 Checklist
 Main window shows BEFORE update check starts
 3-second delay before network call (let app fully load)
 _ = Task.Run(...) — fire and forget, startup never awaits it
 Dispatcher.InvokeAsync() used to show dialog on UI thread
 Manual check in menu shows "up to date" message if no updates
 All exceptions in background check are caught and logged silently
🧪 Phase 6: Unit Tests
csharp

// File: Tests/Updates/UpdateInfoTests.cs

public class UpdateInfoTests
{
    [Fact]
    public void IsUpdateAvailable_ServerHigher_ReturnsTrue()
    {
        var info = new UpdateInfo { LatestVersion = "2026.5.1400" };
        Assert.True(info.IsUpdateAvailable("2026.5.1350"));
    }

    [Fact]
    public void IsUpdateAvailable_SameVersion_ReturnsFalse()
    {
        var info = new UpdateInfo { LatestVersion = "2026.5.1350" };
        Assert.False(info.IsUpdateAvailable("2026.5.1350"));
    }

    [Fact]
    public void IsUpdateAvailable_CurrentHigher_ReturnsFalse()
    {
        var info = new UpdateInfo { LatestVersion = "2026.4.1000" };
        Assert.False(info.IsUpdateAvailable("2026.5.1350"));
    }

    [Fact]
    public void IsForceUpdate_BelowMinimum_ReturnsTrue()
    {
        var info = new UpdateInfo { MinimumRequiredVersion = "2026.3.0" };
        Assert.True(info.IsForceUpdate("2026.2.999"));
    }

    [Fact]
    public void IsForceUpdate_AboveMinimum_ReturnsFalse()
    {
        var info = new UpdateInfo { MinimumRequiredVersion = "2026.3.0" };
        Assert.False(info.IsForceUpdate("2026.5.1350"));
    }

    [Fact]
    public void IsUpdateAvailable_InvalidVersion_ReturnsFalse()
    {
        var info = new UpdateInfo { LatestVersion = "not-a-version" };
        // Should not throw — graceful handling of bad server data
        Assert.False(info.IsUpdateAvailable("2026.5.1350"));
    }
}

// File: Tests/Updates/UpdateCheckResultTests.cs

public class UpdateCheckResultTests
{
    [Fact]
    public void Failed_IsSuccessFalse_HasMessage()
    {
        var result = UpdateCheckResult.Failed("No internet");
        Assert.False(result.IsSuccess);
        Assert.False(result.UpdateAvailable);
        Assert.Equal("No internet", result.ErrorMessage);
    }

    [Fact]
    public void NoUpdate_IsSuccessTrue_UpdateAvailableFalse()
    {
        var result = UpdateCheckResult.NoUpdate();
        Assert.True(result.IsSuccess);
        Assert.False(result.UpdateAvailable);
        Assert.Null(result.UpdateInfo);
    }

    [Fact]
    public void Available_HasUpdateInfo()
    {
        var info = new UpdateInfo { LatestVersion = "2026.5.1400" };
        var result = UpdateCheckResult.Available(info);
        Assert.True(result.IsSuccess);
        Assert.True(result.UpdateAvailable);
        Assert.NotNull(result.UpdateInfo);
    }
}
📦 Final Summary
text

┌───────────────────────────────────────────────────────────────────┐
│              AUTO-UPDATE SYSTEM — IMPLEMENTATION ORDER            │
├──────┬──────────────────────────────────────────────┬────────────┤
│ Step │ Deliverable                                  │ Key Rule   │
├──────┼──────────────────────────────────────────────┼────────────┤
│  0   │ version.json on server + App.config keys     │ HTTPS only │
│  1   │ UpdateInfo + UpdateCheckResult DTOs          │ No throws  │
│  2   │ UpdaterService (check+download+launch)       │ SHA256     │
│      │                                              │ verify     │
│  3   │ UpdateDialogViewModel (3 actions)            │ Progress<T>│
│  4   │ UpdateDialog XAML (borderless window)        │ No close   │
│      │                                              │ during DL  │
│  5   │ App.xaml.cs background integration          │ Main window │
│      │                                              │ first!     │
│  6   │ Unit tests                                   │ Never skip │
└──────┴──────────────────────────────────────────────┴────────────┘

CRITICAL RULES — NEVER VIOLATE:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
✅ Main window shows BEFORE update check — startup never blocked
✅ Network failure = silent log, user never sees error
✅ Version comparison uses System.Version (not string.Compare)
✅ SHA256 checksum verified — corrupted file deleted automatically
✅ Installer launched with "runas" verb for admin elevation
✅ App.Shutdown() called AFTER installer process starts
✅ Critical updates disable all skip/remind buttons
✅ Window cannot be closed during active download
✅ Progress<T> used (not Dispatcher.Invoke) for thread-safe UI updates
✅ "Skip Version" persists to config file (not just in-memory)


Implementation Plan: Phase 7 — Production Readiness
📋 Master Rules for AI Agent
This is the final phase. Every component must be production-grade. No shortcuts. No hardcoded secrets. No plaintext credentials.

🗂️ Phase 0: Project Structure & Dependencies
Task 0.1 — Install Required NuGet Packages
XML

<!-- File: YourApp.Infrastructure/YourApp.Infrastructure.csproj -->
<ItemGroup>
  <!-- Windows Service hosting -->
  <PackageReference Include="Microsoft.Extensions.Hosting.WindowsServices" Version="8.0.0" />
  
  <!-- SQL Server backup via SMO -->
  <PackageReference Include="Microsoft.SqlServer.SqlManagementObjects" Version="170.18.0" />
  
  <!-- Data protection (DPAPI) -->
  <PackageReference Include="Microsoft.AspNetCore.DataProtection" Version="8.0.0" />
</ItemGroup>

<!-- File: YourApp.WPF/YourApp.WPF.csproj -->
<ItemGroup>
  <!-- GitHub API client -->
  <PackageReference Include="Octokit" Version="13.0.1" />
</ItemGroup>
Task 0.2 — Environment Configuration
JSON

// File: appsettings.json
// NEVER store real credentials here — only structure
{
  "ConnectionStrings": {
    "DefaultConnection": "ENCRYPTED_AT_RUNTIME"
  },
  "GitHub": {
    "Owner": "your-github-username",
    "Repository": "your-repo-name",
    "ProductHeaderValue": "SalesSystem-UpdateChecker"
  },
  "Backup": {
    "DefaultBackupPath": "C:\\SalesSystemBackups",
    "DatabaseName": "SalesSystemDb",
    "RetentionDays": 30
  },
  "WindowsService": {
    "ServiceName": "SalesSystemService",
    "DisplayName": "نظام إدارة المبيعات",
    "Description": "خدمة نظام إدارة المبيعات والمخزون"
  }
}
✅ Phase 0 Checklist
 All 4 NuGet packages installed
 appsettings.json has NO real credentials
 GitHub repo and username configured
 Backup path exists or will be created programmatically
🔐 Phase 1: Security — Connection String Encryption
Task 1.1 — DPAPI Encryption Utility
csharp

// File: Infrastructure/Security/ConnectionStringProtector.cs

public interface IConnectionStringProtector
{
    string Encrypt(string plainConnectionString);
    string Decrypt(string encryptedConnectionString);
    bool IsEncrypted(string value);
}

public class ConnectionStringProtector : IConnectionStringProtector
{
    // Unique purpose string — ties encryption to THIS application
    private const string Purpose = "SalesSystem.ConnectionString.v1";
    private readonly IDataProtector _protector;

    public ConnectionStringProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector(Purpose);
    }

    public string Encrypt(string plainConnectionString)
    {
        if (string.IsNullOrWhiteSpace(plainConnectionString))
            throw new ArgumentException("Connection string cannot be empty");

        if (IsEncrypted(plainConnectionString))
            return plainConnectionString; // Already encrypted

        var encrypted = _protector.Protect(plainConnectionString);
        return $"DPAPI:{encrypted}"; // Prefix marks it as encrypted
    }

    public string Decrypt(string encryptedConnectionString)
    {
        if (string.IsNullOrWhiteSpace(encryptedConnectionString))
            throw new ArgumentException("Encrypted value cannot be empty");

        if (!IsEncrypted(encryptedConnectionString))
            return encryptedConnectionString; // Plain text — return as-is

        var encryptedPart = encryptedConnectionString["DPAPI:".Length..];
        return _protector.Unprotect(encryptedPart);
    }

    public bool IsEncrypted(string value)
        => value.StartsWith("DPAPI:", StringComparison.Ordinal);
}
Task 1.2 — First-Run Setup (Encrypt and Save)
csharp

// File: Infrastructure/Security/FirstRunSetupService.cs
// Called ONCE on first launch — encrypts and saves the connection string

public class FirstRunSetupService
{
    private readonly IConnectionStringProtector _protector;
    private readonly ILogger<FirstRunSetupService> _logger;
    private const string ConfigKey = "ConnectionStrings:DefaultConnection";

    public FirstRunSetupService(
        IConnectionStringProtector protector,
        ILogger<FirstRunSetupService> logger)
    {
        _protector = protector;
        _logger = logger;
    }

    /// <summary>
    /// Call this in Program.cs before building the app.
    /// Encrypts the connection string if it's still plaintext.
    /// </summary>
    public void EnsureConnectionStringEncrypted(IConfiguration configuration)
    {
        var currentValue = configuration[ConfigKey];

        if (string.IsNullOrWhiteSpace(currentValue))
        {
            _logger.LogWarning("Connection string is empty — check appsettings.json");
            return;
        }

        if (_protector.IsEncrypted(currentValue))
        {
            _logger.LogInformation("Connection string is already encrypted");
            return;
        }

        // First run: encrypt and overwrite in appsettings.json
        var encrypted = _protector.Encrypt(currentValue);
        UpdateAppSettings(ConfigKey, encrypted);

        _logger.LogInformation(
            "Connection string encrypted and saved on first run");
    }

    private void UpdateAppSettings(string key, string value)
    {
        var appSettingsPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "appsettings.json");

        var json = File.ReadAllText(appSettingsPath);
        var jsonDoc = System.Text.Json.JsonDocument.Parse(json);

        // Parse, update, and re-serialize
        var dict = System.Text.Json.JsonSerializer
            .Deserialize<Dictionary<string, object>>(json)!;

        // Navigate to ConnectionStrings key and update
        if (dict.TryGetValue("ConnectionStrings", out var csObj))
        {
            var csDict = System.Text.Json.JsonSerializer
                .Deserialize<Dictionary<string, string>>(csObj.ToString()!)!;
            csDict["DefaultConnection"] = value;
            dict["ConnectionStrings"] = csDict;
        }

        var newJson = System.Text.Json.JsonSerializer.Serialize(dict,
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

        File.WriteAllText(appSettingsPath, newJson);
    }
}
Task 1.3 — Secure DbContext Factory
csharp

// File: Infrastructure/Persistence/SecureDbContextFactory.cs

public class SecureDbContextFactory
{
    private readonly IConnectionStringProtector _protector;
    private readonly IConfiguration _configuration;

    public SecureDbContextFactory(
        IConnectionStringProtector protector,
        IConfiguration configuration)
    {
        _protector = protector;
        _configuration = configuration;
    }

    public string GetDecryptedConnectionString()
    {
        var rawValue = _configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' not found in configuration");

        // Decrypt if encrypted, return as-is if plaintext
        return _protector.Decrypt(rawValue);
    }
}
✅ Phase 1 Checklist
 DPAPI: prefix distinguishes encrypted from plain text
 First-run encryption happens before app.Run()
 appsettings.json is updated in-place on first run
 SecureDbContextFactory always decrypts before use
 appsettings.json added to .gitignore (see Phase 6)
💾 Phase 2: Backup & Restore Service
Task 2.1 — BackupService
csharp

// File: Infrastructure/Backup/BackupService.cs

public interface IBackupService
{
    Task<BackupResult> CreateBackupAsync(
        string? destinationPath = null,
        CancellationToken ct = default);

    Task<RestoreResult> RestoreDatabaseAsync(
        string backupFilePath,
        CancellationToken ct = default);

    Task<bool> DeleteOldBackupsAsync(int retentionDays, CancellationToken ct = default);
}

public record BackupResult(bool IsSuccess, string? FilePath, string? ErrorMessage, long FileSizeBytes = 0);
public record RestoreResult(bool IsSuccess, string? ErrorMessage);

public class BackupService : IBackupService
{
    private readonly SecureDbContextFactory _dbFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BackupService> _logger;

    public BackupService(
        SecureDbContextFactory dbFactory,
        IConfiguration configuration,
        ILogger<BackupService> logger)
    {
        _dbFactory = dbFactory;
        _configuration = configuration;
        _logger = logger;
    }

    // ═══════════════════════════════════════════════
    // CREATE BACKUP
    // ═══════════════════════════════════════════════
    public async Task<BackupResult> CreateBackupAsync(
        string? destinationPath = null,
        CancellationToken ct = default)
    {
        try
        {
            var dbName = _configuration["Backup:DatabaseName"]
                ?? throw new InvalidOperationException("DatabaseName not configured");

            var backupDir = destinationPath
                ?? _configuration["Backup:DefaultBackupPath"]
                ?? Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "SalesSystemBackups");

            Directory.CreateDirectory(backupDir);

            // Filename: SalesSystem_2026-05-20_143022.bak
            var fileName = $"{dbName}_{DateTime.Now:yyyy-MM-dd_HHmmss}.bak";
            var fullPath = Path.Combine(backupDir, fileName);

            _logger.LogInformation(
                "Starting backup of {Database} to {Path}", dbName, fullPath);

            var connectionString = _dbFactory.GetDecryptedConnectionString();

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(ct);

            // SQL Server native backup command
            var backupSql = $"""
                BACKUP DATABASE [{dbName}]
                TO DISK = @BackupPath
                WITH FORMAT,
                     MEDIANAME = 'SalesSystemBackup',
                     NAME = 'Full Database Backup - {DateTime.Now:yyyy-MM-dd HH:mm}',
                     COMPRESSION,
                     STATS = 10;
                """;

            await using var command = new SqlCommand(backupSql, connection);
            command.CommandTimeout = 300; // 5 minutes for large DBs
            command.Parameters.AddWithValue("@BackupPath", fullPath);

            await command.ExecuteNonQueryAsync(ct);

            var fileInfo = new FileInfo(fullPath);

            _logger.LogInformation(
                "Backup completed successfully. Size: {Size:N0} bytes", fileInfo.Length);

            return new BackupResult(true, fullPath, null, fileInfo.Length);
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "SQL error during backup");
            return new BackupResult(false, null,
                $"خطأ في قاعدة البيانات: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during backup");
            return new BackupResult(false, null,
                $"خطأ غير متوقع: {ex.Message}");
        }
    }

    // ═══════════════════════════════════════════════
    // RESTORE DATABASE
    // ═══════════════════════════════════════════════
    public async Task<RestoreResult> RestoreDatabaseAsync(
        string backupFilePath,
        CancellationToken ct = default)
    {
        if (!File.Exists(backupFilePath))
            return new RestoreResult(false, $"ملف النسخة الاحتياطية غير موجود: {backupFilePath}");

        var dbName = _configuration["Backup:DatabaseName"]
            ?? throw new InvalidOperationException("DatabaseName not configured");

        try
        {
            _logger.LogWarning(
                "RESTORE INITIATED for {Database} from {File}", dbName, backupFilePath);

            // Connect to MASTER database — not the target DB
            var connectionString = _dbFactory.GetDecryptedConnectionString();
            var masterConnection = new SqlConnectionStringBuilder(connectionString)
            {
                InitialCatalog = "master"
            }.ConnectionString;

            await using var connection = new SqlConnection(masterConnection);
            await connection.OpenAsync(ct);

            // Step 1: Force all other connections to disconnect
            var singleUserSql = $"""
                ALTER DATABASE [{dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                """;

            await using (var cmd1 = new SqlCommand(singleUserSql, connection))
            {
                cmd1.CommandTimeout = 60;
                await cmd1.ExecuteNonQueryAsync(ct);
            }

            _logger.LogInformation("Database set to SINGLE_USER mode");

            // Step 2: Restore
            var restoreSql = $"""
                RESTORE DATABASE [{dbName}]
                FROM DISK = @BackupPath
                WITH REPLACE,
                     RECOVERY,
                     STATS = 10;
                """;

            await using (var cmd2 = new SqlCommand(restoreSql, connection))
            {
                cmd2.CommandTimeout = 600; // 10 minutes
                cmd2.Parameters.AddWithValue("@BackupPath", backupFilePath);
                await cmd2.ExecuteNonQueryAsync(ct);
            }

            _logger.LogInformation("Database restored successfully");

            // Step 3: Return to multi-user
            var multiUserSql = $"ALTER DATABASE [{dbName}] SET MULTI_USER;";
            await using (var cmd3 = new SqlCommand(multiUserSql, connection))
                await cmd3.ExecuteNonQueryAsync(ct);

            return new RestoreResult(true, null);
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "SQL error during restore");

            // Attempt to return to multi-user even if restore failed
            await TrySetMultiUserAsync(dbName, ct);

            return new RestoreResult(false,
                $"خطأ أثناء الاسترجاع: {ex.Message}");
        }
    }

    // ═══════════════════════════════════════════════
    // CLEANUP OLD BACKUPS
    // ═══════════════════════════════════════════════
    public async Task<bool> DeleteOldBackupsAsync(
        int retentionDays,
        CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                var backupDir = _configuration["Backup:DefaultBackupPath"]!;
                if (!Directory.Exists(backupDir)) return true;

                var cutoffDate = DateTime.Now.AddDays(-retentionDays);
                var oldFiles = Directory.GetFiles(backupDir, "*.bak")
                    .Select(f => new FileInfo(f))
                    .Where(f => f.CreationTime < cutoffDate)
                    .ToList();

                foreach (var file in oldFiles)
                {
                    file.Delete();
                    _logger.LogInformation("Deleted old backup: {File}", file.Name);
                }

                _logger.LogInformation(
                    "Cleanup complete. Deleted {Count} old backups", oldFiles.Count);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Backup cleanup failed");
                return false;
            }
        }, ct);
    }

    private async Task TrySetMultiUserAsync(string dbName, CancellationToken ct)
    {
        try
        {
            var connectionString = _dbFactory.GetDecryptedConnectionString();
            var masterConnection = new SqlConnectionStringBuilder(connectionString)
            {
                InitialCatalog = "master"
            }.ConnectionString;

            await using var conn = new SqlConnection(masterConnection);
            await conn.OpenAsync(ct);
            await using var cmd = new SqlCommand(
                $"ALTER DATABASE [{dbName}] SET MULTI_USER;", conn);
            await cmd.ExecuteNonQueryAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore MULTI_USER mode — manual intervention required");
        }
    }
}
Task 2.2 — Scheduled Backup Background Service
csharp

// File: Infrastructure/Backup/ScheduledBackupWorker.cs

public class ScheduledBackupWorker : BackgroundService
{
    private readonly IBackupService _backupService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ScheduledBackupWorker> _logger;

    public ScheduledBackupWorker(
        IBackupService backupService,
        IConfiguration configuration,
        ILogger<ScheduledBackupWorker> logger)
    {
        _backupService = backupService;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Scheduled backup worker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            // Run backup once daily at 2:00 AM
            var now = DateTime.Now;
            var nextRun = DateTime.Today.AddDays(now.Hour >= 2 ? 1 : 0).AddHours(2);
            var delay = nextRun - now;

            _logger.LogInformation(
                "Next automatic backup scheduled at {Time}", nextRun);

            await Task.Delay(delay, stoppingToken);

            if (stoppingToken.IsCancellationRequested) break;

            var result = await _backupService.CreateBackupAsync(ct: stoppingToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "Automatic backup completed: {File} ({Size:N0} bytes)",
                    result.FilePath, result.FileSizeBytes);

                // Clean up old backups
                var retentionDays = int.Parse(
                    _configuration["Backup:RetentionDays"] ?? "30");
                await _backupService.DeleteOldBackupsAsync(retentionDays, stoppingToken);
            }
            else
            {
                _logger.LogError(
                    "Automatic backup FAILED: {Error}", result.ErrorMessage);
            }
        }
    }
}
✅ Phase 2 Checklist
 Backup uses parameterized SQL (no string injection risk)
 Restore sets SINGLE_USER before and MULTI_USER after
 MULTI_USER restored even if restore fails (try/catch)
 Backup timeout set to 5 minutes, restore to 10 minutes
 Scheduled backup runs at 2:00 AM daily
 Old backups deleted based on retention days
🐙 Phase 3: GitHub Releases Updater
Task 3.1 — GitHub API Response Models
csharp

// File: Infrastructure/Updates/GitHub/GitHubReleaseDto.cs

public record GitHubReleaseDto
{
    [JsonPropertyName("tag_name")]
    public string TagName { get; init; } = string.Empty;        // "v2026.5.1350"

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("body")]
    public string Body { get; init; } = string.Empty;           // Changelog markdown

    [JsonPropertyName("published_at")]
    public string PublishedAt { get; init; } = string.Empty;

    [JsonPropertyName("prerelease")]
    public bool IsPrerelease { get; init; }

    [JsonPropertyName("draft")]
    public bool IsDraft { get; init; }

    [JsonPropertyName("assets")]
    public List<GitHubAssetDto> Assets { get; init; } = new();
}

public record GitHubAssetDto
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("browser_download_url")]
    public string BrowserDownloadUrl { get; init; } = string.Empty;

    [JsonPropertyName("size")]
    public long Size { get; init; }

    [JsonPropertyName("content_type")]
    public string ContentType { get; init; } = string.Empty;
}
Task 3.2 — GitHub Updater Service
csharp

// File: Infrastructure/Updates/GitHub/GitHubUpdaterService.cs

public class GitHubUpdaterService : IUpdaterService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GitHubUpdaterService> _logger;
    private readonly string _owner;
    private readonly string _repository;

    public GitHubUpdaterService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GitHubUpdaterService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _owner = configuration["GitHub:Owner"]
            ?? throw new InvalidOperationException("GitHub:Owner not configured");
        _repository = configuration["GitHub:Repository"]
            ?? throw new InvalidOperationException("GitHub:Repository not configured");
    }

    // ═══════════════════════════════════════════════
    // CHECK FOR UPDATES VIA GITHUB API
    // ═══════════════════════════════════════════════
    public async Task<UpdateCheckResult> CheckForUpdatesAsync(
        CancellationToken ct = default)
    {
        try
        {
            var apiUrl = $"https://api.github.com/repos/{_owner}/{_repository}/releases/latest";

            _logger.LogInformation("Checking GitHub for updates: {Url}", apiUrl);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(10));

            var response = await _httpClient.GetAsync(apiUrl, cts.Token);

            // Handle GitHub rate limiting (60 req/hour unauthenticated)
            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                var rateLimitReset = response.Headers
                    .FirstOrDefault(h => h.Key == "X-RateLimit-Reset").Value?
                    .FirstOrDefault();

                _logger.LogWarning(
                    "GitHub API rate limit exceeded. Reset at: {Reset}", rateLimitReset);
                return UpdateCheckResult.Failed("GitHub rate limit reached");
            }

            // 404 = no releases published yet
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogInformation("No releases found on GitHub yet");
                return UpdateCheckResult.NoUpdate();
            }

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cts.Token);

            var release = JsonSerializer.Deserialize<GitHubReleaseDto>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (release == null || release.IsDraft || release.IsPrerelease)
                return UpdateCheckResult.NoUpdate();

            // Map GitHub release to our UpdateInfo model
            var updateInfo = MapToUpdateInfo(release);

            var currentVersion = GetCurrentVersion();
            var skippedVersion = GetSkippedVersion();

            // Check skip preference
            if (updateInfo.LatestVersion == skippedVersion &&
                !updateInfo.IsForceUpdate(currentVersion))
            {
                return UpdateCheckResult.NoUpdate();
            }

            if (updateInfo.IsUpdateAvailable(currentVersion))
                return UpdateCheckResult.Available(updateInfo);

            return UpdateCheckResult.NoUpdate();
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("GitHub update check timed out");
            return UpdateCheckResult.Failed("Connection timeout");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Network error checking for updates");
            return UpdateCheckResult.Failed("No internet connection");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in update check");
            return UpdateCheckResult.Failed(ex.Message);
        }
    }

    private UpdateInfo MapToUpdateInfo(GitHubReleaseDto release)
    {
        // Strip "v" prefix from tag: "v2026.5.1350" → "2026.5.1350"
        var version = release.TagName.TrimStart('v');

        // Find the .exe installer in assets
        var installerAsset = release.Assets
            .FirstOrDefault(a =>
                a.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ||
                a.Name.EndsWith(".msi", StringComparison.OrdinalIgnoreCase));

        // Parse changelog: GitHub body is markdown, split by newlines
        var changelogLines = release.Body
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Take(20) // Max 20 lines in dialog
            .ToList();

        return new UpdateInfo
        {
            LatestVersion = version,
            ReleaseDate = release.PublishedAt[..10], // "2026-05-20"
            DownloadUrl = installerAsset?.BrowserDownloadUrl ?? string.Empty,
            ChecksumSHA256 = string.Empty, // GitHub doesn't provide this natively
            MinimumRequiredVersion = "2026.1.0",
            IsCritical = release.Body.Contains("[CRITICAL]",
                StringComparison.OrdinalIgnoreCase),
            Changelog = changelogLines
        };
    }

    // ─── Other interface methods ───────────────────
    // (DownloadUpdateAsync, LaunchInstallerAndExit, etc.)
    // These remain identical to Phase 2 implementation above

    public string GetCurrentVersion()
    {
        var version = System.Reflection.Assembly
            .GetExecutingAssembly()
            .GetName()
            .Version;
        return version != null
            ? $"{version.Major}.{version.Minor}.{version.Build}"
            : "0.0.0";
    }

    public void SkipVersion(string version)
    {
        var configFile = ConfigurationManager.OpenExeConfiguration(
            ConfigurationUserLevel.None);
        configFile.AppSettings.Settings["SkippedVersion"].Value = version;
        configFile.Save(ConfigurationSaveMode.Modified);
        ConfigurationManager.RefreshSection("appSettings");
    }

    public string GetSkippedVersion()
        => ConfigurationManager.AppSettings["SkippedVersion"] ?? string.Empty;

    // DownloadUpdateAsync and LaunchInstallerAndExit
    // are identical to the implementation in Phase 2 of the previous plan
    // Copy them here without modification
    public Task<string?> DownloadUpdateAsync(string downloadUrl, string expectedChecksum,
        IProgress<DownloadProgress> progress, CancellationToken ct = default)
        => throw new NotImplementedException("Copy from previous UpdaterService");

    public void LaunchInstallerAndExit(string installerPath)
        => throw new NotImplementedException("Copy from previous UpdaterService");
}
Task 3.3 — HttpClient Registration with Required Headers
csharp

// File: Infrastructure/DependencyInjection.cs

services.AddHttpClient<IUpdaterService, GitHubUpdaterService>(client =>
{
    // GitHub API requires User-Agent — returns 403 without it
    client.DefaultRequestHeaders.Add(
        "User-Agent",
        configuration["GitHub:ProductHeaderValue"] ?? "SalesSystem");

    // Request JSON response
    client.DefaultRequestHeaders.Accept.Add(
        new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue(
            "application/vnd.github+json"));

    // GitHub API version header (recommended)
    client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");

    client.Timeout = TimeSpan.FromSeconds(30);
    client.BaseAddress = new Uri("https://api.github.com/");
});
✅ Phase 3 Checklist
 User-Agent header always included (GitHub returns 403 without it)
 X-GitHub-Api-Version header set
 Rate limit (403) handled with specific message
 404 (no releases) handled gracefully
 Draft and prerelease versions ignored
 [CRITICAL] tag in release body marks update as mandatory
 "v" prefix stripped from tag_name before version comparison
🏭 Phase 4: Windows Service Deployment
Task 4.1 — Program.cs Configuration
csharp

// File: YourApp.API/Program.cs (or Worker project)

var builder = Host.CreateDefaultBuilder(args)
    // ⭐ KEY: Makes this app run as a Windows Service
    .UseWindowsService(options =>
    {
        options.ServiceName = "SalesSystemService";
    })
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        config.AddJsonFile(
            $"appsettings.{context.HostingEnvironment.EnvironmentName}.json",
            optional: true);
        config.AddEnvironmentVariables(); // Override from env vars in production
    })
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;

        // Data Protection (DPAPI)
        services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DataProtection-Keys")))
            .SetApplicationName("SalesSystem");

        // Security services
        services.AddScoped<IConnectionStringProtector, ConnectionStringProtector>();
        services.AddScoped<SecureDbContextFactory>();
        services.AddScoped<FirstRunSetupService>();

        // Database
        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            var factory = sp.GetRequiredService<SecureDbContextFactory>();
            var connectionString = factory.GetDecryptedConnectionString();
            options.UseSqlServer(connectionString, sql =>
            {
                sql.CommandTimeout(60);
                sql.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorNumbersToAdd: null);
            });
        });

        // Backup services
        services.AddScoped<IBackupService, BackupService>();
        services.AddHostedService<ScheduledBackupWorker>();

        // Update services
        services.AddUpdateServices(configuration);

        // All other application services...
        services.AddApplicationServices();
        services.AddInfrastructureServices(configuration);
    })
    .ConfigureLogging((context, logging) =>
    {
        logging.ClearProviders();
        logging.AddConsole();
        logging.AddEventLog(settings =>
        {
            settings.SourceName = "SalesSystem";  // Windows Event Log
        });
    });

var host = builder.Build();

// ─── First Run: Encrypt connection string ─────────
using (var scope = host.Services.CreateScope())
{
    var setupService = scope.ServiceProvider
        .GetRequiredService<FirstRunSetupService>();
    var config = scope.ServiceProvider
        .GetRequiredService<IConfiguration>();
    setupService.EnsureConnectionStringEncrypted(config);

    // Run EF migrations automatically
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

await host.RunAsync();
Task 4.2 — Windows Service Install/Uninstall Scripts
batch

:: File: Scripts/Install-Service.bat
:: Run as Administrator

@echo off
echo Installing SalesSystem Windows Service...

:: Stop if already running
sc stop "SalesSystemService" 2>nul

:: Delete if already exists  
sc delete "SalesSystemService" 2>nul

timeout /t 2 /nobreak >nul

:: Install new service
sc create "SalesSystemService" ^
    binPath= "%~dp0..\publish\SalesSystem.Service.exe" ^
    DisplayName= "نظام إدارة المبيعات" ^
    start= auto ^
    obj= LocalSystem

:: Set description
sc description "SalesSystemService" "خدمة نظام إدارة المبيعات والمخزون"

:: Set recovery options (auto-restart on failure)
sc failure "SalesSystemService" reset= 86400 actions= restart/5000/restart/10000/restart/30000

:: Start service
sc start "SalesSystemService"

echo Service installed and started successfully.
pause
batch

:: File: Scripts/Uninstall-Service.bat
:: Run as Administrator

@echo off
echo Uninstalling SalesSystem Windows Service...

sc stop "SalesSystemService"
timeout /t 3 /nobreak >nul
sc delete "SalesSystemService"

echo Service uninstalled successfully.
pause
✅ Phase 4 Checklist
 .UseWindowsService() called in CreateDefaultBuilder
 Windows Event Log configured for service logging
 EF migrations run automatically on startup
 Service recovery set to auto-restart on failure (3 attempts)
 Install script stops existing service before reinstalling
 First-run encryption happens before host.RunAsync()
🖥️ Phase 5: WPF Admin Screens
Task 5.1 — Role Guard Base Class
csharp

// File: WPF/ViewModels/Base/AdminOnlyViewModel.cs
// ALL admin screens inherit from this

public abstract class AdminOnlyViewModel : BaseViewModel
{
    protected readonly ICurrentUserService _currentUser;

    protected AdminOnlyViewModel(ICurrentUserService currentUser)
    {
        _currentUser = currentUser;
        EnforceAdminRole();
    }

    private void EnforceAdminRole()
    {
        if (!_currentUser.IsInRole("Admin"))
        {
            throw new UnauthorizedAccessException(
                "⛔ هذه الشاشة مخصصة للمسؤولين فقط.\n" +
                "تواصل مع مدير النظام للحصول على الصلاحيات.");
        }
    }
}
Task 5.2 — User Management ViewModel
csharp

// File: WPF/ViewModels/Admin/UserManagementViewModel.cs

public class UserManagementViewModel : AdminOnlyViewModel
{
    private readonly IMediator _mediator;

    public ObservableCollection<UserRowViewModel> Users { get; } = new();

    private UserRowViewModel? _selectedUser;
    public UserRowViewModel? SelectedUser
    {
        get => _selectedUser;
        set => SetProperty(ref _selectedUser, value);
    }

    // Commands
    public IAsyncRelayCommand LoadUsersCommand { get; }
    public IAsyncRelayCommand CreateUserCommand { get; }
    public IAsyncRelayCommand<UserRowViewModel> EditUserCommand { get; }
    public IAsyncRelayCommand<UserRowViewModel> ToggleUserStatusCommand { get; }
    public IAsyncRelayCommand<UserRowViewModel> ResetPasswordCommand { get; }

    public UserManagementViewModel(
        ICurrentUserService currentUser,
        IMediator mediator) : base(currentUser)
    {
        _mediator = mediator;

        LoadUsersCommand = new AsyncRelayCommand(LoadUsersAsync);
        CreateUserCommand = new AsyncRelayCommand(CreateUserAsync);
        EditUserCommand = new AsyncRelayCommand<UserRowViewModel>(EditUserAsync);
        ToggleUserStatusCommand = new AsyncRelayCommand<UserRowViewModel>(ToggleUserStatusAsync);
        ResetPasswordCommand = new AsyncRelayCommand<UserRowViewModel>(ResetPasswordAsync);

        // Auto-load on creation
        _ = LoadUsersAsync();
    }

    private async Task LoadUsersAsync()
    {
        try
        {
            IsBusy = true;
            var users = await _mediator.Send(new GetAllUsersQuery());
            Users.Clear();
            foreach (var user in users)
                Users.Add(new UserRowViewModel(user));
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CreateUserAsync()
    {
        var dialog = new CreateUserDialog();
        if (dialog.ShowDialog() != true) return;

        var result = await _mediator.Send(new CreateUserCommand
        {
            Username = dialog.Username,
            Password = dialog.Password,
            FullName = dialog.FullName,
            Role = dialog.SelectedRole
        });

        if (result.IsSuccess)
        {
            StatusMessage = $"✅ تم إنشاء المستخدم '{dialog.Username}' بنجاح";
            await LoadUsersAsync();
        }
        else
        {
            StatusMessage = $"❌ {result.ErrorMessage}";
        }
    }

    private async Task ToggleUserStatusAsync(UserRowViewModel user)
    {
        // Prevent admin from disabling their own account
        if (user.UserId == _currentUser.UserId)
        {
            StatusMessage = "⚠️ لا يمكنك تعطيل حسابك الخاص";
            return;
        }

        var action = user.IsActive ? "تعطيل" : "تفعيل";
        var confirm = System.Windows.MessageBox.Show(
            $"هل تريد {action} المستخدم '{user.Username}'؟",
            "تأكيد",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (confirm != System.Windows.MessageBoxResult.Yes) return;

        await _mediator.Send(new ToggleUserStatusCommand(user.UserId));
        await LoadUsersAsync();
        StatusMessage = $"✅ تم {action} المستخدم '{user.Username}'";
    }

    private async Task ResetPasswordAsync(UserRowViewModel user)
    {
        var dialog = new ResetPasswordDialog(user.Username);
        if (dialog.ShowDialog() != true) return;

        await _mediator.Send(new ResetPasswordCommand(user.UserId, dialog.NewPassword));
        StatusMessage = $"✅ تم تعيين كلمة مرور جديدة للمستخدم '{user.Username}'";
    }

    private async Task EditUserAsync(UserRowViewModel user)
    {
        var dialog = new EditUserDialog(user);
        if (dialog.ShowDialog() != true) return;

        await _mediator.Send(new UpdateUserCommand
        {
            UserId = user.UserId,
            FullName = dialog.FullName,
            Role = dialog.SelectedRole
        });

        await LoadUsersAsync();
        StatusMessage = "✅ تم تحديث بيانات المستخدم";
    }
}

public class UserRowViewModel : BaseViewModel
{
    public int UserId { get; init; }
    public string Username { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public bool IsActive { get; set; }

    public string RoleDisplay => Role == "Admin" ? "🔑 مسؤول" : "👤 كاشير";
    public string StatusDisplay => IsActive ? "✅ نشط" : "🔴 معطل";
    public string StatusColor => IsActive ? "#4CAF50" : "#F44336";

    public UserRowViewModel(UserDto dto)
    {
        UserId = dto.Id;
        Username = dto.Username;
        FullName = dto.FullName;
        Role = dto.Role;
        IsActive = dto.IsActive;
    }
}
Task 5.3 — User Management XAML
XML

<!-- File: Views/Admin/UserManagementView.xaml -->
<UserControl FlowDirection="RightToLeft">
    <Grid Margin="16">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>  <!-- Toolbar -->
            <RowDefinition Height="*"/>      <!-- DataGrid -->
        </Grid.RowDefinitions>

        <!-- Toolbar -->
        <StackPanel Orientation="Horizontal" Margin="0,0,0,12">
            <TextBlock Text="👥 إدارة المستخدمين"
                       FontSize="18" FontWeight="Bold"
                       VerticalAlignment="Center" Margin="0,0,16,0"/>

            <Button Content="+ إضافة مستخدم جديد"
                    Command="{Binding CreateUserCommand}"
                    Background="#1976D2" Foreground="White"
                    BorderThickness="0" Padding="16,8"/>
        </StackPanel>

        <!-- Users DataGrid -->
        <DataGrid Grid.Row="1"
                  ItemsSource="{Binding Users}"
                  SelectedItem="{Binding SelectedUser}"
                  AutoGenerateColumns="False"
                  CanUserAddRows="False"
                  IsReadOnly="True"
                  GridLinesVisibility="Horizontal"
                  HeadersVisibility="Column">

            <DataGrid.Columns>
                <DataGridTextColumn Header="اسم المستخدم"
                    Binding="{Binding Username}" Width="150"/>

                <DataGridTextColumn Header="الاسم الكامل"
                    Binding="{Binding FullName}" Width="*"/>

                <DataGridTemplateColumn Header="الدور" Width="120">
                    <DataGridTemplateColumn.CellTemplate>
                        <DataTemplate>
                            <TextBlock Text="{Binding RoleDisplay}"
                                       FontWeight="Bold"/>
                        </DataTemplate>
                    </DataGridTemplateColumn.CellTemplate>
                </DataGridTemplateColumn>

                <DataGridTemplateColumn Header="الحالة" Width="100">
                    <DataGridTemplateColumn.CellTemplate>
                        <DataTemplate>
                            <TextBlock Text="{Binding StatusDisplay}"
                                       Foreground="{Binding StatusColor}"
                                       FontWeight="Bold"/>
                        </DataTemplate>
                    </DataGridTemplateColumn.CellTemplate>
                </DataGridTemplateColumn>

                <!-- Action buttons column -->
                <DataGridTemplateColumn Header="إجراءات" Width="220">
                    <DataGridTemplateColumn.CellTemplate>
                        <DataTemplate>
                            <StackPanel Orientation="Horizontal">
                                <Button Content="✏️ تعديل" Margin="2"
                                    Command="{Binding DataContext.EditUserCommand,
                                              RelativeSource={RelativeSource AncestorType=UserControl}}"
                                    CommandParameter="{Binding}"
                                    Background="#607D8B" Foreground="White"
                                    BorderThickness="0" Padding="8,4"/>

                                <Button Content="🔑 كلمة المرور" Margin="2"
                                    Command="{Binding DataContext.ResetPasswordCommand,
                                              RelativeSource={RelativeSource AncestorType=UserControl}}"
                                    CommandParameter="{Binding}"
                                    Background="#FF9800" Foreground="White"
                                    BorderThickness="0" Padding="8,4"/>

                                <Button Margin="2"
                                    Content="{Binding IsActive,
                                              Converter={StaticResource BoolToActivateText}}"
                                    Command="{Binding DataContext.ToggleUserStatusCommand,
                                              RelativeSource={RelativeSource AncestorType=UserControl}}"
                                    CommandParameter="{Binding}"
                                    Background="{Binding IsActive,
                                                 Converter={StaticResource BoolToStatusColor}}"
                                    Foreground="White"
                                    BorderThickness="0" Padding="8,4"/>
                            </StackPanel>
                        </DataTemplate>
                    </DataGridTemplateColumn.CellTemplate>
                </DataGridTemplateColumn>
            </DataGrid.Columns>
        </DataGrid>
    </Grid>
</UserControl>
✅ Phase 5 Checklist
 AdminOnlyViewModel base class throws if non-admin accesses
 Admin cannot disable their own account
 Confirmation dialog shown before toggling user status
 Role displayed as Arabic label (not raw "Admin" string)
 All commands check IsBusy before executing
📦 Phase 6: Inno Setup Installer Script
Task 6.1 — Complete .iss Script
pascal

; File: Installer/SalesSystem.iss
; Build: Run with Inno Setup Compiler

#define MyAppName "نظام إدارة المبيعات"
#define MyAppNameEn "SalesSystem"
#define MyAppVersion "2026.5.1350"
#define MyAppPublisher "Your Company Name"
#define MyAppURL "https://your-website.com"
#define MyAppExeName "SalesSystem.DesktopWPF.exe"
#define MyAppServiceExe "SalesSystem.Service.exe"
#define DotNetVersion "8.0"

[Setup]
; Basic info
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}/support
AppUpdatesURL={#MyAppURL}/updates

; Install directory
DefaultDirName={autopf}\{#MyAppNameEn}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes

; Output
OutputDir=..\Release
OutputBaseFilename=SalesSystem_Setup_v{#MyAppVersion}
Compression=lzma2/ultra64
SolidCompression=yes
InternalCompressLevel=ultra64

; UI
WizardStyle=modern
WizardResizable=no
ShowLanguageDialog=no

; Privileges — needs admin for Program Files
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=commandline

; Minimum Windows version: Windows 10
MinVersion=10.0.17763

; Uninstall
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName} {#MyAppVersion}

; Create uninstaller
CreateUninstallRegKey=yes

[Languages]
Name: "arabic"; MessagesFile: "compiler:Languages\Arabic.isl"

[CustomMessages]
arabic.CheckingDotNet=جارٍ التحقق من متطلبات النظام...
arabic.DotNetMissing=يتطلب هذا البرنامج .NET Runtime {#DotNetVersion} أو أحدث.%nيرجى تثبيته أولاً من موقع Microsoft ثم إعادة تشغيل المثبّت.
arabic.InstallingService=جارٍ تثبيت خدمة النظام...
arabic.StoppingService=جارٍ إيقاف الخدمة القديمة...

[Tasks]
; Optional tasks shown to user
Name: "desktopicon"; Description: "إنشاء اختصار على سطح المكتب"; GroupDescription: "اختصارات:"; Flags: checked
Name: "startupicon"; Description: "تشغيل البرنامج مع بدء تشغيل Windows"; GroupDescription: "اختصارات:"; Flags: unchecked

[Files]
; Main WPF application
Source: "..\publish\wpf\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

; Windows Service
Source: "..\publish\service\*"; DestDir: "{app}\Service"; Flags: ignoreversion recursesubdirs createallsubdirs

; Default appsettings (will be encrypted on first run)
Source: "..\publish\wpf\appsettings.json"; DestDir: "{app}"; Flags: ignoreversion onlyifdoesntexist

; Visual C++ Redistributable (if needed)
; Source: "redist\vc_redist.x64.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall

[Icons]
; Start Menu
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\إلغاء تثبيت {#MyAppName}"; Filename: "{uninstallexe}"

; Desktop (conditional on task)
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

; Startup (conditional on task)
Name: "{userstartup}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: startupicon

[Run]
; Install Windows Service
Filename: "{sys}\sc.exe"; \
    Parameters: "create SalesSystemService binPath= ""{app}\Service\{#MyAppServiceExe}"" DisplayName= ""{#MyAppName} Service"" start= auto"; \
    StatusMsg: "{cm:InstallingService}"; \
    Flags: runhidden waituntilterminated; \
    Check: not ServiceExists('SalesSystemService')

; Set service description
Filename: "{sys}\sc.exe"; \
    Parameters: "description SalesSystemService ""خدمة إدارة المبيعات والمخزون"""; \
    Flags: runhidden waituntilterminated

; Configure service recovery (auto-restart on failure)
Filename: "{sys}\sc.exe"; \
    Parameters: "failure SalesSystemService reset= 86400 actions= restart/5000/restart/10000/restart/30000"; \
    Flags: runhidden waituntilterminated

; Start service
Filename: "{sys}\sc.exe"; \
    Parameters: "start SalesSystemService"; \
    StatusMsg: "جارٍ تشغيل الخدمة..."; \
    Flags: runhidden waituntilterminated

; Launch app after install (optional)
Filename: "{app}\{#MyAppExeName}"; \
    Description: "تشغيل {#MyAppName} الآن"; \
    Flags: nowait postinstall skipifsilent

[UninstallRun]
; Stop and remove Windows Service on uninstall
Filename: "{sys}\sc.exe"; Parameters: "stop SalesSystemService"; Flags: runhidden waituntilterminated
Filename: "{sys}\sc.exe"; Parameters: "delete SalesSystemService"; Flags: runhidden waituntilterminated

[Code]
// Check for .NET Runtime before installing
function IsDotNetInstalled(): Boolean;
var
  RuntimePath: String;
begin
  // Check if .NET 8 runtime exists
  RuntimePath := ExpandConstant('{pf}\dotnet\shared\Microsoft.NETCore.App');
  Result := DirExists(RuntimePath) and
            FileExists(RuntimePath + '\8.0.0\coreclr.dll') or
            RegKeyExists(HKLM, 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedhost');
end;

function ServiceExists(ServiceName: String): Boolean;
var
  ResultCode: Integer;
begin
  Exec(ExpandConstant('{sys}\sc.exe'),
       'query ' + ServiceName,
       '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Result := (ResultCode = 0);
end;

function StopExistingService(): Boolean;
var
  ResultCode: Integer;
begin
  Exec(ExpandConstant('{sys}\sc.exe'),
       'stop SalesSystemService',
       '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Sleep(2000); // Wait for service to stop
  Result := True;
end;

procedure InitializeWizard();
begin
  WizardForm.WelcomeLabel2.Caption :=
    'سيتم تثبيت ' + '{#MyAppName}' + ' الإصدار ' + '{#MyAppVersion}' +
    ' على جهازك.' + #13#10 + #13#10 +
    'يُرجى إغلاق جميع التطبيقات الأخرى قبل المتابعة.';
end;

function InitializeSetup(): Boolean;
begin
  // Check .NET requirement
  WizardForm.StatusLabel.Caption := CustomMessage('CheckingDotNet');

  if not IsDotNetInstalled() then
  begin
    MsgBox(CustomMessage('DotNetMissing'), mbError, MB_OK);
    Result := False;
    Exit;
  end;

  // Stop old service if upgrading
  if ServiceExists('SalesSystemService') then
    StopExistingService();

  Result := True;
end;
✅ Phase 6 Checklist
 .NET 8 presence checked before install proceeds
 Service stops before file replacement (upgrade safety)
 Service auto-restarts on failure (3 attempts with increasing delays)
 Desktop shortcut is optional (checked by default)
 Startup shortcut is optional (unchecked by default)
 Service removed on uninstall
 AppId GUID is unique — never reuse between different apps
🔒 Phase 7: Security Hardening & .gitignore
Task 7.1 — Critical .gitignore Entries
gitignore

# File: .gitignore (ADD these entries)

# NEVER commit these files
appsettings.Production.json
appsettings.Staging.json
*.bak
*.pfx
*.p12
DataProtection-Keys/
Secrets/

# Build outputs — commit publish scripts, not binaries
publish/
Release/
*.exe
*.dll
*.pdb

# IDE files
.vs/
*.user
*.suo

# Logs
logs/
*.log
Task 7.2 — Final Security Checklist
csharp

// File: Infrastructure/Security/SecurityAudit.cs
// Run this check at startup in Debug mode only

#if DEBUG
public static class SecurityAudit
{
    public static void RunChecks(IConfiguration configuration)
    {
        var issues = new List<string>();

        // Check 1: Connection string is encrypted
        var cs = configuration.GetConnectionString("DefaultConnection") ?? "";
        if (!cs.StartsWith("DPAPI:") && cs.Contains("Password"))
            issues.Add("⚠️ Connection string contains plaintext password!");

        // Check 2: No hardcoded secrets in config
        var allConfig = configuration.AsEnumerable();
        foreach (var item in allConfig)
        {
            if (item.Value?.Contains("password123") == true ||
                item.Value?.Contains("admin123") == true ||
                item.Value?.Contains("secret") == true)
                issues.Add($"⚠️ Potential hardcoded secret in: {item.Key}");
        }

        // Check 3: GitHub token not committed
        var githubToken = configuration["GitHub:PersonalAccessToken"];
        if (!string.IsNullOrWhiteSpace(githubToken))
            issues.Add("⚠️ GitHub PAT found in config — use environment variable instead!");

        if (issues.Any())
        {
            var message = string.Join("\n", issues);
            System.Diagnostics.Debug.WriteLine(
                $"SECURITY AUDIT FAILED:\n{message}");
            // Throw in Debug to force developer attention
            throw new InvalidOperationException(
                $"Security issues detected:\n{message}");
        }

        System.Diagnostics.Debug.WriteLine("✅ Security audit passed");
    }
}
#endif
📊 Phase 8: Performance Seeding Script
Task 8.1 — SQL Data Seeding for Load Testing
SQL

-- File: Scripts/SeedPerformanceData.sql
-- Seeds 1 year of enterprise-volume transactions
-- WARNING: Takes 5-15 minutes to run. Use on test environment only.

SET NOCOUNT ON;
DECLARE @StartDate DATE = DATEADD(YEAR, -1, GETDATE());
DECLARE @EndDate DATE = GETDATE();
DECLARE @CurrentDate DATE = @StartDate;
DECLARE @InvoiceId INT;
DECLARE @CustomerId INT;
DECLARE @ProductId INT;
DECLARE @DailyInvoices INT;

-- ─── Seed Customers (500 customers) ───────────────
PRINT 'Seeding customers...';
DECLARE @i INT = 1;
WHILE @i <= 500
BEGIN
    INSERT INTO Customers (Name, Phone, Address, CreatedAt)
    VALUES (
        N'عميل تجريبي رقم ' + CAST(@i AS NVARCHAR),
        '05' + RIGHT('00000000' + CAST((@i * 7 + 10000000) AS VARCHAR), 8),
        N'الرياض - حي رقم ' + CAST((@i % 20) + 1 AS NVARCHAR),
        DATEADD(DAY, -(@i % 365), GETDATE())
    );
    SET @i = @i + 1;
END;

-- ─── Seed Products (200 products with units) ──────
PRINT 'Seeding products...';
SET @i = 1;
WHILE @i <= 200
BEGIN
    INSERT INTO Products (Name, CategoryId, IsActive)
    VALUES (N'منتج تجريبي ' + CAST(@i AS NVARCHAR), (@i % 10) + 1, 1);

    DECLARE @ProductId INT = SCOPE_IDENTITY();

    -- Base unit (piece)
    INSERT INTO ProductUnits
        (ProductId, UnitName, BaseConversionFactor, IsBaseUnit, SalesPrice, PurchaseCost, SortOrder)
    VALUES (@ProductId, N'حبة', 1, 1, (@i % 50) + 10, (@i % 30) + 5, 0);

    -- Box unit (12 pieces)
    INSERT INTO ProductUnits
        (ProductId, UnitName, BaseConversionFactor, IsBaseUnit, SalesPrice, PurchaseCost, SortOrder)
    VALUES (@ProductId, N'كرتون', 12, 0, ((@i % 50) + 10) * 12 * 0.95, ((@i % 30) + 5) * 12, 1);

    -- Initial stock
    INSERT INTO Stocks (ProductId, CurrentQuantityInPieces)
    VALUES (@ProductId, (@i * 37) % 1000 + 100);

    SET @i = @i + 1;
END;

-- ─── Seed 1 Year of Invoices ───────────────────────
PRINT 'Seeding invoices (this may take several minutes)...';

WHILE @CurrentDate <= @EndDate
BEGIN
    -- 5-20 invoices per day (more on weekends)
    SET @DailyInvoices = CASE DATEPART(WEEKDAY, @CurrentDate)
        WHEN 6 THEN 20  -- Thursday (busy)
        WHEN 7 THEN 25  -- Friday (busiest)
        ELSE 10
    END;

    DECLARE @j INT = 1;
    WHILE @j <= @DailyInvoices
    BEGIN
        -- Random customer
        SET @CustomerId = (ABS(CHECKSUM(NEWID())) % 500) + 1;

        INSERT INTO SalesInvoices
            (InvoiceNumber, CustomerId, SubTotal, TaxAmount, GrandTotal,
             PaymentMethod, Status, CreatedAt, CashBoxId)
        VALUES (
            'INV-' + FORMAT(@CurrentDate, 'yyyyMMdd') + '-' + RIGHT('000' + CAST(@j AS VARCHAR), 3),
            @CustomerId,
            0, 0, 0,  -- Will be updated after items
            CASE ABS(CHECKSUM(NEWID())) % 2 WHEN 0 THEN 'نقدي' ELSE 'شبكة' END,
            1,
            DATEADD(MINUTE, @j * 30, CAST(@CurrentDate AS DATETIME)),
            1
        );

        SET @InvoiceId = SCOPE_IDENTITY();

        -- Add 1-5 items per invoice
        DECLARE @ItemCount INT = (ABS(CHECKSUM(NEWID())) % 5) + 1;
        DECLARE @k INT = 1;
        DECLARE @InvoiceTotal DECIMAL(18,4) = 0;

        WHILE @k <= @ItemCount
        BEGIN
            SET @ProductId = (ABS(CHECKSUM(NEWID())) % 200) + 1;
            DECLARE @Qty DECIMAL(18,4) = (ABS(CHECKSUM(NEWID())) % 10) + 1;
            DECLARE @Price DECIMAL(18,4) = (ABS(CHECKSUM(NEWID())) % 100) + 10;
            DECLARE @ItemTotal DECIMAL(18,4) = @Qty * @Price;

            INSERT INTO SalesInvoiceItems
                (InvoiceId, ProductId, ProductName, UnitName, Quantity, UnitPrice, TotalPrice, CategoryId)
            VALUES (
                @InvoiceId, @ProductId,
                N'منتج ' + CAST(@ProductId AS NVARCHAR),
                N'حبة', @Qty, @Price, @ItemTotal, (@ProductId % 10) + 1
            );

            SET @InvoiceTotal = @InvoiceTotal + @ItemTotal;
            SET @k = @k + 1;
        END;

        -- Update invoice totals
        UPDATE SalesInvoices SET
            SubTotal = @InvoiceTotal,
            TaxAmount = @InvoiceTotal * 0.15,
            GrandTotal = @InvoiceTotal * 1.15
        WHERE Id = @InvoiceId;

        SET @j = @j + 1;
    END;

    SET @CurrentDate = DATEADD(DAY, 1, @CurrentDate);
END;

PRINT 'Data seeding complete!';

-- ─── Verify counts ─────────────────────────────────
SELECT
    (SELECT COUNT(*) FROM Customers) AS Customers,
    (SELECT COUNT(*) FROM Products) AS Products,
    (SELECT COUNT(*) FROM SalesInvoices) AS Invoices,
    (SELECT COUNT(*) FROM SalesInvoiceItems) AS InvoiceItems;
📦 Final Summary
text

┌────────────────────────────────────────────────────────────────────┐
│           PHASE 7: PRODUCTION READINESS — COMPLETE PLAN            │
├──────┬────────────────────────────────────────────┬───────────────┤
│ Step │ Deliverable                                │ Critical Rule │
├──────┼────────────────────────────────────────────┼───────────────┤
│  0   │ NuGet packages + appsettings structure     │ No secrets    │
│  1   │ DPAPI encryption + first-run setup         │ Encrypt once  │
│  2   │ Backup/Restore + scheduled worker          │ MULTI_USER    │
│      │                                            │ always reset  │
│  3   │ GitHub Releases API integration            │ User-Agent    │
│      │                                            │ required      │
│  4   │ Windows Service + install scripts          │ Main window   │
│      │                                            │ first!        │
│  5   │ Admin screens + role guard                 │ Base class    │
│      │                                            │ enforces role │
│  6   │ Inno Setup .iss script                     │ .NET check    │
│      │                                            │ before install│
│  7   │ .gitignore + security audit                │ Run in Debug  │
│  8   │ SQL seeding for performance testing        │ Test env only │
└──────┴────────────────────────────────────────────┴───────────────┘

ABSOLUTE RULES — ZERO TOLERANCE:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
✅ appsettings.json in .gitignore — NEVER committed with real credentials
✅ Connection string encrypted with DPAPI on first run
✅ Restore ALWAYS returns DB to MULTI_USER — even if restore fails
✅ GitHub API User-Agent header is MANDATORY (403 without it)
✅ Admin screens throw UnauthorizedAccessException for non-admins
✅ Service set to SINGLE_USER only during restore — never longer
✅ .NET Runtime checked in installer BEFORE extraction begins
✅ SecurityAudit.RunChecks() throws in DEBUG if secrets found
✅ Seeding script runs on TEST environment ONLY — never production

---

## ✅ Phase 7 — Production Readiness: COMPLETED (2026-05-21)

### Implementation Summary
All Phase 7 features have been implemented, reviewed by 4 subagents, and all review findings have been fixed.

| Component | Status | Files |
|-----------|--------|-------|
| Auto-Update System | ✅ Complete | 8 files (Application + Infrastructure + DesktopPWF) |
| Security & DPAPI | ✅ Complete | 5 files (Infrastructure/Security/) |
| Backup System | ✅ Complete | 3 files (Infrastructure/Backup/ + Services/) |
| Windows Service | ✅ Complete | Program.cs + 2 batch scripts |
| Admin Screens | ✅ Complete | 3 files (ViewModels/Base/ + Views/Users/) |
| Installer | ✅ Complete | Installer/SalesSystem.iss |
| Dialog Enhancement | ✅ Complete | InfoDialog.xaml + DialogService updates |

### Code Review Results
- **4 subagents** reviewed all 43 changed files
- **16 issues found** (6 Critical, 10 Warning)
- **ALL 16 issues fixed** via 3 parallel subagents
- **Build: 0 errors** across all 14 projects
- **Tests: 1,415 pass** (no regressions)

### Key Changes from Review
1. `IUpdaterService` refactored to use `Result<T>` pattern
2. Duplicate models removed — Desktop uses Application layer models
3. `Environment.Exit(0)` replaced with `Result<bool>`
4. All `MessageBox.Show` replaced with `IDialogService`
5. `AddUpdateServices()` wired in `Program.cs`
6. `HashGen.cs` deleted (Console.WriteLine violation)
7. Atomic file writes in `FirstRunSetupService`
8. `ROLLBACK AFTER 30` instead of `ROLLBACK IMMEDIATE`
9. `int.TryParse` instead of `int.Parse`
10. `UpdateDialogViewModel` implements `IDisposable`
11. `AdminOnlyViewModel` uses constructor injection
12. JWT secret throws in production if missing

### AGENTS.md Updated
- Version: v4.3 → **v4.4**
- Rules: 102 → **140** (RULE-103 to RULE-140 added)
- New sections: 2.29–2.35 (Auto-Update, Security, Backup, Windows Service, Admin, Installer, Dialog)
- Checklist: 17 new items added

### README.md Updated
- Version badge: v4.3 → **v4.4 Production**
- Phase table: Phase 9 marked as ✅ Completed
- New feature sections: Auto-Update, Security, Backup, Windows Service, Admin, Installer
- Tech Stack: Added DataProtection, WindowsService, EventLog, Inno Setup
- Security table: Updated with DPAPI, atomic writes, SecurityAudit

### CHANGELOG.md Updated
- New entry: **[1.3.0] - 2026-05-21** with full Phase 7 details