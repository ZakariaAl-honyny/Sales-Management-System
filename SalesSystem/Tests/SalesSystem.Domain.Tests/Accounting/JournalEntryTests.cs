using FluentAssertions;
using SalesSystem.Domain.Accounting.Entities;
using SalesSystem.Domain.Accounting.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Tests.Accounting;

public class JournalEntryTests
{
    private static JournalEntry CreateEmptyEntry()
    {
        return JournalEntry.Create(
            "JE-2026-000001",
            new DateTime(2026, 6, 1),
            "اختبار",
            JournalEntryType.Manual,
            createdBy: 1);
    }

    private static JournalEntry CreateBalancedEntry()
    {
        var entry = CreateEmptyEntry();
        entry.AddDebitLine(accountId: 1, "101", "نقدي", amount: 100m);
        entry.AddCreditLine(accountId: 2, "201", "دائن", amount: 100m);
        return entry;
    }

    // ─── Create ───────────────────────────────────────

    [Fact]
    public void Create_ValidInput_CreatesEntry()
    {
        // Act
        var entry = JournalEntry.Create(
            "JE-2026-000001",
            new DateTime(2026, 6, 1),
            "اختبار",
            JournalEntryType.Manual,
            createdBy: 1);

        // Assert
        entry.EntryNumber.Should().Be("JE-2026-000001");
        entry.TransactionDate.Should().Be(new DateTime(2026, 6, 1));
        entry.EntryType.Should().Be(JournalEntryType.Manual);
        entry.IsPosted.Should().BeFalse();
        entry.IsReversed.Should().BeFalse();
        entry.Lines.Should().BeEmpty();
        entry.Description.Should().Be("اختبار");
        entry.ReferenceType.Should().BeNull();
        entry.ReferenceId.Should().BeNull();
        entry.ReferenceNumber.Should().BeNull();
        entry.PostedBy.Should().BeNull();
        entry.PostedAt.Should().BeNull();
    }

    [Fact]
    public void Create_EmptyEntryNumber_ThrowsDomainException()
    {
        // Act
        var act = () => JournalEntry.Create(
            "",
            new DateTime(2026, 6, 1),
            "اختبار",
            JournalEntryType.Manual,
            createdBy: 1);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.Message.Should().Contain("رقم القيد المحاسبي مطلوب");
    }

    [Fact]
    public void Create_DefaultTransactionDate_ThrowsDomainException()
    {
        // Act
        var act = () => JournalEntry.Create(
            "JE-2026-000002",
            default,
            "اختبار",
            JournalEntryType.Manual,
            createdBy: 1);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.Message.Should().Contain("تاريخ القيد المحاسبي مطلوب");
    }

    [Fact]
    public void Create_InvalidEntryType_ThrowsDomainException()
    {
        // Act
        var act = () => JournalEntry.Create(
            "JE-2026-000003",
            new DateTime(2026, 6, 1),
            "اختبار",
            (JournalEntryType)99,
            createdBy: 1);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.Message.Should().Contain("نوع القيد المحاسبي غير صالح");
    }

    [Fact]
    public void Create_NegativeCreatedBy_ThrowsDomainException()
    {
        // Act
        var act = () => JournalEntry.Create(
            "JE-2026-000004",
            new DateTime(2026, 6, 1),
            "اختبار",
            JournalEntryType.Manual,
            createdBy: -1);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.Message.Should().Contain("منشئ القيد المحاسبي مطلوب");
    }

    [Fact]
    public void Create_ZeroCreatedBy_ThrowsDomainException()
    {
        // Act
        var act = () => JournalEntry.Create(
            "JE-2026-000005",
            new DateTime(2026, 6, 1),
            "اختبار",
            JournalEntryType.Manual,
            createdBy: 0);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.Message.Should().Contain("منشئ القيد المحاسبي مطلوب");
    }

    [Fact]
    public void Create_WithDescription_SetsDescription()
    {
        // Act
        var entry = JournalEntry.Create(
            "JE-2026-000006",
            new DateTime(2026, 6, 1),
            "قيد يومية مبيعات",
            JournalEntryType.Sales,
            createdBy: 1);

        // Assert
        entry.Description.Should().Be("قيد يومية مبيعات");
    }

