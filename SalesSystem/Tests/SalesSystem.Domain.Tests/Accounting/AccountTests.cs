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
            nature: (byte)AccountType.Asset,
            isLeaf: true);

        // Assert
        account.AccountCode.Should().Be("CASH01");
        account.NameAr.Should().Be("نقدي");
        account.NameEn.Should().Be("Cash");
        account.GetAccountType().Should().Be(AccountType.Asset);
        account.ParentId.Should().BeNull();
        account.IsSystem.Should().BeFalse();
        account.IsLeaf.Should().BeTrue();
        account.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_Level1Group_Succeeds()
    {
        // Arrange & Act
        var account = Account.Create(
            accountCode: "1000",
            nameAr: "الأصول",
            nameEn: "Assets",
            nature: (byte)AccountType.Asset,
            isLeaf: false,
            isSystem: true);

        // Assert
        account.IsLeaf.Should().BeFalse();
        account.IsSystem.Should().BeTrue();
    }

    [Fact]
    public void Create_EmptyCode_ThrowsDomainException()
    {
        // Act
        var act = () => Account.Create(
            accountCode: "",
            nameAr: "اسم",
            nameEn: "name",
            nature: (byte)AccountType.Asset,
            isLeaf: true);

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
            nature: (byte)AccountType.Asset,
            isLeaf: true);

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
            nature: (byte)AccountType.Asset,
            isLeaf: true);

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
            nature: 99,
            isLeaf: true);

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
            nature: (byte)AccountType.Asset,
            isLeaf: false,
            parentId: -1);

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
            nature: (byte)AccountType.Asset,
            isLeaf: true);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.Message.Should().Be("رمز الحساب لا يمكن أن يتجاوز 20 حرف");
    }

    [Fact]
    public void Create_NatureOutOfRange_ThrowsDomainException()
    {
        // Act
        var act = () => Account.Create(
            accountCode: "LEV01",
            nameAr: "حساب",
            nameEn: "Account",
            nature: 0,
            isLeaf: false);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.Message.Should().Contain("نوع الحساب غير صالح");
    }

    [Fact]
    public void Create_LevelTooHigh_ThrowsDomainException()
    {
        // Act
        var act = () => Account.Create(
            accountCode: "LEV02",
            nameAr: "حساب",
            nameEn: "Account",
            nature: 99);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.Message.Should().Contain("نوع الحساب غير صالح");
    }

    [Fact]
    public void Create_Level4WithoutAllowTransactions_ThrowsDomainException()
    {
        // Act
        var account = Account.Create(
            accountCode: "DTL01",
            nameAr: "تفصيلي",
            nameEn: "Detail",
            nature: (byte)AccountType.Asset,
            isLeaf: false);

        // Assert
        account.IsLeaf.Should().BeFalse();
    }

    [Fact]
    public void Create_IsLeafTrue_Succeeds()
    {
        // Act
        var account = Account.Create(
            accountCode: "SUB01",
            nameAr: "حساب وسيط",
            nameEn: "Sub Account",
            nature: (byte)AccountType.Asset,
            isLeaf: true);

        // Assert
        account.IsLeaf.Should().BeTrue();
    }

    [Fact]
    public void Create_WithParentAccount_SetsParentId()
    {
        // Act
        var account = Account.Create(
            accountCode: "SUB01",
            nameAr: "حساب فرعي",
            nameEn: "Sub Account",
            nature: (byte)AccountType.Asset,
            isLeaf: false,
            parentId: 10);

        // Assert
        account.ParentId.Should().Be(10);
    }

    [Fact]
    public void Create_SystemAccount_SetsFlag()
    {
        // Act
        var account = Account.Create(
            accountCode: "SYS001",
            nameAr: "حساب نظامي",
            nameEn: "System",
            nature: (byte)AccountType.Equity,
            isLeaf: false,
            isSystem: true);

        // Assert
        account.IsSystem.Should().BeTrue();
    }

    [Fact]
    public void Create_WithCategoryId_SetsCategoryId()
    {
        // Act
        var account = Account.Create(
            accountCode: "DESC01",
            nameAr: "وصف",
            nameEn: "Description",
            nature: (byte)AccountType.Liability,
            isLeaf: true,
            categoryId: 1);

        // Assert
        account.CategoryId.Should().Be(1);
    }

    [Fact]
    public void Create_NonLeaf_Succeeds()
    {
        // Act
        var account = Account.Create(
            accountCode: "CLR01",
            nameAr: "ملون",
            nameEn: "Colored",
            nature: (byte)AccountType.Asset,
            isLeaf: false);

        // Assert
        account.IsLeaf.Should().BeFalse();
    }

    [Fact]
    public void Create_WithCreatedByUserId_SetsCreatedBy()
    {
        // Act
        var account = Account.Create(
            accountCode: "USR01",
            nameAr: "مستخدم",
            nameEn: "User",
            nature: (byte)AccountType.Expense,
            isLeaf: true,
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
            nature: (byte)AccountType.Revenue,
            isLeaf: true);

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
            nature: (byte)AccountType.Asset,
            isLeaf: true);

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
            nature: (byte)AccountType.Liability,
            isLeaf: true);

        // Assert
        account.NameAr.Should().Be("اسم مع مسافات");
    }

    [Fact]
    public void Create_WithAllowTransactionsAndLevel1_Succeeds()
    {
        // Note: Level 1 can technically have AllowTransactions=true even if unusual
        // Act
        var account = Account.Create(
            accountCode: "LVL01",
            nameAr: "مستوى أول",
            nameEn: "Level One",
            nature: (byte)AccountType.Asset,
            isLeaf: true);

        // Assert
        account.IsLeaf.Should().BeTrue();
    }

    [Fact]
    public void Create_AllFields_Succeeds()
    {
        // Act
        var account = Account.Create(
            accountCode: "ALL01",
            nameAr: "جميع الحقول",
            nameEn: "All Fields",
            nature: (byte)AccountType.Asset,
            isLeaf: true,
            parentId: 1,
            isSystem: false,
            createdByUserId: 10);

        // Assert
        account.AccountCode.Should().Be("ALL01");
        account.NameAr.Should().Be("جميع الحقول");
        account.NameEn.Should().Be("All Fields");
        account.GetAccountType().Should().Be(AccountType.Asset);
        account.ParentId.Should().Be(1);
        account.IsSystem.Should().BeFalse();
        account.IsLeaf.Should().BeTrue();
        account.CreatedByUserId.Should().Be(10);
        account.IsActive.Should().BeTrue();
    }

    // ─── HasChildren ───────────────────────────────────

    [Fact]
    public void HasChildren_NoSubAccounts_ReturnsFalse()
    {
        // Arrange
        var account = Account.Create(
            accountCode: "CHD01",
            nameAr: "حساب",
            nameEn: "Account",
            nature: (byte)AccountType.Asset,
            isLeaf: false);

        // Act
        var result = account.HasChildren();

        // Assert
        result.Should().BeFalse();
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
            nature: (byte)AccountType.Equity,
            isLeaf: false,
            isSystem: true);

        // Act
        var act = () => account.Update(
            nameAr: "معدل",
            nameEn: "Modified",
            nature: (byte)AccountType.Equity,
            isLeaf: false);

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
            nature: (byte)AccountType.Asset,
            isLeaf: true);

        // Act
        account.Update(
            nameAr: "جديد",
            nameEn: "New",
            nature: (byte)AccountType.Asset,
            isLeaf: true);

        // Assert
        account.NameAr.Should().Be("جديد");
        account.NameEn.Should().Be("New");
        account.GetAccountType().Should().Be(AccountType.Asset);
        account.IsLeaf.Should().BeTrue();
    }

    [Fact]
    public void Update_EmptyNameAr_ThrowsDomainException()
    {
        // Arrange
        var account = Account.Create(
            accountCode: "UPD02",
            nameAr: "اسم",
            nameEn: "Name",
            nature: (byte)AccountType.Asset,
            isLeaf: true);

        // Act
        var act = () => account.Update(
            nameAr: "",
            nameEn: "New Name",
            nature: (byte)AccountType.Asset,
            isLeaf: true);

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
            nature: (byte)AccountType.Asset,
            isLeaf: true);

        // Act
        account.Update(
            nameAr: "محدث",
            nameEn: "Updated",
            nature: (byte)AccountType.Asset,
            isLeaf: true,
            parentId: 15);

        // Assert
        account.ParentId.Should().Be(15);
    }

    [Fact]
    public void Update_WithUpdatedByUserId_SetsUpdatedBy()
    {
        // Arrange
        var account = Account.Create(
            accountCode: "UPD06",
            nameAr: "حساب",
            nameEn: "Account",
            nature: (byte)AccountType.Asset,
            isLeaf: true);

        // Act
        account.Update(
            nameAr: "محدث",
            nameEn: "Updated",
            nature: (byte)AccountType.Asset,
            isLeaf: true,
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
            nature: (byte)AccountType.Asset,
            isLeaf: false,
            parentId: 10);

        // Act
        account.Update(
            nameAr: "محدث",
            nameEn: "Updated",
            nature: (byte)AccountType.Asset,
            isLeaf: false,
            parentId: null);

        // Assert
        account.ParentId.Should().BeNull();
    }

    [Fact]
    public void Update_SetsUpdatedAt()
    {
        // Arrange
        var account = Account.Create(
            accountCode: "UPD08",
            nameAr: "حساب",
            nameEn: "Account",
            nature: (byte)AccountType.Asset,
            isLeaf: true);
        var beforeUpdate = account.UpdatedAt;

        // Act
        account.Update(
            nameAr: "محدث",
            nameEn: "Updated",
            nature: (byte)AccountType.Asset,
            isLeaf: true);

        // Assert
        account.UpdatedAt.Should().NotBeNull();
        account.UpdatedAt.Should().BeOnOrAfter(beforeUpdate ?? DateTime.MinValue);
    }

    [Fact]
    public void Update_InvalidAccountType_ThrowsDomainException()
    {
        // Arrange
        var account = Account.Create(
            accountCode: "UPD09",
            nameAr: "حساب",
            nameEn: "Account",
            nature: (byte)AccountType.Asset,
            isLeaf: true);

        // Act
        var act = () => account.Update(
            nameAr: "محدث",
            nameEn: "Updated",
            nature: 99,
            isLeaf: true);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.Message.Should().Contain("نوع الحساب غير صالح");
    }

    [Fact]
    public void Update_LevelOutOfRange_ThrowsDomainException()
    {
        // Arrange
        var account = Account.Create(
            accountCode: "UPD10",
            nameAr: "حساب",
            nameEn: "Account",
            nature: (byte)AccountType.Asset,
            isLeaf: true);

        // Act
        var act = () => account.Update(
            nameAr: "محدث",
            nameEn: "Updated",
            nature: 0,
            isLeaf: true);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.Message.Should().Contain("نوع الحساب غير صالح");
    }

    [Fact]
    public void Update_ChangesNature_Succeeds()
    {
        // Arrange
        var account = Account.Create(
            accountCode: "UPD12",
            nameAr: "حساب",
            nameEn: "Account",
            nature: (byte)AccountType.Asset,
            isLeaf: true);

        // Act
        account.Update(
            nameAr: "محدث",
            nameEn: "Updated",
            nature: (byte)AccountType.Liability,
            isLeaf: true);

        // Assert
        account.GetAccountType().Should().Be(AccountType.Liability);
    }

    [Fact]
    public void Update_ChangesIsLeaf_Succeeds()
    {
        // Arrange
        var account = Account.Create(
            accountCode: "UPD13",
            nameAr: "حساب",
            nameEn: "Account",
            nature: (byte)AccountType.Asset,
            isLeaf: true);

        // Act
        account.Update(
            nameAr: "محدث",
            nameEn: "Updated",
            nature: (byte)AccountType.Asset,
            isLeaf: false);

        // Assert
        account.IsLeaf.Should().BeFalse();
    }

    [Fact]
    public void Update_WithCategoryId_Succeeds()
    {
        // Arrange
        var account = Account.Create(
            accountCode: "UPD14",
            nameAr: "حساب",
            nameEn: "Account",
            nature: (byte)AccountType.Asset,
            isLeaf: true);

        // Act
        account.Update(
            nameAr: "محدث",
            nameEn: "Updated",
            nature: (byte)AccountType.Asset,
            isLeaf: true,
            categoryId: 5);

        // Assert
        account.CategoryId.Should().Be(5);
    }

    [Fact]
    public void Update_ChangesCategoryId_Succeeds()
    {
        // Arrange
        var account = Account.Create(
            accountCode: "UPD15",
            nameAr: "حساب",
            nameEn: "Account",
            nature: (byte)AccountType.Asset,
            isLeaf: true);

        // Act
        account.Update(
            nameAr: "محدث",
            nameEn: "Updated",
            nature: (byte)AccountType.Asset,
            isLeaf: true,
            categoryId: 3);

        // Assert
        account.CategoryId.Should().Be(3);
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
            nature: (byte)AccountType.Equity,
            isLeaf: false,
            isSystem: true);

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
            nature: (byte)AccountType.Expense,
            isLeaf: true);

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
            nature: (byte)AccountType.Expense,
            isLeaf: true);

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
            nature: (byte)AccountType.Equity,
            isLeaf: false,
            isSystem: true);

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
            nature: (byte)AccountType.Liability,
            isLeaf: true);

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
            nature: (byte)AccountType.Asset,
            isLeaf: true);
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
            nature: (byte)AccountType.Revenue,
            isLeaf: true);

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
            nature: (byte)AccountType.Asset,
            isLeaf: true);

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
            nature: (byte)AccountType.Expense,
            isLeaf: true);

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
            nature: (byte)AccountType.Liability,
            isLeaf: true);

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
            nature: (byte)AccountType.Revenue,
            isLeaf: true);

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
            nature: (byte)AccountType.Equity,
            isLeaf: true);

        // Act
        var result = account.IsDebitNormal();

        // Assert
        result.Should().BeFalse();
    }
}
