using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;

namespace SalesSystem.Application.Interfaces.Services;

public interface IExpenseService
{
    Task<Result<ExpenseDto>> CreateAsync(CreateExpenseRequest request, int userId, CancellationToken ct);
    Task<Result<ExpenseDto>> GetByIdAsync(int id, CancellationToken ct);
    Task<Result<PagedResult<ExpenseDto>>> GetAllAsync(string? search, DateTime? from, DateTime? to, int page, int pageSize, CancellationToken ct);
    Task<Result<ExpenseDto>> UpdateAsync(int id, UpdateExpenseRequest request, int userId, CancellationToken ct);
    Task<Result> DeleteAsync(int id, CancellationToken ct);
    Task<Result<ExpenseDto>> PostAsync(int id, int userId, CancellationToken ct);
    Task<Result<ExpenseDto>> CancelAsync(int id, CancellationToken ct);
}
