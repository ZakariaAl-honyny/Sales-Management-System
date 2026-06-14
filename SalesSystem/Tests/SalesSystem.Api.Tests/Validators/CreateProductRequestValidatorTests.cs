using FluentValidation.TestHelper;
using SalesSystem.Api.Validators;
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

    #region Stock Validation

    [Fact]
    public void GivenZeroReorderLevel_WhenValidating_ThenPasses()
    {
        // Arrange
        var request = CreateValidRequest() with { ReorderLevel = 0 };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ReorderLevel);
    }

    [Fact]
    public void GivenNegativeReorderLevel_WhenValidating_ThenFails()
    {
        // Arrange
        var request = CreateValidRequest() with { ReorderLevel = -1 };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ReorderLevel)
            .WithErrorMessage("مستوى إعادة الطلب لا يمكن أن يكون سالباً");
    }

    #endregion

    #region Foreign Key Validation

    [Theory]
    [InlineData(1, true)]
    [InlineData(100, true)]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    public void GivenCategoryId_WhenValidating_ThenCorrectResult(int categoryId, bool isValid)
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
                .WithErrorMessage("التصنيف مطلوب");
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
        Name: "Valid Product",
        CategoryId: 1,
        Description: "Test description",
        Barcode: "123456789",
        ReorderLevel: 10
    );
}