    [Fact]
    public void Create_WithReference_SetsReference()
    {
        // Act
        var entry = JournalEntry.Create(
            "JE-2026-000007",
            new DateTime(2026, 6, 1),
            "اختبار",
            JournalEntryType.Sales,
            createdBy: 1,
            referenceType: "SalesInvoice",
            referenceId: 42,
            referenceNumber: "INV-2026-000042");

        // Assert
        entry.ReferenceType.Should().Be("SalesInvoice");
        entry.ReferenceId.Should().Be(42);
        entry.ReferenceNumber.Should().Be("INV-2026-000042");
    }

    [Fact]
    public void Create_WithAllEntryTypes_Succeeds()
    {
        // Act & Assert for each entry type
        foreach (JournalEntryType entryType in Enum.GetValues<JournalEntryType>())
        {
            var entry = JournalEntry.Create(
                $"JE-{(int)entryType}",
                new DateTime(2026, 6, 1),
                "اختبار",
                entryType,
                createdBy: 1);

            entry.EntryType.Should().Be(entryType);
        }
    }

    [Fact]
    public void Create_EntryNumberWithWhitespace_TrimsOnCreate()
    {
        // Act
        var entry = JournalEntry.Create(
            "  JE-2026-000008  ",
            new DateTime(2026, 6, 1),
            "اختبار",
            JournalEntryType.Manual,
            createdBy: 1);

        // Assert
        entry.EntryNumber.Should().Be("JE-2026-000008");
    }

    [Fact]
    public void Create_DescriptionWithWhitespace_TrimsOnCreate()
    {
        // Act
        var entry = JournalEntry.Create(
            "JE-2026-000009",
            new DateTime(2026, 6, 1),
            "  وصف به مسافات  ",
            JournalEntryType.Manual,
            createdBy: 1);

        // Assert
        entry.Description.Should().Be("وصف به مسافات");
    }

    [Fact]
    public void Create_SetsCreatedByUserId()
    {
        // Act
        var entry = JournalEntry.Create(
            "JE-2026-000010",
            new DateTime(2026, 6, 1),
            "اختبار",
            JournalEntryType.Manual,
            createdBy: 7);

        // Assert
        entry.CreatedByUserId.Should().Be(7);
    }

    [Fact]
    public void Create_EmptyDescription_ThrowsDomainException()
    {
        // Act
        var act = () => JournalEntry.Create(
            "JE-2026-000011",
            new DateTime(2026, 6, 1),
            "",
            JournalEntryType.Manual,
            createdBy: 1);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.Message.Should().Contain("الوصف مطلوب");
    }

    [Fact]
    public void Create_WhitespaceDescription_ThrowsDomainException()
    {
        // Act
        var act = () => JournalEntry.Create(
            "JE-2026-000012",
            new DateTime(2026, 6, 1),
            "   ",
            JournalEntryType.Manual,
            createdBy: 1);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.Message.Should().Contain("الوصف مطلوب");
    }

    // ─── Lines ─────────────────────────────────────────

    [Fact]
    public void Lines_Initially_IsEmpty()
    {
        // Arrange
        var entry = CreateEmptyEntry();

        // Assert
        entry.Lines.Should().BeEmpty();
    }

    [Fact]
    public void AddDebitLine_AddsLineToCollection()
    {
        // Arrange
        var entry = CreateEmptyEntry();

        // Act
        entry.AddDebitLine(accountId: 1, "101", "نقدي", amount: 100m);

        // Assert
        entry.Lines.Should().HaveCount(1);
        entry.Lines[0].Debit.Should().Be(100m);
        entry.Lines[0].Credit.Should().Be(0);
    }

    [Fact]
    public void AddCreditLine_AddsLineToCollection()
    {
        // Arrange
        var entry = CreateEmptyEntry();

        // Act
        entry.AddCreditLine(accountId: 2, "201", "دائن", amount: 50m);

        // Assert
        entry.Lines.Should().HaveCount(1);
        entry.Lines[0].Credit.Should().Be(50m);
        entry.Lines[0].Debit.Should().Be(0);
    }

    [Fact]
    public void AddMultipleLines_AllAddedToCollection()
    {
        // Arrange
        var entry = CreateEmptyEntry();

        // Act
        entry.AddDebitLine(accountId: 1, "101", "نقدي", amount: 200m);
        entry.AddDebitLine(accountId: 3, "301", "صندوق", amount: 150m);
        entry.AddCreditLine(accountId: 2, "201", "دائن", amount: 350m);

        // Assert
        entry.Lines.Should().HaveCount(3);
    }

