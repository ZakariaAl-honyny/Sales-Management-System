using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Application.Services;

/// <summary>
/// Thread-safe document sequence generator.
/// Schema: DocumentType (unique key) + NextNumber (int).
/// All formatting (e.g., "INV-2026-000001") is done by the caller, not stored in the entity.
/// </summary>
public class DocumentSequenceService : IDocumentSequenceService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<DocumentSequenceService> _logger;
    private static readonly SemaphoreSlim _lock = new(1, 1);

    public DocumentSequenceService(IUnitOfWork uow, ILogger<DocumentSequenceService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    /// <summary>
    /// Generates the next formatted sequential number for a given prefix + year.
    /// Uses the schema's DocumentType key (derived from prefix) for storage.
    /// Format: {prefix}-{year}-{number:D6} (e.g., INV-2026-000001).
    /// </summary>
    public async Task<Result<string>> GetNextNumberAsync(string prefix, CancellationToken ct)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var year = DateTime.Now.Year;
            var documentType = GetDocumentTypeKey(prefix);

            var sequence = await _uow.DocumentSequences
                .FirstOrDefaultAsync(s => s.DocumentType == documentType, ct);

            if (sequence == null)
            {
                sequence = DocumentSequence.Create(documentType);
                await _uow.DocumentSequences.AddAsync(sequence, ct);
            }

            var nextNumber = sequence.GetNext();
            await _uow.SaveChangesAsync(ct);

            var formatted = $"{prefix}-{year:D4}-{nextNumber:D6}";
            _logger.LogInformation("Generated sequence number: {Number}", formatted);

            return Result<string>.Success(formatted);
        }
        catch (DomainException ex)
        {
            return Result<string>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating next number for sequence {Prefix}", prefix);
            return Result<string>.Failure("حدث خطأ أثناء توليد الرقم المتسلسل");
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Generates the next integer sequence number for a given document type key.
    /// Thread-safe via SemaphoreSlim. Used for InvoiceNo and other int-only sequences.
    /// </summary>
    public async Task<Result<int>> GetNextIntAsync(string sequenceKey, CancellationToken ct)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var sequence = await _uow.DocumentSequences
                .FirstOrDefaultAsync(s => s.DocumentType == sequenceKey, ct);

            if (sequence == null)
            {
                sequence = DocumentSequence.Create(sequenceKey);
                await _uow.DocumentSequences.AddAsync(sequence, ct);
            }

            var nextNumber = sequence.GetNext();
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Generated sequence int {Number} for {Key}", nextNumber, sequenceKey);

            return Result<int>.Success(nextNumber);
        }
        catch (DomainException ex)
        {
            return Result<int>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating next int for sequence {Key}", sequenceKey);
            return Result<int>.Failure("حدث خطأ أثناء توليد الرقم المتسلسل");
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Maps a prefix (e.g., "INV") to a canonical DocumentType key for storage.
    /// </summary>
    private static string GetDocumentTypeKey(string prefix) => prefix.ToUpper() switch
    {
        "INV" => "SalesInvoice",
        "PUR" => "PurchaseInvoice",
        "SR" => "SalesReturn",
        "PR" => "PurchaseReturn",
        "TRF" => "WarehouseTransfer",
        "CP" => "CustomerPayment",
        "SP" => "SupplierPayment",
        _ => prefix.ToUpper()
    };
}
