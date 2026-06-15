using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Application.Interfaces.Services;

public interface IChequeService
{
    Task<Result<ChequeDto>> CreateAsync(CreateChequeRequest request, int userId, CancellationToken ct);
    Task<Result<ChequeDto>> UpdateAsync(int id, UpdateChequeRequest request, int userId, CancellationToken ct);
    Task<Result> DeactivateAsync(int id, int userId, CancellationToken ct);
    Task<Result<ChequeDto>> GetByIdAsync(int id, CancellationToken ct);
    Task<Result<List<ChequeDto>>> GetAllAsync(CancellationToken ct);
    Task<Result> MarkAsDepositedAsync(int id, int userId, CancellationToken ct);
    Task<Result> MarkAsClearedAsync(int id, int userId, CancellationToken ct);
    Task<Result> MarkAsBouncedAsync(int id, int userId, CancellationToken ct);
    Task<Result> MarkAsCancelledAsync(int id, int userId, CancellationToken ct);
}