    [Fact]
    public void AddDebitLine_NegativeAmount_ThrowsDomainException()
    {
        // Arrange
        var entry = CreateEmptyEntry();

        // Act
        var act = () => entry.AddDebitLine(accountId: 1, "101", "نقدي", amount: -100m);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.Message.Should().Contain("الخصم");
    }

    [Fact]
    public void AddDebitLine_ZeroAmount_ThrowsDomainException()
    {
        // Arrange
        var entry = CreateEmptyEntry();

        // Act
        var act = () => entry.AddDebitLine(accountId: 1, "101", "نقدي", amount: 0m);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.Message.Should().Contain("الخصم");
    }

    [Fact]
    public void AddCreditLine_NegativeAmount_ThrowsDomainException()
    {
        // Arrange
        var entry = CreateEmptyEntry();

        // Act
        var act = () => entry.AddCreditLine(accountId: 2, "201", "دائن", amount: -50m);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.Message.Should().Contain("الإيداع");
    }

    [Fact]
    public void AddCreditLine_ZeroAmount_ThrowsDomainException()
    {
        // Arrange
        var entry = CreateEmptyEntry();

        // Act
        var act = () => entry.AddCreditLine(accountId: 2, "201", "دائن", amount: 0m);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.Message.Should().Contain("الإيداع");
    }

    [Fact]
    public void AddDebitLine_ThrowsDomainException_WhenAlreadyPosted()
    {
        // Arrange
        var entry = CreateBalancedEntry();
        entry.ValidateAndPost(postedBy: 1);

        // Act
        var act = () => entry.AddDebitLine(accountId: 3, "301", "رأس المال", amount: 50m);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.Message.Should().Contain("تم ترحيله");
    }

    [Fact]
    public void AddCreditLine_ThrowsDomainException_WhenAlreadyPosted()
    {
        // Arrange
        var entry = CreateBalancedEntry();
        entry.ValidateAndPost(postedBy: 1);

        // Act
        var act = () => entry.AddCreditLine(accountId: 3, "301", "رأس المال", amount: 50m);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.Message.Should().Contain("تم ترحيله");
    }

    // ─── TotalDebit / TotalCredit ──────────────────────

    [Fact]
    public void TotalDebit_NoLines_ReturnsZero()
    {
        // Arrange
        var entry = CreateEmptyEntry();

        // Assert
        entry.TotalDebit.Should().Be(0m);
    }

    [Fact]
    public void TotalCredit_NoLines_ReturnsZero()
    {
        // Arrange
        var entry = CreateEmptyEntry();

        // Assert
        entry.TotalCredit.Should().Be(0m);
    }

    [Fact]
    public void TotalDebit_MultipleLines_ReturnsSum()
    {
        // Arrange
        var entry = CreateEmptyEntry();
        entry.AddDebitLine(accountId: 1, "101", "نقدي", amount: 100m);
        entry.AddDebitLine(accountId: 3, "301", "صندوق", amount: 200m);
        entry.AddDebitLine(accountId: 5, "501", "بنك", amount: 50m);
        entry.AddCreditLine(accountId: 2, "201", "دائن", amount: 350m);

        // Assert
        entry.TotalDebit.Should().Be(350m);
    }

    [Fact]
    public void TotalCredit_MultipleLines_ReturnsSum()
    {
        // Arrange
        var entry = CreateEmptyEntry();
        entry.AddDebitLine(accountId: 1, "101", "نقدي", amount: 500m);
        entry.AddCreditLine(accountId: 2, "201", "دائن", amount: 300m);
        entry.AddCreditLine(accountId: 4, "401", "مورد", amount: 150m);
        entry.AddCreditLine(accountId: 6, "601", "ضريبة", amount: 50m);

        // Assert
        entry.TotalCredit.Should().Be(500m);
    }

    [Fact]
    public void TotalDebit_LargeDecimal_ReturnsCorrectPrecision()
    {
        // Arrange
        var entry = CreateEmptyEntry();
        entry.AddDebitLine(accountId: 1, "101", "نقدي", amount: 12345.67m);
        entry.AddCreditLine(accountId: 2, "201", "دائن", amount: 12345.67m);

        // Assert
        entry.TotalDebit.Should().Be(12345.67m);
    }

    [Fact]
    public void TotalCredit_LargeDecimal_ReturnsCorrectPrecision()
    {
        // Arrange
        var entry = CreateEmptyEntry();
        entry.AddDebitLine(accountId: 1, "101", "نقدي", amount: 999999.99m);
        entry.AddCreditLine(accountId: 2, "201", "دائن", amount: 999999.99m);

        // Assert
        entry.TotalCredit.Should().Be(999999.99m);
    }

