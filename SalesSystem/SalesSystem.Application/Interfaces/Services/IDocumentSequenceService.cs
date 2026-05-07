using SalesSystem.Contracts.Common;

namespace SalesSystem.Application.Interfaces.Services;

public interface IDocumentSequenceService
{
    /// <summary>
    /// Generates the next sequential number for a given prefix and current year.
    /// Thread-safe implementation using SemaphoreSlim.
    /// Format: {prefix}-{year}-{number:D6} (e.g., INV-2026-000001)
    /// </summary>
    Task<Result<string>> GetNextNumberAsync(string prefix, CancellationToken ct);
}
