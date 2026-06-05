using FluentAssertions;
using SalesSystem.Domain.Accounting.Entities;
using SalesSystem.Domain.Accounting.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Tests.Accounting;

public class AccountTests
{
    // ─── Create ───────────────────────────────────────

    [Fact]
    public void Create_ValidAccount_Succeeds()
    {
        // Arrange & Act
        var account = Account.Create(
            accountCode: "CASH01",
            nameAr: "نقدي",
            nameEn: "Cash",
            accountType: AccountType.Asset);

        // Assert
        account.AccountCode.Should().Be("CASH01");
        account.NameAr.Should().Be("نقدي");
        account.NameEn.Should().Be("Cash");
        account.AccountType.Should().Be(AccountType.Asset);
        account.ParentAccountId.Should().BeNull();
        account.IsSystemAccount.Should().BeFalse();
        account.Notes.Should().BeNull();
        account.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_EmptyCode_ThrowsDomainException()
    {
        // Act
        var act = () => Account.Create(
            accountCode: "",
            nameAr: "اسم",
            nameEn: "name",
            accountType: AccountType.Asset);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.Message.Should().Contain("رمز الحساب مطلوب");
    }

    // ─── Update ───────────────────────────────────────

    [Fact]
    public void Update_SystemAccount_ThrowsDomainException()
    {
        // Arrange
        var account = Account.Create(
            accountCode: "SYS001",
            nameAr: "نظامي",
            nameEn: "System",
            accountType: AccountType.Equity,
            isSystemAccount: true);

        // Act
        var act = () => account.Update(
            nameAr: "معدل",
            nameEn: "Modified",
            accountType: AccountType.Liability);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.Message.Should().Contain("نظامي");
    }

    // ─── Deactivate ───────────────────────────────────

    [Fact]
    public void Deactivate_SystemAccount_ThrowsDomainException()
    {
        // Arrange
        var account = Account.Create(
            accountCode: "SYS002",
            nameAr: "نظامي",
            nameEn: "System",
            accountType: AccountType.Equity,
            isSystemAccount: true);

        // Act
        var act = () => account.Deactivate();

        // Assert
        act.Should().Throw<DomainException>()
            .Which.Message.Should().Contain("نظامي");
    }

    // ─── MarkAsDeleted ────────────────────────────────

    [Fact]
    public void MarkAsDeleted_SystemAccount_ThrowsDomainException()
    {
        // Arrange
        var account = Account.Create(
            accountCode: "SYS003",
            nameAr: "نظامي",
            nameEn: "System",
            accountType: AccountType.Equity,
            isSystemAccount: true);

        // Act
        var act = () => account.MarkAsDeleted();

        // Assert
        act.Should().Throw<DomainException>()
            .Which.Message.Should().Contain("نظامي");
    }

    // ─── IsDebitNormal ────────────────────────────────

    [Fact]
    public void IsDebitNormal_Asset_ReturnsTrue()
    {
        // Arrange
        var account = Account.Create(
            accountCode: "AST01",
            nameAr: "أصل",
            nameEn: "Asset",
            accountType: AccountType.Asset);

        // Act
        var result = account.IsDebitNormal();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsDebitNormal_Expense_ReturnsTrue()
    {
        // Arrange
        var account = Account.Create(
            accountCode: "EXP01",
            nameAr: "مصروف",
            nameEn: "Expense",
            accountType: AccountType.Expense);

        // Act
        var result = account.IsDebitNormal();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsDebitNormal_Liability_ReturnsFalse()
    {
        // Arrange
        var account = Account.Create(
            accountCode: "LIB01",
            nameAr: "التزام",
            nameEn: "Liability",
            accountType: AccountType.Liability);

        // Act
        var result = account.IsDebitNormal();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsDebitNormal_Revenue_ReturnsFalse()
    {
        // Arrange
        var account = Account.Create(
            accountCode: "REV01",
            nameAr: "إيراد",
            nameEn: "Revenue",
            accountType: AccountType.Revenue);

        // Act
        var result = account.IsDebitNormal();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsDebitNormal_Equity_ReturnsFalse()
    {
        // Arrange
        var account = Account.Create(
            accountCode: "EQT01",
            nameAr: "حقوق ملكية",
            nameEn: "Equity",
            accountType: AccountType.Equity);

        // Act
        var result = account.IsDebitNormal();

        // Assert
        result.Should().BeFalse();
    }
}
