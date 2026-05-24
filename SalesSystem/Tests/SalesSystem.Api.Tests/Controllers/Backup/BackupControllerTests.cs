using System.IO;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using SalesSystem.Api.Controllers;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Tests.Controllers.Backup;

public class BackupControllerTests
{
    private readonly Mock<IBackupService> _backupServiceMock;
    private readonly BackupController _controller;
    private readonly string _backupDir;

    public BackupControllerTests()
    {
        _backupServiceMock = new Mock<IBackupService>();
        _backupDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups"));

        var backupSettings = Options.Create(new BackupSettings { DefaultBackupPath = _backupDir });

        _controller = new BackupController(_backupServiceMock.Object, backupSettings);
    }

    [Fact]
    public async Task Create_WhenBackupSucceeds_ReturnsOkWithSuccessMessage()
    {
        _backupServiceMock
            .Setup(x => x.CreateBackupAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Success("backup_2026_05_11.bak"));

        var result = await _controller.Create(CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task Create_WhenServiceFails_ReturnsBadRequest()
    {
        _backupServiceMock
            .Setup(x => x.CreateBackupAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Failure("فشل في إنشاء النسخة الاحتياطية"));

        var result = await _controller.Create(CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetList_WhenBackupsExist_ReturnsOkWithBackupList()
    {
        var backups = new List<string> { "backup_2026_05_10.bak", "backup_2026_05_11.bak" };

        _backupServiceMock
            .Setup(x => x.GetBackupListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<string>>.Success(backups));

        var result = await _controller.GetList(CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetList_WhenServiceFails_ReturnsBadRequest()
    {
        _backupServiceMock
            .Setup(x => x.GetBackupListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<string>>.Failure("فشل في جلب قائمة النسخ"));

        var result = await _controller.GetList(CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Restore_WhenValidFileName_ReturnsOkWithSuccessMessage()
    {
        Directory.CreateDirectory(_backupDir);
        var backupFilePath = Path.Combine(_backupDir, "backup_2026_05_11.bak");
        await File.WriteAllTextAsync(backupFilePath, "test");

        _backupServiceMock
            .Setup(x => x.RestoreBackupAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await _controller.Restore(new RestoreBackupRequest("backup_2026_05_11.bak"), CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        if (File.Exists(backupFilePath)) File.Delete(backupFilePath);
    }

    [Fact]
    public async Task Restore_WhenEmptyFileName_ReturnsBadRequest()
    {
        var result = await _controller.Restore(new RestoreBackupRequest(""), CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Restore_WhenWhitespaceFileName_ReturnsBadRequest()
    {
        var result = await _controller.Restore(new RestoreBackupRequest("   "), CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Restore_WhenServiceFails_ReturnsBadRequest()
    {
        Directory.CreateDirectory(_backupDir);
        var backupFilePath = Path.Combine(_backupDir, "backup_2026_05_11.bak");
        await File.WriteAllTextAsync(backupFilePath, "test");

        _backupServiceMock
            .Setup(x => x.RestoreBackupAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("فشل في استعادة النسخة الاحتياطية"));

        var result = await _controller.Restore(new RestoreBackupRequest("backup_2026_05_11.bak"), CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();

        if (File.Exists(backupFilePath)) File.Delete(backupFilePath);
    }
}
