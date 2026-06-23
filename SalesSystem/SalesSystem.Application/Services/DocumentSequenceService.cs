using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
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
    /// Gets all document sequences for admin management.
    /// </summary>
    public async Task<Result<List<DocumentSequenceDto>>> GetAllAsync(CancellationToken ct)
    {
        try
        {
            var sequences = await _uow.DocumentSequences.ToListAsync(ct);
            var dtos = sequences
                .OrderByDescending(s => s.Id)
                .Select(s => new DocumentSequenceDto(s.Id, s.DocumentType, s.NextNumber))
                .ToList();
            return Result<List<DocumentSequenceDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving document sequences");
            return Result<List<DocumentSequenceDto>>.Failure("حدث خطأ أثناء استرجاع تسلسل المستندات");
        }
    }

    /// <summary>
    /// Updates the next number for a document sequence (manual reset).
    /// Thread-safe via SemaphoreSlim.
    /// </summary>
    public async Task<Result<DocumentSequenceDto>> UpdateSequenceAsync(int id, int nextNumber, CancellationToken ct)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var sequence = await _uow.DocumentSequences.GetByIdAsync(id, ct);
            if (sequence == null)
                return Result<DocumentSequenceDto>.Failure("تسلسل المستند غير موجود", ErrorCodes.NotFound);

            sequence.SetNextNumber(nextNumber);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Document sequence {Id} ({Type}) updated. New NextNumber: {Next}",
                id, sequence.DocumentType, nextNumber);

            return Result<DocumentSequenceDto>.Success(
                new DocumentSequenceDto(sequence.Id, sequence.DocumentType, sequence.NextNumber));
        }
        catch (DomainException ex)
        {
            return Result<DocumentSequenceDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating document sequence {Id}", id);
            return Result<DocumentSequenceDto>.Failure("حدث خطأ أثناء تحديث تسلسل المستند");
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
