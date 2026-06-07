using SalesSystem.Contracts.Responses;

namespace SalesSystem.DesktopPWF.Models;

/// <summary>
/// Enhanced login result that carries extra information beyond a standard Result&lt;LoginResponse&gt;.
/// When the API returns RequiresPasswordSetup, the UserId field contains the user's ID so the
/// desktop can open the SetPassword screen without an additional API call.
/// </summary>
public class LoginResult
{
    public bool IsSuccess { get; init; }
    public LoginResponse? Response { get; init; }
    public string? Error { get; init; }
    public string? ErrorCode { get; init; }

    /// <summary>
    /// Populated when ErrorCode == RequiresPasswordSetup.
    /// The user's ID needed for the set-password flow.
    /// </summary>
    public int? RequiresPasswordSetupUserId { get; init; }

    /// <summary>
    /// Populated when ErrorCode == RequiresPasswordSetup and a password reset token is available.
    /// The one-time token authorizes the password set operation.
    /// </summary>
    public string? PasswordResetToken { get; init; }
}
