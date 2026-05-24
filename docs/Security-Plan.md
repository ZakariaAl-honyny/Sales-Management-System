Implementation Plan: 7-Layer Security Architecture
📋 Master Rules for AI Agent
Security layers are cumulative. Each layer assumes the previous layer is working. Never skip a layer. Never duplicate logic across layers.

🗺️ Architecture Overview
text

┌─────────────────────────────────────────────────────────────────┐
│                  REQUEST LIFECYCLE                               │
│                                                                 │
│  Client Request                                                 │
│       │                                                         │
│       ▼                                                         │
│  ┌─────────────┐  Layer 1: HTTPS + CORS                        │
│  │ TLS/CORS    │  ← Reject unencrypted or unauthorized origins  │
│  └──────┬──────┘                                                │
│         │                                                       │
│  ┌──────▼──────┐  Layer 2: JWT Authentication                  │
│  │ Auth Middle │  ← Reject missing/invalid/expired tokens       │
│  └──────┬──────┘                                                │
│         │                                                       │
│  ┌──────▼──────┐  Layer 3: Role Authorization                  │
│  │ Role Guard  │  ← Reject wrong role (Admin vs Cashier)        │
│  └──────┬──────┘                                                │
│         │                                                       │
│  ┌──────▼──────┐  Layer 4: Ownership Policy                    │
│  │ Ownership   │  ← Reject access to other users' data         │
│  └──────┬──────┘                                                │
│         │                                                       │
│  ┌──────▼──────┐  Layer 5: Token Refresh & Revocation          │
│  │ Token Mgmt  │  ← Handle refresh/logout/rotation             │
│  └──────┬──────┘                                                │
│         │                                                       │
│  ┌──────▼──────┐  Layer 6: Rate Limiting                       │
│  │ Rate Limiter│  ← Block brute-force and abuse                 │
│  └──────┬──────┘                                                │
│         │                                                       │
│  ┌──────▼──────┐  Layer 7: Audit Logging                       │
│  │ Audit Log   │  ← Record everything for forensics            │
│  └──────┬──────┘                                                │
│         │                                                       │
│  ✅ Handler / Controller                                        │
└─────────────────────────────────────────────────────────────────┘

## 🚦 Security Implementation Status (v4.6.4)

| Layer | Feature | Status | Notes |
|-------|---------|--------|-------|
| 1 | HTTPS | ✅ Implemented | `UseHttpsRedirection()` + HSTS in Program.cs |
| 1 | CORS | ✅ Implemented | Desktop-only origins (`localhost:5221`, `localhost:5222`) |
| 2 | JWT Authentication | ✅ Implemented | `SALESSYSTEM_JWT_SECRET` from env var, `ClockSkew = Zero` |
| 2 | BCrypt (work factor 12) | ✅ Implemented | In AuthService and UserService |
| 2 | Account Lockout | ❌ Not Implemented | Still planned |
| 2 | 500ms delay on failure | ❌ Not Implemented | Still planned |
| 2 | Refresh Tokens | ❌ Not Implemented | JWT-only, no refresh rotation |
| 3 | Role Authorization | ✅ Implemented | `AdminOnly`, `ManagerAndAbove`, `AllStaff` policies |
| 3 | FallbackPolicy | ✅ Implemented | All endpoints require auth |
| 4 | Ownership Guard | ❌ Not Implemented | Not applicable (single-tenant desktop app) |
| 5 | Refresh Token Rotation | ❌ Not Implemented | Not implemented |
| 5 | Token Reuse Detection | ❌ Not Implemented | Not implemented |
| 5 | `RefreshTokens` table | ❌ Not Implemented | Not in database |
| 6 | **Rate Limiting** | ✅ **v4.6.4** | Login: 5/15min, Global: 100/min |
| 6 | Account Lockout | ❌ Not Implemented | Planned for future |
| 7 | Audit Middleware | ❌ Not Implemented | Serilog file logging used instead |
| 7 | `AuditLogs` table | ❌ Not Implemented | Not in database |
| 7 | `ProductPriceHistory` | ✅ Implemented | Price change audit trail |
| — | Soft Delete (Users) | ✅ **v4.6.4** | `PermanentDeleteAsync` returns `Result.Failure` |
| — | Connection String Security | ✅ **v4.6.4** | Env var only; removed from `appsettings.Development.json` |
| — | FluentValidation | ✅ **v4.6.4** | All 7 validators enhanced with additional rules |

🗂️ Phase 0: Database Schema & Setup
Task 0.1 — Security Tables
SQL

-- =============================================
-- Run in order. No skipping.
-- =============================================

-- 1. Users table (if not exists)
CREATE TABLE Users (
    Id              INT PRIMARY KEY IDENTITY(1,1),
    Username        NVARCHAR(100) NOT NULL UNIQUE,
    PasswordHash    NVARCHAR(500) NOT NULL,    -- BCrypt hash ONLY
    FullName        NVARCHAR(200) NOT NULL,
    Role            NVARCHAR(50)  NOT NULL,    -- 'Admin', 'Cashier', 'Viewer'
    IsActive        BIT NOT NULL DEFAULT 1,
    LastLoginAt     DATETIME2 NULL,
    FailedLoginCount INT NOT NULL DEFAULT 0,
    LockoutUntil    DATETIME2 NULL,
    CreatedAt       DATETIME2 NOT NULL DEFAULT GETDATE()
);

-- 2. Refresh tokens
CREATE TABLE RefreshTokens (
    Id              INT PRIMARY KEY IDENTITY(1,1),
    UserId          INT NOT NULL,
    TokenHash       NVARCHAR(500) NOT NULL,    -- Hashed — never store plain
    ExpiresAt       DATETIME2 NOT NULL,
    IsRevoked       BIT NOT NULL DEFAULT 0,
    RevokedAt       DATETIME2 NULL,
    RevokedReason   NVARCHAR(100) NULL,        -- 'Logout', 'Rotated', 'Admin'
    CreatedAt       DATETIME2 NOT NULL DEFAULT GETDATE(),
    CreatedByIp     NVARCHAR(50) NULL,
    DeviceInfo      NVARCHAR(200) NULL,

    CONSTRAINT FK_RefreshTokens_Users
        FOREIGN KEY (UserId) REFERENCES Users(Id)
);

