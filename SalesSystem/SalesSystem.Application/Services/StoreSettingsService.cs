using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Repositories;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Application.Services;

/// <summary>
/// Manages store-level settings and system-wide configuration.
/// All settings are now stored in the <see cref="SystemSetting"/> key-value table.
/// </summary>
public sealed class StoreSettingsService : IStoreSettingsService
{
    private readonly IUnitOfWork _uow;
    private readonly ISystemSettingsRepository _systemSettingsRepo;
    private readonly ILogger<StoreSettingsService> _logger;

    // SystemSetting key prefixes
    private const string Prefix = "Store.";
    private const string KeyStoreName = Prefix + "Name";
    private const string KeyPhone = Prefix + "Phone";
    private const string KeyAddress = Prefix + "Address";
    private const string KeyLogoPath = Prefix + "LogoPath";
    private const string KeyEmail = Prefix + "Email";
    private const string KeyTaxNumber = Prefix + "TaxNumber";
    private const string KeyEnableStockAlerts = Prefix + "EnableStockAlerts";
    private const string KeyAllowNegativeStock = Prefix + "AllowNegativeStock";
    private const string KeySignaturePath = Prefix + "SignaturePath";

    public StoreSettingsService(IUnitOfWork uow, ISystemSettingsRepository systemSettingsRepo, ILogger<StoreSettingsService> logger)
    {
        _uow = uow;
        _systemSettingsRepo = systemSettingsRepo;
        _logger = logger;
    }

    public async Task<Result<StoreSettingsDto>> GetSettingsAsync(CancellationToken ct = default)
    {
        try
        {
            var dto = await LoadSettingsFromSystemAsync(ct);
            return Result<StoreSettingsDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading store settings");
            return Result<StoreSettingsDto>.Failure("فشل في تحميل إعدادات المتجر");
        }
    }

