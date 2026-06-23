using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Per-document-type auto-increment sequence tracker.
/// Schema: DocumentType (unique), NextNumber (int). No Prefix/Year.
/// Thread-safe service layer (SemaphoreSlim) handles concurrency.
/// </summary>
public class DocumentSequence : AuditableEntity
{
    /// <summary>
    /// Unique document type key (e.g., "SalesInvoice", "PurchaseInvoice", "PaymentVoucher").
    /// </summary>
    public string DocumentType { get; private set; } = string.Empty;

    /// <summary>
    /// The next available sequence number.
    /// </summary>
    public int NextNumber { get; private set; }

    private DocumentSequence() { }

    /// <summary>
    /// Creates a new sequence starting at 1.
    /// </summary>
    /// <param name="documentType">Unique document type key.</param>
    /// <returns>A new DocumentSequence instance.</returns>
    public static DocumentSequence Create(string documentType)
    {
        if (string.IsNullOrWhiteSpace(documentType))
            throw new DomainException("نوع المستند مطلوب.");

        return new DocumentSequence
        {
            DocumentType = documentType.Trim(),
            NextNumber = 1
        };
    }

    /// <summary>
    /// Returns the current NextNumber and advances it by 1.
    /// Thread-safe operations are handled by the service-layer SemaphoreSlim.
    /// </summary>
    public int GetNext()
    {
        var current = NextNumber;
        NextNumber++;
        UpdateTimestamp();
        return current;
    }

    /// <summary>
    /// Sets the next number (e.g., during manual correction or year reset).
    /// </summary>
    /// <param name="newNext">The next number to use (must be >= 1).</param>
    public void SetNextNumber(int newNext)
    {
        if (newNext < 1)
            throw new DomainException("الرقم التسلسلي يجب أن يكون 1 أو أكثر.");
        NextNumber = newNext;
        UpdateTimestamp();
    }
}