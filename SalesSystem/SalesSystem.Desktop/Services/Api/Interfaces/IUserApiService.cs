using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;

namespace SalesSystem.Desktop.Services.Api.Interfaces;

public interface IUserApiService
{
    Task<Result<IReadOnlyList<UserDto>>> GetAllAsync(CancellationToken ct = default);
}

