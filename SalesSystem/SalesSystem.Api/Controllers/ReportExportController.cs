using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Controllers;

/// <summary>
/// Controller for exporting reports to Excel and PDF formats.
/// </summary>
[ApiController]
[Route("api/v1/reports/export")]
[Authorize(Policy = "ManagerAndAbove")]
public class ReportExportController : ControllerBase
{
    private readonly IReportExportService _reportExportService;

    public ReportExportController(IReportExportService reportExportService)
    {
        _reportExportService = reportExportService;
    }

    /// <summary>
    /// Exports report data to Excel or PDF format.
    /// Accepts a ReportExportRequest with report type, format, and optional filters.
    /// Returns the file as a downloadable response.
    /// </summary>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Export([FromBody] ReportExportRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.ReportType))
            return BadRequest(new { error = "نوع التقرير مطلوب" });

        if (string.IsNullOrWhiteSpace(request.Format))
            return BadRequest(new { error = "صيغة التصدير مطلوبة (Excel أو PDF)" });

        var format = request.Format.ToLower();
        if (format != "excel" && format != "pdf")
            return BadRequest(new { error = "صيغة التصدير غير مدعومة. استخدم Excel أو PDF" });

        var result = await _reportExportService.ExportAsync(
            request.ReportType,
            format,
            request.Filters,
            request.ReportName,
            ct);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return File(result.Value!.FileContent, result.Value.ContentType, result.Value.FileName);
    }
}
