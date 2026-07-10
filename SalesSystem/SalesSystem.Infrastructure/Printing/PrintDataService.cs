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

        var (storeName, storePhone, storeAddress, storeTaxNumber, logoBytes, taxRate, footerNote, showBalanceOnPrint, printSignature, showExpiryInInvoices, paperSize, signaturePath, printBarcode, printQRCode, printCompanyAddress) = await LoadAllStoreInfoAsync(ct);
        var dto = await _builder.BuildFromSalesAsync(invoice, storeName, storePhone, storeAddress, storeTaxNumber, logoBytes, taxRate, ct);
        if (!string.IsNullOrWhiteSpace(footerNote))
            dto.FooterNote = footerNote;
        dto.ShowBalanceOnPrint = showBalanceOnPrint;
        dto.PrintSignature = printSignature;
        dto.ShowExpiryInInvoices = showExpiryInInvoices;
        dto.PaperSize = paperSize;
        dto.SignatureImagePath = string.IsNullOrWhiteSpace(signaturePath) ? null : signaturePath;
        dto.PrintBarcode = printBarcode;
        dto.PrintQRCode = printQRCode;
        dto.PrintCompanyAddress = printCompanyAddress;
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

        var (storeName, storePhone, storeAddress, storeTaxNumber, logoBytes, taxRate, footerNote, showBalanceOnPrint, printSignature, showExpiryInInvoices, paperSize, signaturePath, printBarcode, printQRCode, printCompanyAddress) = await LoadAllStoreInfoAsync(ct);
        var dto = await _builder.BuildFromPurchaseAsync(invoice, storeName, storePhone, storeAddress, storeTaxNumber, logoBytes, taxRate, ct);
        if (!string.IsNullOrWhiteSpace(footerNote))
            dto.FooterNote = footerNote;
        dto.ShowBalanceOnPrint = showBalanceOnPrint;
        dto.PrintSignature = printSignature;
        dto.ShowExpiryInInvoices = showExpiryInInvoices;
        dto.PaperSize = paperSize;
        dto.SignatureImagePath = string.IsNullOrWhiteSpace(signaturePath) ? null : signaturePath;
        dto.PrintBarcode = printBarcode;
        dto.PrintQRCode = printQRCode;
        dto.PrintCompanyAddress = printCompanyAddress;
        return Result<InvoicePrintDto>.Success(dto);
    }

    public async Task<Result<InvoicePrintDto>> GetSalesReturnPrintDataAsync(int returnId, CancellationToken ct = default)
    {
        var returnEntity = await _uow.SalesReturns.Query()
            .Include(r => r.Customer)
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == returnId, ct);

        if (returnEntity == null)
            return Result<InvoicePrintDto>.Failure("مرتجع المبيعات غير موجود");

        var (storeName, storePhone, storeAddress, storeTaxNumber, logoBytes, taxRate, footerNote, showBalanceOnPrint, printSignature, showExpiryInInvoices, paperSize, signaturePath, printBarcode, printQRCode, printCompanyAddress) = await LoadAllStoreInfoAsync(ct);
        var dto = await _builder.BuildFromSalesReturnAsync(returnEntity, storeName, storePhone, storeAddress, storeTaxNumber, logoBytes, taxRate, ct);
        if (!string.IsNullOrWhiteSpace(footerNote))
            dto.FooterNote = footerNote;
        dto.ShowBalanceOnPrint = showBalanceOnPrint;
        dto.PrintSignature = printSignature;
        dto.ShowExpiryInInvoices = showExpiryInInvoices;
        dto.PaperSize = paperSize;
        dto.SignatureImagePath = string.IsNullOrWhiteSpace(signaturePath) ? null : signaturePath;
        dto.PrintBarcode = printBarcode;
        dto.PrintQRCode = printQRCode;
        dto.PrintCompanyAddress = printCompanyAddress;
        return Result<InvoicePrintDto>.Success(dto);
    }

    public async Task<Result<InvoicePrintDto>> GetPurchaseReturnPrintDataAsync(int returnId, CancellationToken ct = default)
    {
        var returnEntity = await _uow.PurchaseReturns.Query()
            .Include(r => r.Supplier)
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == returnId, ct);

        if (returnEntity == null)
            return Result<InvoicePrintDto>.Failure("مرتجع المشتريات غير موجود");

        var (storeName, storePhone, storeAddress, storeTaxNumber, logoBytes, taxRate, footerNote, showBalanceOnPrint, printSignature, showExpiryInInvoices, paperSize, signaturePath, printBarcode, printQRCode, printCompanyAddress) = await LoadAllStoreInfoAsync(ct);
        var dto = await _builder.BuildFromPurchaseReturnAsync(returnEntity, storeName, storePhone, storeAddress, storeTaxNumber, logoBytes, taxRate, ct);
        if (!string.IsNullOrWhiteSpace(footerNote))
            dto.FooterNote = footerNote;
        dto.ShowBalanceOnPrint = showBalanceOnPrint;
        dto.PrintSignature = printSignature;
        dto.ShowExpiryInInvoices = showExpiryInInvoices;
        dto.PaperSize = paperSize;
        dto.SignatureImagePath = string.IsNullOrWhiteSpace(signaturePath) ? null : signaturePath;
        dto.PrintBarcode = printBarcode;
        dto.PrintQRCode = printQRCode;
        dto.PrintCompanyAddress = printCompanyAddress;
        return Result<InvoicePrintDto>.Success(dto);
    }

    public async Task<Result<InvoicePrintDto>> GetSalesQuotationPrintDataAsync(int quotationId, CancellationToken ct = default)
    {
        var quotation = await _uow.SalesQuotations.Query()
            .Include(q => q.Customer)
            .Include(q => q.Items).ThenInclude(i => i.Product)
            .Include(q => q.Items).ThenInclude(i => i.ProductUnit).ThenInclude(pu => pu.Unit)
            .FirstOrDefaultAsync(q => q.Id == quotationId, ct);

        if (quotation == null)
            return Result<InvoicePrintDto>.Failure("عرض السعر غير موجود");

        var (storeName, storePhone, storeAddress, storeTaxNumber, logoBytes, taxRate, footerNote, showBalanceOnPrint, printSignature, showExpiryInInvoices, paperSize, signaturePath, printBarcode, printQRCode, printCompanyAddress) = await LoadAllStoreInfoAsync(ct);
        var dto = await _builder.BuildFromSalesQuotationAsync(quotation, storeName, storePhone, storeAddress, storeTaxNumber, logoBytes, taxRate, ct);
        if (!string.IsNullOrWhiteSpace(footerNote))
            dto.FooterNote = footerNote;
        dto.ShowBalanceOnPrint = showBalanceOnPrint;
        dto.PrintSignature = printSignature;
        dto.ShowExpiryInInvoices = showExpiryInInvoices;
        dto.PaperSize = paperSize;
        dto.SignatureImagePath = string.IsNullOrWhiteSpace(signaturePath) ? null : signaturePath;
        dto.PrintBarcode = printBarcode;
        dto.PrintQRCode = printQRCode;
        dto.PrintCompanyAddress = printCompanyAddress;
        return Result<InvoicePrintDto>.Success(dto);
    }

    /// <summary>
    /// Loads store info PLUS FooterNote from print settings.
    /// Returns 15-tuple: name, phone, address, taxNumber, logoBytes, taxRate, footerNote,
    /// showBalanceOnPrint, printSignature, showExpiryInInvoices, paperSize, signaturePath,
    /// printBarcode, printQRCode, printCompanyAddress.
    /// </summary>
    private async Task<(string name, string phone, string address, string taxNumber,
        byte[]? logoBytes, decimal taxRate, string? footerNote, bool showBalanceOnPrint, bool printSignature,
        bool showExpiryInInvoices, string paperSize, string? signaturePath,
        bool printBarcode, bool printQRCode, bool printCompanyAddress)> LoadAllStoreInfoAsync(CancellationToken ct)
    {
        var (name, phone, address, taxNumber, logoBytes, taxRate) = await LoadStoreInfoAsync(ct);
        var footerNote = string.Empty;
        bool showBalanceOnPrint = true;
        bool printSignature = false;
        bool showExpiryInInvoices = false;
        string paperSize = "A4";
        string? signaturePath = null;
        bool printBarcode = false;
        bool printQRCode = false;
        bool printCompanyAddress = true;
        var sysSettingsResult = await GetPrintSystemSettingsAsync(ct);
        if (sysSettingsResult.IsSuccess && sysSettingsResult.Value != null)
        {
            footerNote = sysSettingsResult.Value
                .FirstOrDefault(s => s.SettingKey == "FooterNote")?.SettingValue ?? string.Empty;
            var showBalanceStr = sysSettingsResult.Value
                .FirstOrDefault(s => s.SettingKey == "ShowBalanceOnPrint")?.SettingValue ?? "true";
            var printSignatureStr = sysSettingsResult.Value
                .FirstOrDefault(s => s.SettingKey == "PrintSignature")?.SettingValue ?? "false";
            var showExpiryStr = sysSettingsResult.Value
                .FirstOrDefault(s => s.SettingKey == "ShowExpiryInInvoices")?.SettingValue ?? "false";
            var paperSizeStr = sysSettingsResult.Value
                .FirstOrDefault(s => s.SettingKey == "PaperSize")?.SettingValue ?? "A4";
            var printBarcodeStr = sysSettingsResult.Value
                .FirstOrDefault(s => s.SettingKey == "PrintBarcode")?.SettingValue ?? "false";
            var printQRCodeStr = sysSettingsResult.Value
                .FirstOrDefault(s => s.SettingKey == "PrintQRCode")?.SettingValue ?? "false";
            var printCompanyAddressStr = sysSettingsResult.Value
                .FirstOrDefault(s => s.SettingKey == "PrintCompanyAddress")?.SettingValue ?? "true";
            bool.TryParse(showBalanceStr, out showBalanceOnPrint);
            bool.TryParse(printSignatureStr, out printSignature);
            bool.TryParse(showExpiryStr, out showExpiryInInvoices);
            bool.TryParse(printBarcodeStr, out printBarcode);
            bool.TryParse(printQRCodeStr, out printQRCode);
            bool.TryParse(printCompanyAddressStr, out printCompanyAddress);
            paperSize = string.IsNullOrWhiteSpace(paperSizeStr) ? "A4" : paperSizeStr;
        }

        var storeSettingsResult = await GetStoreSettingsAsync(ct);
        if (storeSettingsResult.IsSuccess && storeSettingsResult.Value != null)
            signaturePath = storeSettingsResult.Value.SignaturePath;

        return (name, phone, address, taxNumber, logoBytes, taxRate, footerNote, showBalanceOnPrint, printSignature,
            showExpiryInInvoices, paperSize, signaturePath, printBarcode, printQRCode, printCompanyAddress);
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

        var showLogo = await _systemSettingsRepo.GetBoolAsync("ShowLogo", true, ct);
        if (!showLogo)
        {
            logoBytes = null;
            _logger.LogDebug("Logo hidden via ShowLogo setting");
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
            var taxNumber = await _systemSettingsRepo.GetStringAsync("Store.TaxNumber", "", ct);
            _ = bool.TryParse(await _systemSettingsRepo.GetStringAsync("Store.EnableStockAlerts", "false", ct), out var enableStockAlerts);
            _ = bool.TryParse(await _systemSettingsRepo.GetStringAsync("Store.AllowNegativeStock", "false", ct), out var allowNegativeStock);
            var signaturePath = await _systemSettingsRepo.GetStringAsync("Store.SignaturePath", "", ct);

            var dto = new StoreSettingsDto(
                1,
                storeName ?? "متجري",
                phone,
                address,
                logoPath,
                email,
                0m,         // DEPRECATED: DefaultTaxRate
                true,       // DEPRECATED: IsTaxEnabled
                taxNumber,
                enableStockAlerts,
                allowNegativeStock,
                "",         // DEPRECATED: InvoicePrefix
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
            var printBarcodeStr = GetSettingValue(sysSettings, "PrintBarcode", "false");
            var printQRCodeStr = GetSettingValue(sysSettings, "PrintQRCode", "false");
            var printCompanyAddressStr = GetSettingValue(sysSettings, "PrintCompanyAddress", "true");

            decimal.TryParse(taxRateStr, out var taxRate);
            bool.TryParse(autoPrintStr, out var autoPrintOnPost);
            int.TryParse(escPosCodePageStr, out var escPosCodePage);
            int.TryParse(printCopiesStr, out var printCopies);
            bool.TryParse(showBalanceStr, out var showBalanceOnPrint);
            bool.TryParse(printSignatureStr, out var printSignature);
            bool.TryParse(showLogoStr, out var showLogo);
            bool.TryParse(printBarcodeStr, out var printBarcode);
            bool.TryParse(printQRCodeStr, out var printQRCode);
            bool.TryParse(printCompanyAddressStr, out var printCompanyAddress);

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
                footerNote,
                printBarcode,
                printQRCode,
                printCompanyAddress);

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
            await _systemSettingsRepo.SetStringAsync("PrintBarcode", request.PrintBarcode.ToString().ToLower(), ct: ct);
            await _systemSettingsRepo.SetStringAsync("PrintQRCode", request.PrintQRCode.ToString().ToLower(), ct: ct);
            await _systemSettingsRepo.SetStringAsync("PrintCompanyAddress", request.PrintCompanyAddress.ToString().ToLower(), ct: ct);

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
