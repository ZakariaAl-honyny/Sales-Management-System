using FluentAssertions;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;
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
            code: "WH-001",
            location: "Building A",
            createdByUserId: 1
        );

        warehouse.Name.Should().Be("Main Warehouse");
        warehouse.Location.Should().Be("Building A");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_GivenInvalidName_ShouldThrowDomainException(string? invalidName)
    {
        var action = () => Warehouse.Create(branchId: (short)1, name: invalidName!, code: "WH-001");

        action.Should().Throw<DomainException>()
            .WithMessage("*اسم المستودع مطلوب*");
    }

    [Fact]
    public void Create_GivenOptionalParametersAreNull_ShouldSucceed()
    {
        var warehouse = Warehouse.Create(branchId: (short)1, name: "Test Warehouse", code: "WH-001");

        warehouse.Location.Should().BeNull();
    }

    [Fact]
    public void Update_GivenValidData_ShouldUpdateWarehouse()
    {
        var warehouse = Warehouse.Create(
            branchId: (short)1,
            name: "Original Name",
            code: "WH-001",
            location: "Old Location",
            createdByUserId: 1
        );

        warehouse.Update(
            branchId: (short)1,
            name: "Updated Name",
            code: "WH-001",
            type: WarehouseType.Main,
            location: "New Location",
            updatedByUserId: 1
        );

        warehouse.Name.Should().Be("Updated Name");
        warehouse.Location.Should().Be("New Location");
    }

    [Fact]
    public void Update_GivenAllNullOptional_ShouldClearOptionalFields()
    {
        var warehouse = Warehouse.Create(
            branchId: (short)1,
            name: "Test",
            code: "WH-001",
            location: "Loc",
            createdByUserId: 1
        );

        warehouse.Update(branchId: (short)1, name: "Test", code: "WH-001", type: WarehouseType.Main, location: null, updatedByUserId: 1);

        warehouse.Location.Should().BeNull();
    }
}