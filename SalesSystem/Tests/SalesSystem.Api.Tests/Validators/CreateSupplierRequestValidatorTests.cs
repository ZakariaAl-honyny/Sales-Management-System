using FluentValidation.TestHelper;
using SalesSystem.Api.Validators;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Tests.Validators;

public class CreateSupplierRequestValidatorTests
{
    private readonly CreateSupplierRequestValidator _validator = new();

    #region Name Validation

    [Theory]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData(null, false)]
    [InlineData("Valid Supplier", true)]
    [InlineData("مورد عربي", true)]
    [InlineData("Supplier 123", true)]
    public void GivenSupplierName_WhenValidating_ThenCorrectResult(string? name, bool isValid)
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
                .WithErrorMessage("اسم المورد مطلوب");
    }

    [Fact]
    public void GivenNameExceeds150Chars_WhenValidating_ThenFailsWithMaxLengthError()
    {
        // Arrange
        var longName = new string('ا', 151);
        var request = CreateValidRequest() with { Name = longName };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("اسم المورد لا يمكن أن يتجاوز 150 حرف");
    }

    #endregion

    #region Phone Validation

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("+201234567890", true)]
    [InlineData("01234567890", true)]
    public void GivenPhone_WhenValidating_ThenCorrectResult(string? phone, bool isValid)
    {
        // Arrange
        var request = CreateValidRequest() with { Phone = phone };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        if (isValid)
            result.ShouldNotHaveValidationErrorFor(x => x.Phone);
    }

    [Fact]
    public void GivenPhoneExceeds20Chars_WhenValidating_ThenFailsWithMaxLengthError()
    {
        // Arrange
        var longPhone = new string('1', 21);
        var request = CreateValidRequest() with { Phone = longPhone };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Phone)
            .WithErrorMessage("رقم الهاتف لا يمكن أن يتجاوز 20 حرف");
    }

    #endregion

    #region Email Validation

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("test@supplier.com", true)]
    [InlineData("supplier.name@company.co.uk", true)]
    [InlineData("invalid-email", false)]
    [InlineData("@nodomain.com", false)]
    [InlineData("test@", false)]
    public void GivenEmail_WhenValidating_ThenCorrectResult(string? email, bool isValid)
    {
        // Arrange
        var request = CreateValidRequest() with { Email = email };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        if (isValid)
            result.ShouldNotHaveValidationErrorFor(x => x.Email);
        else
            result.ShouldHaveValidationErrorFor(x => x.Email)
                .WithErrorMessage("البريد الإلكتروني غير صحيح");
    }

    [Fact]
    public void GivenEmailExceeds100Chars_WhenValidating_ThenFailsWithMaxLengthError()
    {
        // Arrange
        var longEmail = $"@{new string('a', 98)}.com";
        var request = CreateValidRequest() with { Email = longEmail };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("البريد الإلكتروني لا يمكن أن يتجاوز 100 حرف");
    }

    #endregion

    #region Arabic Text Handling

    [Fact]
    public void GivenArabicSupplierName_WhenValidating_ThenPasses()
    {
        // Arrange
        var request = CreateValidRequest() with { Name = "اسم المورد العربي" };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void GivenMixedLanguageData_WhenValidating_ThenPasses()
    {
        // Arrange
        var request = CreateValidRequest() with
        {
            Name = "Supplier Name - اسم",
            Phone = "+201234567890"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
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

    private static CreateSupplierRequest CreateValidRequest() => new(
        Name: "Valid Supplier",
        Phone: "01234567890",
        Email: "supplier@example.com",
        Address: "Test Address",
        TaxNumber: null
    );
}

public class UpdateSupplierRequestValidatorTests
{
    private readonly UpdateSupplierRequestValidator _validator = new();

    #region Name Validation

    [Theory]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData(null, false)]
    [InlineData("Updated Supplier", true)]
    [InlineData("مورد محدث", true)]
    public void GivenSupplierName_WhenValidating_ThenCorrectResult(string? name, bool isValid)
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
                .WithErrorMessage("اسم المورد مطلوب");
    }

    [Fact]
    public void GivenNameExceeds150Chars_WhenValidating_ThenFailsWithMaxLengthError()
    {
        // Arrange
        var longName = new string('ا', 151);
        var request = CreateValidRequest() with { Name = longName };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("اسم المورد لا يمكن أن يتجاوز 150 حرف");
    }

    #endregion

    #region Phone Validation

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("+201234567891", true)]
    public void GivenPhone_WhenValidating_ThenCorrectResult(string? phone, bool isValid)
    {
        // Arrange
        var request = CreateValidRequest() with { Phone = phone };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        if (isValid)
            result.ShouldNotHaveValidationErrorFor(x => x.Phone);
    }

    [Fact]
    public void GivenPhoneExceeds20Chars_WhenValidating_ThenFailsWithMaxLengthError()
    {
        // Arrange
        var longPhone = new string('1', 21);
        var request = CreateValidRequest() with { Phone = longPhone };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Phone)
            .WithErrorMessage("رقم الهاتف لا يمكن أن يتجاوز 20 حرف");
    }

    #endregion

    #region Email Validation

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("updated@supplier.com", true)]
    [InlineData("invalid", false)]
    public void GivenEmail_WhenValidating_ThenCorrectResult(string? email, bool isValid)
    {
        // Arrange
        var request = CreateValidRequest() with { Email = email };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        if (isValid)
            result.ShouldNotHaveValidationErrorFor(x => x.Email);
        else
            result.ShouldHaveValidationErrorFor(x => x.Email)
                .WithErrorMessage("البريد الإلكتروني غير صحيح");
    }

    [Fact]
    public void GivenEmailExceeds100Chars_WhenValidating_ThenFailsWithMaxLengthError()
    {
        // Arrange
        var longEmail = $"@{new string('a', 98)}.com";
        var request = CreateValidRequest() with { Email = longEmail };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("البريد الإلكتروني لا يمكن أن يتجاوز 100 حرف");
    }

    #endregion

    #region Arabic Text Handling - Update

    [Theory]
    [InlineData("محمد أحمد", true)]  // Arabic name
    [InlineData("أحمد", true)]         // Short Arabic
    [InlineData("محمود على عبد الله", true)] // Long Arabic name
    public void GivenArabicName_WhenValidatingUpdate_ThenCorrectResult(string name, bool isValid)
    {
        // Arrange
        var request = CreateValidRequest() with { Name = name };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        if (isValid)
            result.ShouldNotHaveValidationErrorFor(x => x.Name);
        else
            result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void GivenArabicSupplierName_WhenValidatingUpdate_ThenPasses()
    {
        // Arrange
        var request = CreateValidRequest() with { Name = "اسم المورد العربي المحدث" };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void GivenMixedLanguageData_WhenValidatingUpdate_ThenPasses()
    {
        // Arrange
        var request = CreateValidRequest() with
        {
            Name = "Supplier Updated - اسم",
            Phone = "+201234567890"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
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
    public void GivenValidRequestWithDeactivatedSupplier_WhenValidating_ThenPasses()
    {
        // Arrange
        var request = CreateValidRequest() with { IsActive = false };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    private static UpdateSupplierRequest CreateValidRequest() => new(
        Name: "Updated Supplier",
        Phone: "01234567890",
        Email: "updated@supplier.com",
        Address: "Updated Address",
        TaxNumber: null,
        IsActive: true
    );
}