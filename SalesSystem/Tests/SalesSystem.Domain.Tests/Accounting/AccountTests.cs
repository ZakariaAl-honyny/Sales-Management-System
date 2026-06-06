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

    [Fact]
    public void Create_EmptyNameAr_ThrowsDomainException()
    {
        // Act
        var act = () => Account.Create(
            accountCode: "CODE01",
            nameAr: "",
            nameEn: "Name",
            accountType: AccountType.Asset);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.Message.Should().Contain("اسم الحساب بالعربية مطلوب");
    }

    [Fact]
    public void Create_WhitespaceNameAr_ThrowsDomainException()
    {
        // Act
        var act = () => Account.Create(
            accountCode: "CODE01",
            nameAr: "   ",
            nameEn: "Name",
            accountType: AccountType.Asset);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.Message.Should().Contain("اسم الحساب بالعربية مطلوب");
    }

    [Fact]
    public void Create_InvalidAccountType_ThrowsDomainException()
    {
        // Act
        var act = () => Account.Create(
            accountCode: "CODE01",
            nameAr: "اسم",
            nameEn: "Name",
            accountType: (AccountType)99);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.Message.Should().Contain("نوع الحساب غير صالح");
    }

    [Fact]
    public void Create_NegativeParentAccountId_ThrowsDomainException()
    {
        // Act
        var act = () => Account.Create(
            accountCode: "PAR01",
            nameAr: "حساب",
            nameEn: "Account",
            accountType: AccountType.Asset,
            parentAccountId: -1);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.Message.Should().Be("رقم الحساب الأب غير صالح");
    }

    [Fact]
    public void Create_AccountCodeTooLong_ThrowsDomainException()
    {
        // Act
        var act = () => Account.Create(
            accountCode: "ABCDEFGHIJKLMNOPQRSTU",
            nameAr: "حساب",
            nameEn: "Account",
            accountType: AccountType.Asset);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.Message.Should().Be("رمز الحساب لا يمكن أن يتجاوز 20 حرف");
    }

    [Fact]
    public void Create_WithParentAccount_SetsParentId()
    {
        // Act
        var account = Account.Create(
            accountCode: "SUB01",
            nameAr: "حساب فرعي",
            nameEn: "Sub Account",
            accountType: AccountType.Asset,
            parentAccountId: 10);

        // Assert
        account.ParentAccountId.Should().Be(10);
    }

    [Fact]
    public void Create_SystemAccount_SetsFlag()
    {
        // Act
        var account = Account.Create(
            accountCode: "SYS001",
            nameAr: "حساب نظامي",
            nameEn: "System",
            accountType: AccountType.Equity,
            isSystemAccount: true);

        // Assert
        account.IsSystemAccount.Should().BeTrue();
    }

    [Fact]
    public void Create_WithNotes_SetsNotes()
    {
        // Act
        var account = Account.Create(
            accountCode: "NOTE01",
            nameAr: "ملاحظات",
            nameEn: "Notes",
            accountType: AccountType.Liability,
            notes: "هذه ملاحظة توضيحية");

        // Assert
        account.Notes.Should().Be("هذه ملاحظة توضيحية");
    }

    [Fact]
    public void Create_WithCreatedByUserId_SetsCreatedBy()
    {
        // Act
        var account = Account.Create(
            accountCode: "USR01",
            nameAr: "مستخدم",
            nameEn: "User",
            accountType: AccountType.Expense,
            createdByUserId: 5);

        // Assert
        account.CreatedByUserId.Should().Be(5);
    }

    [Fact]
    public void Create_NameEnOptional_DefaultsToEmpty()
    {
        // Act
        var account = Account.Create(
            accountCode: "OPT01",
            nameAr: "اختياري",
            nameEn: string.Empty,
            accountType: AccountType.Revenue);

        // Assert
        account.NameEn.Should().Be(string.Empty);
    }

    [Fact]
    public void Create_WhitespaceCode_TrimsAndSucceeds()
    {
        // Act
        var account = Account.Create(
            accountCode: "  CODE01  ",
            nameAr: "اسم",
            nameEn: "Name",
            accountType: AccountType.Asset);

        // Assert
        account.AccountCode.Should().Be("CODE01");
    }

    [Fact]
    public void Create_WhitespaceNameAr_TrimsAndSucceeds()
    {
        // Act
        var account = Account.Create(
            accountCode: "CODE01",
            nameAr: "  اسم مع مسافات  ",
            nameEn: "Name",
            accountType: AccountType.Liability);

        // Assert
        account.NameAr.Should().Be("اسم مع مسافات");
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
            nameEn: "Modified");

        // Assert
        act.Should().Throw<DomainException>()
            .Which.Message.Should().Contain("نظامي");
    }

    [Fact]
    public void Update_ValidData_Succeeds()
    {
        // Arrange
        var account = Account.Create(
            accountCode: "UPD01",
            nameAr: "قديم",
            nameEn: "Old",
            accountType: AccountType.Asset);

        // Act
        account.Update(
            nameAr: "جديد",
            nameEn: "New",
            notes: "ملاحظة محدثة");

        // Assert
        account.NameAr.Should().Be("جديد");
        account.NameEn.Should().Be("New");
        account.Notes.Should().Be("ملاحظة محدثة");
    }

    [Fact]
    public void Update_EmptyNameAr_ThrowsDomainException()
    {
        // Arrange
        var account = Account.Create(
            accountCode: "UPD02",
            nameAr: "اسم",
            nameEn: "Name",
            accountType: AccountType.Asset);

        // Act
        var act = () => account.Update(
            nameAr: "",
            nameEn: "New Name");

        // Assert
        act.Should().Throw<DomainException>()
            .Which.Message.Should().Contain("اسم الحساب بالعربية مطلوب");
    }

    [Fact]
    public void Update_WithParentAccountId_SetsParentId()
    {
        // Arrange
        var account = Account.Create(
            accountCode: "UPD04",
            nameAr: "حساب",
            nameEn: "Account",
            accountType: AccountType.Asset);

        // Act
        account.Update(
            nameAr: "محدث",
            nameEn: "Updated",
            parentAccountId: 15);

        // Assert
        account.ParentAccountId.Should().Be(15);
    }

    [Fact]
    public void Update_WithNotes_SetsNotes()
    {
        // Arrange
        var account = Account.Create(
            accountCode: "UPD05",
            nameAr: "حساب",
            nameEn: "Account",
            accountType: AccountType.Asset);

        // Act
        account.Update(
            nameAr: "محدث",
            nameEn: "Updated",
            notes: "ملاحظة جديدة");

        // Assert
        account.Notes.Should().Be("ملاحظة جديدة");
    }

    [Fact]
    public void Update_WithUpdatedByUserId_SetsUpdatedBy()
    {
        // Arrange
        var account = Account.Create(
            accountCode: "UPD06",
            nameAr: "حساب",
            nameEn: "Account",
            accountType: AccountType.Asset);

        // Act
        account.Update(
            nameAr: "محدث",
            nameEn: "Updated",
            updatedByUserId: 7);

        // Assert
        account.UpdatedByUserId.Should().Be(7);
    }

    [Fact]
    public void Update_ClearsParentAccountId_WhenNull()
    {
        // Arrange
        var account = Account.Create(
            accountCode: "UPD07",
            nameAr: "حساب",
            nameEn: "Account",
            accountType: AccountType.Asset,
            parentAccountId: 10);

        // Act
        account.Update(
            nameAr: "محدث",
            nameEn: "Updated",
            parentAccountId: null);

        // Assert
        account.ParentAccountId.Should().BeNull();
    }

    [Fact]
    public void Update_SetsUpdatedAt()
    {
        // Arrange
        var account = Account.Create(
            accountCode: "UPD08",
            nameAr: "حساب",
            nameEn: "Account",
            accountType: AccountType.Asset);
        var beforeUpdate = account.UpdatedAt;

        // Act
        account.Update(
            nameAr: "محدث",
            nameEn: "Updated");

        // Assert
        account.UpdatedAt.Should().NotBeNull();
        account.UpdatedAt.Should().BeOnOrAfter(beforeUpdate ?? DateTime.MinValue);
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

    [Fact]
    public void Deactivate_NonSystemAccount_SetsIsActiveFalse()
    {
        // Arrange
        var account = Account.Create(
            accountCode: "DEACT01",
            nameAr: "حساب عادي",
            nameEn: "Normal",
            accountType: AccountType.Expense);

        // Act
        account.Deactivate();

        // Assert
        account.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_SetsUpdatedBy()
    {
        // Arrange
        var account = Account.Create(
            accountCode: "DEACT02",
            nameAr: "حساب عادي",
            nameEn: "Normal",
            accountType: AccountType.Expense);

        // Act
        account.Deactivate(updatedByUserId: 3);

        // Assert
        account.UpdatedByUserId.Should().Be(3);
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

    [Fact]
    public void MarkAsDeleted_NonSystemAccount_SetsIsActiveFalse()
    {
        // Arrange
        var account = Account.Create(
            accountCode: "DEL01",
            nameAr: "حساب عادي",
            nameEn: "Normal",
            accountType: AccountType.Liability);

        // Act
        account.MarkAsDeleted();

        // Assert
        account.IsActive.Should().BeFalse();
    }

    // ─── Restore ──────────────────────────────────────

    [Fact]
    public void Restore_AfterDeactivate_SetsIsActiveTrue()
    {
        // Arrange
        var account = Account.Create(
            accountCode: "RST01",
            nameAr: "مستعاد",
            nameEn: "Restored",
            accountType: AccountType.Asset);
        account.Deactivate();
        account.IsActive.Should().BeFalse();

        // Act
        account.Restore();

        // Assert
        account.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Restore_ActiveAccount_RemainsActive()
    {
        // Arrange
        var account = Account.Create(
            accountCode: "RST02",
            nameAr: "نشط",
            nameEn: "Active",
            accountType: AccountType.Revenue);

        // Act
        account.Restore();

        // Assert
        account.IsActive.Should().BeTrue();
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
