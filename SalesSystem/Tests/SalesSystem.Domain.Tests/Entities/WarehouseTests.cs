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
            branchId: (short)1,
            name: "Main Warehouse",
            address: "Building A",
            createdByUserId: 1
        );

        warehouse.Name.Should().Be("Main Warehouse");
        warehouse.Address.Should().Be("Building A");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_GivenInvalidName_ShouldThrowDomainException(string? invalidName)
    {
        var action = () => Warehouse.Create(branchId: (short)1, name: invalidName!);

        action.Should().Throw<DomainException>()
            .WithMessage("*اسم المستودع مطلوب*");
    }

    [Fact]
    public void Create_GivenOptionalParametersAreNull_ShouldSucceed()
    {
        var warehouse = Warehouse.Create(branchId: (short)1, name: "Test Warehouse");

        warehouse.Address.Should().BeNull();
    }

    [Fact]
    public void Update_GivenValidData_ShouldUpdateWarehouse()
    {
        var warehouse = Warehouse.Create(
            branchId: (short)1,
            name: "Original Name",
            address: "Old Location",
            createdByUserId: 1
        );

        warehouse.Update(
            branchId: (short)1,
            name: "Updated Name",
            address: "New Location",
            updatedByUserId: 1
        );

        warehouse.Name.Should().Be("Updated Name");
        warehouse.Address.Should().Be("New Location");
    }

    [Fact]
    public void Update_GivenAllNullOptional_ShouldClearOptionalFields()
    {
        var warehouse = Warehouse.Create(
            branchId: (short)1,
            name: "Test",
            address: "Loc",
            createdByUserId: 1
        );

        warehouse.Update(branchId: (short)1, name: "Test", address: null, updatedByUserId: 1);

        warehouse.Address.Should().BeNull();
    }
}
