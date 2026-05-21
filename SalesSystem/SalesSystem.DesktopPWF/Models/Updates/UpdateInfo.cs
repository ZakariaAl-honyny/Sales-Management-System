namespace SalesSystem.DesktopPWF.Models.Updates;

public record UpdateInfo
{
    public string LatestVersion { get; init; } = string.Empty;
    public string ReleaseDate { get; init; } = string.Empty;
    public string DownloadUrl { get; init; } = string.Empty;
    public string ChecksumSHA256 { get; init; } = string.Empty;
    public string MinimumRequiredVersion { get; init; } = string.Empty;
    public bool IsCritical { get; init; }
    public List<string> Changelog { get; init; } = new();

    public bool IsUpdateAvailable(string currentVersion)
    {
        if (!Version.TryParse(LatestVersion, out var serverVer)) return false;
        if (!Version.TryParse(currentVersion, out var currentVer)) return false;
        return serverVer > currentVer;
    }

    public bool IsForceUpdate(string currentVersion)
    {
        if (!Version.TryParse(MinimumRequiredVersion, out var minVer)) return false;
        if (!Version.TryParse(currentVersion, out var currentVer)) return false;
        return currentVer < minVer;
    }
}
