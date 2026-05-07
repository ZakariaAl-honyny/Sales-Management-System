namespace SalesSystem.Contracts.Common;

/// <summary>
/// JWT configuration settings for token generation.
/// </summary>
public record JwtSettings
{
    public string Secret { get; init; } = string.Empty;
    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public int ExpirationHours { get; init; } = 8;
}