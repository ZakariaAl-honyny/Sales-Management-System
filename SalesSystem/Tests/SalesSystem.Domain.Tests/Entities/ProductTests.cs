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
            conversionFactor: 10,
            minStock: 10m,
            categoryId: 1,
            retailUnitId: 1,
            wholesaleUnitId: 2,
            description: "Test description",
            createdByUserId: 1
        );

        product.Name.Should().Be("Test Product");
        product.ConversionFactor.Should().Be(10);
        product.MinStock.Should().Be(10m);
        product.CategoryId.Should().Be(1);
        product.RetailUnitId.Should().Be(1);
        product.WholesaleUnitId.Should().Be(2);
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
            conversionFactor: 1
        );

        action.Should().Throw<DomainException>()
            .WithMessage("*اسم المنتج مطلوب*");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(-100)]
    public void Create_GivenInvalidConversionFactor_ShouldThrowDomainException(decimal invalidFactor)
    {
        var action = () => Product.Create(
            name: "Test Product",
            conversionFactor: invalidFactor
        );

        action.Should().Throw<DomainException>()
            .WithMessage("*معامل التحويل يجب أن يكون أكبر من الصفر*");
    }

    [Fact]
    public void Update_GivenValidData_ShouldUpdateProduct()
    {
        var product = Product.Create(
            name: "Original Name",
            conversionFactor: 10,
            minStock: 10m,
            categoryId: 1,
            retailUnitId: 1,
            createdByUserId: 1
        );

        product.Update(
            name: "Updated Name",
            conversionFactor: 12,
            minStock: 20m,
            categoryId: 2,
            retailUnitId: 2,
            wholesaleUnitId: 3,
            description: "Updated description",
            updatedByUserId: 1
        );

        product.Name.Should().Be("Updated Name");
        product.ConversionFactor.Should().Be(12);
        product.MinStock.Should().Be(20m);
        product.CategoryId.Should().Be(2);
        product.RetailUnitId.Should().Be(2);
        product.WholesaleUnitId.Should().Be(3);
        product.Description.Should().Be("Updated description");
    }

    [Fact]
    public void GetRetailQuantityEquivalent_GivenWholesaleMode_ShouldMultiplyByFactor()
    {
        var product = Product.Create("Test", conversionFactor: 10);
        
        var result = product.GetRetailQuantityEquivalent(2, SaleMode.Wholesale);
        
        result.Should().Be(20);
    }

    [Fact]
    public void GetRetailQuantityEquivalent_GivenRetailMode_ShouldReturnSameQuantity()
    {
        var product = Product.Create("Test", conversionFactor: 10);
        
        var result = product.GetRetailQuantityEquivalent(5, SaleMode.Retail);
        
        result.Should().Be(5);
    }

    [Fact]
    public void ConvertRetailToWholesaleBoxes_ShouldReturnFloor()
    {
        var product = Product.Create("Test", conversionFactor: 12);
        
        product.ConvertRetailToWholesaleBoxes(25).Should().Be(2);
        product.GetRemainingRetailAfterWholesale(25).Should().Be(1);
    }

    [Fact]
    public void ConvertToSmallestUnit_Wholesale_MultipliesByConversionFactor()
    {
        var product = Product.Create(
            name: "Test Product",
            conversionFactor: 10
        );

        var result = product.ConvertToSmallestUnit(5, UnitType.Wholesale);

        result.Should().Be(50m);
    }

    [Fact]
    public void ConvertToSmallestUnit_Retail_ReturnsSameQuantity()
    {
        var product = Product.Create(
            name: "Test Product",
            conversionFactor: 10
        );

        var result = product.ConvertToSmallestUnit(7, UnitType.Retail);

        result.Should().Be(7m);
    }

    [Fact]
    public void AddPrice_ShouldCreateProductPrice()
    {
        // Pricing is now managed via the ProductPrices entity directly
        // (not through Product.AddPrice). Verify that ProductPrice.Create works.
        var price = ProductPrice.Create(
            productUnitId: 1,
            currencyId: 1,
            priceLevel: PriceLevel.Retail,
            price: 150m,
            effectiveFrom: new DateTime(2026, 1, 1),
            createdByUserId: 1
        );

        price.Should().NotBeNull();
        price.PriceLevel.Should().Be(PriceLevel.Retail);
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
