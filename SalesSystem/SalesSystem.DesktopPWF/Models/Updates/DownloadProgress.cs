namespace SalesSystem.DesktopPWF.Models.Updates;

public record DownloadProgress(
    long BytesReceived,
    long TotalBytes,
    double Percentage,
    double SpeedKbps
);
