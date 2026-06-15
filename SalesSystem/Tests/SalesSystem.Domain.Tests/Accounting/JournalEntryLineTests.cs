using FluentAssertions;
using SalesSystem.Domain.Accounting.Entities;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Tests.Accounting;

public class JournalEntryLineTests
{
    // ─── CreateDebitLine ──────────────────────────────

    [Fact]
    public void CreateDebitLine_Valid_SetsDebitValue()
    {
        // Act
        var line = JournalEntryLine.CreateDebit(
            accountId: 1,
            amount: 150.75m);

        // Assert
        line.Debit.Should().Be(150.75m);
    }

    [Fact]
    public void CreateDebitLine_Valid_CreditIsZero()
    {
        // Act
        var line = JournalEntryLine.CreateDebit(
            accountId: 1,
            amount: 100m);

        // Assert
        line.Credit.Should().Be(0m);
    }

    [Fact]
    public void CreateDebitLine_NegativeAmount_ThrowsDomainException()
    {
        // Act
        var act = () => JournalEntryLine.CreateDebit(
            accountId: 1,
            amount: -50m);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.Message.Should().Contain("الخصم");
    }

    [Fact]
    public void CreateDebitLine_ZeroAmount_ThrowsDomainException()
    {
        // Act
        var act = () => JournalEntryLine.CreateDebit(
            accountId: 1,
            amount: 0m);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.Message.Should().Contain("الخصم");
    }

    [Fact]
    public void CreateDebitLine_SetsAccountId()
    {
        // Act
        var line = JournalEntryLine.CreateDebit(
            accountId: 42,
            amount: 200m);

        // Assert
        line.AccountId.Should().Be(42);
    }

    [Fact]
    public void CreateDebitLine_WithDescription_SetsDescription()
    {
        // Act
        var line = JournalEntryLine.CreateDebit(
            accountId: 1,
            amount: 100m,
            description: "دفع نقدي");

        // Assert
        line.Description.Should().Be("دفع نقدي");
    }

    [Fact]
    public void CreateDebitLine_WithoutDescription_DescriptionIsNull()
    {
        // Act
        var line = JournalEntryLine.CreateDebit(
            accountId: 1,
            amount: 100m);

        // Assert
        line.Description.Should().BeNull();
    }

    // ─── CreateCreditLine ─────────────────────────────

    [Fact]
    public void CreateCreditLine_Valid_SetsCreditValue()
    {
        // Act
        var line = JournalEntryLine.CreateCredit(
            accountId: 2,
            amount: 250.50m);

        // Assert
        line.Credit.Should().Be(250.50m);
    }

    [Fact]
    public void CreateCreditLine_Valid_DebitIsZero()
    {
        // Act
        var line = JournalEntryLine.CreateCredit(
            accountId: 2,
            amount: 100m);

        // Assert
        line.Debit.Should().Be(0m);
    }

    [Fact]
    public void CreateCreditLine_NegativeAmount_ThrowsDomainException()
    {
        // Act
        var act = () => JournalEntryLine.CreateCredit(
            accountId: 2,
            amount: -50m);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.Message.Should().Contain("الإيداع");
    }

    [Fact]
    public void CreateCreditLine_ZeroAmount_ThrowsDomainException()
    {
        // Act
        var act = () => JournalEntryLine.CreateCredit(
            accountId: 2,
            amount: 0m);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.Message.Should().Contain("الإيداع");
    }

    [Fact]
    public void CreateCreditLine_SetsAccountId()
    {
        // Act
        var line = JournalEntryLine.CreateCredit(
            accountId: 55,
            amount: 300m);

        // Assert
        line.AccountId.Should().Be(55);
    }

    [Fact]
    public void CreateCreditLine_WithDescription_SetsDescription()
    {
        // Act
        var line = JournalEntryLine.CreateCredit(
            accountId: 2,
            amount: 100m,
            description: "قيد دائن");

        // Assert
        line.Description.Should().Be("قيد دائن");
    }

    [Fact]
    public void CreateCreditLine_WithoutDescription_DescriptionIsNull()
    {
        // Act
        var line = JournalEntryLine.CreateCredit(
            accountId: 2,
            amount: 100m);

        // Assert
        line.Description.Should().BeNull();
    }

    // ─── BaseEntity Inheritance ───────────────────────

    [Fact]
    public void CreatedLine_HasDefaultId_Zero()
    {
        // Act
        var line = JournalEntryLine.CreateDebit(
            accountId: 1,
            amount: 100m);

        // Assert
        line.Id.Should().Be(0); // Not yet persisted
    }

}
