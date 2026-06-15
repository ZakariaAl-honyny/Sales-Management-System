using System.Text;
using System.Threading.RateLimiting;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SalesSystem.Api.Middleware;
using SalesSystem.Api.Validators.Reports;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Repositories;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Application.Accounting.Services;
using SalesSystem.Application.Printing;
using SalesSystem.Application.Printing.Contracts;
using SalesSystem.Application.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Infrastructure;
using SalesSystem.Infrastructure.Backup;
using SalesSystem.Infrastructure.Data;
using SalesSystem.Infrastructure.Persistence;
using SalesSystem.Infrastructure.Printing;
using SalesSystem.Infrastructure.Repositories;
using SalesSystem.Infrastructure.Security;
using SalesSystem.Infrastructure.Services;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Common;
using Serilog;
using Scalar.AspNetCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SalesSystem.Contracts.Requests;
using SalesSystem.Api.Validators;
using SalesSystem.Api.Validators.Accounting;
using SalesSystem.Api.Validators.Transfers;


var builder = WebApplication.CreateBuilder(args);

// ============================================
// 0. Windows Service + Event Log
// ============================================
builder.Host.UseWindowsService(options =>
{
    options.ServiceName = "SalesSystemService";
});

builder.Host.UseSerilog((context, config) =>
{
    config.ReadFrom.Configuration(context.Configuration)
        .WriteTo.File("logs/salessystem-.log", rollingInterval: RollingInterval.Day)
        .WriteTo.EventLog("SalesSystemService", manageEventSource: true);
});

// ============================================
// 0b. Code Pages Encoding (for Arabic thermal printing - Windows-1256)
// ============================================
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

// ============================================
// 1. Data Protection (DPAPI)
// ============================================
var keyDir = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
    "SalesSystem", "DataProtectionKeys");
Directory.CreateDirectory(keyDir);

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keyDir))
    .ProtectKeysWithDpapi();

// ============================================
// 2. Read Configuration
// ============================================
var jwtSecret = Environment.GetEnvironmentVariable("SALESSYSTEM_JWT_SECRET")
    ?? (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development"
        ? "ThisIsASecretKeyThatIsLongEnoughForHS256Algorithm!"
        : throw new InvalidOperationException("SALESSYSTEM_JWT_SECRET is required in production"));

if (string.IsNullOrEmpty(jwtSecret) || jwtSecret.Length < 32)
    throw new InvalidOperationException("JWT secret must be at least 32 characters");

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
// 3. DbContext with Secure Factory and retry on failure
// ============================================
builder.Services.AddDbContext<SalesDbContext>((serviceProvider, options) =>
{
    var factory = serviceProvider.GetRequiredService<SecureDbContextFactory>();
    var connString = factory.GetDecryptedConnectionString();
    options.UseSqlServer(connString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: new[] { 2, 233, 233 }
        );
        sqlOptions.CommandTimeout(30);
    });
});

// ============================================
// 3b. Security & Backup DI Registrations
// ============================================
builder.Services.AddSingleton<IConnectionStringProtector, ConnectionStringProtector>();
builder.Services.AddScoped<FirstRunSetupService>();
builder.Services.AddSingleton<SecureDbContextFactory>();
builder.Services.Configure<BackupSettings>(builder.Configuration.GetSection(BackupSettings.SectionName));
builder.Services.AddScoped<IBackupService, BackupService>();
builder.Services.AddHostedService<ScheduledBackupWorker>();
builder.Services.AddHostedService<MinStockAlertWorker>();
builder.Services.AddUpdateServices(builder.Configuration);

// ============================================
// 4. DI Registrations
// ============================================
builder.Services.AddMemoryCache(); // Required for SystemSettingsRepository caching
builder.Services.AddInfrastructureServices(); // Registers ILocalImageStorageService, etc.
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IProductImportService, ProductImportService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();

