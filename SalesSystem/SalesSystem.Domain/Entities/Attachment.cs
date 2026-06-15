using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

    /// <summary>
    /// Represents a file attachment linked to any entity in the system via a
    /// polymorphic reference (ReferenceType + ReferenceId).
    /// Attachments are hard-deleted — no soft-delete or IsActive flag.
    /// </summary>
    public class Attachment : AuditableEntity
    {
        /// <summary>
        /// The entity type this attachment belongs to as string (e.g. "SalesInvoice", "PurchaseInvoice").
        /// </summary>
        public string ReferenceType { get; private set; } = string.Empty;

        /// <summary>
        /// The ID of the entity this attachment belongs to.
        /// </summary>
        public int ReferenceId { get; private set; }

        /// <summary>
        /// The stored file name (may be sanitized or renamed).
        /// </summary>
        public string FileName { get; private set; } = string.Empty;

        /// <summary>
        /// The physical or virtual path where the file is stored.
        /// </summary>
        public string FilePath { get; private set; } = string.Empty;

        /// <summary>
        /// File size in bytes.
        /// </summary>
        public long FileSize { get; private set; }

        /// <summary>
        /// Optional MIME content type (e.g. "application/pdf", "image/jpeg").
        /// </summary>
        public string? ContentType { get; private set; }

        private Attachment() { } // EF Core

    /// <summary>
    /// Factory method to create a new attachment.
    /// </summary>
    /// <param name="referenceId">The ID of the entity this attachment belongs to (must be > 0).</param>
    /// <param name="fileName">The stored file name (required, non-empty).</param>
    /// <param name="filePath">The storage path (required, non-empty).</param>
    /// <param name="fileSize">File size in bytes (must be >= 0).</param>
    /// <param name="referenceType">Optional entity type name (e.g. "SalesInvoice").</param>
    /// <param name="contentType">Optional MIME content type.</param>
    /// <returns>A new Attachment instance.</returns>
    /// <exception cref="DomainException">If any guard clause fails.</exception>
    public static Attachment Create(
        int referenceId,
        string fileName,
        string filePath,
        long fileSize,
        string? referenceType = null,
        string? contentType = null)
    {
        if (referenceId <= 0)
            throw new DomainException("معرّف المرجع غير صالح.");
        if (string.IsNullOrWhiteSpace(fileName))
            throw new DomainException("اسم الملف مطلوب.");
        if (string.IsNullOrWhiteSpace(filePath))
            throw new DomainException("مسار الملف مطلوب.");
        if (fileSize < 0)
            throw new DomainException("حجم الملف لا يمكن أن يكون سالباً.");

        return new Attachment
        {
            ReferenceType = referenceType?.Trim() ?? string.Empty,
            ReferenceId = referenceId,
            FileName = fileName.Trim(),
            FilePath = filePath.Trim(),
            FileSize = fileSize,
            ContentType = contentType?.Trim()
        };
    }

    /// <summary>
    /// Updates the file metadata (path, name, size, content type).
    /// </summary>
    /// <param name="fileName">New stored file name.</param>
    /// <param name="filePath">New storage path.</param>
    /// <param name="fileSize">New file size in bytes.</param>
    /// <param name="contentType">New MIME content type.</param>
    /// <exception cref="DomainException">If any guard clause fails.</exception>
    public void UpdateFile(
        string fileName,
        string filePath,
        long fileSize,
        string? contentType = null)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new DomainException("اسم الملف مطلوب.");
        if (string.IsNullOrWhiteSpace(filePath))
            throw new DomainException("مسار الملف مطلوب.");
        if (fileSize < 0)
            throw new DomainException("حجم الملف لا يمكن أن يكون سالباً.");

        FileName = fileName.Trim();
        FilePath = filePath.Trim();
        FileSize = fileSize;
        ContentType = contentType?.Trim();
        UpdateTimestamp();
    }
}
