using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using System.Threading;

namespace SalesSystem.Application.Accounting.Services;

public class JournalEntryNumberGenerator : IJournalEntryNumberGenerator
{
    private static readonly SemaphoreSlim _lock = new(1, 1);

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
            await _lock.WaitAsync(ct);
            try
            {
                // Query by today's date prefix for correct daily reset
                // e.g., "JE-20260606-" prefix returns all entries for today
                var today = DateTime.Today;
                var prefix = $"JE-{today:yyyyMMdd}";
                var todayEntries = await _uow.JournalEntries.ToListAsync(
                    je => je.EntryNumber.StartsWith(prefix), ct: ct);

                var nextNumber = todayEntries.Count + 1;
                var entryNumber = $"JE-{today:yyyyMMdd}-{nextNumber:D4}";

                return Result<string>.Success(entryNumber);
            }
            finally
            {
                _lock.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating journal entry number");
            return Result<string>.Failure("حدث خطأ أثناء إنشاء رقم القيد المحاسبي");
        }
    }
}