builder.Services.AddScoped<ISupplierService, SupplierService>();
builder.Services.AddScoped<IWarehouseService, WarehouseService>();
builder.Services.AddScoped<IDocumentSequenceService, DocumentSequenceService>();
builder.Services.AddScoped<IAccountCategoryService, AccountCategoryService>();
builder.Services.AddScoped<ICompanySettingsService, CompanySettingsService>();
builder.Services.AddScoped<ITaxService, TaxService>();
builder.Services.AddScoped<ICurrencyService, CurrencyService>();
builder.Services.AddScoped<IUnitService, UnitService>();
builder.Services.AddScoped<IBarcodeLookupService, BarcodeLookupService>();
builder.Services.AddScoped<IProductPriceService, ProductPriceService>();
builder.Services.AddScoped<ISalesService, SalesService>();
builder.Services.AddScoped<IPurchaseService, PurchaseService>();
builder.Services.AddScoped<ISalesReturnService, SalesReturnService>();
builder.Services.AddScoped<IPurchaseReturnService, PurchaseReturnService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
// REMOVED: InventoryWriteOffService (Phase 26 — deferred to V2)
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IStoreSettingsService, StoreSettingsService>();
builder.Services.AddScoped<IReportRepository, ReportRepository>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IFinancialReportService, FinancialReportService>();
builder.Services.AddScoped<ISalesReportService, SalesReportService>();
builder.Services.AddScoped<IPurchaseReportService, PurchaseReportService>();
builder.Services.AddScoped<ICashBoxReportService, CashBoxReportService>();
builder.Services.AddScoped<IUserReportService, UserReportService>();
// ReportExportService is in Infrastructure (uses QuestPDF + ClosedXML)
builder.Services.AddScoped<IReportExportService, SalesSystem.Infrastructure.Services.ReportExportService>();
builder.Services.AddScoped<ISystemSettingsRepository, SystemSettingsRepository>();
builder.Services.AddScoped<IUpdateProductPricingService, UpdateProductPricingService>();
builder.Services.AddScoped<IProductUnitService, ProductUnitService>();
builder.Services.AddScoped<IProductCostService, ProductCostService>();
builder.Services.AddScoped<ICashBoxService, CashBoxService>();
builder.Services.AddScoped<IPrintService, PrintService>();
builder.Services.AddScoped<IPrintDataService, PrintDataService>();
builder.Services.AddScoped<InvoicePrintDtoBuilder>();
builder.Services.AddScoped<ILogService, LogService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IInventoryBatchService, InventoryBatchService>();
builder.Services.AddScoped<IFifoAllocationService, FifoAllocationService>();
builder.Services.AddScoped<IProductImageService, ProductImageService>();
builder.Services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
builder.Services.AddScoped<IAdditionalFeeService, AdditionalFeeService>();
// REMOVED: ProductImageService (implementation not yet created)
// REMOVED: AssemblyService (BillOfMaterials deferred to V2)

// ─── Accounting Services ────────────────────────────────────
builder.Services.AddScoped<IJournalEntryService, JournalEntryService>();
builder.Services.AddScoped<ISystemAccountService, SystemAccountService>();
builder.Services.AddScoped<IJournalEntryNumberGenerator, JournalEntryNumberGenerator>();
builder.Services.AddScoped<IAnnualClosingService, AnnualClosingService>();
builder.Services.AddScoped<IAccountService, AccountService>();
        builder.Services.AddScoped<IAccountingIntegrationService, AccountingIntegrationService>();
builder.Services.AddScoped<IFiscalYearService, FiscalYearService>();

// ─── New Entity Services (v4.7+) ──────────────────────────────
builder.Services.AddScoped<IPartyService, PartyService>();
builder.Services.AddScoped<IAttachmentService, AttachmentService>();
builder.Services.AddScoped<IBranchService, BranchService>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<IBankService, BankService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IProductCategoryService, ProductCategoryService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IInventoryCountService, InventoryCountService>();
builder.Services.AddScoped<IInventoryAdjustmentService, InventoryAdjustmentService>();
builder.Services.AddScoped<IExpenseService, ExpenseService>();
builder.Services.AddScoped<ICustomerReceiptService, CustomerReceiptService>();
builder.Services.AddScoped<ISupplierPaymentApplicationService, SupplierPaymentApplicationService>();
builder.Services.AddScoped<ISupplierPaymentService, SupplierPaymentService>();

// ─── Customer/Supplier Contact Services ──────────────
builder.Services.AddScoped<ICustomerContactService, CustomerContactService>();
builder.Services.AddScoped<ISupplierContactService, SupplierContactService>();

// ─── Receipt, Payment Voucher & Transfer Services ────────────
builder.Services.AddScoped<IReceiptVoucherService, ReceiptVoucherService>();
builder.Services.AddScoped<IPaymentVoucherService, PaymentVoucherService>();
builder.Services.AddScoped<IWarehouseTransferService, WarehouseTransferService>();

// ─── Cheque Service (Phase 29) ───────────────────────────────
builder.Services.AddScoped<IChequeService, ChequeService>();

