using System;
using System.Security.Cryptography;
using System.Text;
using SalesSystem.Application.Interfaces.Services;

namespace SalesSystem.Infrastructure.Services;

/// <summary>
/// Secures and parses connection string parameters using Windows Data Protection API (DPAPI).
/// </summary>
public class ConnectionStringProtector : IConnectionStringProtector
{
    private const string Prefix = "DPAPI:";

    /// <inheritdoc />
    public bool IsEncrypted(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return false;

        return connectionString.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public string Protect(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return connectionString;

        if (IsEncrypted(connectionString))
            return connectionString;

        var clearBytes = Encoding.UTF8.GetBytes(connectionString);
        var encryptedBytes = ProtectedData.Protect(clearBytes, null, DataProtectionScope.LocalMachine);
        var base64 = Convert.ToBase64String(encryptedBytes);

        return Prefix + base64;
    }

    /// <inheritdoc />
    public string Unprotect(string encryptedConnectionString)
    {
        if (string.IsNullOrWhiteSpace(encryptedConnectionString))
            return encryptedConnectionString;

        if (!IsEncrypted(encryptedConnectionString))
            return encryptedConnectionString;

        var cipherText = encryptedConnectionString.Substring(Prefix.Length);
        var encryptedBytes = Convert.FromBase64String(cipherText);
        var decryptedBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.LocalMachine);
        
        return Encoding.UTF8.GetString(decryptedBytes);
    }
}