CREATE INDEX IX_RefreshTokens_TokenHash ON RefreshTokens(TokenHash);
CREATE INDEX IX_RefreshTokens_UserId    ON RefreshTokens(UserId);

-- 3. Audit log
CREATE TABLE AuditLogs (
    Id              BIGINT PRIMARY KEY IDENTITY(1,1),
    Timestamp       DATETIME2 NOT NULL DEFAULT GETDATE(),
    UserId          INT NULL,
    Username        NVARCHAR(100) NULL,
    Action          NVARCHAR(100) NOT NULL,    -- 'Login', 'CreateInvoice', etc.
    Resource        NVARCHAR(100) NULL,        -- 'Invoice', 'User', etc.
    ResourceId      NVARCHAR(50) NULL,
    OldValues       NVARCHAR(MAX) NULL,        -- JSON snapshot before change
    NewValues       NVARCHAR(MAX) NULL,        -- JSON snapshot after change
    IpAddress       NVARCHAR(50) NULL,
    UserAgent       NVARCHAR(500) NULL,
    StatusCode      INT NULL,
    IsSuccess       BIT NOT NULL DEFAULT 1,
    ErrorMessage    NVARCHAR(1000) NULL,
    DurationMs      INT NULL
);

CREATE INDEX IX_AuditLogs_UserId    ON AuditLogs(UserId);
CREATE INDEX IX_AuditLogs_Timestamp ON AuditLogs(Timestamp);
CREATE INDEX IX_AuditLogs_Action    ON AuditLogs(Action);

-- 4. Rate limit tracking (optional — can use in-memory cache instead)
CREATE TABLE RateLimitViolations (
    Id              INT PRIMARY KEY IDENTITY(1,1),
    IpAddress       NVARCHAR(50) NOT NULL,
    Endpoint        NVARCHAR(200) NOT NULL,
    ViolationCount  INT NOT NULL DEFAULT 1,
    FirstViolation  DATETIME2 NOT NULL DEFAULT GETDATE(),
    LastViolation   DATETIME2 NOT NULL DEFAULT GETDATE(),
    IsBlocked       BIT NOT NULL DEFAULT 0
);
Task 0.2 — NuGet Packages
XML

<!-- File: YourApp.API/YourApp.API.csproj -->
<ItemGroup>
  <!-- Authentication -->
  <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.0" />

  <!-- Password hashing -->
  <PackageReference Include="BCrypt.Net-Next" Version="4.0.3" />

  <!-- Rate limiting (built-in .NET 7+) -->
  <!-- No extra package needed for AspNetCore RateLimiting -->

  <!-- Audit/structured logging -->
  <PackageReference Include="Serilog.AspNetCore"            Version="8.0.0" />
  <PackageReference Include="Serilog.Sinks.MSSqlServer"     Version="6.7.0" />
  <PackageReference Include="Serilog.Sinks.File"            Version="5.0.0" />
  <PackageReference Include="Serilog.Enrichers.ClientInfo"  Version="2.1.0" />
</ItemGroup>
✅ Phase 0 Checklist
 All 4 tables created in order
 PasswordHash column is 500 chars (BCrypt output is ~60 chars, room to grow)
 TokenHash stored — never the raw token
 Audit log uses BIGINT (security logs grow very large)
 All NuGet packages installed
🟢 Layer 1: HTTPS + CORS
> **Status**: ✅ HTTPS + CORS implemented

Task 1.1 — HTTPS Enforcement
csharp

// File: API/Program.cs (add to pipeline — FIRST middleware)

var builder = WebApplication.CreateBuilder(args);

// ─── Force HTTPS ───────────────────────────────
builder.Services.AddHttpsRedirection(options =>
{
    options.RedirectStatusCode = StatusCodes.Status301MovedPermanently;
    options.HttpsPort = 443;
});

// HSTS — tells browsers to ONLY use HTTPS for this domain
builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = true;
    options.Preload = true;
});

var app = builder.Build();

// Order matters — HTTPS redirect must be FIRST
if (app.Environment.IsProduction())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
Task 1.2 — CORS Configuration
csharp

// File: API/Configuration/CorsConfiguration.cs

public static class CorsConfiguration
{
    public static IServiceCollection AddCorsSecurity(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var allowedOrigins = configuration
            .GetSection("Security:AllowedOrigins")
            .Get<string[]>()
            ?? Array.Empty<string>();

        services.AddCors(options =>
        {
            // ─── Production: Strict origin list ───────────
            options.AddPolicy("ProductionPolicy", policy =>
            {
                policy
                    .WithOrigins(allowedOrigins)   // Explicit list only
                    .WithMethods("GET", "POST", "PUT", "DELETE", "PATCH")
                    .WithHeaders(
                        "Authorization",
                        "Content-Type",
                        "X-Requested-With",
                        "X-Device-Id")
                    .AllowCredentials()
                    .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
            });

            // ─── Development: Permissive ───────────────────
            options.AddPolicy("DevelopmentPolicy", policy =>
            {
                policy
                    .AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
        });

        return services;
    }
}

// In Program.cs:
// app.UseCors(app.Environment.IsProduction()
//     ? "ProductionPolicy"
//     : "DevelopmentPolicy");
JSON

// File: appsettings.Production.json
{
  "Security": {
    "AllowedOrigins": [
      "https://sales.yourcompany.com",
      "https://app.yourcompany.com"
    ]
  }
}
✅ Layer 1 Checklist
 UseHttpsRedirection() is FIRST middleware
 HSTS only enabled in Production
 CORS uses explicit origin list in Production (never AllowAnyOrigin + AllowCredentials)
 Allowed methods list does NOT include OPTIONS (handled automatically)
 Allowed origins stored in config — never hardcoded
🟡 Layer 2: JWT Authentication
> **Status**: ⚠️ JWT + BCrypt implemented. Refresh tokens + lockout + 500ms delay not yet implemented

Task 2.1 — JWT Configuration
csharp

// File: API/Configuration/JwtConfiguration.cs

public record JwtSettings
{
    public string SecretKey { get; init; } = string.Empty;
    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public int AccessTokenExpiryMinutes { get; init; } = 15;    // Short lived
    public int RefreshTokenExpiryDays { get; init; } = 7;
}

public static class JwtConfiguration
{
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSettings = configuration
            .GetSection("Security:Jwt")
            .Get<JwtSettings>()
            ?? throw new InvalidOperationException("JWT settings missing");

        // Validate secret key strength
        if (jwtSettings.SecretKey.Length < 32)
            throw new InvalidOperationException(
                "JWT SecretKey must be at least 32 characters");

        services.Configure<JwtSettings>(
            configuration.GetSection("Security:Jwt"));

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme =
                    JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme =
                    JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        System.Text.Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),

                    ValidateIssuer   = true,
                    ValidIssuer      = jwtSettings.Issuer,

                    ValidateAudience = true,
                    ValidAudience    = jwtSettings.Audience,

                    ValidateLifetime      = true,
                    ClockSkew             = TimeSpan.FromSeconds(30), // Tight tolerance
                    RequireExpirationTime = true
                };

