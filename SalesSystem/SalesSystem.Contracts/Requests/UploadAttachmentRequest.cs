namespace SalesSystem.Contracts.Requests;

/// <summary>
/// طلب رفع مرفق.
/// </summary>
public record UploadAttachmentRequest(string Base64Content, string? FileName);
