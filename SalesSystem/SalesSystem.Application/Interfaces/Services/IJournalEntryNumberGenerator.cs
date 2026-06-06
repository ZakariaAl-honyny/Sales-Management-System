using SalesSystem.Contracts.Common;

namespace SalesSystem.Application.Interfaces.Services;

public interface IJournalEntryNumberGenerator
{
    Task<Result<string>> GenerateAsync(CancellationToken ct = default);
}
