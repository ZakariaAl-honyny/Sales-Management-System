using FluentValidation.TestHelper;
using SalesSystem.Api.Validators;
using SalesSystem.Contracts.Requests;
using Xunit;

namespace SalesSystem.Api.Tests.Validators;

[Trait("Category", "Validator")]
public class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _validator = new();

    #region UserName Validation

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("validuser", true)]
    [InlineData("admin", true)]
    [InlineData("مستخدم عربي", true)]
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
    public void GivenUserNameExceeds50Chars_WhenValidating_ThenFailsWithMaxLengthError()
    {
        // Arrange
        var longUserName = new string('u', 51);
        var request = CreateValidRequest() with { UserName = longUserName };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserName)
            .WithErrorMessage("اسم المستخدم يجب ألا يتجاوز 50 حرفاً");
    }

    [Fact]
    public void GivenUserNameAt50Chars_WhenValidating_ThenPasses()
    {
        // Arrange
        var userName = new string('u', 50);
        var request = CreateValidRequest() with { UserName = userName };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.UserName);
    }

    #endregion

    #region Password Validation

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("ab", false)] // Less than 6 chars
    [InlineData("abcde", false)] // 5 chars, less than 6
    [InlineData("abcdef", true)] // Exactly 6 chars
    [InlineData("password123", true)]
    [InlineData("كلمة مرور", true)]
    public void GivenPassword_WhenValidating_ThenCorrectResult(string? password, bool isValid)
    {
        // Arrange
        var request = CreateValidRequest() with { Password = password! };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        if (isValid)
            result.ShouldNotHaveValidationErrorFor(x => x.Password);
        else
        {
            if (password == null || password == "" || password == "   ")
                result.ShouldHaveValidationErrorFor(x => x.Password)
                    .WithErrorMessage("كلمة المرور مطلوبة");
            else
                result.ShouldHaveValidationErrorFor(x => x.Password)
                    .WithErrorMessage("كلمة المرور يجب أن تكون 6 أحرف على الأقل");
        }
    }

    [Fact]
    public void GivenPasswordExceeds50Chars_WhenValidating_ThenStillPasses()
    {
        // Arrange - no explicit max length on password
        var longPassword = new string('p', 100);
        var request = CreateValidRequest() with { Password = longPassword };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Password);
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
        var request = new LoginRequest("مستخدم123", "كلمة مرور123");

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region Boundary Tests

    [Fact]
    public void GivenEmptyUserNameAndEmptyPassword_WhenValidating_ThenBothFail()
    {
        // Arrange
        var request = new LoginRequest("", "");

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserName);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void GivenPasswordWithExactly5Chars_WhenValidating_ThenFails()
    {
        // Arrange
        var request = new LoginRequest("admin", "abcde");

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("كلمة المرور يجب أن تكون 6 أحرف على الأقل");
    }

    [Fact]
    public void GivenPasswordWithExactly6Chars_WhenValidating_ThenPasses()
    {
        // Arrange
        var request = new LoginRequest("admin", "abcdef");

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }

    #endregion

    private static LoginRequest CreateValidRequest() => new(
        UserName: "admin",
        Password: "password123"
    );
}