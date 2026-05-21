using FluentAssertions;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Tests.Entities;

public class CategoryTests
{
    [Fact]
    public void Create_GivenValidName_ShouldCreateCategory()
    {
        var category = Category.Create(
            name: "Electronics",
            description: "Electronic products",
            createdByUserId: 1
        );

        category.Name.Should().Be("Electronics");
        category.Description.Should().Be("Electronic products");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_GivenInvalidName_ShouldThrowDomainException(string? invalidName)
    {
        var action = () => Category.Create(name: invalidName!);

        action.Should().Throw<DomainException>()
            .WithMessage("*اسم التصنيف مطلوب*");
    }

    [Fact]
    public void Create_GivenNoDescription_ShouldHaveNullDescription()
    {
        var category = Category.Create(name: "Test Category");

        category.Description.Should().BeNull();
    }

    [Fact]
    public void Update_GivenValidData_ShouldUpdateCategory()
    {
        var category = Category.Create(
            name: "Original Name",
            description: "Original Description",
            createdByUserId: 1
        );

        category.Update(
            name: "Updated Name",
            description: "Updated Description",
            updatedByUserId: 1
        );

        category.Name.Should().Be("Updated Name");
        category.Description.Should().Be("Updated Description");
    }

    [Fact]
    public void Update_GivenNullDescription_ShouldClearDescription()
    {
        var category = Category.Create(
            name: "Test",
            description: "Has description",
            createdByUserId: 1
        );

        category.Update(name: "Test", description: null, updatedByUserId: 1);

        category.Description.Should().BeNull();
    }

    [Fact]
    public void Create_GivenEmptyDescription_ShouldAllowEmptyString()
    {
        var category = Category.Create(
            name: "Test Category",
            description: "",
            createdByUserId: 1
        );

        category.Description.Should().BeEmpty();
    }
}