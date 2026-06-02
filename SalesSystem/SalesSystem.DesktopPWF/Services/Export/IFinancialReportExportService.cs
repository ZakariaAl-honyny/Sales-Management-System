using System.Data;

namespace SalesSystem.DesktopPWF.Services.Export;

/// <summary>
/// Service for exporting financial reports to Excel and PDF formats.
/// </summary>
public interface IFinancialReportExportService
{
    /// <summary>
    /// Exports the report data to an Excel (.xlsx) file.
    /// </summary>
    /// <param name="reportName">Name/title of the report to display in the worksheet.</param>
    /// <param name="data">DataTable containing report rows.</param>
    /// <param name="total">Grand total to display at the bottom of the report.</param>
    /// <param name="fileName">Suggested file name for the save dialog.</param>
    Task ExportToExcelAsync(string reportName, DataTable data, decimal total, string fileName);

    /// <summary>
    /// Exports the report data to a PDF file.
    /// </summary>
    /// <param name="reportName">Name/title of the report.</param>
    /// <param name="data">DataTable containing report rows.</param>
    /// <param name="total">Grand total to display at the bottom of the report.</param>
    /// <param name="fileName">Suggested file name for the save dialog.</param>
    Task ExportToPdfAsync(string reportName, DataTable data, decimal total, string fileName);
}
