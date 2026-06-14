namespace SalesSystem.Contracts.Requests;

public record CreateAttachmentRequest(
    int EntityType,
    string ReferenceType,
    int ReferenceId,
    string FileName,
    string FilePath,
    long FileSize,
    string? ContentType = null);

public record UpdateAttachmentRequest(
    string FileName,
    string FilePath,
    long FileSize,
    string? ContentType = null);
