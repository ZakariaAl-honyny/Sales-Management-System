using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Application.Services;

/// <summary>
/// Service for managing company-wide settings.
/// CompanySettings is a singleton row (Id = 1) enforced at the database level.
/// </summary>
public class CompanySettingsService : ICompanySettingsService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<CompanySettingsService> _logger;

    public CompanySettingsService(IUnitOfWork uow, ILogger<CompanySettingsService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<Result<CompanySettingsDto>> GetAsync(CancellationToken ct = default)
    {
        try
        {
            var settings = await _uow.CompanySettings.FirstOrDefaultAsync(s => s.Id == 1, ct);
            if (settings == null)
                return Result<CompanySettingsDto>.Failure("إعدادات الشركة غير موجودة", ErrorCodes.NotFound);

            return Result<CompanySettingsDto>.Success(MapToDto(settings));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving company settings");
            return Result<CompanySettingsDto>.Failure("حدث خطأ أثناء استرجاع إعدادات الشركة");
        }
    }

    public async Task<Result<CompanySettingsDto>> UpdateAsync(UpdateCompanySettingsRequest request, int? userId = null, CancellationToken ct = default)
    {
        try
        {
            var settings = await _uow.CompanySettings.FirstOrDefaultAsync(s => s.Id == 1, ct);
            if (settings == null)
                return Result<CompanySettingsDto>.Failure("إعدادات الشركة غير موجودة", ErrorCodes.NotFound);

            settings.Update(
                request.CompanyName,
                request.DefaultCurrencyId,
                request.Phone,
                request.Email,
                request.Address,
                request.TaxNumber,
                request.LogoPath,
                userId);

            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Company settings updated by user {UserId}", userId);

            return Result<CompanySettingsDto>.Success(MapToDto(settings));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation updating company settings: {Message}", ex.Message);
            return Result<CompanySettingsDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating company settings");
            return Result<CompanySettingsDto>.Failure("حدث خطأ أثناء تحديث إعدادات الشركة");
        }
    }

    private static CompanySettingsDto MapToDto(CompanySettings settings)
    {
        return new CompanySettingsDto(
            settings.Id,
            settings.CompanyName,
            settings.Phone,
            settings.Email,
            settings.Address,
            settings.TaxNumber,
            settings.LogoPath,
            settings.DefaultCurrencyId,
            null // CurrencyName will be populated by controller if needed
        );
    }
}
