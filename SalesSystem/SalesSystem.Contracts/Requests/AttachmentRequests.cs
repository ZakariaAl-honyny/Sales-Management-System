namespace SalesSystem.Contracts.Requests;

public record CreateAttachmentRequest(
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