                // Return structured error responses
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        if (context.Exception is SecurityTokenExpiredException)
                        {
                            context.Response.Headers.Add(
                                "X-Token-Expired", "true");
                        }
                        return Task.CompletedTask;
                    },

                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = 401;
                        context.Response.ContentType = "application/json";

                        var response = new
                        {
                            Error = "غير مصرح بالوصول",
                            Code = "UNAUTHORIZED",
                            Detail = context.ErrorDescription
                        };

                        await context.Response.WriteAsJsonAsync(response);
                    }
                };
            });

        return services;
    }
}
Task 2.2 — Token Service
csharp

// File: Application/Security/TokenService.cs

public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    ClaimsPrincipal? ValidateAccessToken(string token);
}

public class TokenService : ITokenService
{
    private readonly JwtSettings _settings;

    public TokenService(IOptions<JwtSettings> settings)
    {
        _settings = settings.Value;
    }

    public string GenerateAccessToken(User user)
    {
        var key = new SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes(_settings.SecretKey));

        var claims = new List<Claim>
        {
            // Standard claims
            new(JwtRegisteredClaimNames.Sub,  user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti,  Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),

            // Custom claims
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name,           user.Username),
            new(ClaimTypes.Role,           user.Role),
            new("fullName",                user.FullName),
            new("branchId",                user.BranchId?.ToString() ?? "")
        };

        var token = new JwtSecurityToken(
            issuer:             _settings.Issuer,
            audience:           _settings.Audience,
            claims:             claims,
            notBefore:          DateTime.UtcNow,
            expires:            DateTime.UtcNow.AddMinutes(
                                    _settings.AccessTokenExpiryMinutes),
            signingCredentials: new SigningCredentials(
                                    key, SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        // Cryptographically secure random bytes
        var bytes = new byte[64];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    public ClaimsPrincipal? ValidateAccessToken(string token)
    {
        try
        {
            var key = new SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(_settings.SecretKey));

            var handler = new JwtSecurityTokenHandler();

            return handler.ValidateToken(token,
                new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey         = key,
                    ValidateIssuer           = true,
                    ValidIssuer              = _settings.Issuer,
                    ValidateAudience         = true,
                    ValidAudience            = _settings.Audience,
                    ValidateLifetime         = false, // We check expiry manually for refresh
                    ClockSkew                = TimeSpan.Zero
                },
                out _);
        }
        catch
        {
            return null;
        }
    }
}
Task 2.3 — Login Command Handler
csharp

// File: Application/Auth/Commands/LoginCommandHandler.cs

public record LoginCommand(
    string Username,
    string Password,
    string? IpAddress,
    string? DeviceInfo
) : IRequest<LoginResult>;

