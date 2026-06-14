using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;

namespace SalesSystem.Application.Interfaces.Services;

public interface IBankService
{
    Task<Result<List<BankDto>>> GetAllAsync(CancellationToken ct);
    Task<Result<BankDto>> GetByIdAsync(int id, CancellationToken ct);
    Task<Result<BankDto>> CreateAsync(CreateBankRequest request, int userId, CancellationToken ct);
    Task<Result<BankDto>> UpdateAsync(int id, UpdateBankRequest request, int userId, CancellationToken ct);
    Task<Result> DeactivateAsync(int id, CancellationToken ct);
}
