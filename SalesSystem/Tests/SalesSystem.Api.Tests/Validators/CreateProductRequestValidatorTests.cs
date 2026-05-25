using FluentValidation.TestHelper;
using SalesSystem.Api.Validators;
using SalesSystem.Contracts.Enums;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Tests.Validators;

public class CreateProductRequestValidatorTests
{
    private readonly CreateProductRequestValidator _validator = new();

    #region Name Validation

    [Theory]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData(null, false)]
    [InlineData("Valid Product", true)]
    [InlineData("منتج عربي صحيح", true)]
    [InlineData("Product 123", true)]
    public void GivenProductName_WhenValidating_ThenCorrectResult(string? name, bool isValid)
    {
        // Arrange
        var request = CreateValidRequest() with { Name = name! };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        if (isValid)
            result.ShouldNotHaveValidationErrorFor(x => x.Name);
        else
            result.ShouldHaveValidationErrorFor(x => x.Name)
                .WithErrorMessage("اسم المنتج مطلوب");
    }

    [Fact]
    public void GivenNameExceeds200Chars_WhenValidating_ThenFailsWithMaxLengthError()
    {
        // Arrange
        var longName = new string('ا', 201);
        var request = CreateValidRequest() with { Name = longName };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("اسم المنتج لا يمكن أن يتجاوز 200 حرف");
    }

    #endregion

    #region Price Validation

    [Fact]
    public void GivenZeroRetailPrice_WhenValidating_ThenPasses()
    {
        // Arrange
        var request = CreateValidRequest() with { RetailPrice = 0 };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.RetailPrice);
    }

    [Fact]
    public void GivenPositiveRetailPrice_WhenValidating_ThenPasses()
    {
        // Arrange
        var request = CreateValidRequest() with { RetailPrice = 100.50m };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.RetailPrice);
    }

    [Fact]
    public void GivenNegativeRetailPrice_WhenValidating_ThenFails()
    {
        // Arrange
        var request = CreateValidRequest() with { RetailPrice = -1 };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RetailPrice)
            .WithErrorMessage("سعر التجزئة لا يمكن أن يكون سالباً");
    }

    [Fact]
    public void GivenNegativeWholesalePrice_WhenValidating_ThenFails()
    {
        // Arrange
        var request = CreateValidRequest() with { WholesalePrice = -1 };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.WholesalePrice)
            .WithErrorMessage("سعر الجملة لا يمكن أن يكون سالباً");
    }

    [Fact]
    public void GivenNegativePurchasePrice_WhenValidating_ThenFails()
    {
        // Arrange
        var request = CreateValidRequest() with { PurchasePrice = -1 };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PurchasePrice)
            .WithErrorMessage("سعر الشراء لا يمكن أن يكون سالباً");
    }

    #endregion

    #region Stock Validation

    [Fact]
    public void GivenZeroMinStock_WhenValidating_ThenPasses()
    {
        // Arrange
        var request = CreateValidRequest() with { MinStock = 0 };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.MinStock);
    }

    [Fact]
    public void GivenNegativeMinStock_WhenValidating_ThenFails()
    {
        // Arrange
        var request = CreateValidRequest() with { MinStock = -1 };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MinStock)
            .WithErrorMessage("الحد الأدنى للمخزون لا يمكن أن يكون سالباً");
    }

    #endregion

    #region Foreign Key Validation

    [Theory]
    [InlineData(1, true)]
    [InlineData(100, true)]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    public void GivenCategoryId_WhenValidating_ThenCorrectResult(int? categoryId, bool isValid)
    {
        // Arrange
        var request = CreateValidRequest() with { CategoryId = categoryId };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        if (isValid)
            result.ShouldNotHaveValidationErrorFor(x => x.CategoryId);
        else
            result.ShouldHaveValidationErrorFor(x => x.CategoryId)
                .WithErrorMessage("يجب اختيار تصنيف صحيح");
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(50, true)]
    [InlineData(0, false)]
    [InlineData(-5, false)]
    public void GivenRetailUnitId_WhenValidating_ThenCorrectResult(int? unitId, bool isValid)
    {
        // Arrange
        var request = CreateValidRequest() with { RetailUnitId = unitId };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        if (isValid)
            result.ShouldNotHaveValidationErrorFor(x => x.RetailUnitId);
        else
            result.ShouldHaveValidationErrorFor(x => x.RetailUnitId)
                .WithErrorMessage("يجب اختيار وحدة التجزئة");
    }

    #endregion

    #region Valid Request

    [Fact]
    public void GivenValidRequest_WhenValidating_ThenPasses()
    {
        // Arrange
        var request = CreateValidRequest();

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    private static CreateProductRequest CreateValidRequest() => new(
        Barcode: "123456789",
        Name: "Valid Product",
        CategoryId: 1,
        UnitId: 1,
        RetailUnitId: 1,
        WholesaleUnitId: 2,
        ConversionFactor: 10,
        PurchasePrice: 100.00m,
        SalePrice: 150.00m,
        RetailPrice: 150.00m,
        WholesalePrice: 1300.00m,
        MinStock: 10,
        Description: "Test description",
        ExpirationDate: null,
        ImagePath: null
    );
}