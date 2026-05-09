using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Desktop.Services.Api.Interfaces;

public interface IProductApiService
{
    Task<Result<IReadOnlyList<ProductDto>>> GetAllAsync(string? search = null, int? categoryId = null, bool includeInactive = false, CancellationToken ct = default);
    Task<Result<ProductDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Result<ProductDto>> GetByBarcodeAsync(string barcode, CancellationToken ct = default);
    Task<Result<ProductDto>> CreateAsync(CreateProductRequest r, CancellationToken ct = default);
    Task<Result<ProductDto>> UpdateAsync(int id, UpdateProductRequest r, CancellationToken ct = default);
    Task<Result> DeactivateAsync(int id, CancellationToken ct = default);
    Task<Result> ReactivateAsync(int id, CancellationToken ct = default);
}