public record LoginResult(
    bool IsSuccess,
    string? AccessToken,
    string? RefreshToken,
    DateTime? AccessTokenExpiry,
    string? ErrorMessage
);

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResult>
{
    private readonly AppDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly IAuditService _auditService;
    private readonly ILogger<LoginCommandHandler> _logger;

    // Lockout after 5 failed attempts for 15 minutes
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public async Task<LoginResult> Handle(
        LoginCommand request,
        CancellationToken ct)
    {
        // ─── 1. Find user ──────────────────────────────
        var user = await _context.Users
            .FirstOrDefaultAsync(u =>
                u.Username == request.Username && u.IsActive, ct);

        if (user == null)
        {
            await _auditService.LogAsync(new AuditEntry
            {
                Action     = "LoginFailed",
                Resource   = "Auth",
                IsSuccess  = false,
                ErrorMessage = "User not found",
                IpAddress  = request.IpAddress,
                Username   = request.Username
            }, ct);

            // Deliberate delay — prevents username enumeration timing attacks
            await Task.Delay(500, ct);
            return LoginResult.Fail("اسم المستخدم أو كلمة المرور غير صحيحة");
        }

        // ─── 2. Check lockout ──────────────────────────
        if (user.LockoutUntil.HasValue && user.LockoutUntil > DateTime.UtcNow)
        {
            var remaining = (user.LockoutUntil.Value - DateTime.UtcNow).Minutes;
            return LoginResult.Fail(
                $"الحساب مقفل مؤقتاً. حاول مجدداً بعد {remaining} دقيقة.");
        }

        // ─── 3. Verify password ────────────────────────
        var passwordValid = BCrypt.Net.BCrypt.Verify(
            request.Password, user.PasswordHash);

        if (!passwordValid)
        {
            user.FailedLoginCount++;

            if (user.FailedLoginCount >= MaxFailedAttempts)
            {
                user.LockoutUntil = DateTime.UtcNow.Add(LockoutDuration);
                user.FailedLoginCount = 0;
                _logger.LogWarning(
                    "Account {Username} locked after {Count} failed attempts",
                    user.Username, MaxFailedAttempts);
            }

            await _context.SaveChangesAsync(ct);

            await _auditService.LogAsync(new AuditEntry
            {
                Action    = "LoginFailed",
                UserId    = user.Id,
                Username  = user.Username,
                IsSuccess = false,
                IpAddress = request.IpAddress,
                ErrorMessage = $"Invalid password. Attempt {user.FailedLoginCount}"
            }, ct);

            await Task.Delay(500, ct); // Constant time response
            return LoginResult.Fail("اسم المستخدم أو كلمة المرور غير صحيحة");
        }

        // ─── 4. Generate tokens ────────────────────────
        var accessToken  = _tokenService.GenerateAccessToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken();

        // Store hashed refresh token
        var tokenEntity = new RefreshToken
        {
            UserId        = user.Id,
            TokenHash     = HashToken(refreshToken),
            ExpiresAt     = DateTime.UtcNow.AddDays(7),
            CreatedByIp   = request.IpAddress,
            DeviceInfo    = request.DeviceInfo
        };

        _context.RefreshTokens.Add(tokenEntity);

        // Reset failed count on success
        user.FailedLoginCount = 0;
        user.LockoutUntil     = null;
        user.LastLoginAt      = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        await _auditService.LogAsync(new AuditEntry
        {
            Action    = "LoginSuccess",
            UserId    = user.Id,
            Username  = user.Username,
            IsSuccess = true,
            IpAddress = request.IpAddress
        }, ct);

        return new LoginResult(
            IsSuccess: true,
            AccessToken: accessToken,
            RefreshToken: refreshToken,
            AccessTokenExpiry: DateTime.UtcNow.AddMinutes(15),
            ErrorMessage: null);
    }

    private static string HashToken(string token)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(token);
        var hash  = System.Security.Cryptography.SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}

// Extension for clean result creation
public partial record LoginResult
{
    public static LoginResult Fail(string message)
        => new(false, null, null, null, message);
}
✅ Layer 2 Checklist
 Access token expires in 15 minutes (short-lived)
 Password verified with BCrypt (timing-safe comparison)
 Failed login counter increments in DB (survives restarts)
 Account locks after 5 failures for 15 minutes
 500ms delay on failed login (prevents timing attacks)
 Refresh token stored as SHA256 hash (never plain text)
 ClockSkew set to 30 seconds (tight, not 5 minutes default)
🟡 Layer 3: Role-Based Authorization
> **Status**: ✅ Role-based policies (`AdminOnly`, `ManagerAndAbove`, `AllStaff`) fully implemented

Task 3.1 — Role Constants & Policies
csharp

// File: SharedKernel/Security/Roles.cs
// ONE place for all role definitions

public static class Roles
{
    public const string Admin   = "Admin";
    public const string Cashier = "Cashier";
    public const string Viewer  = "Viewer";

    public static readonly string[] All = { Admin, Cashier, Viewer };
}

// File: SharedKernel/Security/Policies.cs
public static class Policies
{
    public const string AdminOnly       = "AdminOnly";
    public const string CanSell         = "CanSell";
    public const string CanPurchase     = "CanPurchase";
    public const string CanViewReports  = "CanViewReports";
    public const string CanManageUsers  = "CanManageUsers";
}
Task 3.2 — Policy Registration
csharp

// File: API/Configuration/AuthorizationConfiguration.cs

public static class AuthorizationConfiguration
{
    public static IServiceCollection AddRoleBasedAuthorization(
        this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            // ─── Role policies ─────────────────────────────
            options.AddPolicy(Policies.AdminOnly, policy =>
                policy.RequireRole(Roles.Admin));

            options.AddPolicy(Policies.CanSell, policy =>
                policy.RequireRole(Roles.Admin, Roles.Cashier));

            options.AddPolicy(Policies.CanPurchase, policy =>
                policy.RequireRole(Roles.Admin, Roles.Cashier));

            options.AddPolicy(Policies.CanViewReports, policy =>
                policy.RequireRole(Roles.Admin, Roles.Viewer));

            options.AddPolicy(Policies.CanManageUsers, policy =>
                policy.RequireRole(Roles.Admin));

            // ─── Global fallback: all endpoints require auth ─
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        return services;
    }
}
Task 3.3 — Controller Usage
csharp

// File: API/Controllers/InvoicesController.cs

[ApiController]
[Route("api/[controller]")]
[Authorize]  // Layer 2: Must be authenticated
public class InvoicesController : ControllerBase
{
    private readonly IMediator _mediator;

    [HttpGet]
    [Authorize(Policy = Policies.CanSell)]  // Layer 3: Must have sell permission
    public async Task<IActionResult> GetAll()
        => Ok(await _mediator.Send(new GetInvoicesQuery(CurrentUserId)));

    [HttpPost]
    [Authorize(Policy = Policies.CanSell)]
    public async Task<IActionResult> Create(CreateInvoiceCommand command)
        => Ok(await _mediator.Send(command));

    [HttpDelete("{id}")]
    [Authorize(Policy = Policies.AdminOnly)]  // Only admin can delete
    public async Task<IActionResult> Delete(int id)
        => Ok(await _mediator.Send(new DeleteInvoiceCommand(id)));

