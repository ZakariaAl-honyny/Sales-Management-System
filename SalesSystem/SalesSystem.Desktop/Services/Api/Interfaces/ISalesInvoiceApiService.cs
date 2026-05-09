using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Desktop.Services.Api.Interfaces;

public interface ISalesInvoiceApiService
{
    Task<Result<IReadOnlyList<SalesInvoiceDto>>> GetAllAsync(string? search = null, DateTime? from = null, DateTime? to = null, byte? status = null, CancellationToken ct = default);
    Task<Result<SalesInvoiceDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Result<SalesInvoiceDto>> CreateAsync(CreateSalesInvoiceRequest r, CancellationToken ct = default);
    Task<Result<SalesInvoiceDto>> UpdateAsync(int id, CreateSalesInvoiceRequest r, CancellationToken ct = default);
    Task<Result> PostAsync(int id, CancellationToken ct = default);
    Task<Result> CancelAsync(int id, CancellationToken ct = default);
}
