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

    // ─── Guard: Add lines after posting ────────────────

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
}
