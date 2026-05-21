namespace SalesSystem.Application.Updates.Models;

public record DownloadProgress(
    long BytesReceived,
    long TotalBytes,
    double Percentage,
    double SpeedKbps
);
