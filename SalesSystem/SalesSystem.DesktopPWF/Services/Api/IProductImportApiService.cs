using SalesSystem.Contracts.DTOs;

namespace SalesSystem.DesktopPWF.Services.Api;

/// <summary>
/// API service interface for product import operations (Excel bulk import).
/// Methods correspond to the backend ImportController endpoints.
/// </summary>
public interface IProductImportApiService
{
    /// <summary>
    /// Sends parsed rows to the API for validation/preview.
    /// Returns validation results with any errors detected.
    /// </summary>
    Task<ProductImportResultDto?> PreviewAsync(List<ProductImportRowDto> rows);

    /// <summary>
    /// Sends parsed rows to the API to execute the import.
    /// Returns import results including success/failure counts.
    /// </summary>
    Task<ProductImportResultDto?> ExecuteAsync(List<ProductImportRowDto> rows);

    /// <summary>
    /// Downloads the Excel template file from the API.
    /// Returns the raw file bytes.
    /// </summary>
    Task<byte[]?> DownloadTemplateAsync();
}
