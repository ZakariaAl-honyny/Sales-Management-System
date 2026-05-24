namespace SalesSystem.Contracts.Responses;

public record UpdateManifest(
    string Version,
    string DownloadUrl,
    string Sha256Hash,
    string? ReleaseNotes
);
