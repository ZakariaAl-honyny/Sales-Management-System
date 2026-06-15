using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;

namespace SalesSystem.Application.Interfaces.Services;

/// <summary>
/// Service for exporting reports to Excel and PDF formats.
/// </summary>
public interface IReportExportService
{
    /// <summary>
    /// Exports a list of report data to Excel using ClosedXML.
    /// </summary>
    Task<Result<ReportExportResult>> ExportToExcelAsync<T>(
        string reportName,
        List<T> data,
        Dictionary<string, string>? columnHeaders = null,
        CancellationToken ct = default);

    /// <summary>
    /// Exports report data to PDF using QuestPDF.
    /// </summary>
    Task<Result<ReportExportResult>> ExportToPdfAsync(
        string reportName,
        System.Data.DataTable data,
        string title,
        CancellationToken ct = default);

    /// <summary>
    /// Unified export endpoint that routes by report type.
    /// Accepts a report type identifier, target format, and optional filters.
    /// Delegates to the appropriate data-fetching service and export method.
    /// </summary>
    Task<Result<ReportExportResult>> ExportAsync(
        string reportType,
        string format,
        Dictionary<string, string>? filters = null,
        string? reportName = null,
        CancellationToken ct = default);
}
