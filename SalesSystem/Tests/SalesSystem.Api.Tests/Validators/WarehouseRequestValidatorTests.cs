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
        public void GivenNameExceeds150Chars_WhenValidating_ThenFailsWithMaxLengthError()
        {
            // Arrange
            var longName = new string('م', 151);
            var request = CreateValidRequest() with { Name = longName };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Name)
                .WithErrorMessage("اسم المخزن لا يمكن أن يتجاوز 150 حرف");
        }

        [Fact]
        public void GivenNameAt150Chars_WhenValidating_ThenPasses()
        {
            // Arrange
            var name = new string('م', 150);
            var request = CreateValidRequest() with { Name = name };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Name);
        }

        #endregion

        #region Address Validation

        [Theory]
        [InlineData(null, true)]
        [InlineData("", true)]
        [InlineData("Cairo, Egypt", true)]
        [InlineData("القاهرة، مصر", true)]
        public void GivenAddress_WhenValidating_ThenCorrectResult(string? Address, bool isValid)
        {
            // Arrange
            var request = CreateValidRequest() with { Address = Address };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            if (isValid)
                result.ShouldNotHaveValidationErrorFor(x => x.Address);
        }

        [Fact]
        public void GivenAddressExceeds200Chars_WhenValidating_ThenFailsWithMaxLengthError()
        {
            // Arrange
            var longAddress = new string('ع', 201);
            var request = CreateValidRequest() with { Address = longAddress };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Address)
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
                BranchId: 1,
                Name: "مستودع - Warehouse",
                Address: "القاهرة - Cairo"
            );

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        #endregion

        private static CreateWarehouseRequest CreateValidRequest() => new(
            BranchId: 1,
            Name: "Main Warehouse",
            Address: "Cairo, Egypt"
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
        public void GivenNameExceeds150Chars_WhenValidating_ThenFailsWithMaxLengthError()
        {
            // Arrange
            var longName = new string('م', 151);
            var request = CreateValidRequest() with { Name = longName };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Name)
                .WithErrorMessage("اسم المخزن لا يمكن أن يتجاوز 150 حرف");
        }

        #endregion

        #region Address Validation

        [Theory]
        [InlineData(null, true)]
        [InlineData("", true)]
        public void GivenAddress_WhenValidating_ThenCorrectResult(string? Address, bool isValid)
        {
            // Arrange
            var request = CreateValidRequest() with { Address = Address };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            if (isValid)
                result.ShouldNotHaveValidationErrorFor(x => x.Address);
        }

        [Fact]
        public void GivenAddressExceeds200Chars_WhenValidating_ThenFailsWithMaxLengthError()
        {
            // Arrange
            var longAddress = new string('ع', 201);
            var request = CreateValidRequest() with { Address = longAddress };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Address)
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
            BranchId: 1,
            Name: "Updated Warehouse",
            Address: "Alexandria, Egypt",
            IsActive: true
        );
    }

    #endregion
}
