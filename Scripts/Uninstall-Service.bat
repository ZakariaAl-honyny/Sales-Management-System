@echo off
title إلغاء تثبيت خدمة نظام إدارة المبيعات
chcp 65001 >nul

:: ============================================
:: Uninstall-Service.bat
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

echo.
echo ============================================
echo إلغاء تثبيت خدمة نظام إدارة المبيعات
echo ============================================
echo.

:: Check if service exists
sc query "%SERVICE_NAME%" >nul 2>&1
if %errorlevel% neq 0 (
    echo ⚠️  الخدمة غير موجودة أو تم إلغاء تثبيتها مسبقاً
    echo.
    pause
    exit /b 0
)

:: Stop service
echo ⏹️  إيقاف الخدمة...
net stop "%SERVICE_NAME%" 2>nul
echo ✅ تم إيقاف الخدمة

:: Delete service
echo.
echo 🗑️  حذف الخدمة...
sc delete "%SERVICE_NAME%" >nul
if %errorlevel% equ 0 (
    echo ✅ تم حذف الخدمة بنجاح
) else (
    echo ❌ فشل حذف الخدمة
)

echo.
echo ============================================
echo ✅ تم إلغاء تثبيت الخدمة بنجاح
echo ============================================
echo.
pause
