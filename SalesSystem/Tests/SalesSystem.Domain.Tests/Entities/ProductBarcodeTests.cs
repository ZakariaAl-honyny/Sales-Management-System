using FluentAssertions;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Tests.Entities;

public class ProductBarcodeTests
{
    [Fact]
    public void Create_GivenValidBarcode_ShouldSetProperties()
    {
        var barcode = ProductBarcode.Create(
            productId: 1,
            barcodeValue: "6281007001230",
            unitType: UnitType.Retail,
            isDefault: true
        );

        barcode.ProductId.Should().Be(1);
        barcode.BarcodeValue.Should().Be("6281007001230");
        barcode.UnitType.Should().Be(UnitType.Retail);
        barcode.IsDefault.Should().BeTrue();
    }

    [Fact]
    public void Create_GivenDefaultParameters_ShouldSetDefaults()
    {
        var barcode = ProductBarcode.Create(
            productId: 1,
            barcodeValue: "123456789"
        );

        barcode.UnitType.Should().Be(UnitType.Retail);
        barcode.IsDefault.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_GivenEmptyBarcode_ShouldThrowDomainException(string? invalidBarcode)
    {
        var action = () => ProductBarcode.Create(
            productId: 1,
            barcodeValue: invalidBarcode!
        );

        action.Should().Throw<DomainException>()
            .WithMessage("*قيمة الباركود مطلوبة*");
    }

    [Fact]
    public void Create_GivenZeroProductId_ShouldSucceed()
    {
        // Entity does not validate ProductId > 0
        var barcode = ProductBarcode.Create(
            productId: 0,
            barcodeValue: "123456789"
        );

        barcode.ProductId.Should().Be(0);
    }

    [Fact]
    public void Create_GivenWholesaleUnitType_ShouldSetCorrectly()
    {
        var barcode = ProductBarcode.Create(
            productId: 1,
            barcodeValue: "999",
            unitType: UnitType.Wholesale
        );

        barcode.UnitType.Should().Be(UnitType.Wholesale);
    }

    [Fact]
    public void Create_GivenIsDefaultTrue_ShouldSetIsDefault()
    {
        var barcode = ProductBarcode.Create(
            productId: 1,
            barcodeValue: "555",
            isDefault: true
        );

        barcode.IsDefault.Should().BeTrue();
    }

    [Fact]
    public void Create_GivenBarcodeWithWhitespace_ShouldTrimBarcode()
    {
        var barcode = ProductBarcode.Create(
            productId: 1,
            barcodeValue: "  123ABC  "
        );

        barcode.BarcodeValue.Should().Be("123ABC");
    }

    [Theory]
    [InlineData(UnitType.Retail)]
    [InlineData(UnitType.Wholesale)]
    public void Create_GivenBothUnitTypes_ShouldSucceed(UnitType unitType)
    {
        var barcode = ProductBarcode.Create(
            productId: 1,
            barcodeValue: "111222333",
            unitType: unitType
        );

        barcode.UnitType.Should().Be(unitType);
    }

    [Fact]
    public void Create_GivenLongBarcode_ShouldSucceed()
    {
        var longBarcode = new string('9', 50);
        var barcode = ProductBarcode.Create(
            productId: 1,
            barcodeValue: longBarcode
        );

        barcode.BarcodeValue.Should().Be(longBarcode);
    }
}
