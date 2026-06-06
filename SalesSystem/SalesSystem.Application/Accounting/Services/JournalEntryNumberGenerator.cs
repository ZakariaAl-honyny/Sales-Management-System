using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;

namespace SalesSystem.Application.Accounting.Services;

public class JournalEntryNumberGenerator : IJournalEntryNumberGenerator
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<JournalEntryNumberGenerator> _logger;

    public JournalEntryNumberGenerator(IUnitOfWork uow, ILogger<JournalEntryNumberGenerator> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<Result<string>> GenerateAsync(CancellationToken ct = default)
    {
        try
        {
            var count = await _uow.JournalEntries.CountAsync(ct: ct);
            var entryNumber = $"JE-{DateTime.Today:yyyyMMdd}-{count + 1:D4}";
            return Result<string>.Success(entryNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating journal entry number");
            return Result<string>.Failure("حدث خطأ أثناء إنشاء رقم القيد المحاسبي");
        }
    }
}
