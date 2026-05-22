using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Printing;
using SalesSystem.Application.Printing.Contracts;
using SalesSystem.Contracts.Common;
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

    public PrintDataService(
        IUnitOfWork uow,
        InvoicePrintDtoBuilder builder,
        ILogger<PrintDataService> logger)
    {
        _uow = uow;
        _builder = builder;
        _logger = logger;
    }

    public async Task<Result<InvoicePrintDto>> GetSalesInvoicePrintDataAsync(int invoiceId, CancellationToken ct = default)
    {
        var invoice = await _uow.SalesInvoices.Query()
            .Include(i => i.Customer)
            .Include(i => i.Items).ThenInclude(it => it.Product)
            .FirstOrDefaultAsync(i => i.Id == invoiceId, ct);

        if (invoice == null)
            return Result<InvoicePrintDto>.Failure("الفاتورة غير موجودة");

        var (storeName, storePhone, storeAddress, storeTaxNumber, logoBytes, taxRate) = await LoadStoreInfoAsync(ct);
        var dto = await _builder.BuildFromSalesAsync(invoice, storeName, storePhone, storeAddress, storeTaxNumber, logoBytes, taxRate, ct);
        return Result<InvoicePrintDto>.Success(dto);
    }

    public async Task<Result<InvoicePrintDto>> GetPurchaseInvoicePrintDataAsync(int invoiceId, CancellationToken ct = default)
    {
        var invoice = await _uow.PurchaseInvoices.Query()
            .Include(i => i.Supplier)
            .Include(i => i.Items).ThenInclude(it => it.Product)
            .FirstOrDefaultAsync(i => i.Id == invoiceId, ct);

        if (invoice == null)
            return Result<InvoicePrintDto>.Failure("الفاتورة غير موجودة");

        var (storeName, storePhone, storeAddress, storeTaxNumber, logoBytes, taxRate) = await LoadStoreInfoAsync(ct);
        var dto = await _builder.BuildFromPurchaseAsync(invoice, storeName, storePhone, storeAddress, storeTaxNumber, logoBytes, taxRate, ct);
        return Result<InvoicePrintDto>.Success(dto);
    }

    private async Task<(string name, string phone, string address, string taxNumber,
        byte[]? logoBytes, decimal taxRate)> LoadStoreInfoAsync(CancellationToken ct)
    {
        var settings = await GetStoreSettingsAsync(ct);
        var sysSettings = await GetPrintSystemSettingsAsync(ct);

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

    public async Task<StoreSettings?> GetStoreSettingsAsync(CancellationToken ct = default)
    {
        return await _uow.StoreSettings.Query().FirstOrDefaultAsync(ct);
    }

    public async Task<List<SystemSetting>> GetPrintSystemSettingsAsync(CancellationToken ct = default)
    {
        return await _uow.SystemSettings.Query()
            .Where(s => s.Category == "Print")
            .ToListAsync(ct);
    }
}
