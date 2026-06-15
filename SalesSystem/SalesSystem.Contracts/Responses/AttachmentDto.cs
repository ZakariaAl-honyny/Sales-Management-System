namespace SalesSystem.Contracts.Responses;

public record AttachmentDto(
    int Id,
    string ReferenceType,
    int ReferenceId,
    string FileName,
    string FilePath,
    long FileSize,
    string? ContentType,
    DateTime CreatedAt
);
