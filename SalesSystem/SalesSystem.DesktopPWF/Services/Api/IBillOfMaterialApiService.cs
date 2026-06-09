using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.DesktopPWF.Services.Api;

public interface IBillOfMaterialApiService
{
    Task<Result<List<BillOfMaterialDto>>> GetAllAsync(CancellationToken ct = default);
    Task<Result<BillOfMaterialDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Result<List<BillOfMaterialDto>>> GetByAssemblyAsync(int productId, CancellationToken ct = default);
    Task<Result<BillOfMaterialDto>> CreateAsync(CreateBillOfMaterialRequest request, CancellationToken ct = default);
    Task<Result<BillOfMaterialDto>> UpdateAsync(int id, UpdateBillOfMaterialRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(int id, CancellationToken ct = default);
    Task<Result<ProduceAssemblyResultDto>> ProduceAsync(ProduceAssemblyRequest request, CancellationToken ct = default);
}
