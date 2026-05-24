namespace SalesSystem.Application.Interfaces.Services;

/// <summary>
/// Service interface for securing and parsing connection string parameters.
/// </summary>
public interface IConnectionStringProtector
{
    /// <summary>
    /// Checks if the connection string is already encrypted.
    /// </summary>
    bool IsEncrypted(string connectionString);

    /// <summary>
    /// Encrypts the plain connection string using machine-bound DPAPI.
    /// </summary>
    string Protect(string connectionString);

    /// <summary>
    /// Decrypts the encrypted connection string.
    /// </summary>
    string Unprotect(string encryptedConnectionString);
}
