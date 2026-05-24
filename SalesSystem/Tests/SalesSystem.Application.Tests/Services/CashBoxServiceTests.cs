using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Repositories;
using SalesSystem.Application.Services;
using SalesSystem.Contracts.Requests;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;
using System.Linq.Expressions;
using Xunit;

namespace SalesSystem.Application.Tests.Services;

public class CashBoxServiceTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<ILogger<CashBoxService>> _loggerMock;
    private readonly Mock<IGenericRepository<CashBox>> _cashBoxRepoMock;
    private readonly Mock<IGenericRepository<DailyClosure>> _closureRepoMock;
    private readonly Mock<IDbContextTransaction> _dbTransactionMock;
    private readonly CashBoxService _service;

    public CashBoxServiceTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<CashBoxService>>();
        _cashBoxRepoMock = new Mock<IGenericRepository<CashBox>>();
        var transactionRepoMock = new Mock<IGenericRepository<CashTransaction>>();
        _closureRepoMock = new Mock<IGenericRepository<DailyClosure>>();
        _dbTransactionMock = new Mock<IDbContextTransaction>();

        _uowMock.Setup(x => x.CashBoxes).Returns(_cashBoxRepoMock.Object);
        _uowMock.Setup(x => x.CashTransactions).Returns(transactionRepoMock.Object);
        _uowMock.Setup(x => x.DailyClosures).Returns(_closureRepoMock.Object);
        _uowMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _uowMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_dbTransactionMock.Object);

        _service = new CashBoxService(_uowMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task RecordExpenseAsync_WithSufficientBalance_CreatesTransaction()
    {
        var box = CashBox.Create("Test Box", initialBalance: 500m);
        _cashBoxRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(box);

        var result = await _service.RecordExpenseAsync(
            1, new AddCashTransactionRequest(50m, "اختبار مصروف"), userId: 1, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Amount.Should().Be(-50m);
        result.Value.BalanceBefore.Should().Be(500m);
        result.Value.BalanceAfter.Should().Be(450m);
        box.CurrentBalance.Should().Be(450m);
        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RecordExpenseAsync_WithInsufficientBalance_ReturnsFailure()
    {
        var box = CashBox.Create("Test Box", initialBalance: 50m);
        _cashBoxRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(box);

        var result = await _service.RecordExpenseAsync(
            1, new AddCashTransactionRequest(100m, "اختبار مصروف"), userId: 1, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("رصيد الصندوق غير كافٍ");
        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TransferAsync_WithInsufficientSource_ReturnsFailure()
    {
        var sourceBox = CashBox.Create("Source Box", initialBalance: 50m);
        var destBox = CashBox.Create("Dest Box", initialBalance: 100m);

        _cashBoxRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sourceBox);
        _cashBoxRepoMock.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(destBox);

        var request = new CashTransferRequest(
            SourceCashBoxId: 1, DestinationCashBoxId: 2, Amount: 200m, Notes: null);
        var result = await _service.TransferAsync(request, userId: 1, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("رصيد الصندوق غير كافٍ");
        _dbTransactionMock.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _dbTransactionMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TransferAsync_WithSufficientBalance_CreatesTwoTransactions()
    {
        var sourceBox = CashBox.Create("Source Box", initialBalance: 500m);
        var destBox = CashBox.Create("Dest Box", initialBalance: 100m);

        _cashBoxRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sourceBox);
        _cashBoxRepoMock.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(destBox);

        var request = new CashTransferRequest(
            SourceCashBoxId: 1, DestinationCashBoxId: 2, Amount: 200m, Notes: null);
        var result = await _service.TransferAsync(request, userId: 1, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        sourceBox.CurrentBalance.Should().Be(300m);
        destBox.CurrentBalance.Should().Be(300m);
        sourceBox.Transactions.Should().ContainSingle(t => t.TransactionType == CashTransactionType.TransferOut);
        destBox.Transactions.Should().ContainSingle(t => t.TransactionType == CashTransactionType.TransferIn);
        _dbTransactionMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _dbTransactionMock.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PerformDailyClosureAsync_DuplicateDate_ReturnsFailure()
    {
        var box = CashBox.Create("Test Box", initialBalance: 500m);
        _cashBoxRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(box);
        _closureRepoMock.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<DailyClosure, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _service.PerformDailyClosureAsync(1, userId: 1, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("تم إغلاق الصندوق بالفعل");
        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
