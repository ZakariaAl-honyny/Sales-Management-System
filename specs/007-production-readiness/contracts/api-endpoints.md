# API Endpoint Contracts: Phase 7 — Production Readiness

**Phase**: 1 — Design & Contracts  
**Date**: 2026-05-23

All endpoints require `Authorization: Bearer {jwt}` unless noted. All responses use `application/json`.

---

## Settings Endpoints

### GET `/api/v1/settings`
- **Policy**: `AdminOnly`
- **Response 200**:
```json
{
  "storeName": "متجر النور",
  "phoneNumber": "0501234567",
  "address": "الرياض، حي النزهة",
  "logoPath": "C:\\SalesSystem\\logo.png",
  "defaultTaxRate": 15.00,
  "isTaxEnabled": true,
  "defaultWarehouseId": 1,
  "costingMethod": 1
}
```
- **Response 401/403**: unauthorized / insufficient role

---

### PUT `/api/v1/settings`
- **Policy**: `AdminOnly`
- **Request Body**:
```json
{
  "storeName": "متجر النور",
  "phoneNumber": "0501234567",
  "address": "الرياض، حي النزهة",
  "logoPath": "C:\\SalesSystem\\logo.png",
  "defaultTaxRate": 15.00,
  "isTaxEnabled": true,
  "defaultWarehouseId": 1,
  "costingMethod": 1
}
```
- **FluentValidation Rules**:
  - `StoreName`: NotEmpty, MaxLength(200)
  - `DefaultTaxRate`: `>= 0 AND <= 100`
  - `CostingMethod`: InclusiveBetween(1, 3)
  - `DefaultWarehouseId`: `> 0` when provided
- **Response 200**: `{ "message": "تم حفظ الإعدادات بنجاح" }`
- **Response 400**: validation errors
- **Response 401/403**: unauthorized / insufficient role

---

## User Management Endpoints

### GET `/api/v1/users`
- **Policy**: `AdminOnly`
- **Query params**: `?includeInactive=true`
- **Response 200**:
```json
[
  {
    "id": 1,
    "fullName": "مدير النظام",
    "userName": "admin",
    "role": 1,
    "roleName": "Admin",
    "isActive": true,
    "createdAt": "2026-01-01T00:00:00Z"
  }
]
```

---

### POST `/api/v1/users`
- **Policy**: `AdminOnly`
- **Request Body**:
```json
{
  "fullName": "كاشير جديد",
  "userName": "cashier01",
  "password": "SecurePass@123",
  "role": 3
}
```
- **FluentValidation Rules**:
  - `FullName`: NotEmpty, MaxLength(150)
  - `UserName`: NotEmpty, MaxLength(100), no spaces
  - `Password`: MinLength(8)
  - `Role`: InclusiveBetween(1, 3)
- **Response 201**: `{ "id": 5, "userName": "cashier01" }`
- **Response 400**: validation errors, duplicate username
- **Response 409**: `{ "error": "اسم المستخدم مستخدم بالفعل" }`

---

### PUT `/api/v1/users/{id}`
- **Policy**: `AdminOnly`
- **Request Body** (partial update — password optional):
```json
{
  "fullName": "كاشير محدّث",
  "role": 2,
  "newPassword": null
}
```
- **Response 200**: updated user DTO
- **Response 404**: `{ "error": "المستخدم غير موجود" }`

---

### DELETE `/api/v1/users/{id}`
- **Policy**: `AdminOnly`
- **Behavior**: Soft delete only — sets `IsActive = false`
- **Guard**: Returns 400 if deleting last active Admin
- **Response 200**: `{ "message": "تم إلغاء تفعيل المستخدم" }`
- **Response 400**: `{ "error": "لا يمكن إلغاء تفعيل آخر مسؤول في النظام" }`
- **Response 404**: user not found

### PUT `/api/v1/users/{id}/restore`
- **Policy**: `AdminOnly`
- **Behavior**: Reactivates a deactivated user (`IsActive = true`)
- **Response 200**: `{ "message": "تم إعادة تفعيل المستخدم" }`

---

## Backup & Restore Endpoints

### GET `/api/v1/backup/list`
- **Policy**: `AdminOnly`
- **Response 200**:
```json
[
  {
    "fileName": "SalesSystem_20260523_235900.bak",
    "filePath": "C:\\SalesSystemBackups\\SalesSystem_20260523_235900.bak",
    "createdAt": "2026-05-23T23:59:00Z",
    "sizeBytes": 10485760
  }
]
```

---

### POST `/api/v1/backup`
- **Policy**: `AdminOnly`
- **Request Body**: `{}` (no body required — uses configured backup path)
- **Response 200**: `{ "fileName": "SalesSystem_20260523_235900.bak", "message": "تم إنشاء النسخة الاحتياطية بنجاح" }`
- **Response 500**: `{ "error": "فشل إنشاء النسخة الاحتياطية: [detail]" }`

---

### POST `/api/v1/backup/restore`
- **Policy**: `AdminOnly`
- **Request Body**:
```json
{
  "fileName": "SalesSystem_20260523_235900.bak"
}
```
- **FluentValidation Rules**:
  - `FileName`: NotEmpty, must end in `.bak`, file must exist in backup directory
- **Behavior**:
  1. Force single-user mode (`ROLLBACK AFTER 30`)
  2. Restore database
  3. Return `multiuser` mode
  4. Client receives 200 and must redirect to login
- **Response 200**: `{ "message": "تمت استعادة قاعدة البيانات. يرجى إعادة تسجيل الدخول." }`
- **Response 400**: validation errors (file not found, invalid name)
- **Response 500**: `{ "error": "فشلت الاستعادة: [detail]" }`

---

## Health Check Endpoints *(existing — verify)*

### GET `/api/v1/health`
- **Policy**: Anonymous (no auth required)
- **Response 200**: `{ "status": "healthy", "database": "connected" }`
- **Response 503**: `{ "status": "unhealthy", "database": "disconnected" }`

### GET `/api/v1/health/database`
- **Policy**: Anonymous
- **Response 200**: `{ "status": "connected" }`
- **Response 503**: `{ "status": "disconnected" }`
