using System.IO;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace SalesSystem.Api.Tests.Security;

[Trait("Category", "Security")]
public class ConnectionStringSecurityTests
{
    [Fact]
    public void AppSettingsDevelopment_ShouldNotContainPlaintextConnectionStrings()
    {
        var apiProjectDir = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "..", "..", "..", "..", "..",
            "SalesSystem.Api");

        var devSettingsPath = Path.GetFullPath(
            Path.Combine(apiProjectDir, "appsettings.Development.json"));

        File.Exists(devSettingsPath).Should().BeTrue(
            $"appsettings.Development.json should exist at: {devSettingsPath}");

        var jsonText = File.ReadAllText(devSettingsPath);
        var json = JsonSerializer.Deserialize<JsonElement>(jsonText);

        json.TryGetProperty("ConnectionStrings", out var connStringsSection).Should().BeTrue();

        connStringsSection.TryGetProperty("DefaultConnection", out var connStrElement).Should().BeTrue();

        var connectionString = connStrElement.GetString();

        connectionString.Should().BeNullOrEmpty(
            "Connection string should be empty; use SALESSYSTEM_DB_CONNECTION env var instead");
    }
}