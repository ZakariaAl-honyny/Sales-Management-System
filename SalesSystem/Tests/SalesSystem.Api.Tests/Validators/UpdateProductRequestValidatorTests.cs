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
        Name: "Valid Product",
        CategoryId: 1,
        Description: "Test description",
        ReorderLevel: 10,
        IsActive: true
    );
}