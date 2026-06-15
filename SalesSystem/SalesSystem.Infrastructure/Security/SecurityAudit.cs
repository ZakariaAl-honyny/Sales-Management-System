using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SalesSystem.Infrastructure.Security;

public static class SecurityAudit
{
    /// <summary>
    /// Runs a comprehensive security audit. Call this from any layer at any time.
    /// Unlike RunChecks (which is DEBUG-only), this method always executes.
    /// Logs warnings via ILogger and throws on critical findings.
    /// </summary>
    public static void RunSecurityAudit(IConfiguration configuration, ILogger logger)
    {
        var warnings = new List<string>();

        var connString = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrEmpty(connString))
        {
            if (!connString.StartsWith("DPAPI:", StringComparison.Ordinal))
                warnings.Add("WARNING: Connection string is NOT encrypted with DPAPI.");
        }

        var allConfig = configuration.AsEnumerable().ToList();
        foreach (var kvp in allConfig)
        {
            if (string.IsNullOrWhiteSpace(kvp.Value))
                continue;

            if (kvp.Value.Contains("Password=", StringComparison.OrdinalIgnoreCase) &&
                !kvp.Key.Contains("ConnectionStrings", StringComparison.OrdinalIgnoreCase))
                warnings.Add($"WARNING: Possible hardcoded password in config key '{kvp.Key}'.");

            if (kvp.Value.StartsWith("ghp_", StringComparison.Ordinal) ||
                kvp.Value.StartsWith("gho_", StringComparison.Ordinal) ||
                kvp.Value.StartsWith("github_pat_", StringComparison.Ordinal))
                warnings.Add($"CRITICAL: GitHub PAT or token found in config key '{kvp.Key}'!");
        }

        if (warnings.Count <= 0) return;

        var message = string.Join(Environment.NewLine, warnings);
        logger.LogWarning("Security audit findings:\n{Findings}", message);

        if (warnings.Any(w => w.StartsWith("CRITICAL", StringComparison.Ordinal)))
            throw new InvalidOperationException(
                "Security audit failed — sensitive tokens found in configuration." +
                Environment.NewLine + message);
    }

    [Conditional("DEBUG")]
    public static void RunChecks(IConfiguration configuration)
    {
        var warnings = new List<string>();

        var connString = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrEmpty(connString))
        {
            if (!connString.StartsWith("DPAPI:", StringComparison.Ordinal))
                warnings.Add("WARNING: Connection string is NOT encrypted with DPAPI.");
        }

        var allConfig = configuration.AsEnumerable().ToList();
        foreach (var kvp in allConfig)
        {
            if (string.IsNullOrWhiteSpace(kvp.Value))
                continue;

            if (kvp.Value.Contains("Password=", StringComparison.OrdinalIgnoreCase) &&
                !kvp.Key.Contains("ConnectionStrings", StringComparison.OrdinalIgnoreCase))
                warnings.Add($"WARNING: Possible hardcoded password in config key '{kvp.Key}'.");

            if (kvp.Value.StartsWith("ghp_", StringComparison.Ordinal) ||
                kvp.Value.StartsWith("gho_", StringComparison.Ordinal) ||
                kvp.Value.StartsWith("github_pat_", StringComparison.Ordinal))
                warnings.Add($"CRITICAL: GitHub PAT or token found in config key '{kvp.Key}'!");
        }

        if (warnings.Count > 0)
        {
            var message = string.Join(Environment.NewLine, warnings);
            Debug.WriteLine(message);
            if (warnings.Any(w => w.StartsWith("CRITICAL", StringComparison.Ordinal)))
                throw new InvalidOperationException(
                    "Security audit failed — sensitive tokens found in configuration." +
                    Environment.NewLine + message);
        }
    }
}
