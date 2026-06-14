namespace SalesSystem.Domain.Common;

/// <summary>
/// Auditable entity for transactional documents (invoices, receipts, journal entries, etc.).
/// Documents use a Status property (Draft/Posted/Cancelled) for their life cycle.
/// They do NOT have IsActive (soft-delete) — documents transition through a fixed life cycle instead.
/// Posting and cancellation timestamps are tracked for audit purposes.
/// </summary>
public abstract class DocumentEntity : AuditableEntity
{
    /// <summary>
    /// Timestamp when the document was posted (transitioned from Draft to Posted).
    /// </summary>
    public DateTime? PostedAt { get; protected set; }

    /// <summary>
    /// Timestamp when the document was cancelled (transitioned to Cancelled).
    /// </summary>
    public DateTime? CancelledAt { get; protected set; }

    protected DocumentEntity() : base() { }
}
