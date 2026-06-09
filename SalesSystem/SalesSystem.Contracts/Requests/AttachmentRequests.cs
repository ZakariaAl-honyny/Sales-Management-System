namespace SalesSystem.Contracts.Requests;

public record UploadAttachmentRequest(string Base64Content, string FileName);
