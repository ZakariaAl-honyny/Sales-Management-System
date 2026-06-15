using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Domain.Accounting.Entities;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Application.Accounting.Services;

/// <summary>
/// Service for managing fiscal year lifecycle: create, open, close.
/// </summary>
public class FiscalYearService : IFiscalYearService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<FiscalYearService> _logger;

    public FiscalYearService(
        IUnitOfWork uow,
        ILogger<FiscalYearService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<Result<List<FiscalYearDto>>> GetAllAsync(CancellationToken ct = default)
    {
        try
        {
            var years = await _uow.FiscalYears.ToListAsync(
                fy => true,
                q => q.OrderByDescending(fy => fy.Year),
                ct: ct);

            var dtos = years.Select(MapToDto).ToList();
            return Result<List<FiscalYearDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving fiscal years");
            return Result<List<FiscalYearDto>>.Failure("حدث خطأ أثناء استرجاع السنوات المالية");
        }
    }

    public async Task<Result<FiscalYearDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        try
        {
            var fiscalYear = await _uow.FiscalYears.GetByIdAsync(id, ct);
            if (fiscalYear == null)
                return Result<FiscalYearDto>.Failure("السنة المالية غير موجودة", ErrorCodes.NotFound);

            return Result<FiscalYearDto>.Success(MapToDto(fiscalYear));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving fiscal year {Id}", id);
            return Result<FiscalYearDto>.Failure("حدث خطأ أثناء استرجاع السنة المالية");
        }
    }

    public async Task<Result<FiscalYearDto>> GetByYearAsync(int year, CancellationToken ct = default)
    {
        try
        {
            var fiscalYear = await _uow.FiscalYears.FirstOrDefaultAsync(
                fy => fy.Year == year, ct: ct);

            if (fiscalYear == null)
                return Result<FiscalYearDto>.Failure($"السنة المالية {year} غير موجودة", ErrorCodes.NotFound);

            return Result<FiscalYearDto>.Success(MapToDto(fiscalYear));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving fiscal year {Year}", year);
            return Result<FiscalYearDto>.Failure("حدث خطأ أثناء استرجاع السنة المالية");
        }
    }

    public async Task<Result<FiscalYearDto>> CreateAsync(CreateFiscalYearRequest request, int userId, CancellationToken ct = default)
    {
        try
        {
            if (request.Year < 2000 || request.Year > DateTime.UtcNow.Year + 10)
                return Result<FiscalYearDto>.Failure(
                    $"السنة المالية غير صالحة — يجب أن تكون بين 2000 و{DateTime.UtcNow.Year + 10}");

            // Check no active fiscal year with same year
            var existing = await _uow.FiscalYears.FirstOrDefaultAsync(
                fy => fy.Year == request.Year && fy.IsOpen, ct: ct);
            if (existing != null)
                return Result<FiscalYearDto>.Failure(
                    $"السنة المالية {request.Year} موجودة بالفعل");

            var fiscalYear = FiscalYear.Create(request.Year, userId);
            await _uow.FiscalYears.AddAsync(fiscalYear, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Fiscal year {Year} created by User {UserId}", fiscalYear.Year, userId);

            return Result<FiscalYearDto>.Success(MapToDto(fiscalYear));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain validation failed for fiscal year creation");
            return Result<FiscalYearDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating fiscal year {Year}", request.Year);
            return Result<FiscalYearDto>.Failure("حدث خطأ أثناء إنشاء السنة المالية");
        }
    }

    public async Task<Result<FiscalYearDto>> OpenAsync(int id, int userId, CancellationToken ct = default)
    {
        try
        {
            var fiscalYear = await _uow.FiscalYears.GetByIdAsync(id, ct);
            if (fiscalYear == null)
                return Result<FiscalYearDto>.Failure("السنة المالية غير موجودة", ErrorCodes.NotFound);

            fiscalYear.Reopen(userId);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Fiscal year {Year} reopened by User {UserId}", fiscalYear.Year, userId);

            return Result<FiscalYearDto>.Success(MapToDto(fiscalYear));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain validation failed for reopening fiscal year");
            return Result<FiscalYearDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reopening fiscal year {Id}", id);
            return Result<FiscalYearDto>.Failure("حدث خطأ أثناء إعادة فتح السنة المالية");
        }
    }

    public async Task<Result<FiscalYearDto>> CloseAsync(int id, int userId, CancellationToken ct = default)
    {
        try
        {
            var fiscalYear = await _uow.FiscalYears.GetByIdAsync(id, ct);
            if (fiscalYear == null)
                return Result<FiscalYearDto>.Failure("السنة المالية غير موجودة", ErrorCodes.NotFound);

            // Check no unposted journal entries for this fiscal year
            var unpostedCount = await _uow.JournalEntries.CountAsync(
                je => je.EntryDate.Year == fiscalYear.Year
                    && je.Status == Domain.Accounting.Enums.JournalEntryStatus.Draft, ct);
            if (unpostedCount > 0)
                return Result<FiscalYearDto>.Failure(
                    $"لا يمكن إغلاق السنة المالية {fiscalYear.Year} — يوجد {unpostedCount} قيد محاسبي غير مرحل");

            fiscalYear.Close(userId);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Fiscal year {Year} closed by User {UserId}", fiscalYear.Year, userId);

            return Result<FiscalYearDto>.Success(MapToDto(fiscalYear));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain validation failed for closing fiscal year");
            return Result<FiscalYearDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing fiscal year {Id}", id);
            return Result<FiscalYearDto>.Failure("حدث خطأ أثناء إغلاق السنة المالية");
        }
    }

    // ─── Private Mapping ──────────────────────────────

    private static FiscalYearDto MapToDto(FiscalYear fy) => new(
        fy.Id,
        fy.Year,
        fy.StartDate,
        fy.EndDate,
        fy.IsOpen,
        fy.OpenedAt,
        fy.OpenedByUserId,
        fy.ClosedAt,
        fy.ClosedByUserId
    );
}
