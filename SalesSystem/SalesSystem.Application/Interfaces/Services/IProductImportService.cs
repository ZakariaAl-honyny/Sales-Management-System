using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;

namespace SalesSystem.Application.Interfaces.Services;

/// <summary>
/// Service for importing products in bulk from Excel data.
/// The Desktop client parses the Excel file using ClosedXML and sends
/// structured JSON (List&lt;ProductImportRowDto&gt;) to the API for validation and persistence.
/// </summary>
public interface IProductImportService
{
    /// <summary>
    /// Validates the imported rows without making any database changes.
    /// Returns preview results including per-row errors.
    /// </summary>
    Task<Result<ProductImportResultDto>> PreviewAsync(List<ProductImportRowDto> rows, CancellationToken ct);

    /// <summary>
    /// Executes the import by persisting valid rows inside a transaction.
    /// Creates categories (if they don't exist), products, and base product units.
    /// </summary>
    Task<Result<ProductImportResultDto>> ExecuteAsync(List<ProductImportRowDto> rows, int userId, CancellationToken ct);

    /// <summary>
    /// Generates a CSV template file with headers for product import.
    /// The template can be opened in Excel or any spreadsheet application.
    /// </summary>
    byte[] GenerateTemplate();
}
