using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Repositories;
using SalesSystem.Application.Printing;
using SalesSystem.Application.Printing.Contracts;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Printing;

/// <summary>
/// Loads invoice data from the database and builds InvoicePrintDto via InvoicePrintDtoBuilder.
/// Encapsulates ALL database access for PrintController — controller injects only this service.
/// </summary>
public class PrintDataService : IPrintDataService
{
    private readonly IUnitOfWork _uow;
    private readonly InvoicePrintDtoBuilder _builder;
    private readonly ILogger<PrintDataService> _logger;
    private readonly ISystemSettingsRepository _systemSettingsRepo;

    public PrintDataService(
        IUnitOfWork uow,
        InvoicePrintDtoBuilder builder,
        ILogger<PrintDataService> logger,
        ISystemSettingsRepository systemSettingsRepo)
    {
        _uow = uow;
        _builder = builder;
        _logger = logger;
        _systemSettingsRepo = systemSettingsRepo;
    }

    public async Task<Result<InvoicePrintDto>> GetSalesInvoicePrintDataAsync(int invoiceId, CancellationToken ct = default)
    {
        var invoice = await _uow.SalesInvoices.Query()
            .Include(i => i.Customer)
            .Include(i => i.Items).ThenInclude(it => it.Product)
            .Include(i => i.Items).ThenInclude(it => it.ProductUnit).ThenInclude(pu => pu.Unit)
            .FirstOrDefaultAsync(i => i.Id == invoiceId, ct);

        if (invoice == null)
            return Result<InvoicePrintDto>.Failure("الفاتورة غير موجودة");

        var (storeName, storePhone, storeAddress, storeTaxNumber, logoBytes, taxRate, footerNote) = await LoadAllStoreInfoAsync(ct);
        var dto = await _builder.BuildFromSalesAsync(invoice, storeName, storePhone, storeAddress, storeTaxNumber, logoBytes, taxRate, ct);
        if (!string.IsNullOrWhiteSpace(footerNote))
            dto.FooterNote = footerNote;
        return Result<InvoicePrintDto>.Success(dto);
    }

    public async Task<Result<InvoicePrintDto>> GetPurchaseInvoicePrintDataAsync(int invoiceId, CancellationToken ct = default)
    {
        var invoice = await _uow.PurchaseInvoices.Query()
            .Include(i => i.Supplier)
            .Include(i => i.Items).ThenInclude(it => it.Product)
            .Include(i => i.Items).ThenInclude(it => it.ProductUnit).ThenInclude(pu => pu.Unit)
            .FirstOrDefaultAsync(i => i.Id == invoiceId, ct);

        if (invoice == null)
            return Result<InvoicePrintDto>.Failure("الفاتورة غير موجودة");

        var (storeName, storePhone, storeAddress, storeTaxNumber, logoBytes, taxRate, footerNote) = await LoadAllStoreInfoAsync(ct);
        var dto = await _builder.BuildFromPurchaseAsync(invoice, storeName, storePhone, storeAddress, storeTaxNumber, logoBytes, taxRate, ct);
        if (!string.IsNullOrWhiteSpace(footerNote))
            dto.FooterNote = footerNote;
        return Result<InvoicePrintDto>.Success(dto);
    }

    public async Task<Result<InvoicePrintDto>> GetSalesReturnPrintDataAsync(int returnId, CancellationToken ct = default)
    {
        var returnEntity = await _uow.SalesReturns.Query()
            .Include(r => r.Customer).ThenInclude(c => c!.Party)
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == returnId, ct);

        if (returnEntity == null)
            return Result<InvoicePrintDto>.Failure("مرتجع المبيعات غير موجود");