    private int CurrentUserId => int.Parse(
        User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
✅ Layer 3 Checklist
 All role names defined in Roles static class — never inline string
 All policy names defined in Policies static class — never inline string
 FallbackPolicy requires authentication for ALL endpoints
 Sensitive operations (Delete, Export) require AdminOnly
 No [AllowAnonymous] except on Login and Health endpoints
🟡 Layer 4: Ownership Policies
> **Status**: ⏸️ Ownership guard not applicable (single-tenant desktop application)

Task 4.1 — Current User Service
csharp

// File: Application/Security/ICurrentUserService.cs

public interface ICurrentUserService
{
    int UserId { get; }
    string Username { get; }
    string Role { get; }
    int? BranchId { get; }
    bool IsAdmin { get; }
    bool IsAuthenticated { get; }
}

// File: API/Services/CurrentUserService.cs
// Reads from HttpContext — lives in API layer only

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContext;

    public CurrentUserService(IHttpContextAccessor httpContext)
    {
        _httpContext = httpContext;
    }

    private ClaimsPrincipal? User
        => _httpContext.HttpContext?.User;

    public int UserId => int.Parse(
        User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

    public string Username
        => User?.FindFirstValue(ClaimTypes.Name) ?? string.Empty;

    public string Role
        => User?.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

    public int? BranchId
    {
        get
        {
            var val = User?.FindFirstValue("branchId");
            return int.TryParse(val, out var id) ? id : null;
        }
    }

    public bool IsAdmin => Role == Roles.Admin;

    public bool IsAuthenticated
        => User?.Identity?.IsAuthenticated == true;
}
Task 4.2 — Ownership Enforcement in Handlers
csharp

// File: Application/Security/OwnershipGuard.cs
// Centralized ownership check — used by ALL query handlers

public static class OwnershipGuard
{
    /// <summary>
    /// Throws if the resource doesn't belong to the requesting user
    /// AND the user is not an Admin.
    /// Call this in EVERY handler that returns user-specific data.
    /// </summary>
    public static void Enforce(
        int resourceOwnerId,
        ICurrentUserService currentUser,
        string resourceName = "Resource")
    {
        // Admins can access everything
        if (currentUser.IsAdmin) return;

        // Regular users can only access their own data
        if (resourceOwnerId != currentUser.UserId)
            throw new ForbiddenAccessException(
                $"ليس لديك صلاحية الوصول إلى هذا {resourceName}. " +
                $"يمكنك الوصول إلى بياناتك الخاصة فقط.");
    }

    /// <summary>
    /// Filter query to only return records owned by current user
    /// (unless admin).
    /// </summary>
    public static IQueryable<T> FilterByOwner<T>(
        IQueryable<T> query,
        ICurrentUserService currentUser)
        where T : class, IOwnedEntity
    {
        if (currentUser.IsAdmin) return query; // Admin sees all
        return query.Where(x => x.OwnerId == currentUser.UserId);
    }
}

// File: SharedKernel/Domain/IOwnedEntity.cs
public interface IOwnedEntity
{
    int OwnerId { get; }
}

// File: SharedKernel/Exceptions/ForbiddenAccessException.cs
public class ForbiddenAccessException : Exception
{
    public ForbiddenAccessException(string message) : base(message) { }
}
Task 4.3 — Handler Using Ownership Guard
csharp

// File: Features/Sales/Queries/GetInvoiceByIdQueryHandler.cs

public class GetInvoiceByIdQueryHandler
    : IRequestHandler<GetInvoiceByIdQuery, InvoiceDto>
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public async Task<InvoiceDto> Handle(
        GetInvoiceByIdQuery request,
        CancellationToken ct)
    {
        var invoice = await _context.Invoices
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == request.InvoiceId, ct)
            ?? throw new NotFoundException("Invoice", request.InvoiceId);

        // Layer 4: Enforce ownership
        OwnershipGuard.Enforce(invoice.CashierId, _currentUser, "فاتورة");

        return invoice.ToDto();
    }
}

// File: Features/Sales/Queries/GetAllInvoicesQueryHandler.cs

public class GetAllInvoicesQueryHandler
    : IRequestHandler<GetAllInvoicesQuery, List<InvoiceDto>>
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public async Task<List<InvoiceDto>> Handle(
        GetAllInvoicesQuery request,
        CancellationToken ct)
    {
        var query = _context.Invoices.AsNoTracking();

        // Layer 4: Filter by owner automatically
        query = OwnershipGuard.FilterByOwner(query, _currentUser);

        return await query
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => i.ToDto())
            .ToListAsync(ct);
    }
}
✅ Layer 4 Checklist
 OwnershipGuard.Enforce() called in EVERY handler that returns specific record
 OwnershipGuard.FilterByOwner() used in ALL list queries
 Admin bypass is INSIDE OwnershipGuard — not scattered in handlers
 ForbiddenAccessException returns 403 (not 404 — which leaks existence)
 ICurrentUserService injected — never read JWT claims directly in handlers
🟡 Layer 5: Refresh Tokens & Logout
> **Status**: ❌ Not implemented — JWT-only authentication without refresh rotation

Task 5.1 — Token Refresh Handler
csharp

// File: Application/Auth/Commands/RefreshTokenCommandHandler.cs

public record RefreshTokenCommand(
    string AccessToken,
    string RefreshToken,
    string? IpAddress
) : IRequest<LoginResult>;

