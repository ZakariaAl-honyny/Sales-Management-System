using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using SalesSystem.Infrastructure.Data;
using SalesSystem.Infrastructure.Security;

namespace SalesSystem.Infrastructure;

public class SalesDbContextFactory : IDesignTimeDbContextFactory<SalesDbContext>
{
    public SalesDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SalesDbContext>();

        var connectionString = Environment.GetEnvironmentVariable("SALESSYSTEM_DB_CONNECTION");

        if (string.IsNullOrEmpty(connectionString))
        {
            connectionString = "Server=.;Database=SalesSystemDb;Trusted_Connection=True;TrustServerCertificate=True;";
        }

        optionsBuilder.UseSqlServer(connectionString);

        return new SalesDbContext(optionsBuilder.Options);
    }

    public static string GetDecryptedConnectionString(
        IConnectionStringProtector protector,
        IConfiguration configuration)
    {
        var rawValue = configuration.GetConnectionString("DefaultConnection")
            ?? Environment.GetEnvironmentVariable("SALESSYSTEM_DB_CONNECTION")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' not found in configuration or environment");

        return protector.Decrypt(rawValue);
    }
}