builder.Services.AddSingleton(jwtSettings);

// ============================================
// 4b. Health Checks
// ============================================
builder.Services.AddHealthChecks()
    .AddCheck<SalesSystem.Infrastructure.Health.DatabaseHealthCheck>("database");

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
// 7. Other Services
// ============================================
// CORS — Restrict to localhost for security
builder.Services.AddCors(options =>
{
    options.AddPolicy("DesktopOnly", policy =>
        policy.WithOrigins("http://localhost:5221", "http://localhost:5222")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

builder.Services.AddControllers();

// Layer 6: Rate Limiting — brute-force protection
builder.Services.AddRateLimiter(options =>
{
    // Global: 100 requests per minute per IP
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
        context => RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    // Strict policy for login endpoint: 5 attempts per 15 minutes
    options.AddPolicy("LoginPolicy", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(15),
                QueueLimit = 0
            }));

    // Arabic response when rate limit exceeded
    options.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        context.HttpContext.Response.ContentType = "application/json";

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter =
                retryAfter.TotalSeconds.ToString("0");
        }

        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            Error = "تم تجاوز الحد المسموح من الطلبات. حاول مجدداً بعد قليل",
            Code = "RATE_LIMIT_EXCEEDED"
        }, ct);
    };
});

builder.Services.AddHttpClient();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Explicit validator registrations (redundant with auto-discovery, but ensure DI clarity)
builder.Services.AddScoped<IValidator<CreateCustomerRequest>, CreateCustomerRequestValidator>();
builder.Services.AddScoped<IValidator<UpdateCustomerRequest>, UpdateCustomerRequestValidator>();

// REMOVED: BillOfMaterials/Assembly validators (deferred to V2)
builder.Services.AddScoped<IValidator<UpdateAllocationsRequest>, UpdateAllocationsRequestValidator>();
builder.Services.AddScoped<IValidator<CreateReceiptVoucherRequest>, CreateReceiptVoucherRequestValidator>();
builder.Services.AddScoped<IValidator<UpdateReceiptVoucherRequest>, UpdateReceiptVoucherRequestValidator>();
builder.Services.AddScoped<IValidator<CreatePaymentVoucherRequest>, CreatePaymentVoucherRequestValidator>();
builder.Services.AddScoped<IValidator<UpdatePaymentVoucherRequest>, UpdatePaymentVoucherRequestValidator>();
builder.Services.AddScoped<IValidator<CreateSystemAccountMappingRequest>, CreateSystemAccountMappingRequestValidator>();
builder.Services.AddScoped<IValidator<UpdateSystemAccountMappingRequest>, UpdateSystemAccountMappingRequestValidator>();
builder.Services.AddScoped<IValidator<UpdateCompanySettingsRequest>, UpdateCompanySettingsRequestValidator>();
builder.Services.AddScoped<IValidator<CreateFiscalYearRequest>, CreateFiscalYearRequestValidator>();
builder.Services.AddScoped<IValidator<CreateWarehouseTransferRequest>, CreateWarehouseTransferValidator>();
builder.Services.AddScoped<IValidator<ReportDateRangeRequest>, ReportDateRangeValidator>();
builder.Services.AddScoped<IValidator<CreateInventoryTransactionRequest>, CreateInventoryTransactionRequestValidator>();

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
// Initialize QuestPDF license (must be done before any PDF generation)
PrintingBootstrapper.Initialize();

// Build Application

var app = builder.Build();

// ============================================
// 8. Database Initialization, Seed & First-Run Encryption
// ============================================
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<SalesDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Program>>();
    await InitializeDatabaseAsync(dbContext, logger);

    // First-run: encrypt connection string if plaintext
    var firstRun = scope.ServiceProvider.GetRequiredService<FirstRunSetupService>();
    firstRun.EnsureConnectionStringEncrypted(app.Configuration);
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
            try
            {
                await db.Database.MigrateAsync();
            }
            catch (Exception migrateEx)
            {
                logger.LogWarning(migrateEx, "Migration note: {Message}", migrateEx.Message);
            }
        }

        await DbSeeder.SeedAsync(db, logger);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database initialization error: {Message}", ex.Message);
    }
}

// ============================================
// 9. Middleware Pipeline
// ============================================
app.UseMiddleware<ExceptionMiddleware>();

app.UseCors("DesktopOnly");

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.UseHttpsRedirection();



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
        options.AddPreferredSecuritySchemes("Bearer");
    });
}

app.Run();
