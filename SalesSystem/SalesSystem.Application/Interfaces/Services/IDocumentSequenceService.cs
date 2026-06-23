using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;

namespace SalesSystem.Application.Interfaces.Services;

public interface IDocumentSequenceService
{
    /// <summary>
    /// Generates the next sequential number for a given prefix and current year.
    /// Thread-safe implementation using SemaphoreSlim.
    /// Format: {prefix}-{year}-{number:D6} (e.g., INV-2026-000001)
    /// </summary>
    Task<Result<string>> GetNextNumberAsync(string prefix, CancellationToken ct);

    /// <summary>
    /// Generates the next integer sequence number for a given document type.
    /// Thread-safe using SemaphoreSlim. Used for InvoiceNo and other int-only sequences.
    /// Returns an auto-incremented int (e.g., SalesInvoice = 1, 2, 3...).
    /// </summary>
    /// <param name="sequenceKey">Document type key (e.g., "SalesInvoice", "PurchaseInvoice").</param>
    Task<Result<int>> GetNextIntAsync(string sequenceKey, CancellationToken ct);

    /// <summary>
    /// Gets all document sequences for admin management.
    /// </summary>
    Task<Result<List<DocumentSequenceDto>>> GetAllAsync(CancellationToken ct);

    /// <summary>
    /// Updates the next number for a document sequence (manual reset).
    /// </summary>
    Task<Result<DocumentSequenceDto>> UpdateSequenceAsync(int id, int nextNumber, CancellationToken ct);
}
