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
            accountCode: "101",
            accountNameAr: "نقدي",
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
            accountCode: "101",
            accountNameAr: "نقدي",
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
            accountCode: "101",
            accountNameAr: "نقدي",
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
            accountCode: "101",
            accountNameAr: "نقدي",
            amount: 0m);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.Message.Should().Contain("الخصم");
    }

    [Fact]
    public void CreateDebitLine_EmptyAccountCode_ThrowsDomainException()
    {
        // Act
        var act = () => JournalEntryLine.CreateDebit(
            accountId: 1,
            accountCode: "",
            accountNameAr: "نقدي",
            amount: 100m);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.Message.Should().Contain("رمز الحساب مطلوب");
    }

    [Fact]
    public void CreateDebitLine_EmptyAccountNameAr_ThrowsDomainException()
    {
        // Act
        var act = () => JournalEntryLine.CreateDebit(
            accountId: 1,
            accountCode: "101",
            accountNameAr: "",
            amount: 100m);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.Message.Should().Contain("اسم الحساب بالعربية مطلوب");
    }

    [Fact]
    public void CreateDebitLine_SetsAccountIdAndCode()
    {
        // Act
        var line = JournalEntryLine.CreateDebit(
            accountId: 42,
            accountCode: "ACC042",
            accountNameAr: "حساب تجريبي",
            amount: 200m);

        // Assert
        line.AccountId.Should().Be(42);
        line.AccountCode.Should().Be("ACC042");
    }

    [Fact]
    public void CreateDebitLine_SetsAccountNameAr()
    {
        // Act
        var line = JournalEntryLine.CreateDebit(
            accountId: 1,
            accountCode: "101",
            accountNameAr: "نقدي",
            amount: 100m);

        // Assert
        line.AccountNameAr.Should().Be("نقدي");
    }

    [Fact]
    public void CreateDebitLine_WithDescription_SetsDescription()
    {
        // Act
        var line = JournalEntryLine.CreateDebit(
            accountId: 1,
            accountCode: "101",
            accountNameAr: "نقدي",
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
            accountCode: "101",
            accountNameAr: "نقدي",
            amount: 100m);

        // Assert
        line.Description.Should().BeNull();
    }

    [Fact]
    public void CreateDebitLine_TrimsAccountCode()
    {
        // Act
        var line = JournalEntryLine.CreateDebit(
            accountId: 1,
            accountCode: "  101  ",
            accountNameAr: "نقدي",
            amount: 100m);

        // Assert
        line.AccountCode.Should().Be("101");
    }

    [Fact]
    public void CreateDebitLine_TrimsAccountNameAr()
    {
        // Act
        var line = JournalEntryLine.CreateDebit(
            accountId: 1,
            accountCode: "101",
            accountNameAr: "  نقدي  ",
            amount: 100m);

        // Assert
        line.AccountNameAr.Should().Be("نقدي");
    }

    // ─── CreateCreditLine ─────────────────────────────

    [Fact]
    public void CreateCreditLine_Valid_SetsCreditValue()
    {
        // Act
        var line = JournalEntryLine.CreateCredit(
            accountId: 2,
            accountCode: "201",
            accountNameAr: "دائن",
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
            accountCode: "201",
            accountNameAr: "دائن",
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
            accountCode: "201",
            accountNameAr: "دائن",
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
            accountCode: "201",
            accountNameAr: "دائن",
            amount: 0m);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.Message.Should().Contain("الإيداع");
    }

    [Fact]
    public void CreateCreditLine_EmptyAccountCode_ThrowsDomainException()
    {
        // Act
        var act = () => JournalEntryLine.CreateCredit(
            accountId: 2,
            accountCode: "",
            accountNameAr: "دائن",
            amount: 100m);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.Message.Should().Contain("رمز الحساب مطلوب");
    }

    [Fact]
    public void CreateCreditLine_EmptyAccountNameAr_ThrowsDomainException()
    {
        // Act
        var act = () => JournalEntryLine.CreateCredit(
            accountId: 2,
            accountCode: "201",
            accountNameAr: "",
            amount: 100m);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.Message.Should().Contain("اسم الحساب بالعربية مطلوب");
    }

    [Fact]
    public void CreateCreditLine_SetsAccountIdAndCode()
    {
        // Act
        var line = JournalEntryLine.CreateCredit(
            accountId: 55,
            accountCode: "ACC055",
            accountNameAr: "حساب دائن",
            amount: 300m);

        // Assert
        line.AccountId.Should().Be(55);
        line.AccountCode.Should().Be("ACC055");
    }

    [Fact]
    public void CreateCreditLine_SetsAccountNameAr()
    {
        // Act
        var line = JournalEntryLine.CreateCredit(
            accountId: 2,
            accountCode: "201",
            accountNameAr: "دائن رئيسي",
            amount: 100m);

        // Assert
        line.AccountNameAr.Should().Be("دائن رئيسي");
    }

    [Fact]
    public void CreateCreditLine_WithDescription_SetsDescription()
    {
        // Act
        var line = JournalEntryLine.CreateCredit(
            accountId: 2,
            accountCode: "201",
            accountNameAr: "دائن",
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
            accountCode: "201",
            accountNameAr: "دائن",
            amount: 100m);

        // Assert
        line.Description.Should().BeNull();
    }

    [Fact]
    public void CreateCreditLine_TrimsAccountCode()
    {
        // Act
        var line = JournalEntryLine.CreateCredit(
            accountId: 2,
            accountCode: "  201  ",
            accountNameAr: "دائن",
            amount: 100m);

        // Assert
        line.AccountCode.Should().Be("201");
    }

    [Fact]
    public void CreateCreditLine_TrimsAccountNameAr()
    {
        // Act
        var line = JournalEntryLine.CreateCredit(
            accountId: 2,
            accountCode: "201",
            accountNameAr: "  دائن  ",
            amount: 100m);

        // Assert
        line.AccountNameAr.Should().Be("دائن");
    }

    // ─── BaseEntity Inheritance ───────────────────────

    [Fact]
    public void CreatedLine_HasDefaultId_Zero()
    {
        // Act
        var line = JournalEntryLine.CreateDebit(
            accountId: 1,
            accountCode: "101",
            accountNameAr: "نقدي",
            amount: 100m);

        // Assert
        line.Id.Should().Be(0); // Not yet persisted
    }

    [Fact]
    public void CreatedLine_IsActive_ByDefault()
    {
        // Act
        var line = JournalEntryLine.CreateDebit(
            accountId: 1,
            accountCode: "101",
            accountNameAr: "نقدي",
            amount: 100m);

        // Assert
        line.IsActive.Should().BeTrue();
    }
}
