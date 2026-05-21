using FluentAssertions;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Tests.Entities;

public class UnitTests
{
    [Fact]
    public void Create_GivenValidName_ShouldCreateUnit()
    {
        var unit = Unit.Create(
            name: "Kilogram",
            symbol: "kg",
            createdByUserId: 1
        );

        unit.Name.Should().Be("Kilogram");
        unit.Symbol.Should().Be("kg");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_GivenInvalidName_ShouldThrowDomainException(string? invalidName)
    {
        var action = () => Unit.Create(name: invalidName!);

        action.Should().Throw<DomainException>()
            .WithMessage("*اسم الوحدة مطلوب*");
    }

    [Fact]
    public void Create_GivenNoSymbol_ShouldHaveNullSymbol()
    {
        var unit = Unit.Create(name: "Piece");

        unit.Symbol.Should().BeNull();
    }

    [Fact]
    public void Update_GivenValidData_ShouldUpdateUnit()
    {
        var unit = Unit.Create(
            name: "Original Name",
            symbol: "kg",
            createdByUserId: 1
        );

        unit.Update(
            name: "Updated Name",
            symbol: "g",
            updatedByUserId: 1
        );

        unit.Name.Should().Be("Updated Name");
        unit.Symbol.Should().Be("g");
    }

    [Fact]
    public void Update_GivenNullSymbol_ShouldClearSymbol()
    {
        var unit = Unit.Create(
            name: "Test",
            symbol: "kg",
            createdByUserId: 1
        );

        unit.Update(name: "Test", symbol: null, updatedByUserId: 1);

        unit.Symbol.Should().BeNull();
    }

    [Fact]
    public void Create_GivenEmptySymbol_ShouldAllowEmptyString()
    {
        var unit = Unit.Create(
            name: "Dozen",
            symbol: "",
            createdByUserId: 1
        );

        unit.Symbol.Should().BeEmpty();
    }
}