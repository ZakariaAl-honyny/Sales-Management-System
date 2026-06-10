using FluentAssertions;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Tests.Entities;

public class ProductTests
{
    [Fact]
    public void Create_GivenValidData_ShouldCreateProduct()
    {
        var product = Product.Create(
            name: "Test Product",
            minStockLevel: 10m,
            categoryId: 1,
            description: "Test description",
            createdByUserId: 1
        );

        product.Name.Should().Be("Test Product");
        product.MinStockLevel.Should().Be(10m);
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
            name: invalidName!
        );

        action.Should().Throw<DomainException>()
            .WithMessage("*اسم المنتج مطلوب*");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Create_GivenNegativeMinStockLevel_ShouldThrowDomainException(decimal invalidMinStockLevel)
    {
        var action = () => Product.Create(
            name: "Test Product",
            minStockLevel: invalidMinStockLevel
        );

        action.Should().Throw<DomainException>()
            .WithMessage("*الحد الأدنى للمخزون لا يمكن أن يكون سالباً*");
    }

    [Fact]
    public void Update_GivenValidData_ShouldUpdateProduct()
    {
        var product = Product.Create(
            name: "Original Name",
            minStockLevel: 10m,
            categoryId: 1,
            createdByUserId: 1
        );

        product.Update(
            name: "Updated Name",
            categoryId: 2,
            minStockLevel: 20m,
            reorderLevel: 0,
            hasExpiry: false,
            barcode: null,
            description: "Updated description",
            updatedByUserId: 1
        );

        product.Name.Should().Be("Updated Name");
        product.MinStockLevel.Should().Be(20m);
        product.CategoryId.Should().Be(2);
        product.Description.Should().Be("Updated description");
    }

    [Fact]
    public void AddPrice_ShouldCreateProductPrice()
    {
        // Pricing is now managed via the ProductPrices entity directly
        // (not through Product.AddPrice). Verify that ProductPrice.Create works.
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
    public void AddImage_ShouldAddToImagesCollection()
    {
        var product = Product.Create("Test Product");

        var image = ProductImage.Create(
            productId: 1,
            imagePath: "/images/test.jpg",
            isPrimary: true,
            sortOrder: 1,
            createdByUserId: 1
        );

        product.AddImage(image);

        product.Images.Should().HaveCount(1);
    }

    [Fact]
    public void AddInventoryBatch_ShouldAddToBatchesCollection()
    {
        var product = Product.Create("Test Product");

        var batch = InventoryBatch.Create(
            productId: 1,
            warehouseId: 1,
            quantity: 100m,
            unitCost: 50m,
            batchNo: "BATCH-001",
            createdByUserId: 1
        );

        product.AddInventoryBatch(batch);

        product.InventoryBatches.Should().HaveCount(1);
    }
}
