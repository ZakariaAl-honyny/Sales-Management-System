using FluentAssertions;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Tests.Entities;

public class UnitBarcodeTests
{
    [Fact]
    public void Create_GivenValidBarcode_ShouldSetProperties()
    {
        var barcode = UnitBarcode.Create(
            productUnitId: 1,
            barcodeValue: "6281007001230",
            isDefault: true,
            supplierCode: "SUP-001"
        );

        barcode.ProductUnitId.Should().Be(1);
        barcode.BarcodeValue.Should().Be("6281007001230");
        barcode.IsDefault.Should().BeTrue();
        barcode.SupplierCode.Should().Be("SUP-001");
    }

    [Fact]
    public void Create_GivenDefaultParameters_ShouldSetDefaults()
    {
        var barcode = UnitBarcode.Create(
            productUnitId: 1,
            barcodeValue: "123456789"
        );

        barcode.IsDefault.Should().BeFalse();
        barcode.SupplierCode.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_GivenEmptyBarcode_ShouldThrowDomainException(string? invalidBarcode)
    {
        var action = () => UnitBarcode.Create(
            productUnitId: 1,
            barcodeValue: invalidBarcode!
        );

        action.Should().Throw<DomainException>()
            .WithMessage("*قيمة الباركود لا يمكن أن تكون فارغة*");
    }

    [Fact]
    public void Create_GivenZeroProductUnitId_ShouldSucceed()
    {
        // Entity does not validate ProductUnitId > 0
        var barcode = UnitBarcode.Create(
            productUnitId: 0,
            barcodeValue: "123456789"
        );

        barcode.ProductUnitId.Should().Be(0);
    }

    [Theory]
    [InlineData("abc123", "ABC123")]
    [InlineData("ABC-123", "ABC-123")]
    [InlineData(" barcode ", "BARCODE")]
    public void Create_ShouldNormalizeBarcodeToUpperAndTrim(string input, string expected)
    {
        var barcode = UnitBarcode.Create(
            productUnitId: 1,
            barcodeValue: input
        );

        barcode.BarcodeValue.Should().Be(expected);
    }

    [Fact]
    public void Create_GivenSupplierCode_ShouldNormalizeToUpperAndTrim()
    {
        var barcode = UnitBarcode.Create(
            productUnitId: 1,
            barcodeValue: "123",
            supplierCode: " sup-001 "
        );

        barcode.SupplierCode.Should().Be("SUP-001");
    }

    [Fact]
    public void Create_GivenIsDefaultTrue_ShouldSetIsDefault()
    {
        var barcode = UnitBarcode.Create(
            productUnitId: 1,
            barcodeValue: "999",
            isDefault: true
        );

        barcode.IsDefault.Should().BeTrue();
    }

    [Fact]
    public void UnmarkDefault_ShouldSetIsDefaultToFalse()
    {
        var barcode = UnitBarcode.Create(
            productUnitId: 1,
            barcodeValue: "123",
            isDefault: true
        );

        barcode.UnmarkDefault();

        barcode.IsDefault.Should().BeFalse();
    }

    [Fact]
    public void UnmarkDefault_WhenAlreadyFalse_ShouldStayFalse()
    {
        var barcode = UnitBarcode.Create(
            productUnitId: 1,
            barcodeValue: "123",
            isDefault: false
        );

        barcode.UnmarkDefault();

        barcode.IsDefault.Should().BeFalse();
    }
}
