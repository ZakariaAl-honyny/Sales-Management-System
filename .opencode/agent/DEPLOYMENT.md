# Deployment Guide
# Sales Management System — v1.0

---

## 1. API as Windows Service

```csharp
// SalesSystem.Api/Program.cs
var builder = WebApplication.CreateBuilder(args);

// Run as Windows Service
builder.Host.UseWindowsService(options =>
{
    options.ServiceName = "SalesSystem API";
});

// ... rest of configuration

// Install command (run as Administrator):
// sc create "SalesSystemAPI"
//   binpath="C:\SalesSystem\Api\SalesSystem.Api.exe"
//   start=auto
// sc start "SalesSystemAPI"
```

## 2. Inno Setup Script

```pascal
; SalesSystem_Setup.iss

[Setup]
AppName=Sales Management System
AppVersion=1.0.0
AppPublisher=Your Company Name
DefaultDirName={autopf}\SalesSystem
DefaultGroupName=Sales Management System
OutputBaseFilename=SalesSystem_Setup_v1.0.0
Compression=lzma2
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "arabic"; MessagesFile: "compiler:Languages\Arabic.isl"

[Files]
; API (Windows Service)
Source: "publish\api\*"; DestDir: "{app}\Api"; Flags: recursesubdirs

; Desktop Application
Source: "publish\desktop\*"; DestDir: "{app}\Desktop"; Flags: recursesubdirs

; SQL Setup Script
Source: "setup\CreateDatabase.sql"; DestDir: "{app}\Setup"

[Icons]
Name: "{group}\Sales System"; Filename: "{app}\Desktop\SalesSystem.Desktop.exe"
Name: "{commondesktop}\Sales System"; Filename: "{app}\Desktop\SalesSystem.Desktop.exe"

[Run]
; Install and start the API Windows Service
Filename: "sc.exe"; Parameters: "create ""SalesSystemAPI"" binpath=""{app}\Api\SalesSystem.Api.exe"" start=auto"; Flags: runhidden
Filename: "sc.exe"; Parameters: "start SalesSystemAPI"; Flags: runhidden

[UninstallRun]
Filename: "sc.exe"; Parameters: "stop SalesSystemAPI"; Flags: runhidden
Filename: "sc.exe"; Parameters: "delete SalesSystemAPI"; Flags: runhidden
```

## 3. Connection String Security

```csharp
// Set during installation via Inno Setup Pascal script:
// [Code] section sets environment variable with connection string

// Read in API:
var connectionString =
    Environment.GetEnvironmentVariable("SALESSYSTEM_CONNECTION")
    ?? configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrEmpty(connectionString))
    throw new InvalidOperationException(
        "Database connection string not configured");
```

```json
// appsettings.json — NEVER store real connection string here
{
  "ConnectionStrings": {
    "DefaultConnection": "USE_ENVIRONMENT_VARIABLE"
  }
}
```