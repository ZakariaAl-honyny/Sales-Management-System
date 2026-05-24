; ============================================
; Inno Setup Script — Sales Management System
; ============================================
; AppName:    نظام إدارة المبيعات
; Version:    4.6.4
; Publisher:  Sales System
; ============================================

#define MyAppName "نظام إدارة المبيعات"
#define MyAppVersion "4.6.4"
#define MyAppPublisher "Sales System"
#define MyAppURL "https://github.com/anomalyco/SalesSystem"
#define MyAppExeName "SalesSystem.DesktopPWF.exe"
#define MyServiceExeName "SalesSystem.Api.exe"

[Setup]
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\SalesSystem
DefaultGroupName={#MyAppName}
OutputDir=.\Output
OutputBaseFilename=SalesSystem-Setup-{#MyAppVersion}
SetupIconFile=..\SalesSystem\SalesSystem.DesktopPWF\Resources\AppIcon.ico
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=commandline
DisableProgramGroupPage=yes
UsePreviousAppDir=yes
UsePreviousGroup=yes
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName}
LanguageDetectionMethod=uilanguage
ShowLanguageDialog=auto

[Languages]
Name: "arabic"; MessagesFile: "compiler:Languages\Arabic.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Messages]
Arabic.WelcomeLabel2=سيتم تثبيت [name] على جهازك.%n%nيرجى التأكد من تثبيت .NET 10 Runtime قبل المتابعة.
Arabic.FinishedLabel=تم تثبيت [name] بنجاح على جهازك.
Arabic.ConfirmUninstall=هل أنت متأكد من إلغاء تثبيت [name] وجميع مكوناته؟
Arabic.UninstallStatusLabel=جاري إلغاء تثبيت [name]...

[Dirs]
Name: "{app}\DataProtection-Keys"; Permissions: users-modify
Name: "{app}\logs"; Permissions: users-modify

[Files]
; WPF Desktop Application (self-contained publish)
Source: "..\SalesSystem\SalesSystem.DesktopPWF\bin\Release\net10.0-windows\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; API / Windows Service (self-contained publish)
Source: "..\SalesSystem\SalesSystem.Api\bin\Release\net10.0-windows\win-x64\publish\*"; DestDir: "{app}\Service"; Flags: ignoreversion recursesubdirs createallsubdirs

[Run]
; Register Windows Service
Filename: "sc.exe"; Parameters: "create ""SalesSystemService"" binPath=""{app}\Service\{#MyServiceExeName}"" start=auto DisplayName=""{#MyAppName}"""; Flags: runhidden; StatusMsg: "جاري تثبيت خدمة النظام..."
Filename: "sc.exe"; Parameters: "description ""SalesSystemService"" ""خدمة نظام إدارة المبيعات والمخزون"""; Flags: runhidden
Filename: "sc.exe"; Parameters: "failure ""SalesSystemService"" reset=86400 actions=restart/60000/restart/300000/restart/900000"; Flags: runhidden
Filename: "sc.exe"; Parameters: "failureflag ""SalesSystemService"" 1"; Flags: runhidden
; Start Windows Service
Filename: "net.exe"; Parameters: "start ""SalesSystemService"""; Flags: runhidden; StatusMsg: "جاري بدء خدمة النظام..."
; Launch Desktop application (optional)
Filename: "{app}\{#MyAppExeName}"; Description: "تشغيل التطبيق"; Flags: postinstall nowait skipifsilent unchecked

[UninstallRun]
; Stop and remove Windows Service
Filename: "net.exe"; Parameters: "stop ""SalesSystemService"""; Flags: runhidden; RunOnceId: "StopService"
Filename: "sc.exe"; Parameters: "delete ""SalesSystemService"""; Flags: runhidden; RunOnceId: "DeleteService"

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"
Name: "{autoprograms}\إلغاء تثبيت {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "إنشاء اختصار على سطح المكتب"; GroupDescription: "اختصارات:"; Flags: checkedonce

[Code]
function IsDotNet10Installed: Boolean;
var
  ResultCode: Integer;
begin
  Result := ShellExec('', 'cmd.exe', '/C "dotnet --list-runtimes | findstr "Microsoft.NETCore.App 10.""', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  if ResultCode = 0 then
    Result := True
  else
    Result := False;
end;

function InitializeSetup: Boolean;
begin
  if not IsDotNet10Installed then
  begin
    SuppressibleMsgBox(
      'لم يتم العثور على .NET 10 Runtime.' + #13#10 +
      'يرجى تثبيت .NET 10 Runtime من:' + #13#10 +
      'https://dotnet.microsoft.com/download/dotnet/10.0' + #13#10#13#10 +
      'بعد تثبيت .NET 10، يرجى تشغيل المثبت مرة أخرى.',
      mbError,
      MB_OK,
      IDOK
    );
    Result := False;
  end
  else
    Result := True;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usUninstall then
  begin
    Exec('net.exe', 'stop "SalesSystemService"', '', SW_HIDE, ewWaitUntilTerminated);
    Exec('sc.exe', 'delete "SalesSystemService"', '', SW_HIDE, ewWaitUntilTerminated);
  end;
end;