public class RefreshTokenCommandHandler
    : IRequestHandler<RefreshTokenCommand, LoginResult>
{
    private readonly AppDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly IAuditService _auditService;

    public async Task<LoginResult> Handle(
        RefreshTokenCommand request,
        CancellationToken ct)
    {
        // ─── 1. Validate old access token (ignore expiry) ───
        var principal = _tokenService.ValidateAccessToken(request.AccessToken);
        if (principal == null)
            return LoginResult.Fail("رمز الوصول غير صالح");

        var userId = int.Parse(
            principal.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // ─── 2. Find and validate refresh token ─────────────
        var tokenHash = HashToken(request.RefreshToken);

        var storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(t =>
                t.TokenHash == tokenHash &&
                t.UserId == userId, ct);

        if (storedToken == null)
            return LoginResult.Fail("رمز التحديث غير موجود");

        if (storedToken.IsRevoked)
        {
            // SECURITY: Refresh token reuse detected!
            // Revoke ALL tokens for this user (token family attack)
            await RevokeAllUserTokensAsync(userId, "TokenReuseDetected", ct);

            await _auditService.LogAsync(new AuditEntry
            {
                Action    = "RefreshTokenReuseDetected",
                UserId    = userId,
                IsSuccess = false,
                IpAddress = request.IpAddress,
                ErrorMessage = "Possible token theft — all sessions terminated"
            }, ct);

            return LoginResult.Fail(
                "تم اكتشاف نشاط مشبوه. تم تسجيل خروجك من جميع الأجهزة لحمايتك.");
        }

        if (storedToken.ExpiresAt < DateTime.UtcNow)
            return LoginResult.Fail("انتهت صلاحية رمز التحديث. سجل دخولك مجدداً.");

        // ─── 3. Rotate refresh token (revoke old, create new) ─
        storedToken.IsRevoked  = true;
        storedToken.RevokedAt  = DateTime.UtcNow;
        storedToken.RevokedReason = "Rotated";

        var user = await _context.Users.FindAsync(new object[] { userId }, ct)!;

        var newAccessToken  = _tokenService.GenerateAccessToken(user!);
        var newRefreshToken = _tokenService.GenerateRefreshToken();

        _context.RefreshTokens.Add(new RefreshToken
        {
            UserId      = userId,
            TokenHash   = HashToken(newRefreshToken),
            ExpiresAt   = DateTime.UtcNow.AddDays(7),
            CreatedByIp = request.IpAddress
        });

        await _context.SaveChangesAsync(ct);

        return new LoginResult(
            true, newAccessToken, newRefreshToken,
            DateTime.UtcNow.AddMinutes(15), null);
    }

    private async Task RevokeAllUserTokensAsync(
        int userId, string reason, CancellationToken ct)
    {
        var activeTokens = await _context.RefreshTokens
            .Where(t => t.UserId == userId && !t.IsRevoked)
            .ToListAsync(ct);

        foreach (var token in activeTokens)
        {
            token.IsRevoked     = true;
            token.RevokedAt     = DateTime.UtcNow;
            token.RevokedReason = reason;
        }

        await _context.SaveChangesAsync(ct);
    }

    private static string HashToken(string token)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(token);
        var hash  = System.Security.Cryptography.SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}
Task 5.2 — Logout Handler
csharp

// File: Application/Auth/Commands/LogoutCommandHandler.cs

public record LogoutCommand(
    string RefreshToken,
    bool LogoutAllDevices,
    int UserId
) : IRequest<bool>;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand, bool>
{
    private readonly AppDbContext _context;
    private readonly IAuditService _auditService;

    public async Task<bool> Handle(
        LogoutCommand request, CancellationToken ct)
    {
        if (request.LogoutAllDevices)
        {
            // Revoke ALL active sessions for this user
            var allTokens = await _context.RefreshTokens
                .Where(t => t.UserId == request.UserId && !t.IsRevoked)
                .ToListAsync(ct);

            foreach (var token in allTokens)
            {
                token.IsRevoked     = true;
                token.RevokedAt     = DateTime.UtcNow;
                token.RevokedReason = "Logout-AllDevices";
            }
        }
        else
        {
            // Revoke only current session token
            var tokenHash = HashToken(request.RefreshToken);
            var token = await _context.RefreshTokens
                .FirstOrDefaultAsync(t =>
                    t.TokenHash == tokenHash &&
                    t.UserId == request.UserId, ct);

            if (token != null)
            {
                token.IsRevoked     = true;
                token.RevokedAt     = DateTime.UtcNow;
                token.RevokedReason = "Logout";
            }
        }

        await _context.SaveChangesAsync(ct);

        await _auditService.LogAsync(new AuditEntry
        {
            Action    = request.LogoutAllDevices ? "LogoutAll" : "Logout",
            UserId    = request.UserId,
            IsSuccess = true
        }, ct);

        return true;
    }

    private static string HashToken(string token)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(token);
        var hash  = System.Security.Cryptography.SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}
✅ Layer 5 Checklist
 Access token: 15 minutes. Refresh token: 7 days
 Refresh token rotated on EVERY use (old revoked, new issued)
 Token reuse detection triggers full session termination for that user
 LogoutAllDevices option available
 Refresh token stored as SHA256 hash — raw token never in DB
🟠 Layer 6: Rate Limiting
> **Status**: ✅ **NEW** Rate limiting added (Login: 5/15min, Global: 100/min) with Arabic 429 response

Task 6.1 — Rate Limiter Configuration
csharp

// File: API/Configuration/RateLimitConfiguration.cs

public static class RateLimitConfiguration
{
    public static IServiceCollection AddRateLimiting(
        this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            // ─── Global fallback ─────────────────────────────
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                context => RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit         = 100,
                        Window              = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit          = 0
                    }));

            // ─── Login endpoint: Very strict ──────────────────
            options.AddPolicy("LoginPolicy", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,              // 5 attempts
                        Window      = TimeSpan.FromMinutes(15),  // per 15 min
                        QueueLimit  = 0
                    }));

            // ─── API general: Per user (authenticated) ────────
            options.AddPolicy("ApiPolicy", context =>
            {
                // Authenticated users get higher limit
                if (context.User.Identity?.IsAuthenticated == true)
                {
                    var userId = context.User.FindFirstValue(
                        ClaimTypes.NameIdentifier) ?? "anonymous";

                    return RateLimitPartition.GetSlidingWindowLimiter(
                        partitionKey: $"user:{userId}",
                        factory: _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit         = 200,
                            Window              = TimeSpan.FromMinutes(1),
                            SegmentsPerWindow   = 4,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit          = 0
                        });
                }

                // Unauthenticated: IP-based strict limit
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 20,
                        Window      = TimeSpan.FromMinutes(1),
                        QueueLimit  = 0
                    });
            });

            // ─── Response when rate limit exceeded ────────────
            options.OnRejected = async (context, ct) =>
            {
                context.HttpContext.Response.StatusCode = 429;
                context.HttpContext.Response.ContentType = "application/json";

                // Add Retry-After header
                if (context.Lease.TryGetMetadata(
                    MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        retryAfter.TotalSeconds.ToString("0");
                }

                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    Error  = "تم تجاوز الحد المسموح من الطلبات",
                    Code   = "RATE_LIMIT_EXCEEDED",
                    Detail = "حاول مجدداً بعد قليل"
                }, ct);
            };
        });

        return services;
    }
}
Task 6.2 — Apply Policies to Controllers
csharp