    // ─── IsBalanced ───────────────────────────────────

    [Fact]
    public void IsBalanced_ReturnsTrue_WhenDebitEqualsCredit()
    {
        // Arrange
        var entry = CreateBalancedEntry();

        // Act
        var balanced = entry.IsBalanced();

        // Assert
        balanced.Should().BeTrue();
    }

    [Fact]
    public void IsBalanced_ReturnsFalse_WhenDebitDoesNotEqualCredit()
    {
        // Arrange
        var entry = CreateEmptyEntry();
        entry.AddDebitLine(accountId: 1, "101", "نقدي", amount: 100m);
        entry.AddCreditLine(accountId: 2, "201", "دائن", amount: 50m);

        // Act
        var balanced = entry.IsBalanced();

        // Assert
        balanced.Should().BeFalse();
    }

    [Fact]
    public void IsBalanced_EmptyEntry_ReturnsTrue()
    {
        // Arrange
        var entry = CreateEmptyEntry();

        // Act
        var balanced = entry.IsBalanced();

        // Assert
        balanced.Should().BeTrue();
    }

    [Fact]
    public void IsBalanced_ThreeLinesAllBalanced_ReturnsTrue()
    {
        // Arrange
        var entry = CreateEmptyEntry();
        entry.AddDebitLine(accountId: 1, "101", "نقدي", amount: 1000m);
        entry.AddDebitLine(accountId: 3, "301", "بنك", amount: 500m);
        entry.AddCreditLine(accountId: 2, "201", "دائن", amount: 1500m);

        // Act
        var balanced = entry.IsBalanced();

        // Assert
        balanced.Should().BeTrue();
    }

    [Fact]
    public void IsBalanced_MultiLineSalesEntry_ReturnsTrue()
    {
        // Arrange: typical sales journal — Dr Cash 1150, Cr Revenue 1000, Cr VAT 150
        var entry = JournalEntry.Create(
            "JE-SALES-001",
            new DateTime(2026, 6, 1),
            "قيد مبيعات",
            JournalEntryType.Sales,
            createdBy: 1);
        entry.AddDebitLine(accountId: 1, "101", "نقدي", amount: 1150m);
        entry.AddCreditLine(accountId: 9, "401", "إيرادات المبيعات", amount: 1000m);
        entry.AddCreditLine(accountId: 6, "601", "ضريبة المخرجات", amount: 150m);

        // Act
        var balanced = entry.IsBalanced();

        // Assert
        balanced.Should().BeTrue();
        entry.TotalDebit.Should().Be(1150m);
        entry.TotalCredit.Should().Be(1150m);
    }

    [Fact]
    public void IsBalanced_MultiLinePurchaseEntry_ReturnsTrue()
    {
        // Arrange: typical purchase journal — Dr Inventory 500, Dr VAT Input 25, Cr Cash 525
        var entry = JournalEntry.Create(
            "JE-PUR-001",
            new DateTime(2026, 6, 1),
            "قيد مشتريات",
            JournalEntryType.Purchase,
            createdBy: 1);
        entry.AddDebitLine(accountId: 3, "301", "المخزون", amount: 500m);
        entry.AddDebitLine(accountId: 7, "701", "ضريبة المدخلات", amount: 25m);
        entry.AddCreditLine(accountId: 1, "101", "نقدي", amount: 525m);

        // Act
        var balanced = entry.IsBalanced();

        // Assert
        balanced.Should().BeTrue();
        entry.TotalDebit.Should().Be(525m);
        entry.TotalCredit.Should().Be(525m);
    }

    [Fact]
    public void IsBalanced_WithinTolerance_ReturnsTrue()
    {
        // Arrange: difference of 0.0005m is less than 0.001 tolerance
        var entry = CreateEmptyEntry();
        entry.AddDebitLine(accountId: 1, "101", "نقدي", amount: 100.0005m);
        entry.AddCreditLine(accountId: 2, "201", "دائن", amount: 100m);

        // Act
        var balanced = entry.IsBalanced();

        // Assert
        balanced.Should().BeTrue();
    }

