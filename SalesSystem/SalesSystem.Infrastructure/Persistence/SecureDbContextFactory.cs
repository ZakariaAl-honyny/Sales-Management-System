using Microsoft.Extensions.Configuration;
using SalesSystem.Infrastructure.Security;

namespace SalesSystem.Infrastructure.Persistence;

public sealed class SecureDbContextFactory
{
    private readonly IConnectionStringProtector _protector;
    private readonly IConfiguration _configuration;

    public SecureDbContextFactory(
        IConnectionStringProtector protector,
        IConfiguration configuration)
    {
        _protector = protector;
        _configuration = configuration;
    }

    public string GetDecryptedConnectionString()
    {
        var rawValue = _configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' not found in configuration");

        return _protector.Decrypt(rawValue);
    }
}
