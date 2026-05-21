using FluentAssertions;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Tests.Entities;

public class InventoryMovementTests
{
    [Fact]
    public void Create_GivenValidData_ShouldCreateInventoryMovement()
    {
        var movement = InventoryMovement.Create(
            productId: 1,
            warehouseId: 1,
            movementType: MovementType.PurchaseIn,
            quantityChange: 100m,
            quantityBefore: 0m,
            quantityAfter: 100m,
            referenceType: "PurchaseInvoice",
            referenceId: 10,
            unitCost: 50m,
            notes: "Initial stock",
            createdByUserId: 1
        );

        movement.ProductId.Should().Be(1);
        movement.WarehouseId.Should().Be(1);
        movement.MovementType.Should().Be(MovementType.PurchaseIn);
        movement.QuantityChange.Should().Be(100m);
        movement.QuantityBefore.Should().Be(0m);
        movement.QuantityAfter.Should().Be(100m);
        movement.ReferenceType.Should().Be("PurchaseInvoice");
        movement.ReferenceId.Should().Be(10);
        movement.UnitCost.Should().Be(50m);
        movement.Notes.Should().Be("Initial stock");
    }

    [Fact]
    public void Create_GivenProductIdIsZero_ShouldThrowDomainException()
    {
        var action = () => InventoryMovement.Create(
            productId: 0,
            warehouseId: 1,
            movementType: MovementType.PurchaseIn,
            quantityChange: 100m,
            quantityBefore: 0m,
            quantityAfter: 100m,
            referenceType: "Purchase",
            referenceId: 1
        );

        action.Should().Throw<DomainException>()
            .WithMessage("المنتج مطلوب.");
    }

    [Fact]
    public void Create_GivenProductIdIsNegative_ShouldThrowDomainException()
    {
        var action = () => InventoryMovement.Create(
            productId: -1,
            warehouseId: 1,
            movementType: MovementType.PurchaseIn,
            quantityChange: 100m,
            quantityBefore: 0m,
            quantityAfter: 100m,
            referenceType: "Purchase",
            referenceId: 1
        );

        action.Should().Throw<DomainException>()
            .WithMessage("المنتج مطلوب.");
    }

    [Fact]
    public void Create_GivenWarehouseIdIsZero_ShouldThrowDomainException()
    {
        var action = () => InventoryMovement.Create(
            productId: 1,
            warehouseId: 0,
            movementType: MovementType.PurchaseIn,
            quantityChange: 100m,
            quantityBefore: 0m,
            quantityAfter: 100m,
            referenceType: "Purchase",
            referenceId: 1
        );

        action.Should().Throw<DomainException>()
            .WithMessage("المستودع مطلوب.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_GivenInvalidReferenceType_ShouldThrowDomainException(string? invalidRefType)
    {
        var action = () => InventoryMovement.Create(
            productId: 1,
            warehouseId: 1,
            movementType: MovementType.PurchaseIn,
            quantityChange: 100m,
            quantityBefore: 0m,
            quantityAfter: 100m,
            referenceType: invalidRefType!,
            referenceId: 1
        );

        action.Should().Throw<DomainException>()
            .WithMessage("نوع المرجع مطلوب.");
    }

    [Fact]
    public void Create_GivenReferenceIdIsZero_ShouldThrowDomainException()
    {
        var action = () => InventoryMovement.Create(
            productId: 1,
            warehouseId: 1,
            movementType: MovementType.PurchaseIn,
            quantityChange: 100m,
            quantityBefore: 0m,
            quantityAfter: 100m,
            referenceType: "Purchase",
            referenceId: 0
        );

        action.Should().Throw<DomainException>()
            .WithMessage("معرف المرجع مطلوب.");
    }

    [Fact]
    public void Create_GivenAllMovementTypes_ShouldSucceed()
    {
        foreach (MovementType type in Enum.GetValues<MovementType>())
        {
            var movement = InventoryMovement.Create(
                productId: 1,
                warehouseId: 1,
                movementType: type,
                quantityChange: 10m,
                quantityBefore: 0m,
                quantityAfter: 10m,
                referenceType: "Test",
                referenceId: 1
            );

            movement.MovementType.Should().Be(type);
        }
    }

    [Fact]
    public void Create_GivenNoUnitCost_ShouldBeNull()
    {
        var movement = InventoryMovement.Create(
            productId: 1,
            warehouseId: 1,
            movementType: MovementType.Adjustment,
            quantityChange: 5m,
            quantityBefore: 100m,
            quantityAfter: 105m,
            referenceType: "StockAdjustment",
            referenceId: 1
        );

        movement.UnitCost.Should().BeNull();
    }

    [Fact]
    public void Create_GivenNoNotes_ShouldBeNull()
    {
        var movement = InventoryMovement.Create(
            productId: 1,
            warehouseId: 1,
            movementType: MovementType.SaleOut,
            quantityChange: -10m,
            quantityBefore: 100m,
            quantityAfter: 90m,
            referenceType: "SalesInvoice",
            referenceId: 1
        );

        movement.Notes.Should().BeNull();
    }

    [Theory]
    [InlineData(0.001)]
    [InlineData(0.5)]
    [InlineData(999.999)]
    public void Create_GivenDecimalQuantityChange_ShouldAccept(decimal qtyChange)
    {
        var movement = InventoryMovement.Create(
            productId: 1,
            warehouseId: 1,
            movementType: MovementType.TransferIn,
            quantityChange: qtyChange,
            quantityBefore: 0m,
            quantityAfter: qtyChange,
            referenceType: "Transfer",
            referenceId: 1
        );

        movement.QuantityChange.Should().Be(qtyChange);
        movement.QuantityAfter.Should().Be(qtyChange);
    }

    [Fact]
    public void Create_GivenNegativeQuantityChange_ShouldSucceed()
    {
        var movement = InventoryMovement.Create(
            productId: 1,
            warehouseId: 1,
            movementType: MovementType.SaleOut,
            quantityChange: -50m,
            quantityBefore: 100m,
            quantityAfter: 50m,
            referenceType: "SalesInvoice",
            referenceId: 1
        );

        movement.QuantityChange.Should().Be(-50m);
        movement.QuantityAfter.Should().Be(50m);
    }
}