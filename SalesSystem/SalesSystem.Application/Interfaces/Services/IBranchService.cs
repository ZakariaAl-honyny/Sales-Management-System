using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;

namespace SalesSystem.Application.Interfaces.Services;

public interface IBranchService
{
    Task<Result<List<BranchDto>>> GetAllAsync(CancellationToken ct);
    Task<Result<BranchDto>> GetByIdAsync(int id, CancellationToken ct);
    Task<Result<BranchDto>> CreateAsync(CreateBranchRequest request, CancellationToken ct);
    Task<Result<BranchDto>> UpdateAsync(int id, UpdateBranchRequest request, CancellationToken ct);
    Task<Result> DeactivateAsync(int id, CancellationToken ct);
}
