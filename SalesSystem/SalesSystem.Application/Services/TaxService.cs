using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Application.Services;

public class TaxService : ITaxService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<TaxService> _logger;

    public TaxService(IUnitOfWork uow, ILogger<TaxService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<Result<List<TaxDto>>> GetAllAsync(CancellationToken ct = default)
    {
        try
        {
            var taxes = await _uow.Taxes.GetAllAsync(ct);
            var dtos = taxes.Select(MapToDto).ToList();
            return Result<List<TaxDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load taxes");
            return Result<List<TaxDto>>.Failure("فشل في تحميل الضرائب");
        }
    }

    public async Task<Result<TaxDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        try
        {
            var tax = await _uow.Taxes.GetByIdAsync(id, ct);
            if (tax == null)
                return Result<TaxDto>.Failure("الضريبة غير موجودة", ErrorCodes.NotFound);

            return Result<TaxDto>.Success(MapToDto(tax));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load tax {Id}", id);
            return Result<TaxDto>.Failure("فشل في تحميل الضريبة");
        }
    }

    public async Task<Result<TaxDto>> CreateAsync(CreateTaxRequest request, CancellationToken ct = default)
    {
        try
        {
            var tax = Tax.Create(request.Name, request.Rate, request.IsDefault);

            // If this is the default, unset all other defaults
            if (request.IsDefault)
            {
                var existingDefaults = await _uow.Taxes.ToListAsync(
                    t => t.IsDefault && t.IsActive, ct: ct);
                foreach (var d in existingDefaults)
                {
                    d.Update(d.Name, d.Rate, false);
                }
            }

            await _uow.Taxes.AddAsync(tax, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Tax {Id} created: {Name} @ {Rate}%", tax.Id, tax.Name, tax.Rate);
            return Result<TaxDto>.Success(MapToDto(tax));
        }
        catch (DomainException ex)
        {
            return Result<TaxDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create tax");
            return Result<TaxDto>.Failure("حدث خطأ أثناء إضافة الضريبة.");
        }
    }

    public async Task<Result<TaxDto>> UpdateAsync(int id, UpdateTaxRequest request, CancellationToken ct = default)
    {
        try
        {
            var tax = await _uow.Taxes.GetByIdAsync(id, ct);
            if (tax == null)
                return Result<TaxDto>.Failure("الضريبة غير موجودة", ErrorCodes.NotFound);

            // If setting this as default, unset all other defaults
            if (request.IsDefault && !tax.IsDefault)
            {
                var existingDefaults = await _uow.Taxes.ToListAsync(
                    t => t.IsDefault && t.IsActive && t.Id != id, ct: ct);
                foreach (var d in existingDefaults)
                {
                    d.Update(d.Name, d.Rate, false);
                }
            }

            tax.Update(request.Name, request.Rate, request.IsDefault);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Tax {Id} updated: {Name} @ {Rate}%", id, tax.Name, tax.Rate);
            return Result<TaxDto>.Success(MapToDto(tax));
        }
        catch (DomainException ex)
        {
            return Result<TaxDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update tax {Id}", id);
            return Result<TaxDto>.Failure("حدث خطأ أثناء تحديث الضريبة.");
        }
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct = default)
    {
        try
        {
            var tax = await _uow.Taxes.GetByIdAsync(id, ct);
            if (tax == null)
                return Result.Failure("الضريبة غير موجودة", ErrorCodes.NotFound);

            tax.MarkAsDeleted();
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Tax {Id} deactivated: {Name}", id, tax.Name);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deactivate tax {Id}", id);
            return Result.Failure("حدث خطأ أثناء إلغاء تنشيط الضريبة.");
        }
    }

    public async Task<Result> PermanentDeleteAsync(int id, CancellationToken ct = default)
    {
        try
        {
            var tax = await _uow.Taxes.GetByIdAsync(id, ct);
            if (tax == null)
                return Result.Failure("الضريبة غير موجودة", ErrorCodes.NotFound);

            await _uow.Taxes.HardDeleteAsync(id, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Tax {Id} permanently deleted: {Name}", id, tax.Name);
            return Result.Success();
        }
        catch (Exception ex) when (ex.GetType().Name.Contains("DbUpdate") || ex.GetType().Name.Contains("Sql"))
        {
            _logger.LogWarning(ex, "Cannot permanently delete tax {Id} — referenced by invoices", id);
            return Result.Failure("لا يمكن حذف هذه الضريبة لأنها مرتبطة بفواتير");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to permanently delete tax {Id}", id);
            return Result.Failure("حدث خطأ أثناء حذف الضريبة.");
        }
    }

    private static TaxDto MapToDto(Tax tax) => new(
        tax.Id,
        tax.Name,
        tax.Rate,
        tax.IsDefault,
        tax.IsActive
    );
}
