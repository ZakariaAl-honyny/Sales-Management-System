using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Application.Services;

public class CurrencyService : ICurrencyService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<CurrencyService> _logger;

    public CurrencyService(IUnitOfWork uow, ILogger<CurrencyService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<Result<List<CurrencyDto>>> GetAllAsync(bool includeInactive = false, CancellationToken ct = default)
    {
        try
        {
            var currencies = await _uow.Currencies.GetAllAsync(ct);
            var filtered = includeInactive
                ? currencies
                : currencies.Where(c => c.IsActive).ToList();
            var dtos = filtered.Select(MapToDto).ToList();
            return Result<List<CurrencyDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load currencies");
            return Result<List<CurrencyDto>>.Failure("فشل في تحميل العملات");
        }
    }

    public async Task<Result<CurrencyDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        try
        {
            var currency = await _uow.Currencies.GetByIdAsync(id, ct);
            if (currency == null)
                return Result<CurrencyDto>.Failure("العملة غير موجودة", ErrorCodes.NotFound);

            return Result<CurrencyDto>.Success(MapToDto(currency));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load currency {Id}", id);
            return Result<CurrencyDto>.Failure("فشل في تحميل العملة");
        }
    }

    public async Task<Result<CurrencyDto>> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        try
        {
            var currency = await _uow.Currencies.FirstOrDefaultAsync(
                c => c.Code == code.Trim().ToUpperInvariant(), ct);
            if (currency == null)
                return Result<CurrencyDto>.Failure("العملة غير موجودة", ErrorCodes.NotFound);

            return Result<CurrencyDto>.Success(MapToDto(currency));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load currency by code {Code}", code);
            return Result<CurrencyDto>.Failure("فشل في تحميل العملة");
        }
    }

    public async Task<Result<CurrencyDto>> GetBaseCurrencyAsync(CancellationToken ct = default)
    {
        try
        {
            var currency = await _uow.Currencies.FirstOrDefaultAsync(
                c => c.IsBaseCurrency && c.IsActive, ct);
            if (currency == null)
                return Result<CurrencyDto>.Failure("لا توجد عملة أساسية", ErrorCodes.NotFound);

            return Result<CurrencyDto>.Success(MapToDto(currency));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load base currency");
            return Result<CurrencyDto>.Failure("فشل في تحميل العملة الأساسية");
        }
    }

    public async Task<Result<CurrencyDto>> CreateAsync(CreateCurrencyRequest request, int userId, CancellationToken ct = default)
    {
        try
        {
            // Validate unique Name
            var nameExists = await _uow.Currencies.AnyAsync(c => c.Name == request.Name.Trim(), ct);
            if (nameExists)
                return Result<CurrencyDto>.Failure("اسم العملة موجود بالفعل", ErrorCodes.DuplicateEntry);

            // Validate unique Code
            var codeExists = await _uow.Currencies.AnyAsync(c => c.Code == request.Code.Trim().ToUpperInvariant(), ct);
            if (codeExists)
                return Result<CurrencyDto>.Failure("رمز العملة موجود بالفعل", ErrorCodes.DuplicateEntry);

            var currency = Currency.Create(
                request.Name,
                request.Code,
                request.Symbol,
                request.IsBaseCurrency,
                request.FractionName,
                decimalPlaces: (byte)request.DecimalPlaces);

            currency.SetCreatedBy(userId);

            await _uow.Currencies.AddAsync(currency, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Currency {Id} created: {Name} ({Code})", currency.Id, currency.Name, currency.Code);
            return Result<CurrencyDto>.Success(MapToDto(currency));
        }
        catch (DomainException ex)
        {
            return Result<CurrencyDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create currency");
            return Result<CurrencyDto>.Failure("حدث خطأ أثناء إضافة العملة.");
        }
    }

    public async Task<Result<CurrencyDto>> UpdateAsync(int id, UpdateCurrencyRequest request, int userId, CancellationToken ct = default)
    {
        try
        {
            var currency = await _uow.Currencies.GetByIdAsync(id, ct);
            if (currency == null)
                return Result<CurrencyDto>.Failure("العملة غير موجودة", ErrorCodes.NotFound);

            currency.Update(
                request.Name,
                request.Symbol,
                request.FractionName,
                decimalPlaces: (byte)request.DecimalPlaces);
            currency.SetUpdatedBy(userId);

            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Currency {Id} updated: {Name} ({Code})", id, currency.Name, currency.Code);
            return Result<CurrencyDto>.Success(MapToDto(currency));
        }
        catch (DomainException ex)
        {
            return Result<CurrencyDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update currency {Id}", id);
            return Result<CurrencyDto>.Failure("حدث خطأ أثناء تحديث العملة.");
        }
    }

    public async Task<Result> DeleteAsync(int id, int userId, CancellationToken ct = default)
    {
        try
        {
            var currency = await _uow.Currencies.GetByIdAsync(id, ct);
            if (currency == null)
                return Result.Failure("العملة غير موجودة", ErrorCodes.NotFound);

            if (currency.IsSystem)
                return Result.Failure("لا يمكن حذف عملة النظام.", ErrorCodes.InvalidOperation);

            currency.MarkAsDeleted();
            currency.SetUpdatedBy(userId);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Currency {Id} deactivated: {Name}", id, currency.Name);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deactivate currency {Id}", id);
            return Result.Failure("حدث خطأ أثناء إلغاء تنشيط العملة.");
        }
    }

    public async Task<Result> DeletePermanentlyAsync(int id, int userId, CancellationToken ct = default)
    {
        try
        {
            var currency = await _uow.Currencies.GetByIdAsync(id, ct);
            if (currency == null)
                return Result.Failure("العملة غير موجودة", ErrorCodes.NotFound);

            if (currency.IsSystem)
                return Result.Failure("لا يمكن حذف عملة النظام.", ErrorCodes.InvalidOperation);

            await _uow.Currencies.HardDeleteAsync(id, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Currency {Id} permanently deleted: {Name}", id, currency.Name);
            return Result.Success();
        }
        catch (Exception ex) when (ex.GetType().Name.Contains("DbUpdate") || ex.GetType().Name.Contains("Sql"))
        {
            _logger.LogWarning(ex, "Cannot permanently delete currency {Id} — has related records", id);
            return Result.Failure("لا يمكن حذف العملة لأنها مرتبطة بفواتير أو مدفوعات");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to permanently delete currency {Id}", id);
            return Result.Failure("حدث خطأ أثناء حذف العملة.");
        }
    }

    private static CurrencyDto MapToDto(Currency c) => new(
        c.Id,
        c.Name,
        c.Code,
        c.Symbol,
        c.IsBaseCurrency,
        c.FractionName,
        c.DecimalPlaces,
        c.IsSystem,
        c.IsActive
    );
}
