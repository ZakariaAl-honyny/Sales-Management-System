using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Desktop.Services.Api.Interfaces;

public interface ISupplierPaymentApiService
{
    Task<Result<IReadOnlyList<SupplierPaymentDto>>> GetAllAsync(int? supplierId = null, DateTime? from = null, DateTime? to = null, CancellationToken ct = default);
    Task<Result<SupplierPaymentDto>> CreateAsync(CreateSupplierPaymentRequest r, CancellationToken ct = default);
}

