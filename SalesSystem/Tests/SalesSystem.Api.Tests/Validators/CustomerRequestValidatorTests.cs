using FluentValidation.TestHelper;
using SalesSystem.Api.Validators;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Tests.Validators;

public class CreateCustomerRequestValidatorTests
{
    private readonly CreateCustomerRequestValidator _validator = new();

    #region Name Validation

    [Theory]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData(null, false)]
    [InlineData("Valid Customer", true)]
    [InlineData("عميل عربي", true)]
    [InlineData("Customer 123", true)]
    public void GivenCustomerName_WhenValidating_ThenCorrectResult(string? name, bool isValid)
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
                .WithErrorMessage("اسم العميل مطلوب");
    }

    [Fact]
    public void GivenNameExceeds100Chars_WhenValidating_ThenFailsWithMaxLengthError()
    {
        // Arrange
        var longName = new string('ا', 101);
        var request = CreateValidRequest() with { Name = longName };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("اسم العميل لا يمكن أن يتجاوز 100 حرف");
    }

    #endregion

    #region Phone Validation

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("0551234567", true)]
    [InlineData("0509876543", true)]
    [InlineData("0560000000", true)]
    [InlineData("0533333333", true)]
    [InlineData("01234567890", false)]
    [InlineData("+966551234567", false)]
    public void GivenPhone_WhenValidating_ThenCorrectResult(string? phone, bool isValid)
    {
        // Arrange
        var request = CreateValidRequest() with { Phone = phone };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        if (isValid)
            result.ShouldNotHaveValidationErrorFor(x => x.Phone);
        else
            result.ShouldHaveValidationErrorFor(x => x.Phone)
                .WithErrorMessage("رقم الهاتف يجب أن يبدأ بـ 05 ويتكون من 10 أرقام");
    }

    [Fact]
    public void GivenPhoneExceeds50Chars_WhenValidating_ThenFailsWithMaxLengthError()
    {
        // Arrange
        var longPhone = new string('5', 51);
        var request = CreateValidRequest() with { Phone = longPhone };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Phone)
            .WithErrorMessage("رقم الهاتف لا يمكن أن يتجاوز 50 حرف");
    }

    #endregion

    #region Email Validation

[Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("test@example.com", true)]
    [InlineData("user.name@domain.co.uk", true)]
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
    public void GivenArabicCustomerName_WhenValidating_ThenPasses()
    {
        // Arrange
        var request = CreateValidRequest() with { Name = "اسم العميل العربي" };

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
            Name = "Customer Name - اسم",
            Phone = "0551234567"
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

    private static CreateCustomerRequest CreateValidRequest() => new(
        Name: "Valid Customer",
        Phone: "0551234567",
        Email: "customer@example.com",
        Address: "Test Address",
        TaxNumber: null,
        CreditLimit: 1000
    );
}

public class UpdateCustomerRequestValidatorTests
{
    private readonly UpdateCustomerRequestValidator _validator = new();

    #region Name Validation

    [Theory]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData(null, false)]
    [InlineData("Updated Customer", true)]
    [InlineData("عميل محدث", true)]
    public void GivenCustomerName_WhenValidating_ThenCorrectResult(string? name, bool isValid)
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
                .WithErrorMessage("اسم العميل مطلوب");
    }

    [Fact]
    public void GivenNameExceeds100Chars_WhenValidating_ThenFailsWithMaxLengthError()
    {
        // Arrange
        var longName = new string('ا', 101);
        var request = CreateValidRequest() with { Name = longName };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("اسم العميل لا يمكن أن يتجاوز 100 حرف");
    }

    #endregion

    #region Phone Validation

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("0551234567", true)]
    [InlineData("0509876543", true)]
    [InlineData("+966551234567", false)]
    [InlineData("01234567890", false)]
    public void GivenPhone_WhenValidating_ThenCorrectResult(string? phone, bool isValid)
    {
        // Arrange
        var request = CreateValidRequest() with { Phone = phone };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        if (isValid)
            result.ShouldNotHaveValidationErrorFor(x => x.Phone);
        else
            result.ShouldHaveValidationErrorFor(x => x.Phone)
                .WithErrorMessage("رقم الهاتف يجب أن يبدأ بـ 05 ويتكون من 10 أرقام");
    }

    [Fact]
    public void GivenPhoneExceeds50Chars_WhenValidating_ThenFailsWithMaxLengthError()
    {
        // Arrange
        var longPhone = new string('5', 51);
        var request = CreateValidRequest() with { Phone = longPhone };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Phone)
            .WithErrorMessage("رقم الهاتف لا يمكن أن يتجاوز 50 حرف");
    }

    #endregion

    #region Email Validation

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("updated@example.com", true)]
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
    public void GivenArabicCustomerName_WhenValidatingUpdate_ThenPasses()
    {
        // Arrange
        var request = CreateValidRequest() with { Name = "اسم العميل العربي المحدث" };

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
            Name = "Customer Updated - اسم",
            Phone = "0551234567"
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

    private static UpdateCustomerRequest CreateValidRequest() => new(
        Name: "Updated Customer",
        Phone: "0551234567",
        Email: "updated@example.com",
        Address: "Updated Address",
        TaxNumber: null,
        CreditLimit: 2000,
        IsActive: true
    );
}