// File: API/Controllers/AuthController.cs

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    [HttpPost("login")]
    [EnableRateLimiting("LoginPolicy")]  // Layer 6: Strict rate limit for login
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var command = new LoginCommand(
            request.Username,
            request.Password,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
            return Unauthorized(new { Error = result.ErrorMessage });

        return Ok(result);
    }

    [HttpPost("refresh")]
    [EnableRateLimiting("ApiPolicy")]
    public async Task<IActionResult> Refresh(RefreshRequest request)
        => Ok(await _mediator.Send(
            new RefreshTokenCommand(request.AccessToken, request.RefreshToken,
                HttpContext.Connection.RemoteIpAddress?.ToString())));
}
✅ Layer 6 Checklist
 Login endpoint limited to 5 attempts per 15 minutes per IP
 Authenticated users have higher limits than anonymous
 429 response includes Retry-After header
 Response body in Arabic with RATE_LIMIT_EXCEEDED code
 Global fallback limiter set (100 req/min per IP)
🟢 Layer 7: Audit Logging
> **Status**: ⚠️ Serilog file logging implemented. `AuditLogs` table not yet created (use `ProductPriceHistory` for price audit)

Task 7.1 — Audit Service
csharp

// File: Application/Security/IAuditService.cs

public interface IAuditService
{
    Task LogAsync(AuditEntry entry, CancellationToken ct = default);
}

public record AuditEntry
{
    public int? UserId { get; init; }
    public string? Username { get; init; }
    public string Action { get; init; } = string.Empty;
    public string? Resource { get; init; }
    public string? ResourceId { get; init; }
    public object? OldValues { get; init; }
    public object? NewValues { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public int? StatusCode { get; init; }
    public bool IsSuccess { get; init; } = true;
    public string? ErrorMessage { get; init; }
    public int? DurationMs { get; init; }
}

// File: Infrastructure/Security/AuditService.cs

public class AuditService : IAuditService
{
    private readonly AppDbContext _context;
    private readonly ILogger<AuditService> _logger;

    public AuditService(AppDbContext context, ILogger<AuditService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task LogAsync(AuditEntry entry, CancellationToken ct = default)
    {
        try
        {
            var log = new AuditLog
            {
                Timestamp    = DateTime.UtcNow,
                UserId       = entry.UserId,
                Username     = entry.Username,
                Action       = entry.Action,
                Resource     = entry.Resource,
                ResourceId   = entry.ResourceId?.ToString(),
                OldValues    = entry.OldValues != null
                               ? JsonSerializer.Serialize(entry.OldValues) : null,
                NewValues    = entry.NewValues != null
                               ? JsonSerializer.Serialize(entry.NewValues) : null,
                IpAddress    = entry.IpAddress,
                UserAgent    = entry.UserAgent,
                StatusCode   = entry.StatusCode,
                IsSuccess    = entry.IsSuccess,
                ErrorMessage = entry.ErrorMessage,
                DurationMs   = entry.DurationMs
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            // Audit failure should NEVER crash the main operation
            _logger.LogError(ex, "Failed to write audit log for action {Action}",
                entry.Action);
        }
    }
}
Task 7.2 — Global Audit Middleware
csharp

// File: API/Middleware/AuditMiddleware.cs
// Automatically logs every request — no per-controller code needed

public class AuditMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditMiddleware> _logger;

    // Endpoints to exclude from detailed logging (health checks, etc.)
    private static readonly HashSet<string> ExcludedPaths = new(
        StringComparer.OrdinalIgnoreCase)
    {
        "/health", "/metrics", "/favicon.ico"
    };

    public AuditMiddleware(RequestDelegate next, ILogger<AuditMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IAuditService auditService)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        if (ExcludedPaths.Contains(path))
        {
            await _next(context);
            return;
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            // Only log non-GET requests + failed requests
            var isWrite = context.Request.Method != "GET";
            var isFailed = context.Response.StatusCode >= 400;

            if (isWrite || isFailed)
            {
                var userId = context.User.FindFirstValue(
                    ClaimTypes.NameIdentifier);
                var username = context.User.FindFirstValue(ClaimTypes.Name);

                await auditService.LogAsync(new AuditEntry
                {
                    UserId     = int.TryParse(userId, out var id) ? id : null,
                    Username   = username,
                    Action     = $"{context.Request.Method} {path}",
                    IpAddress  = context.Connection.RemoteIpAddress?.ToString(),
                    UserAgent  = context.Request.Headers.UserAgent.ToString(),
                    StatusCode = context.Response.StatusCode,
                    IsSuccess  = context.Response.StatusCode < 400,
                    DurationMs = (int)stopwatch.ElapsedMilliseconds
                });
            }
        }
    }
}
Task 7.3 — Serilog Structured Logging Setup
csharp

// File: API/Program.cs (add at top before builder)

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithClientIp()
    .Enrich.WithProperty("Application", "SalesSystem")
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/sales-system-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate:
            "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] " +
            "{ClientIp} {Message:lj}{NewLine}{Exception}")
    .WriteTo.MSSqlServer(
        connectionString: connectionString,
        sinkOptions: new MSSqlServerSinkOptions
        {
            TableName   = "SecurityLogs",
            AutoCreateSqlTable = true
        },
        restrictedToMinimumLevel: LogEventLevel.Warning) // Only warnings+ to DB
    .CreateLogger();

builder.Host.UseSerilog();
✅ Layer 7 Checklist
 Audit failure does NOT propagate (silent catch with log)
 GET requests NOT logged (reduce noise) — only writes and failures
 OldValues and NewValues stored as JSON snapshots for change tracking
 Logs rotate daily, retained 30 days
 Serilog enriches with ClientIp automatically
 Security events (login, logout, token reuse) always logged regardless of method
