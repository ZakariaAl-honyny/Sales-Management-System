namespace SalesSystem.Contracts.Common;

/// <summary>
/// Configuration options for the backup system.
/// </summary>
public class BackupSettings
{
    public const string SectionName = "Backup";

    /// <summary>
    /// Default directory path where backup files are stored.
    /// </summary>
    public string DefaultBackupPath { get; set; } = string.Empty;
}
