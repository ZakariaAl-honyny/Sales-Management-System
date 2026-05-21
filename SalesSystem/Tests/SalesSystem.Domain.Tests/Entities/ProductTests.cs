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
            purchasePrice: 100m,
            retailPrice: 150m,
            wholesalePrice: 1200m,
            conversionFactor: 10,
            minStock: 10m,
            code: "P001",
            barcode: "123456789",
            categoryId: 1,
            retailUnitId: 1,
            wholesaleUnitId: 2,
            description: "Test description",
            createdByUserId: 1
        );

        product.Name.Should().Be("Test Product");
        product.PurchasePrice.Should().Be(100m);
        product.RetailPrice.Should().Be(150m);
        product.WholesalePrice.Should().Be(1200m);
        product.ConversionFactor.Should().Be(10);
        product.MinStock.Should().Be(10m);
        product.Code.Should().Be("P001");
        product.Barcode.Should().Be("123456789");
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
            purchasePrice: 100m,
            retailPrice: 150m
        );

        action.Should().Throw<DomainException>()
            .WithMessage("*اسم المنتج مطلوب*");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Create_GivenNegativePurchasePrice_ShouldThrowDomainException(decimal negativePrice)
    {
        var action = () => Product.Create(
            name: "Test Product",
            purchasePrice: negativePrice,
            retailPrice: 150m
        );

        action.Should().Throw<DomainException>()
            .WithMessage("*سعر الشراء لا يمكن أن يكون سالباً*");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Create_GivenNegativeRetailPrice_ShouldThrowDomainException(decimal negativePrice)
    {
        var action = () => Product.Create(
            name: "Test Product",
            purchasePrice: 100m,
            retailPrice: negativePrice
        );

        action.Should().Throw<DomainException>()
            .WithMessage("*سعر التجزئة لا يمكن أن يكون سالباً*");
    }

    [Fact]
    public void Update_GivenValidData_ShouldUpdateProduct()
    {
        var product = Product.Create(
            name: "Original Name",
            purchasePrice: 100m,
            retailPrice: 150m,
            minStock: 10m,
            createdByUserId: 1
        );

        product.Update(
            name: "Updated Name",
            purchasePrice: 200m,
            retailPrice: 250m,
            wholesalePrice: 2000m,
            conversionFactor: 12,
            minStock: 20m,
            code: "P002",
            barcode: "987654321",
            categoryId: 2,
            retailUnitId: 2,
            wholesaleUnitId: 3,
            description: "Updated description",
            updatedByUserId: 1
        );

        product.Name.Should().Be("Updated Name");
        product.PurchasePrice.Should().Be(200m);
        product.RetailPrice.Should().Be(250m);
        product.WholesalePrice.Should().Be(2000m);
        product.ConversionFactor.Should().Be(12);
        product.MinStock.Should().Be(20m);
        product.Code.Should().Be("P002");
        product.Barcode.Should().Be("987654321");
        product.CategoryId.Should().Be(2);
        product.RetailUnitId.Should().Be(2);
        product.WholesaleUnitId.Should().Be(3);
        product.Description.Should().Be("Updated description");
    }

    [Fact]
    public void GetRetailQuantityEquivalent_GivenWholesaleMode_ShouldMultiplyByFactor()
    {
        var product = Product.Create("Test", 0, 10, 80, 10);
        
        var result = product.GetRetailQuantityEquivalent(2, SaleMode.Wholesale);
        
        result.Should().Be(20);
    }

    [Fact]
    public void GetRetailQuantityEquivalent_GivenRetailMode_ShouldReturnSameQuantity()
    {
        var product = Product.Create("Test", 0, 10, 80, 10);
        
        var result = product.GetRetailQuantityEquivalent(5, SaleMode.Retail);
        
        result.Should().Be(5);
    }

    [Fact]
    public void GetUnitPrice_GivenMode_ShouldReturnCorrectPrice()
    {
        var product = Product.Create("Test", 0, 10, 85, 10);
        
        product.GetUnitPrice(SaleMode.Retail).Should().Be(10);
        product.GetUnitPrice(SaleMode.Wholesale).Should().Be(85);
    }

    [Fact]
    public void ConvertRetailToWholesaleBoxes_ShouldReturnFloor()
    {
        var product = Product.Create("Test", 0, 10, 100, 12);
        
        product.ConvertRetailToWholesaleBoxes(25).Should().Be(2);
        product.GetRemainingRetailAfterWholesale(25).Should().Be(1);
    }

    [Fact]
    public void GetPriceByUnit_Wholesale_ReturnsWholesalePrice()
    {
        var product = Product.Create(
            name: "Test Product",
            purchasePrice: 100m,
            retailPrice: 150m,
            wholesalePrice: 1200m,
            conversionFactor: 10
        );

        var result = product.GetPriceByUnit(UnitType.Wholesale);

        result.Should().Be(1200m);
    }

    [Fact]
    public void GetPriceByUnit_Retail_ReturnsRetailPrice()
    {
        var product = Product.Create(
            name: "Test Product",
            purchasePrice: 100m,
            retailPrice: 150m,
            wholesalePrice: 1200m,
            conversionFactor: 10
        );

        var result = product.GetPriceByUnit(UnitType.Retail);

        result.Should().Be(150m);
    }

    [Fact]
    public void ConvertToSmallestUnit_Wholesale_MultipliesByConversionFactor()
    {
        var product = Product.Create(
            name: "Test Product",
            purchasePrice: 100m,
            retailPrice: 150m,
            wholesalePrice: 1200m,
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
            purchasePrice: 100m,
            retailPrice: 150m,
            wholesalePrice: 1200m,
            conversionFactor: 10
        );

        var result = product.ConvertToSmallestUnit(7, UnitType.Retail);

        result.Should().Be(7m);
    }
}