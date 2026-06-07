using FluentValidation.TestHelper;
using SalesSystem.Api.Validators;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Tests.Validators;

public class CreateUserRequestValidatorTests
{
    private readonly CreateUserRequestValidator _validator = new();

    #region UserName Validation

    [Theory]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData(null, false)]
    [InlineData("Updated Name", true)]
    [InlineData("الاسم المحدث", true)]
    public void GivenFullName_WhenValidating_ThenCorrectResult(string? fullName, bool isValid)
    {
        // Arrange
        var request = CreateValidRequest() with { FullName = fullName! };


        // Act
        var result = _validator.TestValidate(request);

        // Assert
        if (isValid)
            result.ShouldNotHaveValidationErrorFor(x => x.FullName);
        else
            result.ShouldHaveValidationErrorFor(x => x.FullName)
                .WithErrorMessage("الاسم الكامل مطلوب");
    }

    [Fact]
    public void GivenFullNameExceeds150Chars_WhenValidating_ThenFailsWithMaxLengthError()
    {
        // Arrange
        var longFullName = new string('ا', 151);
        var request = CreateValidRequest() with { FullName = longFullName };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FullName)
            .WithErrorMessage("الاسم الكامل يجب ألا يتجاوز 150 حرفاً");
    }

    #endregion

    #region Role Validation

    [Theory]
    [InlineData((byte)1, true)]  // Admin
    [InlineData((byte)2, true)]  // Manager
    [InlineData((byte)3, true)]  // Cashier
    [InlineData((byte)0, false)] // Invalid
    [InlineData((byte)4, false)] // Invalid
    [InlineData((byte)10, false)] // Invalid
    [InlineData((byte)255, false)] // Invalid
    public void GivenRole_WhenValidating_ThenCorrectResult(byte role, bool isValid)
    {
        // Arrange
        var request = CreateValidRequest() with { Role = role };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        if (isValid)
            result.ShouldNotHaveValidationErrorFor(x => x.Role);
        else
            result.ShouldHaveValidationErrorFor(x => x.Role)
                .WithErrorMessage("دور المستخدم غير صالح");
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

    #region Arabic Text Support

    [Fact]
    public void GivenArabicFullName_WhenValidating_ThenPasses()
    {
        // Arrange
        var request = CreateValidRequest() with { FullName = "اسم المستخدم العربي" };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.FullName);
    }

    #endregion

    private static CreateUserRequest CreateValidRequest() => new(
        UserName: "testuser",
        FullName: "Test User",
        Role: 1
    );
}

public class UpdateUserRequestValidatorTests
{
    private readonly UpdateUserRequestValidator _validator = new();

    #region FullName Validation

    [Theory]
    [InlineData("   ", false)]
    [InlineData(null, false)]
    [InlineData("Full Name", true)]
    [InlineData("الاسم الكامل", true)]
    [InlineData("John Doe", true)]
    public void GivenFullName_WhenValidating_ThenCorrectResult(string? fullName, bool isValid)
    {
        // Arrange
        var request = CreateValidRequest() with { FullName = fullName! };


        // Act
        var result = _validator.TestValidate(request);

        // Assert
        if (isValid)
            result.ShouldNotHaveValidationErrorFor(x => x.FullName);
        else
            result.ShouldHaveValidationErrorFor(x => x.FullName)
                .WithErrorMessage("الاسم الكامل مطلوب");
    }

    [Fact]
    public void GivenFullNameExceeds150Chars_WhenValidating_ThenFailsWithMaxLengthError()
    {
        // Arrange
        var longFullName = new string('ا', 151);
        var request = CreateValidRequest() with { FullName = longFullName };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FullName)
            .WithErrorMessage("الاسم الكامل يجب ألا يتجاوز 150 حرفاً");
    }

    #endregion

    #region Role Validation

    [Theory]
    [InlineData((byte)1, true)]  // Admin
    [InlineData((byte)2, true)]  // Manager
    [InlineData((byte)3, true)]  // Cashier
    [InlineData((byte)0, false)] // Invalid
    [InlineData((byte)4, false)] // Invalid
    public void GivenRole_WhenValidating_ThenCorrectResult(byte role, bool isValid)
    {
        // Arrange
        var request = CreateValidRequest() with { Role = role };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        if (isValid)
            result.ShouldNotHaveValidationErrorFor(x => x.Role);
        else
            result.ShouldHaveValidationErrorFor(x => x.Role)
                .WithErrorMessage("دور المستخدم غير صالح");
    }

    #endregion

    #region Password Validation

    [Fact]
    public void GivenNullPassword_WhenValidating_ThenPasses()
    {
        // Arrange
        var request = CreateValidRequest() with { Password = null };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void GivenEmptyPassword_WhenValidating_ThenPasses()
    {
        // Arrange
        var request = CreateValidRequest() with { Password = "" };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }

    [Theory]
    [InlineData("12345", false)] // 5 chars
    [InlineData("12345678", true)]  // 8 chars - minimum
    [InlineData("newpassword", true)]
    public void GivenPassword_WhenValidating_ThenCorrectResult(string? password, bool isValid)
    {
        // Arrange
        var request = CreateValidRequest() with { Password = password };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        if (isValid)
            result.ShouldNotHaveValidationErrorFor(x => x.Password);
        else
            result.ShouldHaveValidationErrorFor(x => x.Password)
                .WithErrorMessage("كلمة المرور يجب أن تكون 8 أحرف على الأقل");
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
        var request = CreateValidRequest() with { FullName = name };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        if (isValid)
            result.ShouldNotHaveValidationErrorFor(x => x.FullName);
        else
            result.ShouldHaveValidationErrorFor(x => x.FullName);
    }

    [Fact]
    public void GivenArabicFullName_WhenValidatingUpdate_ThenPasses()
    {
        // Arrange
        var request = CreateValidRequest() with { FullName = "اسم المستخدم العربي المحدث" };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.FullName);
    }

    [Fact]
    public void GivenMixedLanguageData_WhenValidatingUpdate_ThenPasses()
    {
        // Arrange
        var request = CreateValidRequest() with
        {
            FullName = "User Updated - اسم"
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
    public void GivenRequestWithNoPassword_WhenValidating_ThenPasses()
    {
        // Arrange
        var request = new UpdateUserRequest(
            FullName: "Updated User",
            Role: 2,
            Status: 1,
            Password: null
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    private static UpdateUserRequest CreateValidRequest() => new(
        FullName: "Updated User",
        Role: 2,
        Status: 1,
        Password: "newpass123"
    );
}