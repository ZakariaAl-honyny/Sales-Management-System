using Microsoft.AspNetCore.DataProtection;

namespace SalesSystem.Infrastructure.Security;

public sealed class ConnectionStringProtector : IConnectionStringProtector
{
    private const string Purpose = "SalesSystem.ConnectionString.v1";
    private const string Prefix = "DPAPI:";
    private readonly IDataProtector _protector;

    public ConnectionStringProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector(Purpose);
    }

    public string Encrypt(string plainConnectionString)
    {
        if (string.IsNullOrWhiteSpace(plainConnectionString))
            throw new ArgumentException("Connection string cannot be empty");

        if (IsEncrypted(plainConnectionString))
            return plainConnectionString;

        var encrypted = _protector.Protect(plainConnectionString);
        return $"{Prefix}{encrypted}";
    }

    public string Decrypt(string encryptedConnectionString)
    {
        if (string.IsNullOrWhiteSpace(encryptedConnectionString))
            throw new ArgumentException("Encrypted value cannot be empty");

        if (!IsEncrypted(encryptedConnectionString))
            return encryptedConnectionString;

        var encryptedPart = encryptedConnectionString[Prefix.Length..];
        return _protector.Unprotect(encryptedPart);
    }

    public bool IsEncrypted(string value)
        => value.StartsWith(Prefix, StringComparison.Ordinal);
}
