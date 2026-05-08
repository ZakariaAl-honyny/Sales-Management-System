using SalesSystem.Contracts.Common;
using SalesSystem.Desktop.Models;

namespace SalesSystem.Desktop.Services.Interfaces;

public interface IAuthApiService
{
    Task<Result<UserSession>> LoginAsync(string userName, string password, CancellationToken ct = default);
}
