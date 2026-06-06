using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Application.Interfaces.Services;

public interface ITaxService
{
    Task<Result<List<TaxDto>>> GetAllAsync(CancellationToken ct = default);
    Task<Result<TaxDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Result<TaxDto>> CreateAsync(CreateTaxRequest request, CancellationToken ct = default);
    Task<Result<TaxDto>> UpdateAsync(int id, UpdateTaxRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(int id, CancellationToken ct = default);
    Task<Result> PermanentDeleteAsync(int id, CancellationToken ct = default);
}
