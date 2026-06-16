using FluentAssertions;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Tests.Entities;

public class ProductTests
{
    [Fact]
    public void Create_GivenValidData_ShouldCreateProduct()
    {
        var product = Product.Create(
            name: "Test Product",
            categoryId: 1,
            description: "Test description",
            createdByUserId: 1
        );

        product.Name.Should().Be("Test Product");
        product.CategoryId.Should().Be(1);
        product.Description.Should().Be("Test description");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_GivenInvalidName_ShouldThrowDomainException(string? invalidName)
    {
        var action = () => Product.Create(
            name: invalidName!,
            categoryId: 1
        );

        action.Should().Throw<DomainException>()
            .WithMessage("*اسم المنتج مطلوب*");
    }

    [Fact]
    public void Update_GivenValidData_ShouldUpdateProduct()
    {
        var product = Product.Create(
            name: "Original Name",
            categoryId: 1,
            createdByUserId: 1
        );

        product.Update(
            name: "Updated Name",
            categoryId: 2,
            description: "Updated description",
            updatedByUserId: 1
        );

        product.Name.Should().Be("Updated Name");
        product.CategoryId.Should().Be(2);
        product.Description.Should().Be("Updated description");
    }

    [Fact]
    public void AddPrice_ShouldCreateProductPrice()
    {
        // Pricing is now managed via the ProductPrices entity directly
        var price = ProductPrice.Create(
            productUnitId: 1,
            currencyId: 1,
            price: 150m,
            effectiveFrom: new DateTime(2026, 1, 1),
            createdByUserId: 1
        );

        price.Should().NotBeNull();
        price.Price.Should().Be(150m);
    }

    [Fact]
    public void AddInventoryBatch_ShouldCreateBatch()
    {
        var batch = InventoryBatch.Create(
            batchNo: "BATCH-001",
            productId: 1,
            warehouseId: (short)1,
            quantityReceived: 100m,
            unitCost: 50m,
            createdByUserId: 1
        );

        batch.Should().NotBeNull();
        batch.BatchNo.Should().Be("BATCH-001");
        batch.QuantityReceived.Should().Be(100m);
        batch.UnitCost.Should().Be(50m);
        batch.IsFullyConsumed.Should().BeFalse();
    }
}
