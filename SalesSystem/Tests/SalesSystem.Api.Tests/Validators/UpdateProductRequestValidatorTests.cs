using FluentValidation.TestHelper;
using SalesSystem.Api.Validators;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Tests.Validators;

public class UpdateProductRequestValidatorTests
{
    private readonly UpdateProductRequestValidator _validator = new();

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

    private static UpdateProductRequest CreateValidRequest() => new(
        Code: "PROD001",
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
        IsActive: true
    );
}