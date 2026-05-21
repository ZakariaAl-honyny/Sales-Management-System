using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SalesSystem.Api.Controllers;
using SalesSystem.Application.Interfaces.Services;

namespace SalesSystem.Api.Tests.Controllers.Backup;

/// <summary>
/// Unit tests for BackupController HTTP status codes
/// </summary>
public class BackupControllerTests
{
    private readonly Mock<IBackupService> _backupServiceMock;
    private readonly BackupController _controller;

    public BackupControllerTests()
    {
        _backupServiceMock = new Mock<IBackupService>();
        _controller = new BackupController(_backupServiceMock.Object);
    }

    #region Create (Backup) Tests

    /// <summary>
    /// Given backup succeeds, when creating backup, then returns 200 OK
    /// </summary>
    [Fact]
    public async Task Create_WhenBackupSucceeds_ReturnsOkWithSuccessMessage()
    {
        // Arrange
        _backupServiceMock
            .Setup(x => x.CreateBackupAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Success("backup_2026_05_11.bak"));

        // Act
        var result = await _controller.Create(CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    /// <summary>
    /// Given service fails, when creating backup, then returns 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Create_WhenServiceFails_ReturnsBadRequest()
    {
        // Arrange
        _backupServiceMock
            .Setup(x => x.CreateBackupAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Failure("فشل في إنشاء النسخة الاحتياطية"));

        // Act
        var result = await _controller.Create(CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region GetList Tests

    /// <summary>
    /// Given backups exist, when getting backup list, then returns 200 OK
    /// </summary>
    [Fact]
    public async Task GetList_WhenBackupsExist_ReturnsOkWithBackupList()
    {
        // Arrange
        var backups = new List<string> { "backup_2026_05_10.bak", "backup_2026_05_11.bak" };

        _backupServiceMock
            .Setup(x => x.GetBackupListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<string>>.Success(backups));

        // Act
        var result = await _controller.GetList(CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    /// <summary>
    /// Given service fails, when getting backup list, then returns 400 Bad Request
    /// </summary>
    [Fact]
    public async Task GetList_WhenServiceFails_ReturnsBadRequest()
    {
        // Arrange
        _backupServiceMock
            .Setup(x => x.GetBackupListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<string>>.Failure("فشل في جلب قائمة النسخ"));

        // Act
        var result = await _controller.GetList(CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region Restore Tests

    /// <summary>
    /// Given valid filename, when restoring backup, then returns 200 OK
    /// </summary>
    [Fact]
    public async Task Restore_WhenValidFileName_ReturnsOkWithSuccessMessage()
    {
        // Arrange
        _backupServiceMock
            .Setup(x => x.RestoreBackupAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _controller.Restore("backup_2026_05_11.bak", CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    /// <summary>
    /// Given empty filename, when restoring backup, then returns 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Restore_WhenEmptyFileName_ReturnsBadRequest()
    {
        // Arrange
        // Act
        var result = await _controller.Restore("", CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Given whitespace filename, when restoring backup, then returns 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Restore_WhenWhitespaceFileName_ReturnsBadRequest()
    {
        // Arrange
        // Act
        var result = await _controller.Restore("   ", CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Given service fails, when restoring backup, then returns 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Restore_WhenServiceFails_ReturnsBadRequest()
    {
        // Arrange
        _backupServiceMock
            .Setup(x => x.RestoreBackupAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("فشل في استعادة النسخة الاحتياطية"));

        // Act
        var result = await _controller.Restore("backup_2026_05_11.bak", CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion
}