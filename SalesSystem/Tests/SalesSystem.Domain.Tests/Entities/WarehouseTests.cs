using FluentAssertions;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Tests.Entities;

public class WarehouseTests
{
    [Fact]
    public void Create_GivenValidName_ShouldCreateWarehouse()
    {
        var warehouse = Warehouse.Create(
            name: "Main Warehouse",
            location: "Building A",
            isDefault: true,
            createdByUserId: 1
        );

        warehouse.Name.Should().Be("Main Warehouse");
        warehouse.Location.Should().Be("Building A");
        warehouse.IsDefault.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_GivenInvalidName_ShouldThrowDomainException(string? invalidName)
    {
        var action = () => Warehouse.Create(name: invalidName!);

        action.Should().Throw<DomainException>()
            .WithMessage("*اسم المستودع مطلوب*");
    }

    [Fact]
    public void Create_GivenOptionalParametersAreNull_ShouldSucceed()
    {
        var warehouse = Warehouse.Create(name: "Test Warehouse");

        warehouse.Location.Should().BeNull();
        warehouse.IsDefault.Should().BeFalse();
    }

    [Fact]
    public void Update_GivenValidData_ShouldUpdateWarehouse()
    {
        var warehouse = Warehouse.Create(
            name: "Original Name",
            location: "Old Location",
            createdByUserId: 1
        );

        warehouse.Update(
            name: "Updated Name",
            location: "New Location",
            isDefault: true,
            updatedByUserId: 1
        );

        warehouse.Name.Should().Be("Updated Name");
        warehouse.Location.Should().Be("New Location");
        warehouse.IsDefault.Should().BeTrue();
    }

    [Fact]
    public void SetAsDefault_ShouldSetIsDefaultToTrue()
    {
        var warehouse = Warehouse.Create(name: "Test Warehouse", isDefault: false);

        warehouse.SetAsDefault();

        warehouse.IsDefault.Should().BeTrue();
    }

    [Fact]
    public void SetAsDefault_WhenAlreadyDefault_ShouldRemainTrue()
    {
        var warehouse = Warehouse.Create(name: "Test Warehouse", isDefault: true);

        warehouse.SetAsDefault();

        warehouse.IsDefault.Should().BeTrue();
    }

    [Fact]
    public void Update_GivenAllNullOptional_ShouldClearOptionalFields()
    {
        var warehouse = Warehouse.Create(
            name: "Test",
            location: "Loc",
            createdByUserId: 1
        );

        warehouse.Update(name: "Test", location: null, isDefault: false, updatedByUserId: 1);

        warehouse.Location.Should().BeNull();
    }
}