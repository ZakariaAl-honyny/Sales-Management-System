using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SalesSystem.Infrastructure.Data;

namespace SalesSystem.Infrastructure;

public class SalesDbContextFactory : IDesignTimeDbContextFactory<SalesDbContext>
{
    public SalesDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SalesDbContext>();

        var connectionString = Environment.GetEnvironmentVariable("SALESSYSTEM_DB_CONNECTION");

        if (string.IsNullOrEmpty(connectionString))
        {
            connectionString = "Server=.;Database=SalesSystemDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";
        }

        optionsBuilder.UseSqlServer(connectionString);

        return new SalesDbContext(optionsBuilder.Options);
    }
}
