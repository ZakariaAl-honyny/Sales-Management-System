using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SalesSystem.Contracts.Common;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Application.Printing.Contracts;

/// <summary>
/// Service interface for loading invoice data for printing.
/// Replaces direct SalesDbContext access from PrintController.
/// </summary>
public interface IPrintDataService
{
    /// <summary>
    /// Gets sales invoice print data including store info, or null if not found.
    /// </summary>
    Task<Result<InvoicePrintDto>> GetSalesInvoicePrintDataAsync(int invoiceId, CancellationToken ct = default);

    /// <summary>
    /// Gets purchase invoice print data including store info, or null if not found.
    /// </summary>
    Task<Result<InvoicePrintDto>> GetPurchaseInvoicePrintDataAsync(int invoiceId, CancellationToken ct = default);

    /// <summary>
    /// Gets the store settings for print header info.
    /// </summary>
    Task<Result<StoreSettings>> GetStoreSettingsAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets the print system settings (LogoPath, TaxRate, StoreTaxNumber).
    /// </summary>
    Task<Result<List<SystemSetting>>> GetPrintSystemSettingsAsync(CancellationToken ct = default);
}
