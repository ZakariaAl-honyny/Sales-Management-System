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
            code: "WH001",
            location: "Building A",
            isDefault: true,
            createdByUserId: 1
        );

        warehouse.Name.Should().Be("Main Warehouse");
        warehouse.Code.Should().Be("WH001");
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

        warehouse.Code.Should().BeNull();
        warehouse.Location.Should().BeNull();
        warehouse.IsDefault.Should().BeFalse();
    }

    [Fact]
    public void Update_GivenValidData_ShouldUpdateWarehouse()
    {
        var warehouse = Warehouse.Create(
            name: "Original Name",
            code: "W001",
            location: "Old Location",
            createdByUserId: 1
        );

        warehouse.Update(
            name: "Updated Name",
            code: "W002",
            location: "New Location",
            isDefault: true,
            updatedByUserId: 1
        );

        warehouse.Name.Should().Be("Updated Name");
        warehouse.Code.Should().Be("W002");
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
            code: "C001",
            location: "Loc",
            createdByUserId: 1
        );

        warehouse.Update(name: "Test", code: null, location: null, isDefault: false, updatedByUserId: 1);

        warehouse.Code.Should().BeNull();
        warehouse.Location.Should().BeNull();
    }
}