<#
.SYNOPSIS
    Installs the Sales Management System as a Windows Service.

.DESCRIPTION
    Creates or updates the SalesSystemService Windows service with
    auto-start, failure recovery (3 retries), and Arabic/English display names.
#>

param(
    [string]$ServiceName = "SalesSystemService",
    [string]$BinPath = "",
    [string]$DisplayName = "نظام إدارة المبيعات — Sales Management System",
    [string]$Description = "خدمة نظام إدارة المبيعات — Sales Management System API Service"
)

function Write-Step {
    param([string]$Message)
    Write-Host ":: $Message" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "[✔] $Message" -ForegroundColor Green
}

function Write-ErrorMsg {
    param([string]$Message)
    Write-Host "[✘] $Message" -ForegroundColor Red
}

# ── Step 0: Elevation check ──────────────────────────────────────────────
Write-Step "التحقق من صلاحية المدير (Administrator)..."
$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = New-Object Security.Principal.WindowsPrincipal($identity)
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator))
{
    Write-ErrorMsg "يجب تشغيل هذا السكريبت بصلاحية Administrator"
    Write-ErrorMsg "This script must be run as Administrator"
    exit 1
}
Write-Success "تم التحقق — السكريبت يعمل بصلاحية Administrator"

# ── Step 0.5: Validate BinPath ───────────────────────────────────────────
if ([string]::IsNullOrWhiteSpace($BinPath))
{
    Write-ErrorMsg "لم يتم تحديد BinPath — يرجى تمرير المسار إلى ملف API التنفيذي"
    Write-ErrorMsg "BinPath is required. Usage: .\install-service.ps1 -BinPath 'C:\...\SalesSystem.Api.exe'"
    exit 1
}

if (-not (Test-Path -LiteralPath $BinPath))
{
    Write-ErrorMsg "الملف التنفيذي غير موجود في المسار: $BinPath"
    Write-ErrorMsg "File not found at: $BinPath"
    exit 1
}
Write-Success "تم التحقق من BinPath: $BinPath"

# ── Step 1: Stop & delete existing service ───────────────────────────────
Write-Step "البحث عن الخدمة الموجودة '$ServiceName'..."
try
{
    $existing = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    if ($existing)
    {
        Write-Host "  الخدمة موجودة — سيتم إيقافها وحذفها" -ForegroundColor Yellow
        if ($existing.Status -eq 'Running')
        {
            Write-Step "إيقاف الخدمة..."
            sc.exe stop "$ServiceName" 2>&1 | Out-Null
            if ($LASTEXITCODE -ne 0)
            {
                Write-ErrorMsg "فشل إيقاف الخدمة (Exit code: $LASTEXITCODE)"
                exit 1
            }
            Write-Success "تم إيقاف الخدمة"
        }

        Write-Step "حذف الخدمة..."
        sc.exe delete "$ServiceName" 2>&1 | Out-Null
        if ($LASTEXITCODE -ne 0)
        {
            Write-ErrorMsg "فشل حذف الخدمة (Exit code: $LASTEXITCODE)"
            exit 1
        }
        Write-Success "تم حذف الخدمة القديمة"
    }
    else
    {
        Write-Host "  لا توجد خدمة موجودة — سيتم إنشاء خدمة جديدة" -ForegroundColor Yellow
    }
}
catch
{
    Write-ErrorMsg "حدث خطأ أثناء محاولة إيقاف/حذف الخدمة: $_"
    exit 1
}

# ── Step 2: Create service ────────────────────────────────────────────────
Write-Step "إنشاء الخدمة '$ServiceName'..."
try
{
    sc.exe create "$ServiceName" binPath="$BinPath" start=auto DisplayName="$DisplayName" 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0)
    {
        Write-ErrorMsg "فشل إنشاء الخدمة (Exit code: $LASTEXITCODE)"
        exit 1
    }
    Write-Success "تم إنشاء الخدمة بنجاح"
}
catch
{
    Write-ErrorMsg "حدث خطأ أثناء إنشاء الخدمة: $_"
    exit 1
}

# ── Step 3: Set failure recovery ─────────────────────────────────────────
Write-Step "تكوين إعادة التشغيل التلقائي عند الفشل..."
try
{
    sc.exe failure "$ServiceName" reset=86400 actions=restart/60000/restart/300000/restart/900000 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0)
    {
        Write-ErrorMsg "فشل تكوين استراتيجية إعادة التشغيل (Exit code: $LASTEXITCODE)"
        exit 1
    }
    Write-Success "تم تكوين إعادة التشغيل: المحاولة1 بعد 60ث، المحاولة2 بعد 5د، المحاولة3 بعد 15د"
}
catch
{
    Write-ErrorMsg "حدث خطأ أثناء تكوين استراتيجية الفشل: $_"
    exit 1
}

# ── Step 4: Set failure flag ─────────────────────────────────────────────
Write-Step "تفعيل علامة الفشل..."
try
{
    sc.exe failureflag "$ServiceName" mark=1 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0)
    {
        Write-ErrorMsg "فشل تفعيل علامة الفشل (Exit code: $LASTEXITCODE)"
        exit 1
    }
    Write-Success "تم تفعيل failureflag"
}
catch
{
    Write-ErrorMsg "حدث خطأ أثناء تفعيل علامة الفشل: $_"
    exit 1
}

# ── Step 5: Set description ──────────────────────────────────────────────
Write-Step "تعيين وصف الخدمة..."
try
{
    sc.exe description "$ServiceName" "$Description" 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0)
    {
        Write-ErrorMsg "فشل تعيين الوصف (Exit code: $LASTEXITCODE)"
        exit 1
    }
    Write-Success "تم تعيين وصف الخدمة"
}
catch
{
    Write-ErrorMsg "حدث خطأ أثناء تعيين الوصف: $_"
    exit 1
}

# ── Step 6: Start service ────────────────────────────────────────────────
Write-Step "بدء الخدمة..."
try
{
    sc.exe start "$ServiceName" 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0)
    {
        Write-ErrorMsg "فشل بدء الخدمة (Exit code: $LASTEXITCODE) — قد تحتاج إلى بدئها يدوياً"
        exit 1
    }
    Write-Success "تم بدء الخدمة بنجاح"
}
catch
{
    Write-ErrorMsg "حدث خطأ أثناء بدء الخدمة: $_"
    exit 1
}

# ── Done ──────────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "═════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host "  ✅  تم تثبيت الخدمة بنجاح" -ForegroundColor Green
Write-Host "  ✅  Service installed successfully" -ForegroundColor Green
Write-Host "═════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host "  الاسم (Name)       : $ServiceName" -ForegroundColor White
Write-Host "  الاسم المعروض     : $DisplayName" -ForegroundColor White
Write-Host "  المسار (BinPath)   : $BinPath" -ForegroundColor White
Write-Host "═════════════════════════════════════════════════════" -ForegroundColor Green
