using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Application.Interfaces.Services;

public interface ISalesService
{
    Task<Result<SalesInvoiceDto>> CreateAsync(CreateSalesInvoiceRequest request, int userId, CancellationToken ct);
    Task<Result<SalesInvoiceDto>> PostAsync(int id, int userId, CancellationToken ct);
    Task<Result<SalesInvoiceDto>> CancelAsync(int id, int userId, CancellationToken ct);
    Task<Result<SalesInvoiceDto>> GetByIdAsync(int id, CancellationToken ct);
    Task<Result<PagedResult<SalesInvoiceDto>>> GetAllAsync(int? customerId, int? status, int page, int pageSize, CancellationToken ct);
}
