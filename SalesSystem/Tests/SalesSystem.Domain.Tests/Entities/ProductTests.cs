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
            reorderLevel: 10m,
            createdByUserId: 1
        );

        product.Name.Should().Be("Test Product");
        product.ReorderLevel.Should().Be(10m);
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

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Create_GivenNegativeReorderLevel_ShouldThrowDomainException(decimal invalidReorderLevel)
    {
        var action = () => Product.Create(
            name: "Test Product",
            categoryId: 1,
            reorderLevel: invalidReorderLevel
        );

        action.Should().Throw<DomainException>()
            .WithMessage("*مستوى إعادة الطلب لا يمكن أن يكون سالباً*");
    }

    [Fact]
    public void Update_GivenValidData_ShouldUpdateProduct()
    {
        var product = Product.Create(
            name: "Original Name",
            categoryId: 1,
            reorderLevel: 10m,
            createdByUserId: 1
        );

        product.Update(
            name: "Updated Name",
            categoryId: 2,
            description: "Updated description",
            reorderLevel: 20m,
            updatedByUserId: 1
        );

        product.Name.Should().Be("Updated Name");
        product.ReorderLevel.Should().Be(20m);
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
            productId: 1,
            warehouseId: 1,
            quantity: 100m,
            unitCost: 50m,
            batchNo: "B-1001",
            createdByUserId: 1
        );

        batch.Should().NotBeNull();
        batch.Quantity.Should().Be(100m);
        batch.UnitCost.Should().Be(50m);
        batch.BatchNo.Should().Be("B-1001");
    }
}
