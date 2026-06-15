// ═══════════════════════════════════════════════════════════════════════════
//  LEGACY: CashBoxServiceTests — CashBoxService class was REMOVED from the
//  Application layer in the 65-table schema migration. CashBox operations
//  are now handled by ReceiptVoucher/PaymentVoucher services.
//  This file is preserved for reference only — NOT included in build.
// ═══════════════════════════════════════════════════════════════════════════
#if false
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Repositories;
using SalesSystem.Application.Services;
using SalesSystem.Contracts.Requests;
using SalesSystem.Domain.Accounting.Entities;
using SalesSystem.Domain.Accounting.Enums;
using SalesSystem.Domain.Common;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Entities;
using System.Linq.Expressions;
using Xunit;

namespace SalesSystem.Application.Tests.Services;

public class CashBoxServiceTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<ILogger<CashBoxService>> _loggerMock;
    private readonly Mock<IGenericRepository<CashBox>> _cashBoxRepoMock;
    private readonly Mock<IGenericRepository<CashTransaction>> _cashTransactionRepoMock;
    private readonly Mock<IGenericRepository<DailyClosure>> _closureRepoMock;
    private readonly Mock<IGenericRepository<Account>> _accountsRepoMock;
    private readonly Mock<IDbContextTransaction> _dbTransactionMock;
    private readonly CashBoxService _service;

    public CashBoxServiceTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<CashBoxService>>();
        _cashBoxRepoMock = new Mock<IGenericRepository<CashBox>>();
        _cashTransactionRepoMock = new Mock<IGenericRepository<CashTransaction>>();
        _closureRepoMock = new Mock<IGenericRepository<DailyClosure>>();
        _accountsRepoMock = new Mock<IGenericRepository<Account>>();
        _dbTransactionMock = new Mock<IDbContextTransaction>();

        _uowMock.Setup(x => x.CashBoxes).Returns(_cashBoxRepoMock.Object);
        _uowMock.Setup(x => x.CashTransactions).Returns(_cashTransactionRepoMock.Object);
        _uowMock.Setup(x => x.DailyClosures).Returns(_closureRepoMock.Object);
        _uowMock.Setup(x => x.Accounts).Returns(_accountsRepoMock.Object);
        _uowMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _uowMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_dbTransactionMock.Object);

        // ExecuteTransactionAsync must invoke the callback (per RULE-275)
        _uowMock.Setup(x => x.ExecuteTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .Callback<Func<Task>, CancellationToken>(async (operation, _) => await operation())
            .Returns(Task.CompletedTask);

        _service = new CashBoxService(_uowMock.Object, _loggerMock.Object);
    }

    // ─── CreateAsync Tests ────────────────────────────────

    [Fact]
    public async Task CreateAsync_WithAccountId_UsesProvidedAccount()
    {
        var request = new CreateCashBoxRequest(
            BoxName: "Test Box",
            AccountId: 5,
            CategoryId: null,
            BranchId: null,
            AssignedUserId: null,
            CurrencyId: null,
            PhoneNumber: null,
            TaxNumber: null,
            Address: null,
            Notes: null);

        _cashBoxRepoMock.Setup(r => r.AddAsync(It.IsAny<CashBox>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CashBox box, CancellationToken _) => box);

        var result = await _service.CreateAsync(request, userId: 1, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.AccountId.Should().Be(5);
        result.Value.BoxName.Should().Be("Test Box");
        _cashBoxRepoMock.Verify(r => r.AddAsync(It.Is<CashBox>(b => b.AccountId == 5), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_AutoCreatesAccount()
    {
        var request = new CreateCashBoxRequest(
            BoxName: "New Cash Box",
            AccountId: null,
            CategoryId: null,
            BranchId: null,
            AssignedUserId: null,
            CurrencyId: null,
            PhoneNumber: null,
            TaxNumber: null,
            Address: null,
            Notes: null);

        // Create parent account (1110 — النقدية, Level 3)
        var parentAccount = Account.Create(
            "1110", "النقدية", "Cash", AccountType.Asset, 3,
            null, true, "النقدية بالصندوق", "#2196F3",
            false, 0m, null, null, 1);
        typeof(Entity).GetProperty("Id")!.SetValue(parentAccount, 10);

        // Mock parent lookup: accept any predicate
        _accountsRepoMock.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<Account, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
            .ReturnsAsync(parentAccount);

        // Mock no existing children under 1110
        _accountsRepoMock.Setup(r => r.ToListAsync(
                It.IsAny<Expression<Func<Account, bool>>?>(),
                It.IsAny<Func<IQueryable<Account>, IQueryable<Account>>?>(),
                It.IsAny<CancellationToken>(),
                false,
                It.IsAny<string[]>()))
            .ReturnsAsync(new List<Account>());

        // Mock AddAsync on accounts and cashboxes
        _accountsRepoMock.Setup(r => r.AddAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account a, CancellationToken _) =>
            {
                // Simulate EF Core auto-generating the Id on Add
                typeof(Entity).GetProperty("Id")!.SetValue(a, 999);
                return a;
            });

        _cashBoxRepoMock.Setup(r => r.AddAsync(It.IsAny<CashBox>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CashBox box, CancellationToken _) => box);

        var result = await _service.CreateAsync(request, userId: 1, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.BoxName.Should().Be("New Cash Box");
        // A new account should have been created
        _accountsRepoMock.Verify(r => r.AddAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()), Times.Once);
        _cashBoxRepoMock.Verify(r => r.AddAsync(It.Is<CashBox>(b => b.AccountId > 0), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    // ─── RecordExpenseAsync Tests ──────────────────────────

    [Fact]
    public async Task RecordExpenseAsync_CreatesTransaction()
    {
        var box = CashBox.Create("Test Box", accountId: 1, currencyId: 1);
        _cashBoxRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(box);

        // Empty transaction list → running balance = 0
        _cashTransactionRepoMock.Setup(r => r.ToListAsync(
                It.IsAny<Expression<Func<CashTransaction, bool>>?>(),
                It.IsAny<Func<IQueryable<CashTransaction>, IQueryable<CashTransaction>>?>(),
                It.IsAny<CancellationToken>(),
                false,
                It.IsAny<string[]>()))
            .ReturnsAsync(new List<CashTransaction>());

        var result = await _service.RecordExpenseAsync(
            1, new AddCashTransactionRequest(50m, "اختبار مصروف"), userId: 1, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Amount.Should().Be(-50m);
        result.Value.RunningBalance.Should().Be(-50m);
        result.Value.CashBoxId.Should().Be(1);
        _cashTransactionRepoMock.Verify(r => r.AddAsync(It.IsAny<CashTransaction>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RecordExpenseAsync_BoxNotFound_ReturnsFailure()
    {
        _cashBoxRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CashBox?)null);

        var result = await _service.RecordExpenseAsync(
            1, new AddCashTransactionRequest(50m, "اختبار"), userId: 1, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("الصندوق غير موجود");
        _cashTransactionRepoMock.Verify(r => r.AddAsync(It.IsAny<CashTransaction>(), It.IsAny<CancellationToken>()), Times.Never);
        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ─── TransferAsync Tests ───────────────────────────────

    [Fact]
    public async Task TransferAsync_WithValidBoxes_CreatesTwoTransactions()
    {
        var sourceBox = CashBox.Create("Source Box", accountId: 1, currencyId: 1);
        var destBox = CashBox.Create("Dest Box", accountId: 2, currencyId: 1);

        _cashBoxRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sourceBox);
        _cashBoxRepoMock.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(destBox);

        // Empty transaction lists → running balances = 0
        _cashTransactionRepoMock.Setup(r => r.ToListAsync(
                It.IsAny<Expression<Func<CashTransaction, bool>>?>(),
                It.IsAny<Func<IQueryable<CashTransaction>, IQueryable<CashTransaction>>?>(),
                It.IsAny<CancellationToken>(),
                false,
                It.IsAny<string[]>()))
            .ReturnsAsync(new List<CashTransaction>());

        var request = new CashTransferRequest(
            SourceCashBoxId: 1, DestinationCashBoxId: 2, Amount: 200m, Notes: null);
        var result = await _service.TransferAsync(request, userId: 1, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _cashTransactionRepoMock.Verify(
            r => r.AddAsync(It.Is<CashTransaction>(t => t.CashBoxId == 1 && t.TransactionType == CashTransactionType.TransferOut && t.Amount == -200m), It.IsAny<CancellationToken>()),
            Times.Once);
        _cashTransactionRepoMock.Verify(
            r => r.AddAsync(It.Is<CashTransaction>(t => t.CashBoxId == 2 && t.TransactionType == CashTransactionType.TransferIn && t.Amount == 200m), It.IsAny<CancellationToken>()),
            Times.Once);
        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TransferAsync_SameBox_ReturnsFailure()
    {
        var request = new CashTransferRequest(
            SourceCashBoxId: 1, DestinationCashBoxId: 1, Amount: 100m, Notes: null);
        var result = await _service.TransferAsync(request, userId: 1, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("نفس الصندوق");
        _cashTransactionRepoMock.Verify(r => r.AddAsync(It.IsAny<CashTransaction>(), It.IsAny<CancellationToken>()), Times.Never);
        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TransferAsync_SourceBoxNotFound_ReturnsFailure()
    {
        _cashBoxRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CashBox?)null);

        var request = new CashTransferRequest(
            SourceCashBoxId: 1, DestinationCashBoxId: 2, Amount: 100m, Notes: null);
        var result = await _service.TransferAsync(request, userId: 1, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("الصندوق المصدر غير موجود");
    }

    // ─── PerformDailyClosureAsync Tests ────────────────────

    [Fact]
    public async Task PerformDailyClosureAsync_DuplicateDate_ReturnsFailure()
    {
        var box = CashBox.Create("Test Box", accountId: 1, currencyId: 1);
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
#endif
