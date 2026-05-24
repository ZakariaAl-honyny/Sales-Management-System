using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Controllers;

/// <summary>
/// Database maintenance and backup API
/// </summary>
[ApiController]
[Route("api/v1/backup")]
[Authorize(Policy = "AdminOnly")]
public class BackupController : ControllerBase
{
    private readonly IBackupService _backupService;
    private readonly string _backupFolder;

    public BackupController(
        IBackupService backupService,
        IConfiguration configuration)
    {
        _backupService = backupService;
        _backupFolder = configuration["Backup:DefaultBackupPath"]
            ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
    }

    /// <summary>
    /// Triggers a manual database backup
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Success message and backup path</returns>
    [HttpPost("create")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        var result = await _backupService.CreateBackupAsync(null, ct);
        return result.IsSuccess
            ? Ok(new { message = "تم إنشاء النسخة الاحتياطية بنجاح", path = result.Value })
            : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Gets a list of available backup files
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of backup filenames</returns>
    [HttpGet("list")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetList(CancellationToken ct)
    {
        var result = await _backupService.GetBackupListAsync(ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Restores database from a specific backup file
    /// </summary>
    /// <param name="request">Backup file name</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Success message</returns>
    [HttpPost("restore")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Restore([FromBody] RestoreBackupRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.FileName))
            return BadRequest(new { error = "اسم ملف النسخة الاحتياطية مطلوب" });

        var filePath = Path.GetFullPath(Path.Combine(_backupFolder, request.FileName));

        // Path traversal guard — ensure resolved path is within allowed directory
        if (!filePath.StartsWith(_backupFolder, StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "مسار ملف غير صالح" });

        if (!System.IO.File.Exists(filePath))
            return NotFound(new { error = "ملف النسخة الاحتياطية غير موجود" });

        var result = await _backupService.RestoreBackupAsync(filePath, ct);
        return result.IsSuccess
            ? Ok(new { message = "تم استعادة النسخة الاحتياطية بنجاح" })
            : BadRequest(new { error = result.Error });
    }
}
