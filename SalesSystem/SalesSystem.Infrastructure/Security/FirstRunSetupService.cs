using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

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
            _logger.LogWarning("Connection string is empty — check appsettings.json");
            return;
        }

        if (_protector.IsEncrypted(currentValue))
        {
            _logger.LogInformation("Connection string is already encrypted");
            return;
        }

        var encrypted = _protector.Encrypt(currentValue);
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

        var json = File.ReadAllText(appSettingsPath);
        var dict = JsonSerializer.Deserialize<Dictionary<string, object?>>(json)!;

        if (dict.TryGetValue("ConnectionStrings", out var csObj))
        {
            var csDict = JsonSerializer.Deserialize<Dictionary<string, string>>(
                JsonSerializer.Serialize(csObj))!;
            csDict["DefaultConnection"] = value;
            dict["ConnectionStrings"] = csDict;
        }

        var newJson = JsonSerializer.Serialize(dict,
            new JsonSerializerOptions { WriteIndented = true });

        File.WriteAllText(appSettingsPath, newJson);
    }
}
