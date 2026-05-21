using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SalesSystem.Infrastructure.Data;

namespace SalesSystem.Infrastructure;

public class SalesDbContextFactory : IDesignTimeDbContextFactory<SalesDbContext>
{
    public SalesDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SalesDbContext>();
        
        // Get connection string from environment variable (as per constitution RULE-040)
        var connectionString = Environment.GetEnvironmentVariable("SALESSYSTEM_DB_CONNECTION");
        
        if (string.IsNullOrEmpty(connectionString))
        {
            // Default for local development if variable is missing during design time
            connectionString = "Server=.;Database=SalesSystemDb;Trusted_Connection=True;TrustServerCertificate=True;";
        }

        optionsBuilder.UseSqlServer(connectionString);

        return new SalesDbContext(optionsBuilder.Options);
    }
}
