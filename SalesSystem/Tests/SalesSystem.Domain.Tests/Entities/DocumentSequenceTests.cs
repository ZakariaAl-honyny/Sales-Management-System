using FluentAssertions;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Tests.Entities;

/// <summary>
/// Tests for DocumentSequence entity (v4 schema: DocumentType + NextNumber).
/// No Prefix/Year — formatting is done by the caller (DocumentSequenceService).
/// </summary>
public class DocumentSequenceTests
{
    [Fact]
    public void Create_GivenValidDocumentType_ShouldCreateSequence()
    {
        var sequence = DocumentSequence.Create("SalesInvoice");

        sequence.DocumentType.Should().Be("SalesInvoice");
        sequence.NextNumber.Should().Be(1);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_GivenInvalidDocumentType_ShouldThrowDomainException(string? invalidType)
    {
        var action = () => DocumentSequence.Create(invalidType!);

        action.Should().Throw<DomainException>()
            .WithMessage("*نوع المستند مطلوب*");
    }

    [Fact]
    public void GetNext_InitialSequence_ShouldReturnOne()
    {
        var sequence = DocumentSequence.Create("SalesInvoice");

        var number = sequence.GetNext();

        number.Should().Be(1);
        sequence.NextNumber.Should().Be(2);
    }

    [Fact]
    public void GetNext_CalledMultipleTimes_ShouldIncrementCorrectly()
    {
        var sequence = DocumentSequence.Create("SalesInvoice");

        sequence.GetNext().Should().Be(1);
        sequence.GetNext().Should().Be(2);
        sequence.GetNext().Should().Be(3);

        sequence.NextNumber.Should().Be(4);
    }

    [Fact]
    public void GetNext_AfterSetNextNumber_ShouldUseNewValue()
    {
        var sequence = DocumentSequence.Create("SalesInvoice");

        sequence.SetNextNumber(100);

        sequence.GetNext().Should().Be(100);
        sequence.NextNumber.Should().Be(101);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void SetNextNumber_GivenInvalidValue_ShouldThrowDomainException(int invalidNext)
    {
        var sequence = DocumentSequence.Create("SalesInvoice");

        var action = () => sequence.SetNextNumber(invalidNext);

        action.Should().Throw<DomainException>()
            .WithMessage("*الرقم التسلسلي يجب أن يكون 1 أو أكثر*");
    }

    [Fact]
    public void GetNext_DifferentSequences_ShouldBeIndependent()
    {
        var salesSeq = DocumentSequence.Create("SalesInvoice");
        var purchaseSeq = DocumentSequence.Create("PurchaseInvoice");

        salesSeq.GetNext().Should().Be(1);
        purchaseSeq.GetNext().Should().Be(1);
        salesSeq.GetNext().Should().Be(2);

        salesSeq.NextNumber.Should().Be(3);
        purchaseSeq.NextNumber.Should().Be(2);
    }

    [Fact]
    public void SetNextNumber_ShouldUpdateTimestamp()
    {
        var sequence = DocumentSequence.Create("SalesInvoice");
        var beforeUpdate = sequence.UpdatedAt;

        System.Threading.Thread.Sleep(10); // Ensure time difference
        sequence.SetNextNumber(50);

        sequence.NextNumber.Should().Be(50);
        sequence.UpdatedAt.Should().NotBeNull();
        sequence.UpdatedAt.Should().BeAfter(beforeUpdate ?? DateTime.MinValue);
    }

    [Fact]
    public void GetNext_MultipleCalls_ShouldBeSequential()
    {
        var sequence = DocumentSequence.Create("PaymentVoucher");

        var results = new int[10];
        for (int i = 0; i < 10; i++)
        {
            results[i] = sequence.GetNext();
        }

        results.Should().BeInAscendingOrder();
        results[0].Should().Be(1);
        results[9].Should().Be(10);
        sequence.NextNumber.Should().Be(11);
    }
}
