using FluentAssertions;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;

namespace SalesSystem.Domain.Tests.Entities;

public class CashTransactionTests
{
    [Fact]
    public void Create_GivenSalesIncomeType_ShouldSetPropertiesCorrectly()
    {
        var transaction = CashTransaction.Create(
            cashBoxId: 1,
            type: CashTransactionType.SalesIncome,
            amount: 500m,
            balanceBefore: 1000m,
            balanceAfter: 1500m,
            referenceType: "SalesInvoice",
            referenceId: 42,
            createdBy: 3,
            notes: "Cash sale"
        );

        transaction.CashBoxId.Should().Be(1);
        transaction.TransactionType.Should().Be(CashTransactionType.SalesIncome);
        transaction.Amount.Should().Be(500m);
        transaction.BalanceBefore.Should().Be(1000m);
        transaction.BalanceAfter.Should().Be(1500m);
        transaction.ReferenceType.Should().Be("SalesInvoice");
        transaction.ReferenceId.Should().Be(42);
        transaction.CreatedByUserId.Should().Be(3);
        transaction.Notes.Should().Be("Cash sale");
    }

    [Fact]
    public void Create_GivenSupplierPaymentType_ShouldSetTypeCorrectly()
    {
        var transaction = CashTransaction.Create(
            cashBoxId: 1,
            type: CashTransactionType.SupplierPayment,
            amount: -200m,
            balanceBefore: 1500m,
            balanceAfter: 1300m,
            referenceType: "PurchaseInvoice",
            referenceId: 10,
            createdBy: 1,
            notes: null
        );

        transaction.TransactionType.Should().Be(CashTransactionType.SupplierPayment);
        transaction.Amount.Should().Be(-200m);
    }

    [Fact]
    public void Create_GivenTransferInType_ShouldSetTypeCorrectly()
    {
        var transaction = CashTransaction.Create(
            cashBoxId: 2,
            type: CashTransactionType.TransferIn,
            amount: 1000m,
            balanceBefore: 500m,
            balanceAfter: 1500m,
            referenceType: "CashTransfer",
            referenceId: 5,
            createdBy: 1,
            notes: null
        );

        transaction.TransactionType.Should().Be(CashTransactionType.TransferIn);
    }

    [Fact]
    public void Create_GivenTransferOutType_ShouldSetTypeCorrectly()
    {
        var transaction = CashTransaction.Create(
            cashBoxId: 1,
            type: CashTransactionType.TransferOut,
            amount: -500m,
            balanceBefore: 2000m,
            balanceAfter: 1500m,
            referenceType: "CashTransfer",
            referenceId: 5,
            createdBy: 1,
            notes: null
        );

        transaction.TransactionType.Should().Be(CashTransactionType.TransferOut);
    }

    [Fact]
    public void Create_GivenCustomerPaymentType_ShouldSetTypeCorrectly()
    {
        var transaction = CashTransaction.Create(
            cashBoxId: 1,
            type: CashTransactionType.CustomerPayment,
            amount: 300m,
            balanceBefore: 1000m,
            balanceAfter: 1300m,
            referenceType: null,
            referenceId: null,
            createdBy: 1,
            notes: "Manual deposit"
        );

        transaction.TransactionType.Should().Be(CashTransactionType.CustomerPayment);
    }

    [Fact]
    public void Create_GivenExpenseType_ShouldSetTypeCorrectly()
    {
        var transaction = CashTransaction.Create(
            cashBoxId: 1,
            type: CashTransactionType.Expense,
            amount: -100m,
            balanceBefore: 1300m,
            balanceAfter: 1200m,
            referenceType: null,
            referenceId: null,
            createdBy: 1,
            notes: "Owner withdrawal"
        );

        transaction.TransactionType.Should().Be(CashTransactionType.Expense);
    }

    [Fact]
    public void Create_GivenNullReferenceTypeAndId_ShouldStoreNull()
    {
        var transaction = CashTransaction.Create(
            cashBoxId: 1,
            type: CashTransactionType.CustomerPayment,
            amount: 100m,
            balanceBefore: 0m,
            balanceAfter: 100m,
            referenceType: null,
            referenceId: null,
            createdBy: 1,
            notes: null
        );

        transaction.ReferenceType.Should().BeNull();
        transaction.ReferenceId.Should().BeNull();
    }

