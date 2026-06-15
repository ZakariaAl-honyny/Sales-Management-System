using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;

namespace SalesSystem.Api.Controllers;

/// <summary>
/// Controller for bulk product import operations.
/// The Desktop client parses Excel files using ClosedXML (approved for Desktop only)
/// and sends structured JSON data to these endpoints.
/// </summary>
/// <remarks>
/// - Preview & Execute: ManagerAndAbove (creating/modifying products requires write access)<br/>
/// - Template download: AllStaff (read-only access to template)
/// </remarks>
[ApiController]
[Route("api/v1/products/import")]
[Authorize]
public class ProductImportController : ControllerBase
{
    private readonly IProductImportService _importService;

    public ProductImportController(IProductImportService importService)
    {
        _importService = importService;
    }

    /// <summary>
    /// Validates imported data and returns a preview with per-row errors, without making DB changes.
    /// </summary>
    /// <param name="rows">List of product rows parsed from Excel by the Desktop client.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Returns preview result with success/failure counts and row-level errors.</returns>
    [HttpPost("preview")]
    [Authorize(Policy = "ManagerAndAbove")]
    [ProducesResponseType(typeof(ProductImportResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Preview([FromBody] List<ProductImportRowDto> rows, CancellationToken ct)
    {
        var result = await _importService.PreviewAsync(rows, ct);
        if (result.IsSuccess && result.Value != null)
            return Ok(result.Value);
        return BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Imports validated product data into the database inside a transaction.
    /// Auto-creates categories if they don't exist, and creates base product units.
    /// </summary>
    /// <param name="rows">List of product rows parsed from Excel by the Desktop client.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Returns import result with success/failure counts and row-level errors.</returns>
    [HttpPost("execute")]
    [Authorize(Policy = "ManagerAndAbove")]
    [ProducesResponseType(typeof(ProductImportResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Execute([FromBody] List<ProductImportRowDto> rows, CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await _importService.ExecuteAsync(rows, userId, ct);
        if (result.IsSuccess && result.Value != null)
            return Ok(result.Value);
        return BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Downloads a CSV template file with headers for product import.
    /// The template can be opened in Excel or any spreadsheet application.
    /// </summary>
    /// <returns>Returns a CSV file download.</returns>
    [HttpGet("template")]
    [Authorize(Policy = "AllStaff")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult DownloadTemplate()
    {
        var bytes = _importService.GenerateTemplate();
        return File(
            bytes,
            "text/csv; charset=utf-8",
            "Products_Import_Template.csv"
        );
    }

    /// <summary>
    /// Extracts the authenticated user's ID from JWT claims.
    /// </summary>
    private int GetUserId()
    {
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (claim != null && int.TryParse(claim.Value, out var userId))
            return userId;
        throw new UnauthorizedAccessException("User not authenticated — JWT claim missing.");
    }
}
