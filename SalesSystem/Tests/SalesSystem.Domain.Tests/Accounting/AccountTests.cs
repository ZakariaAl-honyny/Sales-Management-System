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
            accountType: AccountType.Asset,
            level: 4,
            allowTransactions: true);

        // Assert
        account.AccountCode.Should().Be("CASH01");
        account.NameAr.Should().Be("نقدي");
        account.NameEn.Should().Be("Cash");
        account.AccountType.Should().Be(AccountType.Asset);
        account.Level.Should().Be(4);
        account.ParentAccountId.Should().BeNull();
        account.IsSystemAccount.Should().BeFalse();
        account.AllowTransactions.Should().BeTrue();
        account.Description.Should().BeNull();
        account.ColorCode.Should().BeNull();
        account.OpeningBalance.Should().BeNull();
        account.Notes.Should().BeNull();
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
            accountType: AccountType.Asset,
            level: 1,
            isSystemAccount: true);

        // Assert
        account.Level.Should().Be(1);
        account.AllowTransactions.Should().BeFalse();
        account.IsSystemAccount.Should().BeTrue();
    }

    [Fact]
    public void Create_EmptyCode_ThrowsDomainException()
    {
        // Act
        var act = () => Account.Create(
            accountCode: "",
            nameAr: "اسم",
            nameEn: "name",
            accountType: AccountType.Asset,
            level: 4,
            allowTransactions: true);

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
            accountType: AccountType.Asset,
            level: 4,
            allowTransactions: true);

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
            accountType: AccountType.Asset,
            level: 4,
            allowTransactions: true);

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
            accountType: (AccountType)99,
            level: 4,
            allowTransactions: true);

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
            level: 2,
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
            accountType: AccountType.Asset,
            level: 4,
            allowTransactions: true);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.Message.Should().Be("رمز الحساب لا يمكن أن يتجاوز 20 حرف");
    }

    [Fact]
    public void Create_LevelOutOfRange_ThrowsDomainException()
    {
        // Act
        var act = () => Account.Create(
            accountCode: "LEV01",
            nameAr: "حساب",
            nameEn: "Account",
            accountType: AccountType.Asset,
            level: 0);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.Message.Should().Be("مستوى الحساب يجب أن يكون بين 1 و 10");
    }

    [Fact]
    public void Create_LevelTooHigh_ThrowsDomainException()
    {
        // Act
        var act = () => Account.Create(
            accountCode: "LEV02",
            nameAr: "حساب",
            nameEn: "Account",
            accountType: AccountType.Asset,
            level: 11);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.Message.Should().Be("مستوى الحساب يجب أن يكون بين 1 و 10");
    }

    [Fact]
    public void Create_Level4WithoutAllowTransactions_ThrowsDomainException()
    {
        // Act
        var act = () => Account.Create(
            accountCode: "DTL01",
            nameAr: "تفصيلي",
            nameEn: "Detail",
            accountType: AccountType.Asset,
            level: 4,
            allowTransactions: false);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.Message.Should().Be("الحساب التفصيلي يجب أن يسمح بالحركات");
    }

    [Fact]
    public void Create_Level3WithAllowTransactions_Succeeds()
    {
        // Act
        var account = Account.Create(
            accountCode: "SUB01",
            nameAr: "حساب وسيط",
            nameEn: "Sub Account",
            accountType: AccountType.Asset,
            level: 3,
            allowTransactions: true);

        // Assert
        account.Level.Should().Be(3);
        account.AllowTransactions.Should().BeTrue();
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
            level: 2,
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
            level: 1,
            isSystemAccount: true);

        // Assert
        account.IsSystemAccount.Should().BeTrue();
    }

    [Fact]
    public void Create_WithDescription_SetsDescription()
    {
        // Act
        var account = Account.Create(
            accountCode: "DESC01",
            nameAr: "وصف",
            nameEn: "Description",
            accountType: AccountType.Liability,
            level: 4,
            allowTransactions: true,
            description: "هذا شرح للحساب");

        // Assert
        account.Description.Should().Be("هذا شرح للحساب");
    }

    [Fact]
    public void Create_WithColorCode_SetsColorCode()
    {
        // Act
        var account = Account.Create(
            accountCode: "CLR01",
            nameAr: "ملون",
            nameEn: "Colored",
            accountType: AccountType.Asset,
            level: 4,
            allowTransactions: true,
            colorCode: "#2196F3");

        // Assert
        account.ColorCode.Should().Be("#2196F3");
    }

    [Fact]
    public void Create_WithOpeningBalance_SetsOpeningBalance()
    {
        // Act
        var account = Account.Create(
            accountCode: "BAL01",
            nameAr: "رصيد",
            nameEn: "Balance",
            accountType: AccountType.Asset,
            level: 4,
            allowTransactions: true,
            openingBalance: 15000.50m);

        // Assert
        account.OpeningBalance.Should().Be(15000.50m);
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
            level: 4,
            allowTransactions: true,
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
            level: 4,
            allowTransactions: true,
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
            accountType: AccountType.Revenue,
            level: 4,
            allowTransactions: true);

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
            accountType: AccountType.Asset,
            level: 4,
            allowTransactions: true);

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
            accountType: AccountType.Liability,
            level: 4,
            allowTransactions: true);

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
            accountType: AccountType.Asset,
            level: 1,
            allowTransactions: true);

        // Assert
        account.Level.Should().Be(1);
        account.AllowTransactions.Should().BeTrue();
    }

    [Fact]
    public void Create_AllFields_Succeeds()
    {
        // Act
        var account = Account.Create(
            accountCode: "ALL01",
            nameAr: "جميع الحقول",
            nameEn: "All Fields",
            accountType: AccountType.Asset,
            level: 4,
            parentAccountId: 1,
            isSystemAccount: false,
            description: "شرح كامل",
            colorCode: "#FF9800",
            allowTransactions: true,
            openingBalance: 5000m,
            notes: "ملاحظات",
            createdByUserId: 10);

        // Assert
        account.AccountCode.Should().Be("ALL01");
        account.NameAr.Should().Be("جميع الحقول");
        account.NameEn.Should().Be("All Fields");
        account.AccountType.Should().Be(AccountType.Asset);
        account.Level.Should().Be(4);
        account.ParentAccountId.Should().Be(1);
        account.IsSystemAccount.Should().BeFalse();
        account.Description.Should().Be("شرح كامل");
        account.ColorCode.Should().Be("#FF9800");
        account.AllowTransactions.Should().BeTrue();
        account.OpeningBalance.Should().Be(5000m);
        account.Notes.Should().Be("ملاحظات");
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
            accountType: AccountType.Asset,
            level: 2);

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
            accountType: AccountType.Equity,
            level: 1,
            isSystemAccount: true);

        // Act
        var act = () => account.Update(
            nameAr: "معدل",
            nameEn: "Modified",
            accountType: AccountType.Equity,
            level: 1);

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
            accountType: AccountType.Asset,
            level: 4,
            allowTransactions: true);

        // Act
        account.Update(
            nameAr: "جديد",
            nameEn: "New",
            accountType: AccountType.Asset,
            level: 4,
            description: "وصف محدث",
            colorCode: "#F44336",
            allowTransactions: true,
            notes: "ملاحظة محدثة");

        // Assert
        account.NameAr.Should().Be("جديد");
        account.NameEn.Should().Be("New");
        account.AccountType.Should().Be(AccountType.Asset);
        account.Level.Should().Be(4);
        account.Description.Should().Be("وصف محدث");
        account.ColorCode.Should().Be("#F44336");
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
            accountType: AccountType.Asset,
            level: 4,
            allowTransactions: true);

        // Act
        var act = () => account.Update(
            nameAr: "",
            nameEn: "New Name",
            accountType: AccountType.Asset,
            level: 4);

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
            accountType: AccountType.Asset,
            level: 4,
            allowTransactions: true);

        // Act
        account.Update(
            nameAr: "محدث",
            nameEn: "Updated",
            accountType: AccountType.Asset,
            level: 4,
            parentAccountId: 15,
            allowTransactions: true);

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
            accountType: AccountType.Asset,
            level: 4,
            allowTransactions: true);

        // Act
        account.Update(
            nameAr: "محدث",
            nameEn: "Updated",
            accountType: AccountType.Asset,
            level: 4,
            allowTransactions: true,
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
            accountType: AccountType.Asset,
            level: 4,
            allowTransactions: true);

        // Act
        account.Update(
            nameAr: "محدث",
            nameEn: "Updated",
            accountType: AccountType.Asset,
            level: 4,
            allowTransactions: true,
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
            level: 2,
            parentAccountId: 10);

        // Act
        account.Update(
            nameAr: "محدث",
            nameEn: "Updated",
            accountType: AccountType.Asset,
            level: 2,
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
            accountType: AccountType.Asset,
            level: 4,
            allowTransactions: true);
        var beforeUpdate = account.UpdatedAt;

        // Act
        account.Update(
            nameAr: "محدث",
            nameEn: "Updated",
            accountType: AccountType.Asset,
            level: 4,
            allowTransactions: true);

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
            accountType: AccountType.Asset,
            level: 4,
            allowTransactions: true);

        // Act
        var act = () => account.Update(
            nameAr: "محدث",
            nameEn: "Updated",
            accountType: (AccountType)99,
            level: 4);

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
            accountType: AccountType.Asset,
            level: 4,
            allowTransactions: true);

        // Act
        var act = () => account.Update(
            nameAr: "محدث",
            nameEn: "Updated",
            accountType: AccountType.Asset,
            level: 0);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.Message.Should().Be("مستوى الحساب يجب أن يكون بين 1 و 10");
    }

    [Fact]
    public void Update_Level4WithoutAllowTransactions_ThrowsDomainException()
    {
        // Arrange
        var account = Account.Create(
            accountCode: "UPD11",
            nameAr: "حساب",
            nameEn: "Account",
            accountType: AccountType.Asset,
            level: 4,
            allowTransactions: true);

        // Act
        var act = () => account.Update(
            nameAr: "محدث",
            nameEn: "Updated",
            accountType: AccountType.Asset,
            level: 4,
            allowTransactions: false);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.Message.Should().Be("الحساب التفصيلي يجب أن يسمح بالحركات");
    }

    [Fact]
    public void Update_ChangesAccountType_Succeeds()
    {
        // Arrange
        var account = Account.Create(
            accountCode: "UPD12",
            nameAr: "حساب",
            nameEn: "Account",
            accountType: AccountType.Asset,
            level: 4,
            allowTransactions: true);

        // Act
        account.Update(
            nameAr: "محدث",
            nameEn: "Updated",
            accountType: AccountType.Liability,
            level: 4,
            allowTransactions: true);

        // Assert
        account.AccountType.Should().Be(AccountType.Liability);
    }

    [Fact]
    public void Update_ChangesLevel_Succeeds()
    {
        // Arrange
        var account = Account.Create(
            accountCode: "UPD13",
            nameAr: "حساب",
            nameEn: "Account",
            accountType: AccountType.Asset,
            level: 2);

        // Act
        account.Update(
            nameAr: "محدث",
            nameEn: "Updated",
            accountType: AccountType.Asset,
            level: 3);

        // Assert
        account.Level.Should().Be(3);
    }

    [Fact]
    public void Update_ChangesDescription_Succeeds()
    {
        // Arrange
        var account = Account.Create(
            accountCode: "UPD14",
            nameAr: "حساب",
            nameEn: "Account",
            accountType: AccountType.Asset,
            level: 4,
            allowTransactions: true);

        // Act
        account.Update(
            nameAr: "محدث",
            nameEn: "Updated",
            accountType: AccountType.Asset,
            level: 4,
            description: "شرح جديد",
            allowTransactions: true);

        // Assert
        account.Description.Should().Be("شرح جديد");
    }

    [Fact]
    public void Update_ChangesColorCode_Succeeds()
    {
        // Arrange
        var account = Account.Create(
            accountCode: "UPD15",
            nameAr: "حساب",
            nameEn: "Account",
            accountType: AccountType.Asset,
            level: 4,
            allowTransactions: true);

        // Act
        account.Update(
            nameAr: "محدث",
            nameEn: "Updated",
            accountType: AccountType.Asset,
            level: 4,
            colorCode: "#4CAF50",
            allowTransactions: true);

        // Assert
        account.ColorCode.Should().Be("#4CAF50");
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
            level: 1,
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
            accountType: AccountType.Expense,
            level: 4,
            allowTransactions: true);

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
            accountType: AccountType.Expense,
            level: 4,
            allowTransactions: true);

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
            level: 1,
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
            accountType: AccountType.Liability,
            level: 4,
            allowTransactions: true);

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
            accountType: AccountType.Asset,
            level: 4,
            allowTransactions: true);
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
            accountType: AccountType.Revenue,
            level: 4,
            allowTransactions: true);

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
            accountType: AccountType.Asset,
            level: 4,
            allowTransactions: true);

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
            accountType: AccountType.Expense,
            level: 4,
            allowTransactions: true);

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
            accountType: AccountType.Liability,
            level: 4,
            allowTransactions: true);

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
            accountType: AccountType.Revenue,
            level: 4,
            allowTransactions: true);

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
            accountType: AccountType.Equity,
            level: 4,
            allowTransactions: true);

        // Act
        var result = account.IsDebitNormal();

        // Assert
        result.Should().BeFalse();
    }
}
