using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;

namespace SalesSystem.Application.Accounting.Services;

/// <summary>
/// Generates thread-safe journal entry numbers using IDocumentSequenceService.
/// The sequence key "JournalEntry" is used with DocumentSequenceService's SemaphoreSlim
/// lock held through SaveChangesAsync, ensuring no duplicate entry numbers under concurrency.
/// Format: JE-{yyyyMMdd}-{D4}
/// </summary>
public class JournalEntryNumberGenerator : IJournalEntryNumberGenerator
{
    private readonly IDocumentSequenceService _sequenceService;
    private readonly ILogger<JournalEntryNumberGenerator> _logger;

    public JournalEntryNumberGenerator(
        IDocumentSequenceService sequenceService,
        ILogger<JournalEntryNumberGenerator> logger)
    {
        _sequenceService = sequenceService;
        _logger = logger;
    }

    public async Task<Result<JournalEntryNumberResult>> GenerateAsync(CancellationToken ct = default)
    {
        try
        {
            // Use daily-scoped sequence key so the counter resets each day.
            // JE-20260625-0001, JE-20260625-0002, then JE-20260626-0001 (daily reset).
            var today = DateTime.Today;
            var sequenceKey = $"JournalEntry-{today:yyyyMMdd}";
            var seqResult = await _sequenceService.GetNextIntAsync(sequenceKey, ct);
            if (!seqResult.IsSuccess)
                return Result<JournalEntryNumberResult>.Failure(seqResult.Error!);

            var entryNo = seqResult.Value;
            var entryNumber = $"JE-{today:yyyyMMdd}-{entryNo:D4}";

            _logger.LogDebug("Generated journal entry number: {EntryNumber}", entryNumber);
            return Result<JournalEntryNumberResult>.Success(new JournalEntryNumberResult(entryNumber, entryNo));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating journal entry number");
            return Result<JournalEntryNumberResult>.Failure("حدث خطأ أثناء إنشاء رقم القيد المحاسبي");
        }
    }
}
