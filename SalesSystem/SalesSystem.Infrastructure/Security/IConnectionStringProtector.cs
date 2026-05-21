namespace SalesSystem.Infrastructure.Security;

public interface IConnectionStringProtector
{
    string Encrypt(string plainConnectionString);
    string Decrypt(string encryptedConnectionString);
    bool IsEncrypted(string value);
}