    public async Task<Result<StoreSettingsDto>> UpdateSettingsAsync(UpdateSettingsRequest request, int userId, CancellationToken ct = default)
    {
        try
        {
            await _uow.ExecuteTransactionAsync(async () =>
            {
                // Persist individual SystemSetting key-value pairs
                await _systemSettingsRepo.SetStringAsync(KeyStoreName, request.StoreName ?? "متجر المبيعات", category: "Store", userId: userId, ct: ct);
                await _systemSettingsRepo.SetStringAsync(KeyPhone, request.Phone ?? "", category: "Store", userId: userId, ct: ct);
                await _systemSettingsRepo.SetStringAsync(KeyAddress, request.Address ?? "", category: "Store", userId: userId, ct: ct);
                await _systemSettingsRepo.SetStringAsync(KeyLogoPath, request.LogoUrl ?? "", category: "Store", userId: userId, ct: ct);
                await _systemSettingsRepo.SetStringAsync(KeyEmail, request.Email ?? "", category: "Store", userId: userId, ct: ct);
                await _systemSettingsRepo.SetStringAsync(KeyTaxNumber, request.TaxNumber ?? "", category: "Store", userId: userId, ct: ct);
                await _systemSettingsRepo.SetStringAsync(KeyEnableStockAlerts, request.EnableStockAlerts.ToString().ToLower(), category: "Store", userId: userId, ct: ct);
                await _systemSettingsRepo.SetStringAsync(KeyAllowNegativeStock, request.AllowNegativeStock.ToString().ToLower(), category: "Store", userId: userId, ct: ct);
                await _systemSettingsRepo.SetStringAsync(KeySignaturePath, request.SignatureUrl ?? "", category: "Store", userId: userId, ct: ct);

                await _systemSettingsRepo.SetStringAsync("Backup.BackupPath", request.BackupPath ?? "", userId: userId, ct: ct);
                await _systemSettingsRepo.SetStringAsync("Backup.ScheduleTime", request.BackupScheduleTime ?? "02:00", userId: userId, ct: ct);
                await _systemSettingsRepo.SetStringAsync("Backup.RetentionDays", request.BackupRetentionDays.ToString(), userId: userId, ct: ct);
                await _systemSettingsRepo.SetStringAsync("Update.ServerUrl", request.UpdateServerUrl ?? "", userId: userId, ct: ct);

                await _uow.SaveChangesAsync(ct);
            }, ct);

            _logger.LogInformation("Store settings updated by user {UserId}", userId);

            var dto = await LoadSettingsFromSystemAsync(ct);
            return Result<StoreSettingsDto>.Success(dto);
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

    public async Task<Result<Dictionary<string, string>>> GetAllSystemSettingsAsync(CancellationToken ct = default)
    {
        try
        {
            var settings = await _systemSettingsRepo.GetAllSystemSettingsAsync(ct);
            return Result<Dictionary<string, string>>.Success(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading system settings");
            return Result<Dictionary<string, string>>.Failure("فشل في تحميل إعدادات النظام");
        }
    }

    public async Task<Result> UpdateSystemSettingsAsync(Dictionary<string, string> settings, CancellationToken ct = default)
    {
        try
        {
            var validationError = ValidateSystemSettings(settings);
            if (validationError != null)
                return Result.Failure(validationError);

            await _systemSettingsRepo.SetBatchSystemSettingsAsync(settings, ct);
            await _uow.SaveChangesAsync(ct);
            _logger.LogInformation("System settings updated in batch ({Count} keys)", settings.Count);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving system settings");
            return Result.Failure("فشل في حفظ إعدادات النظام");
        }
    }

    private async Task<StoreSettingsDto> LoadSettingsFromSystemAsync(CancellationToken ct)
    {
        var backupPath = await _systemSettingsRepo.GetStringAsync("Backup.BackupPath", "", ct);
        var scheduleTime = await _systemSettingsRepo.GetStringAsync("Backup.ScheduleTime", "02:00", ct);
        var retentionStr = await _systemSettingsRepo.GetStringAsync("Backup.RetentionDays", "30", ct);
        _ = int.TryParse(retentionStr, out var retentionDays);
        var updateServerUrl = await _systemSettingsRepo.GetStringAsync("Update.ServerUrl", "", ct);

        var storeName = await _systemSettingsRepo.GetStringAsync(KeyStoreName, "متجر المبيعات", ct);
        var phone = await _systemSettingsRepo.GetStringAsync(KeyPhone, "", ct);
        var address = await _systemSettingsRepo.GetStringAsync(KeyAddress, "", ct);
        var logoPath = await _systemSettingsRepo.GetStringAsync(KeyLogoPath, "", ct);
        var email = await _systemSettingsRepo.GetStringAsync(KeyEmail, "", ct);
        var taxNumber = await _systemSettingsRepo.GetStringAsync(KeyTaxNumber, "", ct);
        var signaturePath = await _systemSettingsRepo.GetStringAsync(KeySignaturePath, "", ct);

        _ = bool.TryParse(await _systemSettingsRepo.GetStringAsync(KeyEnableStockAlerts, "false", ct), out var enableStockAlerts);
        _ = bool.TryParse(await _systemSettingsRepo.GetStringAsync(KeyAllowNegativeStock, "false", ct), out var allowNegativeStock);

        return new StoreSettingsDto(
            1,                          // Virtual Id
            storeName ?? "",
            phone,
            address,
            logoPath,
            email,
            0m,                         // DEPRECATED: DefaultTaxRate — Tax entity is source of truth
            true,                       // DEPRECATED: IsTaxEnabled — Tax entity is source of truth
            taxNumber,
            enableStockAlerts,
            allowNegativeStock,
            "",                         // DEPRECATED: InvoicePrefix — use InvoiceNo (int) instead
            backupPath,
            scheduleTime,
            retentionDays,
            updateServerUrl,
            signaturePath);
    }

    private static string? ValidateSystemSettings(Dictionary<string, string> settings)
    {
        foreach (var kvp in settings)
        {
            if (string.IsNullOrWhiteSpace(kvp.Key))
                return "مفتاح الإعداد لا يمكن أن يكون فارغاً";

            var value = kvp.Value;
            switch (kvp.Key)
            {
                // Integer-only settings
                case "StockAlertDays":
                case "DefaultCashCustomerId":
                case "DefaultCashSupplierId":
                case "ExpiryAlertDays":
                    if (!int.TryParse(value, out var intVal) || intVal < 0)
                        return $"قيمة '{kvp.Key}' يجب أن تكون رقماً صحيحاً موجباً";
                    if (kvp.Key == "StockAlertDays" && (intVal < 1 || intVal > 365))
                        return "أيام تنبيه المخزون يجب أن تكون بين 1 و 365";
                    break;

                case "PrintCopies":
                    if (!int.TryParse(value, out var copiesVal))
                        return $"قيمة '{kvp.Key}' يجب أن تكون رقماً صحيحاً";
                    if (copiesVal < 1 || copiesVal > 10)
                        return $"قيمة '{kvp.Key}' يجب أن تكون بين 1 و 10";
                    break;

                // Boolean-only settings
                case "AllowNegativeStock":
                case "EnableFefo":
                case "AutoPostInvoices":
                case "AllowDrafts":
                case "ShowProfitInInvoice":
                case "PreventBelowRetailPrice":
                case "AllowBelowCostSale":
                case "HideTaxInSales":
                case "ShowExpiryInInvoices":
                case "PurchaseAutoPost":
                case "HideTaxInPurchases":
                case "AutoGenerateBarcode":
                case "ShowLogo":
                case "LowStockAlert":
                case "ExpiryAlert":
                case "CreditLimitAlert":
                case "AutoCreateJournalEntry":
                case "ShowBalanceOnPrint":
                case "PrintSignature":
                case "AutoPrintAfterPosting":
                case "AllowNegativeCash":
                case "AllowDuplicateBarcode":
                case "EnableAttachments":
                case "EnableNotifications":
                case "RequireBatchOnPurchase":
                case "RequireExpiryOnPurchase":
                case "PrintBarcode":
                case "PrintQRCode":
                case "PrintCompanyAddress":
                    if (!bool.TryParse(value, out _))
                        return $"قيمة '{kvp.Key}' يجب أن تكون true أو false";
                    break;

                // Integer-only settings (additional)
                case "DefaultSalesTax":
                    if (!int.TryParse(value, out var salesTaxVal) || salesTaxVal < 0 || salesTaxVal > 100)
                        return "نسبة ضريبة المبيعات يجب أن تكون بين 0 و 100";
                    break;

                case "DefaultPurchaseTax":
                    if (!int.TryParse(value, out var purchaseTaxVal) || purchaseTaxVal < 0 || purchaseTaxVal > 100)
                        return "نسبة ضريبة المشتريات يجب أن تكون بين 0 و 100";
                    break;

                case "DefaultBranch":
                    if (!int.TryParse(value, out var branchVal) || branchVal < 1)
                        return "الفرع الافتراضي يجب أن يكون رقماً موجباً أكبر من 0";
                    break;

                case "DefaultWarehouse":
                    if (!int.TryParse(value, out var warehouseVal) || warehouseVal < 1)
                        return "المستودع الافتراضي يجب أن يكون رقماً موجباً أكبر من 0";
                    break;

                case "PaperSize":
                    if (value != "A4" && value != "Letter" && value != "Thermal")
                        return $"قيمة '{kvp.Key}' يجب أن تكون A4 أو Letter أو Thermal";
                    break;
            }
        }
        return null;
    }
}
