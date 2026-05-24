using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces.Services;

namespace SalesSystem.Infrastructure.Security;

public sealed class FirstRunSetupService
{
    private readonly IConnectionStringProtector _protector;
    private readonly ILogger<FirstRunSetupService> _logger;
    private const string ConfigKey = "ConnectionStrings:DefaultConnection";

    public FirstRunSetupService(
        IConnectionStringProtector protector,
        ILogger<FirstRunSetupService> logger)
    {
        _protector = protector;
        _logger = logger;
    }

    public void EnsureConnectionStringEncrypted(IConfiguration configuration)
    {
        var currentValue = configuration[ConfigKey];

        if (string.IsNullOrWhiteSpace(currentValue))
        {
            // Fallback to environment variable if appsettings is empty
            currentValue = Environment.GetEnvironmentVariable("SALESSYSTEM_DB_CONNECTION");
        }

        if (string.IsNullOrWhiteSpace(currentValue))
        {
            _logger.LogWarning("Connection string is empty — check appsettings.json or SALESSYSTEM_DB_CONNECTION environment variable");
            return;
        }

        if (_protector.IsEncrypted(currentValue))
        {
            _logger.LogInformation("Connection string is already encrypted");
            return;
        }

        var encrypted = _protector.Protect(currentValue);
        UpdateAppSettings(ConfigKey, encrypted);

        _logger.LogInformation("Connection string encrypted and saved on first run");
    }

    private static void UpdateAppSettings(string key, string value)
    {
        var appSettingsPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "appsettings.json");

        if (!File.Exists(appSettingsPath))
        {
            appSettingsPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "appsettings.json");
        }

        if (!File.Exists(appSettingsPath))
        {
            File.WriteAllText(appSettingsPath, "{\n  \"Logging\": {\n    \"LogLevel\": {\n      \"Default\": \"Information\"\n    }\n  }\n}", System.Text.Encoding.UTF8);
        }

        var json = File.ReadAllText(appSettingsPath);
        var dict = JsonSerializer.Deserialize<Dictionary<string, object?>>(json)!;

        Dictionary<string, string> csDict;
        if (dict.TryGetValue("ConnectionStrings", out var csObj) && csObj != null)
        {
            csDict = JsonSerializer.Deserialize<Dictionary<string, string>>(
                JsonSerializer.Serialize(csObj))!;
        }
        else
        {
            csDict = new Dictionary<string, string>();
        }

        csDict["DefaultConnection"] = value;
        dict["ConnectionStrings"] = csDict;

        var newJson = JsonSerializer.Serialize(dict,
            new JsonSerializerOptions { WriteIndented = true });

        // Atomic write: write to temp file first, then replace
        var tempPath = appSettingsPath + ".tmp";
        File.WriteAllText(tempPath, newJson, System.Text.Encoding.UTF8);
        File.Replace(tempPath, appSettingsPath, appSettingsPath + ".bak");
    }
}
