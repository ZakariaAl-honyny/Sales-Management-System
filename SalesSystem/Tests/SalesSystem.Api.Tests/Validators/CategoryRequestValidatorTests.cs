using FluentValidation.TestHelper;
using SalesSystem.Api.Validators;
using SalesSystem.Contracts.Requests;
using Xunit;

namespace SalesSystem.Api.Tests.Validators;

[Trait("Category", "Validator")]
public class CategoryRequestValidatorTests
{
    #region CreateCategoryRequestValidator Tests

    [Trait("Category", "Validator")]
    public class CreateCategoryRequestValidatorTests
    {
        private readonly CreateCategoryRequestValidator _validator = new();

        #region Name Validation

        [Theory]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData("   ", false)]
        [InlineData("Electronics", true)]
        [InlineData("إلكترونيات", true)]
        [InlineData("Food & Beverages", true)]
        public void GivenName_WhenValidating_ThenCorrectResult(string? name, bool isValid)
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
                    .WithErrorMessage("اسم الفئة مطلوب");
        }

        [Fact]
        public void GivenNameExceeds100Chars_WhenValidating_ThenFailsWithMaxLengthError()
        {
            // Arrange
            var longName = new string('ف', 101);
            var request = CreateValidRequest() with { Name = longName };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Name)
                .WithErrorMessage("اسم الفئة لا يمكن أن يتجاوز 100 حرف");
        }

        [Fact]
        public void GivenNameAt100Chars_WhenValidating_ThenPasses()
        {
            // Arrange
            var name = new string('ف', 100);
            var request = CreateValidRequest() with { Name = name };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Name);
        }

        #endregion

        #region Description Validation

        [Theory]
        [InlineData(null, true)]
        [InlineData("", true)]
        [InlineData("Basic electronics products", true)]
        [InlineData("منتجات إلكترونية أساسية", true)]
        public void GivenDescription_WhenValidating_ThenCorrectResult(string? description, bool isValid)
        {
            // Arrange
            var request = CreateValidRequest() with { Description = description };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            if (isValid)
                result.ShouldNotHaveValidationErrorFor(x => x.Description);
        }

        [Fact]
        public void GivenDescriptionExceeds500Chars_WhenValidating_ThenFailsWithMaxLengthError()
        {
            // Arrange
            var longDescription = new string('و', 501);
            var request = CreateValidRequest() with { Description = longDescription };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Description)
                .WithErrorMessage("الوصف لا يمكن أن يتجاوز 500 حرف");
        }

        [Fact]
        public void GivenDescriptionAt500Chars_WhenValidating_ThenPasses()
        {
            // Arrange
            var description = new string('و', 500);
            var request = CreateValidRequest() with { Description = description };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Description);
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

        [Fact]
        public void GivenMixedLanguageData_WhenValidating_ThenPasses()
        {
            // Arrange
            var request = new CreateCategoryRequest(
                "Category - فئة",
                "Description - وصف"
            );

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void GivenNameOnlyNoDescription_WhenValidating_ThenPasses()
        {
            // Arrange
            var request = new CreateCategoryRequest("Electronics", null);

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        #endregion

        private static CreateCategoryRequest CreateValidRequest() => new(
            Name: "Electronics",
            Description: "Basic electronics products"
        );
    }

    #endregion

    #region UpdateCategoryRequestValidator Tests

    [Trait("Category", "Validator")]
    public class UpdateCategoryRequestValidatorTests
    {
        private readonly UpdateCategoryRequestValidator _validator = new();

        #region Name Validation

        [Theory]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData("   ", false)]
        [InlineData("Updated Electronics", true)]
        [InlineData("إلكترونيات محدثة", true)]
        public void GivenName_WhenValidating_ThenCorrectResult(string? name, bool isValid)
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
                    .WithErrorMessage("اسم الفئة مطلوب");
        }

        [Fact]
        public void GivenNameExceeds100Chars_WhenValidating_ThenFailsWithMaxLengthError()
        {
            // Arrange
            var longName = new string('ف', 101);
            var request = CreateValidRequest() with { Name = longName };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Name)
                .WithErrorMessage("اسم الفئة لا يمكن أن يتجاوز 100 حرف");
        }

        #endregion

        #region Description Validation

        [Theory]
        [InlineData(null, true)]
        [InlineData("", true)]
        [InlineData("Updated description", true)]
        public void GivenDescription_WhenValidating_ThenCorrectResult(string? description, bool isValid)
        {
            // Arrange
            var request = CreateValidRequest() with { Description = description };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            if (isValid)
                result.ShouldNotHaveValidationErrorFor(x => x.Description);
        }

        [Fact]
        public void GivenDescriptionExceeds500Chars_WhenValidating_ThenFailsWithMaxLengthError()
        {
            // Arrange
            var longDescription = new string('و', 501);
            var request = CreateValidRequest() with { Description = longDescription };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Description)
                .WithErrorMessage("الوصف لا يمكن أن يتجاوز 500 حرف");
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

        private static UpdateCategoryRequest CreateValidRequest() => new(
            Name: "Updated Electronics",
            Description: "Updated electronics products",
            IsActive: true
        );
    }

    #endregion
}