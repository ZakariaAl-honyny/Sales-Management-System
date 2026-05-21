using Xunit;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Tests.Entities;

public class CashBoxTests
{
    [Fact]
    public void Create_EmptyName_ShouldThrow()
    {
        Assert.Throws<DomainException>(() => CashBox.Create(""));
    }

    [Fact]
    public void Create_WithInitialBalance_ShouldSetBalance()
    {
        var box = CashBox.Create("الصندوق الرئيسي", initialBalance: 1000);
        Assert.Equal(1000, box.CurrentBalance);
    }

    [Fact]
    public void Deposit_PositiveAmount_ShouldIncreaseBalance()
    {
        var box = CashBox.Create("صندوق", initialBalance: 500);
        box.Deposit(200, CashTransactionType.ManualIn, createdBy: 1);
        Assert.Equal(700, box.CurrentBalance);
    }

    [Fact]
    public void Deposit_ZeroOrNegative_ShouldThrow()
    {
        var box = CashBox.Create("صندوق");
        Assert.Throws<DomainException>(() => 
            box.Deposit(0, CashTransactionType.ManualIn));
        Assert.Throws<DomainException>(() => 
            box.Deposit(-100, CashTransactionType.ManualIn));
    }

    [Fact]
    public void Withdraw_SufficientBalance_ShouldDecreaseBalance()
    {
        var box = CashBox.Create("صندوق", initialBalance: 1000);
        box.Withdraw(300, CashTransactionType.ManualOut, createdBy: 1);
        Assert.Equal(700, box.CurrentBalance);
    }

    [Fact]
    public void Withdraw_InsufficientBalance_ShouldThrow()
    {
        var box = CashBox.Create("صندوق", initialBalance: 100);
        Assert.Throws<DomainException>(() => 
            box.Withdraw(200, CashTransactionType.ManualOut));
    }

    [Fact]
    public void Deposit_ShouldCreateTransactionWithCorrectSnapshots()
    {
        var box = CashBox.Create("صندوق", initialBalance: 500);
        var tx = box.Deposit(200, CashTransactionType.ManualIn, createdBy: 1);

        Assert.Equal(500, tx.BalanceBefore);
        Assert.Equal(700, tx.BalanceAfter);
        Assert.Equal(200, tx.Amount);
        Assert.Equal(CashTransactionType.ManualIn, tx.TransactionType);
    }

    [Fact]
    public void Withdraw_ShouldCreateTransactionWithCorrectSnapshots()
    {
        var box = CashBox.Create("صندوق", initialBalance: 500);
        var tx = box.Withdraw(200, CashTransactionType.ManualOut, createdBy: 1);

        Assert.Equal(500, tx.BalanceBefore);
        Assert.Equal(300, tx.BalanceAfter);
        Assert.Equal(-200, tx.Amount); // Negative for withdrawals
    }

    [Fact]
    public void ValidateUserAccess_AssignedToDifferentUser_ShouldThrow()
    {
        var box = CashBox.Create("صندوق المدير", assignedUserId: 1);
        Assert.Throws<DomainException>(() => box.ValidateUserAccess(2));
    }

    [Fact]
    public void ValidateUserAccess_SharedBox_ShouldAllowAnyone()
    {
        var box = CashBox.Create("صندوق مشترك"); // No assigned user
        box.ValidateUserAccess(99); // Should not throw
        Assert.True(true);
    }
}
