using SalesSystem.Application.Accounting.Services;
using SalesSystem.Contracts.Common;

namespace SalesSystem.Application.Interfaces.Services;

public interface IJournalEntryNumberGenerator
{
    Task<Result<JournalEntryNumberResult>> GenerateAsync(CancellationToken ct = default);
}
