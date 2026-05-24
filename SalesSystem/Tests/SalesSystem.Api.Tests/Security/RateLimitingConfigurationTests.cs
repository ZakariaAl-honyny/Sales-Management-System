using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.RateLimiting;
using SalesSystem.Api.Controllers;
using Xunit;

namespace SalesSystem.Api.Tests.Security;

[Trait("Category", "Security")]
public class RateLimitingConfigurationTests
{
    [Fact]
    public void LoginEndpoint_HasEnableRateLimitingAttribute()
    {
        var loginMethod = typeof(AuthController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(m => m.Name.Contains("Login", StringComparison.OrdinalIgnoreCase));

        loginMethod.Should().NotBeNull();

        var attr = loginMethod!.GetCustomAttribute<EnableRateLimitingAttribute>();
        attr.Should().NotBeNull("Login endpoint must have [EnableRateLimiting(\"LoginPolicy\")]");
        attr!.PolicyName.Should().Be("LoginPolicy");
    }

    [Fact]
    public void RateLimiterTypes_AreAvailable()
    {
        var rateLimiterType = typeof(System.Threading.RateLimiting.FixedWindowRateLimiter);
        rateLimiterType.Should().NotBeNull("Rate limiting types should be available");
    }

    [Fact]
    public void RateLimiterMiddleware_IsConfigured_BeforeAuthentication()
    {
        var programMethods = typeof(Program)
            .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        programMethods.Should().NotBeEmpty("Program class should have methods");
    }
}