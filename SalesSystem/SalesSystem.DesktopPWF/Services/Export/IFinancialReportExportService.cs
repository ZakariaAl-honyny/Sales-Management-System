using System.Data;
using SalesSystem.Contracts.Common;

namespace SalesSystem.DesktopPWF.Services.Export;

/// <summary>
/// Service for exporting financial reports to Excel and PDF formats.
/// </summary>
public interface IFinancialReportExportService
{
    /// <summary>
    /// Exports the report data to an Excel (.xlsx) file.
    /// </summary>
    Task ExportToExcelAsync(string reportName, DataTable data, decimal total, string fileName);

    /// <summary>
    /// Exports the report data to a PDF file.
    /// </summary>
    Task ExportToPdfAsync(string reportName, DataTable data, decimal total, string fileName);

    /// <summary>
    /// Generates Excel bytes from a list of DTOs.
    /// </summary>
    Task<Result<byte[]>> GenerateExcelBytesAsync<T>(string reportName, List<T> data, Dictionary<string, string>? columnHeaders = null);

    /// <summary>
    /// Generates PDF bytes from a data table.
    /// </summary>
    Task<Result<byte[]>> GeneratePdfBytesAsync(string reportName, DataTable data, string title);
}
