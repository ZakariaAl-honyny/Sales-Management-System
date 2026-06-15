using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;

namespace SalesSystem.Application.Interfaces.Services;

public interface IPartyService
{
    Task<Result<List<PartyDto>>> GetAllAsync(CancellationToken ct);
    Task<Result<PartyDto>> GetByIdAsync(int id, CancellationToken ct);
    Task<Result<PartyDto>> CreateAsync(CreatePartyRequest request, int userId, CancellationToken ct);
    Task<Result<PartyDto>> UpdateAsync(int id, UpdatePartyRequest request, int userId, CancellationToken ct);
    Task<Result> DeactivateAsync(int id, CancellationToken ct);
}
