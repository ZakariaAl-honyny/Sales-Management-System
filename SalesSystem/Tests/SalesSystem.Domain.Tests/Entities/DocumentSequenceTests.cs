using FluentAssertions;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Tests.Entities;

public class DocumentSequenceTests
{
    [Fact]
    public void Create_GivenValidData_ShouldCreateDocumentSequence()
    {
        var sequence = DocumentSequence.Create(
            documentType: "SalesInvoice",
            prefix: "INV",
            year: 2026
        );

        sequence.DocumentType.Should().Be("SalesInvoice");
        sequence.Prefix.Should().Be("INV");
        sequence.Year.Should().Be(2026);
        sequence.LastNumber.Should().Be(0);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_GivenInvalidDocumentType_ShouldThrowDomainException(string? invalidType)
    {
        var action = () => DocumentSequence.Create(
            documentType: invalidType!,
            prefix: "INV",
            year: 2026
        );

        action.Should().Throw<DomainException>()
            .WithMessage("*نوع المستند مطلوب*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_GivenInvalidPrefix_ShouldThrowDomainException(string? invalidPrefix)
    {
        var action = () => DocumentSequence.Create(
            documentType: "Invoice",
            prefix: invalidPrefix!,
            year: 2026
        );

        action.Should().Throw<DomainException>()
            .WithMessage("*البادئة مطلوبة*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-2026)]
    public void Create_GivenInvalidYear_ShouldThrowDomainException(int invalidYear)
    {
        var action = () => DocumentSequence.Create(
            documentType: "Invoice",
            prefix: "INV",
            year: invalidYear
        );

        action.Should().Throw<DomainException>()
            .WithMessage("*السنة مطلوبة*");
    }

    [Fact]
    public void GetNextNumber_GivenInitialSequence_ShouldReturnFirstNumber()
    {
        var sequence = DocumentSequence.Create(
            documentType: "SalesInvoice",
            prefix: "INV",
            year: 2026
        );

        var number = sequence.GetNextNumber();

        number.Should().Be("INV-2026-000001");
        sequence.LastNumber.Should().Be(1);
    }

    [Fact]
    public void GetNextNumber_CalledMultipleTimes_ShouldIncrementCorrectly()
    {
        var sequence = DocumentSequence.Create(
            documentType: "SalesInvoice",
            prefix: "INV",
            year: 2026
        );

        sequence.GetNextNumber().Should().Be("INV-2026-000001");
        sequence.GetNextNumber().Should().Be("INV-2026-000002");
        sequence.GetNextNumber().Should().Be("INV-2026-000003");
        sequence.LastNumber.Should().Be(3);
    }

    [Fact]
    public void GetNextNumber_ShouldFormatWithSixDigits()
    {
        var sequence = DocumentSequence.Create(
            documentType: "Invoice",
            prefix: "INV",
            year: 2026
        );

        // Add items to increment number
        for (int i = 0; i < 99; i++)
        {
            sequence.Increment();
        }

        var number = sequence.GetNextNumber();

        number.Should().Be("INV-2026-000100");
    }

    [Fact]
    public void Increment_ShouldIncreaseLastNumber()
    {
        var sequence = DocumentSequence.Create(
            documentType: "Invoice",
            prefix: "INV",
            year: 2026
        );

        sequence.Increment();

        sequence.LastNumber.Should().Be(1);
    }

    [Fact]
    public void Increment_MultipleTimes_ShouldIncrementCorrectly()
    {
        var sequence = DocumentSequence.Create(
            documentType: "Invoice",
            prefix: "INV",
            year: 2026
        );

        sequence.Increment();
        sequence.Increment();
        sequence.Increment();

        sequence.LastNumber.Should().Be(3);
    }

    [Fact]
    public void GetNextNumber_AfterManyIncrements_ShouldFormatCorrectly()
    {
        var sequence = DocumentSequence.Create(
            documentType: "Invoice",
            prefix: "INV",
            year: 2026
        );

        // Simulate many increments
        for (int i = 0; i < 999; i++)
        {
            sequence.Increment();
        }

        var number = sequence.GetNextNumber();

        number.Should().Be("INV-2026-001000");
        sequence.LastNumber.Should().Be(1000);
    }

    [Fact]
    public void GetNextInt_InitialSequence_ShouldReturnFirstNumber()
    {
        var sequence = DocumentSequence.Create(
            documentType: "SalesInvoice",
            prefix: "INV",
            year: 2026
        );

        var number = sequence.GetNextInt();

        number.Should().Be(1);
        sequence.LastNumber.Should().Be(1);
    }

    [Fact]
    public void GetNextInt_CalledMultipleTimes_ShouldIncrementCorrectly()
    {
        var sequence = DocumentSequence.Create(
            documentType: "SalesInvoice",
            prefix: "INV",
            year: 2026
        );

        sequence.GetNextInt().Should().Be(1);
        sequence.GetNextInt().Should().Be(2);
        sequence.GetNextInt().Should().Be(3);
        sequence.LastNumber.Should().Be(3);
    }

    [Fact]
    public void GetNextInt_AfterIncrement_ShouldReturnCorrectValue()
    {
        var sequence = DocumentSequence.Create(
            documentType: "SalesInvoice",
            prefix: "INV",
            year: 2026
        );

        sequence.Increment();
        sequence.Increment();

        var number = sequence.GetNextInt();

        number.Should().Be(3);
        sequence.LastNumber.Should().Be(3);
    }

    [Fact]
    public void GetNextInt_InterleavedWithGetNextNumber_ShouldIncrementSequentially()
    {
        var sequence = DocumentSequence.Create(
            documentType: "SalesInvoice",
            prefix: "INV",
            year: 2026
        );

        var strNum = sequence.GetNextNumber();
        strNum.Should().Be("INV-2026-000001");
        sequence.LastNumber.Should().Be(1);

        var intNum = sequence.GetNextInt();
        intNum.Should().Be(2);
        sequence.LastNumber.Should().Be(2);

        strNum = sequence.GetNextNumber();
        strNum.Should().Be("INV-2026-000003");
        sequence.LastNumber.Should().Be(3);
    }
}