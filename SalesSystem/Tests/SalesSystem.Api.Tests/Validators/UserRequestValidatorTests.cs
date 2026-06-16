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
    [InlineData("testuser", true)]
    [InlineData("john.doe", true)]
    [InlineData("john_doe", true)]
    public void GivenUserName_WhenValidating_ThenCorrectResult(string? userName, bool isValid)
    {
        // Arrange
        var request = CreateValidRequest() with { UserName = userName! };


        // Act
        var result = _validator.TestValidate(request);

        // Assert
        if (isValid)
            result.ShouldNotHaveValidationErrorFor(x => x.UserName);
        else
            result.ShouldHaveValidationErrorFor(x => x.UserName)
                .WithErrorMessage("اسم المستخدم مطلوب");
    }

    [Fact]
    public void GivenUserNameExceeds100Chars_WhenValidating_ThenFailsWithMaxLengthError()
    {
        // Arrange
        var longUserName = new string('a', 101);
        var request = CreateValidRequest() with { UserName = longUserName };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserName)
            .WithErrorMessage("اسم المستخدم يجب ألا يتجاوز 100 حرفاً");
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

    private static CreateUserRequest CreateValidRequest() => new(
        UserName: "testuser",
        Role: 1
    );
}

public class UpdateUserRequestValidatorTests
{
    private readonly UpdateUserRequestValidator _validator = new();

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
            Role: 2,
            Password: null
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    private static UpdateUserRequest CreateValidRequest() => new(
        Role: 2,
        Password: "newpass123"
    );
}