    [Theory]
    [InlineData(CashTransactionType.OpeningBalance)]
    [InlineData(CashTransactionType.SalesIncome)]
    [InlineData(CashTransactionType.Expense)]
    [InlineData(CashTransactionType.TransferOut)]
    [InlineData(CashTransactionType.TransferIn)]
    [InlineData(CashTransactionType.RefundOut)]
    [InlineData(CashTransactionType.SupplierPayment)]
    [InlineData(CashTransactionType.CustomerPayment)]
    public void Create_GivenAllTransactionTypes_ShouldSucceed(CashTransactionType type)
    {
        var transaction = CashTransaction.Create(
            cashBoxId: 1,
            type: type,
            amount: 100m,
            balanceBefore: 0m,
            balanceAfter: 100m,
            referenceType: null,
            referenceId: null,
            createdBy: 1,
            notes: null
        );

        transaction.TransactionType.Should().Be(type);
    }

    [Fact]
    public void Create_GivenNegativeAmount_ShouldSucceed()
    {
        // Entity has no guard clause — negative amounts are valid (e.g., expenses, transfers out)
        var transaction = CashTransaction.Create(
            cashBoxId: 1,
            type: CashTransactionType.SupplierPayment,
            amount: -500m,
            balanceBefore: 1000m,
            balanceAfter: 500m,
            referenceType: "PurchaseInvoice",
            referenceId: 1,
            createdBy: 1,
            notes: null
        );

        transaction.Amount.Should().Be(-500m);
    }

    [Fact]
    public void Create_GivenZeroBalance_ShouldSucceed()
    {
        var transaction = CashTransaction.Create(
            cashBoxId: 1,
            type: CashTransactionType.CustomerPayment,
            amount: 0m,
            balanceBefore: 0m,
            balanceAfter: 0m,
            referenceType: null,
            referenceId: null,
            createdBy: 1,
            notes: "Opening"
        );

        transaction.Amount.Should().Be(0m);
        transaction.BalanceBefore.Should().Be(0m);
        transaction.BalanceAfter.Should().Be(0m);
    }

    [Fact]
    public void Create_ComputesBalanceAfterFromBalanceBeforePlusAmount()
    {
        var before = 1000m;
        var amount = 250m;
        var after = before + amount;

        var transaction = CashTransaction.Create(
            cashBoxId: 1,
            type: CashTransactionType.SalesIncome,
            amount: amount,
            balanceBefore: before,
            balanceAfter: after,
            referenceType: null,
            referenceId: null,
            createdBy: 1,
            notes: null
        );

        transaction.BalanceAfter.Should().Be(transaction.BalanceBefore + transaction.Amount);
    }

    [Theory]
    [InlineData(0, 100, 100)]
    [InlineData(500, 200, 700)]
    [InlineData(1000, -300, 700)]
    [InlineData(999.99, 0.01, 1000)]
    public void Create_BalanceRelationship_ShouldBePreserved(decimal before, decimal amount, decimal expectedAfter)
    {
        var transaction = CashTransaction.Create(
            cashBoxId: 1,
            type: CashTransactionType.SalesIncome,
            amount: amount,
            balanceBefore: before,
            balanceAfter: expectedAfter,
            referenceType: null,
            referenceId: null,
            createdBy: 1,
            notes: null
        );

        transaction.BalanceAfter.Should().Be(transaction.BalanceBefore + transaction.Amount);
    }

    [Fact]
    public void Create_SetsCreatedAtToUtcNow()
    {
        var before = DateTime.UtcNow;
        var transaction = CashTransaction.Create(1, CashTransactionType.SalesIncome, 100m, 0m, 100m, null, null, 1, null);
        var after = DateTime.UtcNow;

        transaction.CreatedAt.Should().BeOnOrAfter(before);
        transaction.CreatedAt.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void OnceCreated_Properties_ShouldBeImmutable()
    {
        var transaction = CashTransaction.Create(1, CashTransactionType.SalesIncome, 100m, 0m, 100m, null, null, 1, null);

        // All setters are private — no public mutators exist
        transaction.GetType().GetProperty(nameof(CashTransaction.Amount))!.SetMethod!.IsPublic.Should().BeFalse();
        transaction.GetType().GetProperty(nameof(CashTransaction.BalanceBefore))!.SetMethod!.IsPublic.Should().BeFalse();
        transaction.GetType().GetProperty(nameof(CashTransaction.BalanceAfter))!.SetMethod!.IsPublic.Should().BeFalse();
        transaction.GetType().GetProperty(nameof(CashTransaction.TransactionType))!.SetMethod!.IsPublic.Should().BeFalse();
    }
}