    [Fact]
    public void IsBalanced_OutsideTolerance_ReturnsFalse()
    {
        // Arrange: difference of 0.002m is more than 0.001 tolerance
        var entry = CreateEmptyEntry();
        entry.AddDebitLine(accountId: 1, "101", "نقدي", amount: 100.002m);
        entry.AddCreditLine(accountId: 2, "201", "دائن", amount: 100m);

        // Act
        var balanced = entry.IsBalanced();

        // Assert
        balanced.Should().BeFalse();
    }

    // ─── ValidateAndPost ──────────────────────────────

    [Fact]
    public void ValidateAndPost_ThrowsDomainException_WhenNoLines()
    {
        // Arrange
        var entry = CreateEmptyEntry();

        // Act
        var act = () => entry.ValidateAndPost(postedBy: 1);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.Message.Should().Contain("بدون بنود");
    }

    [Fact]
    public void ValidateAndPost_ThrowsDomainException_WhenUnbalanced()
    {
        // Arrange
        var entry = CreateEmptyEntry();
        entry.AddDebitLine(accountId: 1, "101", "نقدي", amount: 100m);
        entry.AddCreditLine(accountId: 2, "201", "دائن", amount: 50m);

        // Act
        var act = () => entry.ValidateAndPost(postedBy: 1);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.Message.Should().Contain("غير متوازن");
    }

    [Fact]
    public void ValidateAndPost_NegativePostedBy_ThrowsDomainException()
    {
        // Arrange
        var entry = CreateBalancedEntry();

        // Act
        var act = () => entry.ValidateAndPost(postedBy: -1);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.Message.Should().Contain("مرحل القيد المحاسبي مطلوب");
    }

    [Fact]
    public void ValidateAndPost_ZeroPostedBy_ThrowsDomainException()
    {
        // Arrange
        var entry = CreateBalancedEntry();

        // Act
        var act = () => entry.ValidateAndPost(postedBy: 0);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.Message.Should().Contain("مرحل القيد المحاسبي مطلوب");
    }

    [Fact]
    public void ValidateAndPost_SetsPosted_WhenBalanced()
    {
        // Arrange
        var entry = CreateBalancedEntry();

        // Act
        entry.ValidateAndPost(postedBy: 1);

        // Assert
        entry.IsPosted.Should().BeTrue();
        entry.PostedBy.Should().Be(1);
        entry.PostedAt.Should().NotBeNull();
    }

    [Fact]
    public void ValidateAndPost_SetsPostedBy_ToValidUser()
    {
        // Arrange
        var entry = CreateBalancedEntry();

        // Act
        entry.ValidateAndPost(postedBy: 42);

        // Assert
        entry.PostedBy.Should().Be(42);
    }

    [Fact]
    public void ValidateAndPost_SetsPostedAt_ToRecentTimestamp()
    {
        // Arrange
        var entry = CreateBalancedEntry();
        var beforePost = DateTime.UtcNow.AddSeconds(-1);

        // Act
        entry.ValidateAndPost(postedBy: 1);

        // Assert
        entry.PostedAt.Should().NotBeNull();
        entry.PostedAt!.Value.Should().BeOnOrAfter(beforePost);
    }

    [Fact]
    public void ValidateAndPost_AfterPost_CanBeCalledAgain()
    {
        // Arrange
        var entry = CreateBalancedEntry();
        entry.ValidateAndPost(postedBy: 1);

        // Act — no guard against double-posting, should succeed again
        entry.ValidateAndPost(postedBy: 2);

        // Assert — values are overwritten
        entry.PostedBy.Should().Be(2);
        entry.IsPosted.Should().BeTrue();
    }

    // ─── IsPosted / IsReversed Defaults ───────────────

    [Fact]
    public void IsPosted_Default_IsFalse()
    {
        // Arrange
        var entry = CreateEmptyEntry();

        // Assert
        entry.IsPosted.Should().BeFalse();
    }

    [Fact]
    public void IsReversed_Default_IsFalse()
    {
        // Arrange
        var entry = CreateEmptyEntry();

        // Assert
        entry.IsReversed.Should().BeFalse();
    }

    // ─── Unbalanced Error Message ────────────────────

    [Fact]
    public void ValidateAndPost_Unbalanced_ErrorMessageContainsTotals()
    {
        // Arrange
        var entry = CreateEmptyEntry();
        entry.AddDebitLine(accountId: 1, "101", "نقدي", amount: 100m);
        entry.AddCreditLine(accountId: 2, "201", "دائن", amount: 50m);

        // Act
        var act = () => entry.ValidateAndPost(postedBy: 1);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.Message.Should().Contain("100")
            .And.Contain("50");
    }
}
