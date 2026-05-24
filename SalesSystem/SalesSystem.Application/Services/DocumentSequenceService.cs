using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Application.Services;

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

    public async Task<Result<string>> GetNextNumberAsync(string prefix, CancellationToken ct)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var year = DateTime.Now.Year;

            // Try to find an existing sequence for this prefix and year
            var sequence = await _uow.DocumentSequences.FirstOrDefaultAsync(
                s => s.Prefix == prefix && s.Year == year, ct);

            if (sequence == null)
            {
                // Create new sequence for the year
                // We assume documentType is derived from prefix or generic for now
                // In a more complex system, documentType would be passed as a param
                string documentType = DetermineDocumentType(prefix);

                sequence = DocumentSequence.Create(documentType, prefix, year);
                await _uow.DocumentSequences.AddAsync(sequence, ct);
            }

            var nextNumber = sequence.GetNextNumber();
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Generated sequence number: {Number}", nextNumber);

            return Result<string>.Success(nextNumber);
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

    private static string DetermineDocumentType(string prefix)
    {
        return prefix.ToUpper() switch
        {
            "INV" => "فاتورة مبيعات",
            "PUR" => "فاتورة مشتريات",
            "SR" => "مرتجع مبيعات",
            "PR" => "مرتجع مشتريات",
            "TRF" => "تحويل مخزني",
            "CP" => "سند قبض عميل",
            "SP" => "سند صرف مورد",
            "PRD" => "منتج",
            "CUST" => "عميل",
            "SUP" => "مورد",
            _ => "أخرى"
        };
    }
}
