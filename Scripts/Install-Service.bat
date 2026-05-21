@echo off
title تثبيت خدمة نظام إدارة المبيعات
chcp 65001 >nul

:: ============================================
:: Install-Service.bat
:: ============================================
:: Run as Administrator check
openfiles >nul 2>&1
if %errorlevel% neq 0 (
    echo.
    echo ⚠️  يرجى تشغيل هذا الملف كمسؤول (Run as Administrator)
    echo.
    pause
    exit /b 1
)

set SERVICE_NAME=SalesSystemService
set SERVICE_DISPLAY_NAME=نظام إدارة المبيعات
set SERVICE_DESCRIPTION=خدمة نظام إدارة المبيعات والمخزون
set SERVICE_EXE=%~dp0..\SalesSystem\SalesSystem.Api\bin\Release\net10.0-windows\SalesSystem.Api.exe

:: Check if published exe exists
if not exist "%SERVICE_EXE%" (
    echo.
    echo ⚠️  لم يتم العثور على الملف التنفيذي للخدمة
    echo المسار: %SERVICE_EXE%
    echo الرجاء نشر المشروع أولاً بأمر:
    echo dotnet publish SalesSystem\SalesSystem.Api -c Release -o bin\Release\net10.0-windows
    echo.
    pause
    exit /b 1
)

echo.
echo ============================================
echo تثبيت خدمة نظام إدارة المبيعات
echo ============================================
echo.

:: Stop existing service if running
sc query "%SERVICE_NAME%" >nul 2>&1
if %errorlevel% equ 0 (
    echo ⏹️  إيقاف الخدمة الموجودة...
    net stop "%SERVICE_NAME%" 2>nul
    sc delete "%SERVICE_NAME%" >nul
    echo ✅ تم إيقاف وحذف الخدمة القديمة
)

:: Create new service
echo.
echo 🔧 إنشاء الخدمة الجديدة...
sc create "%SERVICE_NAME%" binPath="%SERVICE_EXE%" start=auto DisplayName="%SERVICE_DISPLAY_NAME%"
if %errorlevel% neq 0 (
    echo ❌ فشل إنشاء الخدمة
    pause
    exit /b 1
)

:: Set description
sc description "%SERVICE_NAME%" "%SERVICE_DESCRIPTION%"
echo ✅ تم إنشاء الخدمة بنجاح

:: Set recovery options (auto-restart on failure, 3 attempts)
echo.
echo 🔧 ضبط خيارات الاسترداد...
sc failure "%SERVICE_NAME%" reset=86400 actions=restart/5000/restart/10000/restart/15000
sc failureflag "%SERVICE_NAME%" 1
echo ✅ تم ضبط خيارات الاسترداد (إعادة التشغيل التلقائي - 3 محاولات)

:: Start service
echo.
echo 🚀 بدء الخدمة...
net start "%SERVICE_NAME%"
if %errorlevel% equ 0 (
    echo ✅ تم بدء الخدمة بنجاح
) else (
    echo ❌ فشل بدء الخدمة
)

echo.
echo ============================================
echo ✅ تم تثبيت الخدمة بنجاح
echo ============================================
echo.
pause
