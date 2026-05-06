using Microsoft.EntityFrameworkCore;
using SalesSystem.Infrastructure.Data;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Common;

var builder = WebApplication.CreateBuilder(args);

// Read connection string from environment variable
var connectionString = Environment.GetEnvironmentVariable("SALESSYSTEM_DB_CONNECTION")
    ?? "Server=.;Database=SalesSystemDb;Trusted_Connection=true;TrustServerCertificate=true;";

// Add DbContext with retry
builder.Services.AddDbContext<SalesDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5, 
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: new[] { 2, 233, 233 }
        );
        sqlOptions.CommandTimeout(30);
    }));

// Add services
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHttpClient();

var app = builder.Build();

// Initialize database and seed data
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<SalesDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    await InitializeDatabaseAsync(dbContext, logger);
}

async Task InitializeDatabaseAsync(SalesDbContext db, ILogger logger)
{
    try
    {
        // Check if database already exists and has tables
        var databaseExists = await db.Database.CanConnectAsync();
        
        if (!databaseExists)
        {
            // First time - create database using migrations
            await db.Database.MigrateAsync();
            logger.LogInformation("Database created and migrated.");
        }
        else
        {
            // Check if already seeded
            var alreadySeeded = await CheckIfSeededAsync(db);
            if (alreadySeeded)
            {
                logger.LogInformation("Database already initialized. Skipping seed...");
                return;
            }
            
            // Try to run pending migrations instead of EnsureCreated
            try
            {
                await db.Database.MigrateAsync();
            }
            catch (Exception migrateEx)
            {
                // If migrate fails (tables exist), it's OK - just continue
                logger.LogWarning(migrateEx, "Migration note: {Message}", migrateEx.Message);
            }
        }
        
        // Seed data
        await SeedDataAsync(db, logger);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database initialization error: {Message}", ex.Message);
    }
}

async Task<bool> CheckIfSeededAsync(SalesDbContext db)
{
    try
    {
        var count = await db.Users.CountAsync();
        return count > 0;
    }
    catch
    {
        return false;
    }
}

async Task SeedDataAsync(SalesDbContext db, ILogger logger)
{
    // Check if already seeded
    try
    {
        var existingUser = await db.Users.FindAsync(1);
        if (existingUser != null)
        {
            logger.LogInformation("Database already seeded. Skipping...");
            return;
        }
    }
    catch 
    {
        // Table might not exist
    }

    try
    {
        // Seed admin user (password: admin123)
        var adminUser = User.Create(
            userName: "admin",
            passwordHash: "$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewdBPj/RK.s5uN7m",
            fullName: "Administrator",
            role: UserRole.Admin,
            createdBy: "System"
        );
        db.Users.Add(adminUser);

        // Seed default warehouse
        var warehouse = Warehouse.Create(
            name: "المخزن الرئيسي",
            code: "WH-001",
            location: null,
            isDefault: true,
            createdBy: "System"
        );
        db.Warehouses.Add(warehouse);

        // Seed cash customer
        var cashCustomer = Customer.Create(
            name: "عميل نقدي",
            code: "CASH",
            openingBalance: 0,
            createdBy: "System"
        );
        db.Customers.Add(cashCustomer);

        // Seed 5 units
        db.Units.Add(Unit.Create("قطعة", "pcs", "System"));
        db.Units.Add(Unit.Create("كيلو", "kg", "System"));
        db.Units.Add(Unit.Create("لتر", "ltr", "System"));
        db.Units.Add(Unit.Create("متر", "m", "System"));
        db.Units.Add(Unit.Create("صندوق", "box", "System"));

        // Seed document sequences
        db.DocumentSequences.Add(DocumentSequence.Create("INV", "INV", 2026));
        db.DocumentSequences.Add(DocumentSequence.Create("PUR", "PUR", 2026));
        db.DocumentSequences.Add(DocumentSequence.Create("SR", "SR", 2026));
        db.DocumentSequences.Add(DocumentSequence.Create("PR", "PR", 2026));
        db.DocumentSequences.Add(DocumentSequence.Create("TRF", "TRF", 2026));
        db.DocumentSequences.Add(DocumentSequence.Create("CP", "CP", 2026));
        db.DocumentSequences.Add(DocumentSequence.Create("SP", "SP", 2026));

        await db.SaveChangesAsync();
        logger.LogInformation("Seed data completed successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error seeding data: {Message}", ex.Message);
    }
}

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Health check
app.MapGet("/api/health", () => new { Status = "OK", Timestamp = DateTime.UtcNow })
    .WithName("HealthCheck");

app.MapControllers();

app.Run();