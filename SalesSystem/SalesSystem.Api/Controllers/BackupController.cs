using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;

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

    public BackupController(IBackupService backupService)
    {
        _backupService = backupService;
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
    /// <param name="fileName">Backup filename</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Success message</returns>
    [HttpPost("restore")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Restore([FromQuery] string fileName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return BadRequest("اسم الملف مطلوب");

        // We assume files are in the default backup folder
        var defaultFolder = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups"));
        var filePath = Path.GetFullPath(Path.Combine(defaultFolder, fileName));

        // Path traversal guard — ensure resolved path is within allowed directory
        if (!filePath.StartsWith(defaultFolder, StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "مسار ملف غير صالح" });

        if (!System.IO.File.Exists(filePath))
            return NotFound(new { error = "ملف النسخة الاحتياطية غير موجود" });

        var result = await _backupService.RestoreBackupAsync(filePath, ct);
        return result.IsSuccess
            ? Ok(new { message = "تم استعادة النسخة الاحتياطية بنجاح" })
            : BadRequest(new { error = result.Error });
    }
}
