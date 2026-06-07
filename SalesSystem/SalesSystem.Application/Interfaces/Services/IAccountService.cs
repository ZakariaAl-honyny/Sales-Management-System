using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Domain.Accounting.Enums;

namespace SalesSystem.Application.Interfaces.Services;

public interface IAccountService
{
    Task<Result<List<AccountTreeNodeDto>>> GetTreeAsync(CancellationToken ct = default);
    Task<Result<List<AccountDto>>> GetAllAsync(CancellationToken ct = default);
    Task<Result<AccountDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Result<List<AccountDto>>> GetByTypeAsync(AccountType type, CancellationToken ct = default);
    Task<Result<AccountDto>> CreateAsync(CreateAccountRequest request, int userId, CancellationToken ct = default);
    Task<Result<AccountDto>> UpdateAsync(int id, UpdateAccountRequest request, int userId, CancellationToken ct = default);
    Task<Result> DeleteAsync(int id, CancellationToken ct = default);
    Task<Result> PermanentDeleteAsync(int id, CancellationToken ct = default);
}
