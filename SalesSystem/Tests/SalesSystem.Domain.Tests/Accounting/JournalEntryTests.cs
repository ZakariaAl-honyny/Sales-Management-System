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
            1,
            new DateTime(2026, 6, 1),
            "اختبار",
            JournalEntryType.Manual,
            createdBy: 1);
    }

    private static JournalEntry CreateBalancedEntry()
    {
        var entry = CreateEmptyEntry();
        entry.AddDebitLine(accountId: 1, amount: 100m);
        entry.AddCreditLine(accountId: 2, amount: 100m);
        return entry;
    }

    // ─── Create ───────────────────────────────────────

    [Fact]
    public void Create_ValidInput_CreatesEntry()
    {
        // Act
        var entry = JournalEntry.Create(
            "JE-2026-000001",
            1,
            new DateTime(2026, 6, 1),
            "اختبار",
            JournalEntryType.Manual,
            createdBy: 1);

        // Assert
        entry.EntryNumber.Should().Be("JE-2026-000001");
        entry.EntryDate.Should().Be(new DateTime(2026, 6, 1));
        entry.EntryType.Should().Be(JournalEntryType.Manual);
        entry.Status.Should().Be(JournalEntryStatus.Draft);
        entry.ReversedByEntryId.Should().BeNull();
        entry.Lines.Should().BeEmpty();
        entry.Description.Should().Be("اختبار");
        entry.ReferenceType.Should().BeNull();
        entry.ReferenceId.Should().BeNull();
        entry.ReferenceNumber.Should().BeNull();
    }

    [Fact]
    public void Create_EmptyEntryNumber_ThrowsDomainException()
    {
        // Act
        var act = () => JournalEntry.Create(
            "",
            1,
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
            1,
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
            1,
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
            1,
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
            1,
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
            1,
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
            1,
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
                (int)entryType,
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
            1,
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
            1,
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
            1,
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
            1,
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
            1,
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
        entry.AddDebitLine(accountId: 1, amount: 100m);

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
        entry.AddCreditLine(accountId: 2, amount: 50m);

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
        entry.AddDebitLine(accountId: 1, amount: 200m);
        entry.AddDebitLine(accountId: 3, amount: 150m);
        entry.AddCreditLine(accountId: 2, amount: 350m);

        // Assert
        entry.Lines.Should().HaveCount(3);
    }

    [Fact]
    public void AddDebitLine_NegativeAmount_ThrowsDomainException()
    {
        // Arrange
        var entry = CreateEmptyEntry();

        // Act
        var act = () => entry.AddDebitLine(accountId: 1, amount: -100m);

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
        var act = () => entry.AddDebitLine(accountId: 1, amount: 0m);

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
        var act = () => entry.AddCreditLine(accountId: 2, amount: -50m);

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
        var act = () => entry.AddCreditLine(accountId: 2, amount: 0m);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.Message.Should().Contain("الإيداع");
    }

    [Fact]
    public void AddDebitLine_ThrowsDomainException_WhenAlreadyPosted()
    {
        // Arrange
        var entry = CreateBalancedEntry();
        entry.Post(postedByUserId: 1);

        // Act
        var act = () => entry.AddDebitLine(accountId: 3, amount: 50m);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.Message.Should().Contain("ترحيله");
    }

    [Fact]
    public void AddCreditLine_ThrowsDomainException_WhenAlreadyPosted()
    {
        // Arrange
        var entry = CreateBalancedEntry();
        entry.Post(postedByUserId: 1);

        // Act
        var act = () => entry.AddCreditLine(accountId: 3, amount: 50m);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.Message.Should().Contain("ترحيله");
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
        entry.AddDebitLine(accountId: 1, amount: 100m);
        entry.AddDebitLine(accountId: 3, amount: 200m);
        entry.AddDebitLine(accountId: 5, amount: 50m);
        entry.AddCreditLine(accountId: 2, amount: 350m);

        // Assert
        entry.TotalDebit.Should().Be(350m);
    }

    [Fact]
    public void TotalCredit_MultipleLines_ReturnsSum()
    {
        // Arrange
        var entry = CreateEmptyEntry();
        entry.AddDebitLine(accountId: 1, amount: 500m);
        entry.AddCreditLine(accountId: 2, amount: 300m);
        entry.AddCreditLine(accountId: 4, amount: 150m);
        entry.AddCreditLine(accountId: 6, amount: 50m);

        // Assert
        entry.TotalCredit.Should().Be(500m);
    }

    [Fact]
    public void TotalDebit_LargeDecimal_ReturnsCorrectPrecision()
    {
        // Arrange
        var entry = CreateEmptyEntry();
        entry.AddDebitLine(accountId: 1, amount: 12345.67m);
        entry.AddCreditLine(accountId: 2, amount: 12345.67m);

        // Assert
        entry.TotalDebit.Should().Be(12345.67m);
    }

    [Fact]
    public void TotalCredit_LargeDecimal_ReturnsCorrectPrecision()
    {
        // Arrange
        var entry = CreateEmptyEntry();
        entry.AddDebitLine(accountId: 1, amount: 999999.99m);
        entry.AddCreditLine(accountId: 2, amount: 999999.99m);

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
        entry.AddDebitLine(accountId: 1, amount: 100m);
        entry.AddCreditLine(accountId: 2, amount: 50m);

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
        entry.AddDebitLine(accountId: 1, amount: 1000m);
        entry.AddDebitLine(accountId: 3, amount: 500m);
        entry.AddCreditLine(accountId: 2, amount: 1500m);

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
            1,
            new DateTime(2026, 6, 1),
            "قيد مبيعات",
            JournalEntryType.Sales,
            createdBy: 1);
        entry.AddDebitLine(accountId: 1, amount: 1150m);
        entry.AddCreditLine(accountId: 9, amount: 1000m);
        entry.AddCreditLine(accountId: 6, amount: 150m);

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
            1,
            new DateTime(2026, 6, 1),
            "قيد مشتريات",
            JournalEntryType.Purchase,
            createdBy: 1);
        entry.AddDebitLine(accountId: 3, amount: 500m);
        entry.AddDebitLine(accountId: 7, amount: 25m);
        entry.AddCreditLine(accountId: 1, amount: 525m);

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
        entry.AddDebitLine(accountId: 1, amount: 100.0005m);
        entry.AddCreditLine(accountId: 2, amount: 100m);

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
        entry.AddDebitLine(accountId: 1, amount: 100.002m);
        entry.AddCreditLine(accountId: 2, amount: 100m);

        // Act
        var balanced = entry.IsBalanced();

        // Assert
        balanced.Should().BeFalse();
    }

    // ─── Post (Lifecycle) ─────────────────────────────

    [Fact]
    public void Post_ThrowsDomainException_WhenNoLines()
    {
        // Arrange
        var entry = CreateEmptyEntry();

        // Act
        var act = () => entry.Post(postedByUserId: 1);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.Message.Should().Contain("بدون بنود");
    }

    [Fact]
    public void Post_ThrowsDomainException_WhenUnbalanced()
    {
        // Arrange
        var entry = CreateEmptyEntry();
        entry.AddDebitLine(accountId: 1, amount: 100m);
        entry.AddCreditLine(accountId: 2, amount: 50m);

        // Act
        var act = () => entry.Post(postedByUserId: 1);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.Message.Should().Contain("غير متوازن");
    }

    [Fact]
    public void Post_NegativePostedBy_ThrowsDomainException()
    {
        // Arrange
        var entry = CreateBalancedEntry();

        // Act
        var act = () => entry.Post(postedByUserId: -1);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.Message.Should().Contain("مرحل القيد المحاسبي مطلوب");
    }

    [Fact]
    public void Post_ZeroPostedBy_ThrowsDomainException()
    {
        // Arrange
        var entry = CreateBalancedEntry();

        // Act
        var act = () => entry.Post(postedByUserId: 0);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.Message.Should().Contain("مرحل القيد المحاسبي مطلوب");
    }

    [Fact]
    public void Post_SetsStatusPosted_WhenBalanced()
    {
        // Arrange
        var entry = CreateBalancedEntry();

        // Act
        entry.Post(postedByUserId: 1);

        // Assert
        entry.Status.Should().Be(JournalEntryStatus.Posted);
    }

    [Fact]
    public void Post_AlreadyPosted_ThrowsDomainException()
    {
        // Arrange
        var entry = CreateBalancedEntry();
        entry.Post(postedByUserId: 1);

        // Act
        var act = () => entry.Post(postedByUserId: 2);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.Message.Should().Contain("مسودة");
    }

    [Fact]
    public void Post_DraftStatus_CreatesAsDraft()
    {
        // Arrange
        var entry = CreateEmptyEntry();

        // Assert
        entry.Status.Should().Be(JournalEntryStatus.Draft);
    }

    // ─── Cancel (Lifecycle) ───────────────────────────

    [Fact]
    public void Cancel_ThrowsDomainException_WhenDraft()
    {
        // Arrange
        var entry = CreateEmptyEntry();

        // Act
        var act = () => entry.Cancel(cancelledByUserId: 1);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.Message.Should().Contain("مرحلة");
    }

    [Fact]
    public void Cancel_SetsStatusCancelled_WhenPosted()
    {
        // Arrange
        var entry = CreateBalancedEntry();
        entry.Post(postedByUserId: 1);

        // Act
        entry.Cancel(cancelledByUserId: 1);

        // Assert
        entry.Status.Should().Be(JournalEntryStatus.Cancelled);
    }

    [Fact]
    public void Cancel_WithReversalEntryId_SetsReversedByEntryId()
    {
        // Arrange
        var entry = CreateBalancedEntry();
        entry.Post(postedByUserId: 1);

        // Act
        entry.Cancel(cancelledByUserId: 1, reversedByEntryId: 999);

        // Assert
        entry.Status.Should().Be(JournalEntryStatus.Cancelled);
        entry.ReversedByEntryId.Should().Be(999);
    }

    // ─── Unbalanced Error Message ────────────────────

    [Fact]
    public void Post_Unbalanced_ErrorMessageContainsTotals()
    {
        // Arrange
        var entry = CreateEmptyEntry();
        entry.AddDebitLine(accountId: 1, amount: 100m);
        entry.AddCreditLine(accountId: 2, amount: 50m);

        // Act
        var act = () => entry.Post(postedByUserId: 1);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.Message.Should().Contain("100")
            .And.Contain("50");
    }
}