        var (storeName, storePhone, storeAddress, storeTaxNumber, logoBytes, taxRate, footerNote) = await LoadAllStoreInfoAsync(ct);
        var dto = await _builder.BuildFromSalesReturnAsync(returnEntity, storeName, storePhone, storeAddress, storeTaxNumber, logoBytes, taxRate, ct);
        if (!string.IsNullOrWhiteSpace(footerNote))
            dto.FooterNote = footerNote;
        return Result<InvoicePrintDto>.Success(dto);
    }

    public async Task<Result<InvoicePrintDto>> GetPurchaseReturnPrintDataAsync(int returnId, CancellationToken ct = default)
    {
        var returnEntity = await _uow.PurchaseReturns.Query()
            .Include(r => r.Supplier).ThenInclude(s => s!.Party)
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == returnId, ct);

        if (returnEntity == null)
            return Result<InvoicePrintDto>.Failure("مرتجع المشتريات غير موجود");

        var (storeName, storePhone, storeAddress, storeTaxNumber, logoBytes, taxRate, footerNote) = await LoadAllStoreInfoAsync(ct);
        var dto = await _builder.BuildFromPurchaseReturnAsync(returnEntity, storeName, storePhone, storeAddress, storeTaxNumber, logoBytes, taxRate, ct);
        if (!string.IsNullOrWhiteSpace(footerNote))
            dto.FooterNote = footerNote;
        return Result<InvoicePrintDto>.Success(dto);
    }

    /// <summary>
    /// Loads store info PLUS FooterNote from print settings.
    /// Returns 7-tuple: name, phone, address, taxNumber, logoBytes, taxRate, footerNote.
    /// </summary>
    private async Task<(string name, string phone, string address, string taxNumber,
        byte[]? logoBytes, decimal taxRate, string? footerNote)> LoadAllStoreInfoAsync(CancellationToken ct)
    {
        var (name, phone, address, taxNumber, logoBytes, taxRate) = await LoadStoreInfoAsync(ct);
        var footerNote = string.Empty;
        var sysSettingsResult = await GetPrintSystemSettingsAsync(ct);
        if (sysSettingsResult.IsSuccess && sysSettingsResult.Value != null)
        {
            footerNote = sysSettingsResult.Value
                .FirstOrDefault(s => s.SettingKey == "FooterNote")?.SettingValue ?? string.Empty;
        }
        return (name, phone, address, taxNumber, logoBytes, taxRate, footerNote);
    }

    private async Task<(string name, string phone, string address, string taxNumber,
        byte[]? logoBytes, decimal taxRate)> LoadStoreInfoAsync(CancellationToken ct)
    {
        var settingsResult = await GetStoreSettingsAsync(ct);
        var sysSettingsResult = await GetPrintSystemSettingsAsync(ct);

        var settings = settingsResult.IsSuccess && settingsResult.Value != null ? settingsResult.Value : null;
        var sysSettings = sysSettingsResult.IsSuccess && sysSettingsResult.Value != null
            ? sysSettingsResult.Value
            : new List<SystemSetting>();

        var storeName = settings?.StoreName ?? "متجري";
        var storePhone = settings?.Phone ?? string.Empty;
        var storeAddress = settings?.Address ?? string.Empty;
        var storeTaxNumber = sysSettings
            .FirstOrDefault(s => s.SettingKey == "StoreTaxNumber")?.SettingValue ?? string.Empty;
        var logoPath = sysSettings
            .FirstOrDefault(s => s.SettingKey == "LogoPath")?.SettingValue;
        var taxRateStr = sysSettings
            .FirstOrDefault(s => s.SettingKey == "TaxRate")?.SettingValue ?? "15";

        byte[]? logoBytes = null;
        if (!string.IsNullOrWhiteSpace(logoPath) && File.Exists(logoPath))
        {
            try { logoBytes = await File.ReadAllBytesAsync(logoPath, ct); }
            catch { _logger.LogWarning("Could not load logo from {Path}", logoPath); }
        }

        decimal.TryParse(taxRateStr, out var taxRate);
        return (storeName, storePhone, storeAddress, storeTaxNumber, logoBytes, taxRate);
    }

    public async Task<Result<StoreSettingsDto>> GetStoreSettingsAsync(CancellationToken ct = default)
    {
        try
        {
            var storeName = await _systemSettingsRepo.GetStringAsync("Store.Name", "متجري", ct);
            var phone = await _systemSettingsRepo.GetStringAsync("Store.Phone", "", ct);
            var address = await _systemSettingsRepo.GetStringAsync("Store.Address", "", ct);
            var logoPath = await _systemSettingsRepo.GetStringAsync("Store.LogoPath", "", ct);
            var email = await _systemSettingsRepo.GetStringAsync("Store.Email", "", ct);
            var currencyCode = await _systemSettingsRepo.GetStringAsync("Store.CurrencyCode", "SAR", ct);
            var taxNumber = await _systemSettingsRepo.GetStringAsync("Store.TaxNumber", "", ct);
            _ = bool.TryParse(await _systemSettingsRepo.GetStringAsync("Store.EnableStockAlerts", "false", ct), out var enableStockAlerts);
            _ = bool.TryParse(await _systemSettingsRepo.GetStringAsync("Store.AllowNegativeStock", "false", ct), out var allowNegativeStock);
            _ = bool.TryParse(await _systemSettingsRepo.GetStringAsync("Store.AutoUpdatePrices", "false", ct), out var autoUpdatePrices);
            var signaturePath = await _systemSettingsRepo.GetStringAsync("Store.SignaturePath", "", ct);
            var costingMethod = await _systemSettingsRepo.GetCostingMethodAsync(ct);

            var dto = new StoreSettingsDto(
                1,
                storeName ?? "متجري",
                phone,
                address,
                logoPath,
                email,
                currencyCode ?? "SAR",
                0m,         // DEPRECATED: DefaultTaxRate
                true,       // DEPRECATED: IsTaxEnabled
                taxNumber,
                enableStockAlerts,
                allowNegativeStock,
                autoUpdatePrices,
                "",         // DEPRECATED: InvoicePrefix
                (int)costingMethod,
                null, null, 30, null,
                signaturePath);

            return Result<StoreSettingsDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading store settings");
            return Result<StoreSettingsDto>.Failure("فشل في تحميل إعدادات المتجر");
        }
    }

    public async Task<Result<List<SystemSetting>>> GetPrintSystemSettingsAsync(CancellationToken ct = default)
    {
        try
        {
            var settings = await _uow.SystemSettings.Query()
                .Where(s => s.Category == "Print")
                .ToListAsync(ct);
            return Result<List<SystemSetting>>.Success(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading print system settings");
            return Result<List<SystemSetting>>.Failure("فشل في تحميل إعدادات الطباعة");
        }
    }

    // ─── Print Settings Management ────────────────────────────────────────

    public async Task<Result<PrintSettingsDto>> GetPrintSettingsAsync(CancellationToken ct = default)
    {
        try
        {
            var sysSettingsResult = await GetPrintSystemSettingsAsync(ct);
            var sysSettings = sysSettingsResult.IsSuccess && sysSettingsResult.Value != null
                ? sysSettingsResult.Value
                : new List<SystemSetting>();

            var thermalPrinterName = GetSettingValue(sysSettings, "ThermalPrinterName", "");
            var a4PrinterName = GetSettingValue(sysSettings, "A4PrinterName", "");
            var logoPath = GetSettingValue(sysSettings, "LogoPath", "");
            var storeTaxNumber = GetSettingValue(sysSettings, "StoreTaxNumber", "");
            var taxRateStr = GetSettingValue(sysSettings, "TaxRate", "15");
            var autoPrintStr = GetSettingValue(sysSettings, "AutoPrintOnPost", "false");
            var receiptHeader = GetSettingValue(sysSettings, "ReceiptHeader", "");
            var receiptFooter = GetSettingValue(sysSettings, "ReceiptFooter", "");
            var escPosCodePageStr = GetSettingValue(sysSettings, "EscPosCodePage", "22");

            var paperSize = GetSettingValue(sysSettings, "PaperSize", "A4");
            var printCopiesStr = GetSettingValue(sysSettings, "PrintCopies", "1");
            var showBalanceStr = GetSettingValue(sysSettings, "ShowBalanceOnPrint", "true");
            var printSignatureStr = GetSettingValue(sysSettings, "PrintSignature", "false");
            var showLogoStr = GetSettingValue(sysSettings, "ShowLogo", "true");
            var footerNote = GetSettingValue(sysSettings, "FooterNote", "");

            decimal.TryParse(taxRateStr, out var taxRate);
            bool.TryParse(autoPrintStr, out var autoPrintOnPost);
            int.TryParse(escPosCodePageStr, out var escPosCodePage);
            int.TryParse(printCopiesStr, out var printCopies);
            bool.TryParse(showBalanceStr, out var showBalanceOnPrint);
            bool.TryParse(printSignatureStr, out var printSignature);
            bool.TryParse(showLogoStr, out var showLogo);

            var dto = new PrintSettingsDto(
                thermalPrinterName,
                a4PrinterName,
                logoPath,
                storeTaxNumber,
                taxRate,
                autoPrintOnPost,
                receiptHeader,
                receiptFooter,
                escPosCodePage,
                paperSize,
                printCopies,
                showBalanceOnPrint,
                printSignature,
                showLogo,
                footerNote);

            return Result<PrintSettingsDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading print settings");
            return Result<PrintSettingsDto>.Failure("فشل في تحميل إعدادات الطباعة");
        }
    }

    public async Task<Result> UpdatePrintSettingsAsync(UpdatePrintSettingsRequest request, CancellationToken ct = default)
    {
        try
        {
            await _systemSettingsRepo.SetStringAsync("ThermalPrinterName", request.ThermalPrinterName ?? "", ct: ct);
            await _systemSettingsRepo.SetStringAsync("A4PrinterName", request.A4PrinterName ?? "", ct: ct);
            await _systemSettingsRepo.SetStringAsync("LogoPath", request.LogoPath ?? "", ct: ct);
            await _systemSettingsRepo.SetStringAsync("StoreTaxNumber", request.StoreTaxNumber ?? "", ct: ct);
            await _systemSettingsRepo.SetStringAsync("TaxRate", request.TaxRate.ToString("F2"), ct: ct);
            await _systemSettingsRepo.SetStringAsync("AutoPrintOnPost", request.AutoPrintOnPost.ToString().ToLower(), ct: ct);
            await _systemSettingsRepo.SetStringAsync("ReceiptHeader", request.ReceiptHeader ?? "", ct: ct);
            await _systemSettingsRepo.SetStringAsync("ReceiptFooter", request.ReceiptFooter ?? "", ct: ct);
            await _systemSettingsRepo.SetStringAsync("EscPosCodePage", request.EscPosCodePage.ToString(), ct: ct);
            await _systemSettingsRepo.SetStringAsync("PaperSize", request.PaperSize ?? "A4", ct: ct);
            await _systemSettingsRepo.SetStringAsync("PrintCopies", request.PrintCopies.ToString(), ct: ct);
            await _systemSettingsRepo.SetStringAsync("ShowBalanceOnPrint", request.ShowBalanceOnPrint.ToString().ToLower(), ct: ct);
            await _systemSettingsRepo.SetStringAsync("PrintSignature", request.PrintSignature.ToString().ToLower(), ct: ct);
            await _systemSettingsRepo.SetStringAsync("ShowLogo", request.ShowLogo.ToString().ToLower(), ct: ct);
            await _systemSettingsRepo.SetStringAsync("FooterNote", request.FooterNote ?? "", ct: ct);

            await _uow.SaveChangesAsync(ct);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating print settings");
            return Result.Failure("فشل في حفظ إعدادات الطباعة");
        }
    }

    private static string GetSettingValue(List<SystemSetting> settings, string key, string defaultValue)
    {
        return settings.FirstOrDefault(s =>
            s.SettingKey.Equals(key, StringComparison.OrdinalIgnoreCase))?.SettingValue ?? defaultValue;
    }
}
