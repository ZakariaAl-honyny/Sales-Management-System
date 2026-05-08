using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests.Sales;

namespace SalesSystem.Desktop.Services.Interfaces;

public interface ISalesInvoiceApiService
{
    Task<Result<IReadOnlyList<SalesInvoiceDto>>> GetAllAsync(string? search = null, CancellationToken ct = default);
    Task<Result<SalesInvoiceDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Result<SalesInvoiceDto>> CreateAsync(CreateSalesInvoiceRequest request, CancellationToken ct = default);
    Task<Result> PostAsync(int id, CancellationToken ct = default);
    Task<Result> CancelAsync(int id, CancellationToken ct = default);
}
