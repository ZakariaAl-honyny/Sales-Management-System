using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Application.Interfaces.Services;

/// <summary>
/// Service for managing cheques — CRUD and status transitions (Clear/Bounce/Cancel).
/// Cheques are linked to either a CustomerPayment or a SupplierPayment.
/// </summary>
public interface IChequeService
{
    /// <summary>
    /// Gets all cheques with optional filtering by payment ID or status.
    /// </summary>
    Task<Result<List<ChequeDto>>> GetAllAsync(int? paymentId = null, byte? status = null, CancellationToken ct = default);

    /// <summary>
    /// Gets a single cheque by ID.
    /// </summary>
    Task<Result<ChequeDto>> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Creates a new cheque linked to a customer or supplier payment.
    /// </summary>
    Task<Result<ChequeDto>> CreateAsync(CreateChequeRequest request, int userId, CancellationToken ct = default);

    /// <summary>
    /// Updates a cheque's status with proper domain transitions and financial reversals.
    /// Pending → Cleared: creates journal entry
    /// Pending → Bounced: reverses original payment journal entry
    /// Pending → Cancelled: no financial impact
    /// Cleared → Bounced: reverses both payment and clearing entries
    /// </summary>
    Task<Result<ChequeDto>> UpdateStatusAsync(int id, UpdateChequeStatusRequest request, int userId, CancellationToken ct = default);

    /// <summary>
    /// Soft-deletes (deactivates) a cheque.
    /// </summary>
    Task<Result> DeleteAsync(int id, CancellationToken ct = default);
}
