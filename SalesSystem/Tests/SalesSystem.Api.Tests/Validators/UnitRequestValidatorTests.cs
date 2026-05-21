using FluentValidation.TestHelper;
using SalesSystem.Api.Validators;
using SalesSystem.Contracts.Requests;
using Xunit;

namespace SalesSystem.Api.Tests.Validators;

[Trait("Category", "Validator")]
public class UnitRequestValidatorTests
{
    #region CreateUnitRequestValidator Tests

    [Trait("Category", "Validator")]
    public class CreateUnitRequestValidatorTests
    {
        private readonly CreateUnitRequestValidator _validator = new();

        #region Name Validation

        [Theory]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData("   ", false)]
        [InlineData("Piece", true)]
        [InlineData("قطعة", true)]
        [InlineData("Kilogram", true)]
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
                    .WithErrorMessage("اسم الوحدة مطلوب");
        }

        [Fact]
        public void GivenNameExceeds50Chars_WhenValidating_ThenFailsWithMaxLengthError()
        {
            // Arrange
            var longName = new string('و', 51);
            var request = CreateValidRequest() with { Name = longName };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Name)
                .WithErrorMessage("اسم الوحدة لا يمكن أن يتجاوز 50 حرف");
        }

        [Fact]
        public void GivenNameAt50Chars_WhenValidating_ThenPasses()
        {
            // Arrange
            var name = new string('و', 50);
            var request = CreateValidRequest() with { Name = name };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Name);
        }

        #endregion

        #region Symbol Validation

        [Theory]
        [InlineData(null, true)]
        [InlineData("", true)]
        [InlineData("pc", true)]
        [InlineData("كغ", true)]
        [InlineData("kg", true)]
        public void GivenSymbol_WhenValidating_ThenCorrectResult(string? symbol, bool isValid)
        {
            // Arrange
            var request = CreateValidRequest() with { Symbol = symbol };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            if (isValid)
                result.ShouldNotHaveValidationErrorFor(x => x.Symbol);
        }

        [Fact]
        public void GivenSymbolExceeds10Chars_WhenValidating_ThenFailsWithMaxLengthError()
        {
            // Arrange
            var longSymbol = new string('س', 11);
            var request = CreateValidRequest() with { Symbol = longSymbol };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Symbol)
                .WithErrorMessage("الرمز لا يمكن أن يتجاوز 10 أحرف");
        }

        [Fact]
        public void GivenSymbolAt10Chars_WhenValidating_ThenPasses()
        {
            // Arrange
            var symbol = new string('س', 10);
            var request = CreateValidRequest() with { Symbol = symbol };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Symbol);
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
            var request = new CreateUnitRequest("قطعة - Piece", "ق");

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        #endregion

        private static CreateUnitRequest CreateValidRequest() => new(
            Name: "Piece",
            Symbol: "pc"
        );
    }

    #endregion

    #region UpdateUnitRequestValidator Tests

    [Trait("Category", "Validator")]
    public class UpdateUnitRequestValidatorTests
    {
        private readonly UpdateUnitRequestValidator _validator = new();

        #region Name Validation

        [Theory]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData("   ", false)]
        [InlineData("Updated Unit", true)]
        [InlineData("وحدة محدثة", true)]
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
                    .WithErrorMessage("اسم الوحدة مطلوب");
        }

        [Fact]
        public void GivenNameExceeds50Chars_WhenValidating_ThenFailsWithMaxLengthError()
        {
            // Arrange
            var longName = new string('و', 51);
            var request = CreateValidRequest() with { Name = longName };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Name)
                .WithErrorMessage("اسم الوحدة لا يمكن أن يتجاوز 50 حرف");
        }

        #endregion

        #region Symbol Validation

        [Theory]
        [InlineData(null, true)]
        [InlineData("", true)]
        [InlineData("kg", true)]
        public void GivenSymbol_WhenValidating_ThenCorrectResult(string? symbol, bool isValid)
        {
            // Arrange
            var request = CreateValidRequest() with { Symbol = symbol };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            if (isValid)
                result.ShouldNotHaveValidationErrorFor(x => x.Symbol);
        }

        [Fact]
        public void GivenSymbolExceeds10Chars_WhenValidating_ThenFailsWithMaxLengthError()
        {
            // Arrange
            var longSymbol = new string('س', 11);
            var request = CreateValidRequest() with { Symbol = longSymbol };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Symbol)
                .WithErrorMessage("الرمز لا يمكن أن يتجاوز 10 أحرف");
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

        private static UpdateUnitRequest CreateValidRequest() => new(
            Name: "Updated Unit",
            Symbol: "kg",
            IsActive: true
        );
    }

    #endregion
}