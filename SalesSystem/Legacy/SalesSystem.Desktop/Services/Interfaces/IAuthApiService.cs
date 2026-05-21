using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Responses;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Desktop.Services.Interfaces;

public interface IAuthApiService
{
    Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default);
}

