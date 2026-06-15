using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;

namespace SalesSystem.Application.Interfaces.Services;

public interface IAccountCategoryService
{
    Task<Result<List<AccountCategoryDto>>> GetAllAsync(CancellationToken ct);
    Task<Result<AccountCategoryDto>> GetByIdAsync(int id, CancellationToken ct);
    Task<Result<AccountCategoryDto>> CreateAsync(CreateAccountCategoryRequest request, CancellationToken ct);
    Task<Result<AccountCategoryDto>> UpdateAsync(int id, UpdateAccountCategoryRequest request, CancellationToken ct);
    Task<Result> DeactivateAsync(int id, CancellationToken ct);
}
