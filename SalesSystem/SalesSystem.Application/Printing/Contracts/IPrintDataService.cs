using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

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
    /// Gets sales return print data including store info.
    /// </summary>
    Task<Result<InvoicePrintDto>> GetSalesReturnPrintDataAsync(int returnId, CancellationToken ct = default);

    /// <summary>
    /// Gets purchase return print data including store info.
    /// </summary>
    Task<Result<InvoicePrintDto>> GetPurchaseReturnPrintDataAsync(int returnId, CancellationToken ct = default);

    /// <summary>
    /// Gets sales quotation print data including store info.
    /// </summary>
    Task<Result<InvoicePrintDto>> GetSalesQuotationPrintDataAsync(int quotationId, CancellationToken ct = default);

    /// <summary>
    /// Gets the store settings for print header info.
    /// </summary>
    Task<Result<StoreSettingsDto>> GetStoreSettingsAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets all print settings as a PrintSettingsDto.
    /// Reads from SystemSettings (Category="Print").
    /// </summary>
    Task<Result<PrintSettingsDto>> GetPrintSettingsAsync(CancellationToken ct = default);

    /// <summary>
    /// Updates all print settings via upsert into SystemSettings (Category="Print").
    /// </summary>
    Task<Result> UpdatePrintSettingsAsync(UpdatePrintSettingsRequest request, CancellationToken ct = default);
}
