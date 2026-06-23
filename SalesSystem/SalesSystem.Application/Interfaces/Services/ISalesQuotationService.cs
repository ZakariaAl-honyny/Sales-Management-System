using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Application.Interfaces.Services;

/// <summary>
/// Service interface for Sales Quotation management.
/// Quotations are non-binding price quotes with NO stock/accounting impact.
/// </summary>
public interface ISalesQuotationService
{
    /// <summary>
    /// Gets a paginated, filterable list of sales quotations.
    /// </summary>
    Task<Result<PagedResult<SalesQuotationDto>>> GetAllAsync(
        int? customerId,
        int? status,
        string? search,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        bool includeInactive,
        CancellationToken ct);

    /// <summary>
    /// Gets a single sales quotation by ID with full item details.
    /// </summary>
    Task<Result<SalesQuotationDto>> GetByIdAsync(int id, CancellationToken ct);

    /// <summary>
    /// Creates a new sales quotation as Draft.
    /// </summary>
    Task<Result<SalesQuotationDto>> CreateAsync(CreateSalesQuotationRequest request, int userId, CancellationToken ct);

    /// <summary>
    /// Updates an existing draft sales quotation.
    /// </summary>
    Task<Result<SalesQuotationDto>> UpdateAsync(int id, UpdateSalesQuotationRequest request, int userId, CancellationToken ct);

    /// <summary>
    /// Sends a draft quotation to the customer (Draft → Sent).
    /// </summary>
    Task<Result<SalesQuotationDto>> SendAsync(int id, int userId, CancellationToken ct);

    /// <summary>
    /// Accepts a sent quotation (Sent → Accepted).
    /// Validates ValidUntil if set.
    /// </summary>
    Task<Result<SalesQuotationDto>> AcceptAsync(int id, int userId, CancellationToken ct);

    /// <summary>
    /// Rejects a sent or accepted quotation with an optional reason.
    /// </summary>
    Task<Result<SalesQuotationDto>> RejectAsync(int id, string? reason, int userId, CancellationToken ct);

    /// <summary>
    /// Converts an accepted/sent quotation to a sales invoice.
    /// Sets ConvertedToInvoiceId and transitions to Converted status.
    /// Creates a draft SalesInvoice from the quotation data.
    /// </summary>
    Task<Result<SalesQuotationDto>> ConvertToInvoiceAsync(int id, int userId, CancellationToken ct);

    /// <summary>
    /// Cancels/rejects a quotation (non-terminal → Rejected).
    /// </summary>
    Task<Result> CancelAsync(int id, int userId, CancellationToken ct);
}