🔧 Phase 8: Wire Everything in Program.cs
csharp

// File: API/Program.cs — Complete middleware pipeline order

var app = builder.Build();

// ─── ORDER IS CRITICAL ─────────────────────────────────
// 1. HTTPS redirect (Layer 1)
app.UseHttpsRedirection();
app.UseHsts();

// 2. CORS (Layer 1)
app.UseCors(app.Environment.IsProduction()
    ? "ProductionPolicy" : "DevelopmentPolicy");

// 3. Rate limiting (Layer 6 — before auth to block abuse early)
app.UseRateLimiter();

// 4. Audit logging middleware (Layer 7)
app.UseMiddleware<AuditMiddleware>();

// 5. Authentication — validates JWT (Layer 2)
app.UseAuthentication();

// 6. Authorization — checks roles and policies (Layers 3 & 4)
app.UseAuthorization();

// 7. Controllers
app.MapControllers();

// ─── EXCEPTION HANDLER ─────────────────────────────────
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;

        context.Response.ContentType = "application/json";

        context.Response.StatusCode = exception switch
        {
            ForbiddenAccessException => 403,
            NotFoundException        => 404,
            ValidationException      => 422,
            DomainException          => 400,
            _                        => 500
        };

        await context.Response.WriteAsJsonAsync(new
        {
            Error  = exception?.Message ?? "Internal server error",
            Code   = exception?.GetType().Name.Replace("Exception", "").ToUpper()
        });
    });
});
📦 Final Summary

## ✅ Implemented (v4.6.4)
- **HTTPS + HSTS** — Force HTTPS redirect in Program.cs, HSTS enabled in production
- **CORS** — Desktop-only origins (`localhost:5221`, `localhost:5222`), strict headers
- **JWT Authentication** — `SALESSYSTEM_JWT_SECRET` from environment variable, `ClockSkew = Zero`, `RequireExpirationTime = true`
- **BCrypt Password Hashing** — Work factor 12 in AuthService and UserService
- **Role-based Authorization** — `AdminOnly`, `ManagerAndAbove`, `AllStaff` policies with `FallbackPolicy = RequireAuthenticatedUser()`
- **Rate Limiting** ← **NEW** — Login: 5 requests per 15 minutes, Global: 100 requests per minute, Arabic 429 response with `Retry-After` header
- **DPAPI Encryption** — Connection strings encrypted at rest via `IDataProtector` with `"DPAPI:"` prefix
- **Soft Delete for Users** — `PermanentDeleteAsync` returns `Result.Failure("لا يمكن حذف المستخدم")` — hard delete permanently blocked
- **Connection String Security** — Removed from `appsettings.Development.json`; sourced exclusively from `SALESSYSTEM_DB_CONNECTION` env var
- **FluentValidation Enhancement** ← **ENHANCED** — All 7 request validators enhanced (login, product, customer, supplier, category, unit, warehouse) with additional rules
- **Serilog Structured Logging** — File rolling logs, no secrets/passwords logged, `Log.Warning` for user errors, `Log.Error` for system failures
- **ProductPriceHistory** — Price/cost change audit trail recorded on every update
- **Health Check Endpoint** — `GET /api/v1/health/database` with DB connectivity detection

## 🔜 Planned (Future)
- **Refresh Token Rotation** — SHA256 hash storage in `RefreshTokens` table, rotation on every refresh request
- **Token Reuse Detection** — All sessions revoked when a used refresh token is replayed (token family attack)
- **Account Lockout** — 5 failed attempts locks account for 15 minutes (`LockoutUntil` column)
- **500ms Delay on Failed Login** — Prevents timing attacks (username enumeration)
- **AuditMiddleware** — Global request logging middleware writing to `AuditLogs` table
- **`RefreshTokens` table** — `CREATE TABLE RefreshTokens (...)` SQL script exists but not yet deployed
- **`AuditLogs` table** — `CREATE TABLE AuditLogs (...)` SQL script exists but not yet deployed
- **`RateLimitViolations` table** — Persistence layer for rate limiting violations

## Design Reference — 7-Layer Summary & Absolute Rules

The following sections document the **planned architectural design**. These code samples serve as a reference for future implementation when the remaining layers are built.

┌────────────────────────────────────────────────────────────────┐
│              7-LAYER SECURITY — RULES SUMMARY                  │
├───────┬────────────────────────────────────────────────────────┤
│ Layer │ Rule                                                   │
├───────┼────────────────────────────────────────────────────────┤
│  1    │ HTTPS only in prod. CORS: explicit origins ONLY        │
│  2    │ BCrypt passwords. 500ms delay on fail. 5-attempt lock  │
│  3    │ Roles/Policies in static class. FallbackPolicy = auth  │
│  4    │ OwnershipGuard in EVERY handler. Admin bypass inside   │
│  5    │ Refresh rotation on every use. Reuse = revoke ALL      │
│  6    │ Login: 5/15min. API: 200/min auth, 20/min anon        │
│  7    │ Audit never crashes app. Only writes + failures logged │
├───────┼────────────────────────────────────────────────────────┤
│ Order │ HTTPS → CORS → RateLimit → Audit → Auth → Authz       │
└───────┴────────────────────────────────────────────────────────┘

### ABSOLUTE RULES — ZERO TOLERANCE:
- ✅ Passwords stored as BCrypt ONLY — never MD5/SHA1/plain
- ✅ Refresh tokens stored as SHA256 hash — never plain text (reference only — not yet implemented)
- ✅ JWT secret minimum 32 characters — build fails if shorter
- ✅ Token reuse detected → ALL user sessions terminated (reference only — not yet implemented)
- ✅ Rate limit 429 response includes Retry-After header
- ✅ Audit log failure is silent — never affects main operation
- ✅ ForbiddenAccessException returns 403 (not 404 — no existence leak)
- ✅ 500ms delay on failed login — prevents timing attacks (reference only — not yet implemented)
- ✅ AllowAnyOrigin + AllowCredentials = NEVER in production