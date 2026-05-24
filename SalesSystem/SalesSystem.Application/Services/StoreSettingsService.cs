using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Repositories;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;

namespace SalesSystem.Application.Services;

public sealed class StoreSettingsService : IStoreSettingsService
{
    private readonly IUnitOfWork _uow;
    private readonly ISystemSettingsRepository _systemSettingsRepo;
    private readonly ILogger<StoreSettingsService> _logger;

    public StoreSettingsService(IUnitOfWork uow, ISystemSettingsRepository systemSettingsRepo, ILogger<StoreSettingsService> logger)
    {
        _uow = uow;
        _systemSettingsRepo = systemSettingsRepo;
        _logger = logger;
    }

    public async Task<Result<StoreSettingsDto>> GetSettingsAsync(CancellationToken ct = default)
    {
        var settings = (await _uow.StoreSettings.GetAllAsync(ct)).FirstOrDefault();

        if (settings == null)
        {
            // Seed default settings if none exist
            settings = StoreSettings.Create("متجر المبيعات", currencyCode: "SAR");
            await _uow.StoreSettings.AddAsync(settings, ct);
            await _uow.SaveChangesAsync(ct);
        }

        return Result<StoreSettingsDto>.Success(await MapToDto(settings, ct));
    }

    public async Task<Result<StoreSettingsDto>> UpdateSettingsAsync(UpdateSettingsRequest request, int userId, CancellationToken ct = default)
    {
        try
        {
            var settings = (await _uow.StoreSettings.GetAllAsync(ct)).FirstOrDefault();

            if (settings == null)
            {
                settings = StoreSettings.Create(
                    request.StoreName,
                    request.Phone,
                    request.Address,
                    request.LogoUrl,
                    request.Email,
                    request.Currency,
                    request.DefaultTaxRate,
                    request.IsTaxEnabled,
                    request.TaxNumber,
                    request.EnableStockAlerts,
                    request.AllowNegativeStock,
                    request.AutoUpdatePrices,
                    request.InvoicePrefix);

                await _uow.StoreSettings.AddAsync(settings, ct);
            }
            else
            {
                settings.Update(
                    request.StoreName,
                    request.Phone,
                    request.Address,
                    request.LogoUrl,
                    request.Email,
                    request.Currency,
                    request.DefaultTaxRate,
                    request.IsTaxEnabled,
                    request.TaxNumber,
                    request.EnableStockAlerts,
                    request.AllowNegativeStock,
                    request.AutoUpdatePrices,
                    request.InvoicePrefix);

            }

            await _uow.SaveChangesAsync(ct);

            try
            {
                var costingMethod = (Domain.Enums.CostingMethod)request.CostingMethod;
                await _systemSettingsRepo.SetCostingMethodAsync(costingMethod, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to persist costing method");
            }

            // Persist backup & update settings via key-value store
            await _systemSettingsRepo.SetStringAsync("Backup.BackupPath", request.BackupPath ?? "", userId, ct);
            await _systemSettingsRepo.SetStringAsync("Backup.ScheduleTime", request.BackupScheduleTime ?? "02:00", userId, ct);
            await _systemSettingsRepo.SetStringAsync("Backup.RetentionDays", request.BackupRetentionDays.ToString(), userId, ct);
            await _systemSettingsRepo.SetStringAsync("Update.ServerUrl", request.UpdateServerUrl ?? "", userId, ct);

            _logger.LogInformation("Store settings updated by user {UserId}", userId);

            return Result<StoreSettingsDto>.Success(await MapToDto(settings, ct));
        }
        catch (DomainException ex)
        {
            return Result<StoreSettingsDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving store settings");
            return Result<StoreSettingsDto>.Failure("حدث خطأ أثناء حفظ الإعدادات");
        }
    }

    public async Task<Result<CostingMethod?>> GetCostingMethodAsync(CancellationToken ct = default)
    {
        try
        {
            var costingMethod = await _systemSettingsRepo.GetCostingMethodAsync(ct);
            return Result<CostingMethod?>.Success(costingMethod);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading costing method");
            return Result<CostingMethod?>.Failure("فشل في تحميل طريقة التكلفة");
        }
    }

    public async Task<Result> SetCostingMethodAsync(CostingMethod method, int userId, CancellationToken ct = default)
    {
        try
        {
            await _systemSettingsRepo.SetCostingMethodAsync(method, ct);
            _logger.LogInformation("Costing method updated by user {UserId}", userId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving costing method");
            return Result.Failure("فشل في حفظ طريقة التكلفة");
        }
    }

    private async Task<StoreSettingsDto> MapToDto(StoreSettings s, CancellationToken ct)
    {
        var costingMethod = await _systemSettingsRepo.GetCostingMethodAsync(ct);
        var backupPath = await _systemSettingsRepo.GetStringAsync("Backup.BackupPath", "", ct);
        var scheduleTime = await _systemSettingsRepo.GetStringAsync("Backup.ScheduleTime", "02:00", ct);
        var retentionStr = await _systemSettingsRepo.GetStringAsync("Backup.RetentionDays", "30", ct);
        _ = int.TryParse(retentionStr, out var retentionDays);
        var updateServerUrl = await _systemSettingsRepo.GetStringAsync("Update.ServerUrl", "", ct);
        return new StoreSettingsDto(
            s.Id,
            s.StoreName,
            s.Phone,
            s.Address,
            s.LogoPath,
            s.Email,
            s.CurrencyCode,
            s.DefaultTaxRate,
            s.IsTaxEnabled,
            s.TaxNumber,
            s.EnableStockAlerts,
            s.AllowNegativeStock,
            s.AutoUpdatePrices,
            s.InvoicePrefix,
            (int)costingMethod,
            backupPath,
            scheduleTime,
            retentionDays,
            updateServerUrl);
    }
}
