using FluentValidation.TestHelper;
using SalesSystem.Api.Validators;
using SalesSystem.Contracts.Requests;
using Xunit;

namespace SalesSystem.Api.Tests.Validators;

[Trait("Category", "Validator")]
public class WarehouseRequestValidatorTests
{
    #region CreateWarehouseRequestValidator Tests

    [Trait("Category", "Validator")]
    public class CreateWarehouseRequestValidatorTests
    {
        private readonly CreateWarehouseRequestValidator _validator = new();

        #region Name Validation

        [Theory]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData("   ", false)]
        [InlineData("Main Warehouse", true)]
        [InlineData("مستودع رئيسي", true)]
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
                    .WithErrorMessage("اسم المخزن مطلوب");
        }

        [Fact]
        public void GivenNameExceeds100Chars_WhenValidating_ThenFailsWithMaxLengthError()
        {
            // Arrange
            var longName = new string('م', 101);
            var request = CreateValidRequest() with { Name = longName };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Name)
                .WithErrorMessage("اسم المخزن لا يمكن أن يتجاوز 100 حرف");
        }

        [Fact]
        public void GivenNameAt100Chars_WhenValidating_ThenPasses()
        {
            // Arrange
            var name = new string('م', 100);
            var request = CreateValidRequest() with { Name = name };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Name);
        }

        #endregion

        #region Code Validation

        [Theory]
        [InlineData(null, true)]
        [InlineData("", true)]
        [InlineData("WH001", true)]
        [InlineData("كود123", true)]
        public void GivenCode_WhenValidating_ThenCorrectResult(string? code, bool isValid)
        {
            // Arrange
            var request = CreateValidRequest() with { Code = code };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            if (isValid)
                result.ShouldNotHaveValidationErrorFor(x => x.Code);
        }

        [Fact]
        public void GivenCodeExceeds30Chars_WhenValidating_ThenFailsWithMaxLengthError()
        {
            // Arrange
            var longCode = new string('C', 31);
            var request = CreateValidRequest() with { Code = longCode };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Code)
                .WithErrorMessage("كود المخزن لا يمكن أن يتجاوز 30 حرف");
        }

        [Fact]
        public void GivenCodeAt30Chars_WhenValidating_ThenPasses()
        {
            // Arrange
            var code = new string('C', 30);
            var request = CreateValidRequest() with { Code = code };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Code);
        }

        #endregion

        #region Location Validation

        [Theory]
        [InlineData(null, true)]
        [InlineData("", true)]
        [InlineData("Cairo, Egypt", true)]
        [InlineData("القاهرة، مصر", true)]
        public void GivenLocation_WhenValidating_ThenCorrectResult(string? location, bool isValid)
        {
            // Arrange
            var request = CreateValidRequest() with { Location = location };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            if (isValid)
                result.ShouldNotHaveValidationErrorFor(x => x.Location);
        }

        [Fact]
        public void GivenLocationExceeds200Chars_WhenValidating_ThenFailsWithMaxLengthError()
        {
            // Arrange
            var longLocation = new string('ع', 201);
            var request = CreateValidRequest() with { Location = longLocation };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Location)
                .WithErrorMessage("العنوان لا يمكن أن يتجاوز 200 حرف");
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
            var request = new CreateWarehouseRequest(
                "مستودع - Warehouse",
                "WH-م",
                "القاهرة - Cairo",
                false
            );

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        #endregion

        private static CreateWarehouseRequest CreateValidRequest() => new(
            Name: "Main Warehouse",
            Code: "WH001",
            Location: "Cairo, Egypt",
            IsDefault: false
        );
    }

    #endregion

    #region UpdateWarehouseRequestValidator Tests

    [Trait("Category", "Validator")]
    public class UpdateWarehouseRequestValidatorTests
    {
        private readonly UpdateWarehouseRequestValidator _validator = new();

        #region Name Validation

        [Theory]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData("   ", false)]
        [InlineData("Updated Warehouse", true)]
        [InlineData("مستودع محدث", true)]
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
                    .WithErrorMessage("اسم المخزن مطلوب");
        }

        [Fact]
        public void GivenNameExceeds100Chars_WhenValidating_ThenFailsWithMaxLengthError()
        {
            // Arrange
            var longName = new string('م', 101);
            var request = CreateValidRequest() with { Name = longName };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Name)
                .WithErrorMessage("اسم المخزن لا يمكن أن يتجاوز 100 حرف");
        }

        #endregion

        #region Code Validation

        [Theory]
        [InlineData(null, true)]
        [InlineData("", true)]
        [InlineData("WH002", true)]
        public void GivenCode_WhenValidating_ThenCorrectResult(string? code, bool isValid)
        {
            // Arrange
            var request = CreateValidRequest() with { Code = code };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            if (isValid)
                result.ShouldNotHaveValidationErrorFor(x => x.Code);
        }

        [Fact]
        public void GivenCodeExceeds30Chars_WhenValidating_ThenFailsWithMaxLengthError()
        {
            // Arrange
            var longCode = new string('C', 31);
            var request = CreateValidRequest() with { Code = longCode };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Code)
                .WithErrorMessage("كود المخزن لا يمكن أن يتجاوز 30 حرف");
        }

        #endregion

        #region Location Validation

        [Theory]
        [InlineData(null, true)]
        [InlineData("", true)]
        public void GivenLocation_WhenValidating_ThenCorrectResult(string? location, bool isValid)
        {
            // Arrange
            var request = CreateValidRequest() with { Location = location };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            if (isValid)
                result.ShouldNotHaveValidationErrorFor(x => x.Location);
        }

        [Fact]
        public void GivenLocationExceeds200Chars_WhenValidating_ThenFailsWithMaxLengthError()
        {
            // Arrange
            var longLocation = new string('ع', 201);
            var request = CreateValidRequest() with { Location = longLocation };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Location)
                .WithErrorMessage("العنوان لا يمكن أن يتجاوز 200 حرف");
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

        private static UpdateWarehouseRequest CreateValidRequest() => new(
            Name: "Updated Warehouse",
            Code: "WH002",
            Location: "Alexandria, Egypt",
            IsDefault: true,
            IsActive: true
        );
    }

    #endregion
}