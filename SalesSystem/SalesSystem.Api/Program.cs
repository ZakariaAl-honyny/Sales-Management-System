using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SalesSystem.Api.Middleware;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Repositories;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Application.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Infrastructure.Data;
using SalesSystem.Infrastructure.Repositories;
using SalesSystem.Infrastructure.Services;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Common;
using Serilog;
using Scalar.AspNetCore;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;


var builder = WebApplication.CreateBuilder(args);

// ============================================
// 1. Serilog Configuration
// ============================================
Log.Logger = new LoggerConfiguration()
    .WriteTo.File("logs/salessystem-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// ============================================
// 2. Read Configuration
// ============================================
var connectionString = Environment.GetEnvironmentVariable("SALESSYSTEM_DB_CONNECTION")
    ?? "Server=.;Database=SalesSystemDb;Trusted_Connection=true;TrustServerCertificate=true;";

var jwtSecret = Environment.GetEnvironmentVariable("SALESSYSTEM_JWT_SECRET")
    ?? "ThisIsASecretKeyThatIsLongEnoughForHS256Algorithm!";
var jwtIssuer = Environment.GetEnvironmentVariable("SALESSYSTEM_JWT_ISSUER") ?? "SalesSystem";
var jwtAudience = Environment.GetEnvironmentVariable("SALESSYSTEM_JWT_AUDIENCE") ?? "SalesSystem";
var jwtExpirationHours = int.TryParse(Environment.GetEnvironmentVariable("SALESSYSTEM_JWT_EXPIRATION_HOURS"), out var hours) ? hours : 8;

var jwtSettings = new JwtSettings
{
    Secret = jwtSecret,
    Issuer = jwtIssuer,
    Audience = jwtAudience,
    ExpirationHours = jwtExpirationHours
};

// ============================================
// 3. DbContext
// ============================================
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

// ============================================
// 4. DI Registrations
// ============================================
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<ISupplierService, SupplierService>();
builder.Services.AddScoped<IWarehouseService, WarehouseService>();
builder.Services.AddScoped<IDocumentSequenceService, DocumentSequenceService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IUnitService, UnitService>();
builder.Services.AddScoped<ISalesService, SalesService>();
builder.Services.AddScoped<IPurchaseService, PurchaseService>();
builder.Services.AddScoped<ISalesReturnService, SalesReturnService>();
builder.Services.AddScoped<IPurchaseReturnService, PurchaseReturnService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IStoreSettingsService, StoreSettingsService>();
builder.Services.AddScoped<IReportRepository, ReportRepository>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<ISystemSettingsRepository, SystemSettingsRepository>();
builder.Services.AddScoped<IUpdateProductPricingService, UpdateProductPricingService>();
builder.Services.AddSingleton(jwtSettings);

// ============================================
// 5. JWT Authentication
// ============================================
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

// ============================================
// 6. Authorization Policies
// ============================================
builder.Services.AddAuthorization(opts =>
{
    opts.AddPolicy("AdminOnly", p => p.RequireRole("1"));
    opts.AddPolicy("ManagerAndAbove", p => p.RequireRole("1", "2"));
    opts.AddPolicy("AllStaff", p => p.RequireRole("1", "2", "3"));
});

// ============================================
// 8. Other Services
// ============================================
builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, ct) =>
    {
        var scheme = new Microsoft.OpenApi.OpenApiSecurityScheme
        {
            Type = Microsoft.OpenApi.SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Name = "Authorization",
            In = Microsoft.OpenApi.ParameterLocation.Header,
            Description = "Enter JWT token"
        };

        document.Components ??= new Microsoft.OpenApi.OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, Microsoft.OpenApi.IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = scheme;

        document.Security ??= new List<Microsoft.OpenApi.OpenApiSecurityRequirement>();
        var requirement = new Microsoft.OpenApi.OpenApiSecurityRequirement();
        requirement.Add(new Microsoft.OpenApi.OpenApiSecuritySchemeReference("Bearer", document), new List<string>());
        document.Security.Add(requirement);

        return Task.CompletedTask;
    });
});



// ============================================
// Build Application
// ============================================
var app = builder.Build();

// ============================================
// 8. Database Initialization & Seed
// ============================================
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<SalesDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Program>>();
    await InitializeDatabaseAsync(dbContext, logger);
}

async Task InitializeDatabaseAsync(SalesDbContext db, Microsoft.Extensions.Logging.ILogger logger)
{
    try
    {
        var databaseExists = await db.Database.CanConnectAsync();

        if (!databaseExists)
        {
            await db.Database.MigrateAsync();
            logger.LogInformation("Database created and migrated.");
        }
        else
        {
            var alreadySeeded = await CheckIfSeededAsync(db);
            if (alreadySeeded)
            {
                logger.LogInformation("Database already initialized. Skipping seed...");
                return;
            }

            try
            {
                await db.Database.MigrateAsync();
            }
            catch (Exception migrateEx)
            {
                logger.LogWarning(migrateEx, "Migration note: {Message}", migrateEx.Message);
            }
        }

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

async Task SeedDataAsync(SalesDbContext db, Microsoft.Extensions.Logging.ILogger logger)
{
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
        // Seed admin user (password: admin123) - CreatedByUserId = null for system seed
        var adminUser = User.Create(
            userName: "admin",
            passwordHash: BCrypt.Net.BCrypt.HashPassword("admin123", workFactor: 12),
            fullName: "Administrator",
            role: UserRole.Admin,
            createdByUserId: null
        );
        db.Users.Add(adminUser);

        // Seed default warehouse - CreatedByUserId = null for system seed
        var warehouse = Warehouse.Create(
            name: "المخزن الرئيسي",
            code: "WH-001",
            location: null,
            isDefault: true,
            createdByUserId: null
        );
        db.Warehouses.Add(warehouse);

        // Seed cash customer - CreatedByUserId = null for system seed
        var cashCustomer = Customer.Create(
            name: "عميل نقدي",
            code: "CASH",
            openingBalance: 0,
            createdByUserId: null
        );
        db.Customers.Add(cashCustomer);

        // Seed 5 units - CreatedByUserId = null for system seed
        db.Units.Add(Unit.Create("قطعة", "pcs", null));
        db.Units.Add(Unit.Create("كيلو", "kg", null));
        db.Units.Add(Unit.Create("لتر", "ltr", null));
        db.Units.Add(Unit.Create("متر", "m", null));
        db.Units.Add(Unit.Create("صندوق", "box", null));

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

// ============================================
// 9. Middleware Pipeline
// ============================================
app.UseMiddleware<ExceptionMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.UseHttpsRedirection();

// Health check
app.MapGet("/api/v1/health", () => new { Status = "OK", Version = "1.0", Timestamp = DateTime.UtcNow })
    .WithName("HealthCheck");

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithOpenApiRoutePattern("/openapi/v1.json");
        options.WithTitle("Sales Management API");
        options.WithTheme(ScalarTheme.Mars);
        options.WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
        options.AddPreferredSecuritySchemes("Bearer"); // Auto-select JWT scheme
    });
}

app.Run();