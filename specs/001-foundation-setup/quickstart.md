# Quickstart: Foundation Setup

**Date**: 2026-05-06
**Prerequisites**: .NET 10 LTS SDK, SQL Server 2019+ (Express/LocalDB OK)

---

## 1. Clone and Build

```bash
git clone https://github.com/ZakariaAl-honyny/Sales-Management-System.git
cd Sales-Management-System
git checkout 001-foundation-setup

# Build entire solution
dotnet build SalesSystem/SalesSystem.sln
```

**Expected**: Build succeeds with 0 errors across all 6 projects.

## 2. Configure Database Connection

```bash
# Windows (Command Prompt)
set SALESSYSTEM_DB_CONNECTION=Server=.;Database=SalesSystemDb;Trusted_Connection=true;TrustServerCertificate=true;

# Windows (PowerShell)
$env:SALESSYSTEM_DB_CONNECTION="Server=.;Database=SalesSystemDb;Trusted_Connection=true;TrustServerCertificate=true;"
```

## 3. Run Database Migration

```bash
cd SalesSystem/SalesSystem.Infrastructure
dotnet ef database update --startup-project ../SalesSystem.Api
```

**Expected**: Database `SalesSystemDb` created with all tables.

## 4. Verify Seed Data

Connect to SQL Server and run:

```sql
USE SalesSystemDb;

-- Verify admin user
SELECT UserName, FullName, Role FROM Users WHERE UserName = 'admin';
-- Expected: admin, Administrator, 1

-- Verify default warehouse
SELECT Name, IsDefault FROM Warehouses WHERE IsDefault = 1;
-- Expected: المخزن الرئيسي, 1

-- Verify cash customer
SELECT Code, Name FROM Customers WHERE Code = 'CASH';
-- Expected: CASH, عميل نقدي

-- Verify units
SELECT COUNT(*) FROM Units;
-- Expected: >= 5

-- Verify document sequences
SELECT DocumentType, LastNumber FROM DocumentSequences;
-- Expected: 7 rows (INV, PUR, SR, PR, TRF, CP, SP), all LastNumber=0
```

## 5. Verify Schema Constraints

```sql
-- Check decimal types on money columns
SELECT COLUMN_NAME, DATA_TYPE, NUMERIC_PRECISION, NUMERIC_SCALE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'SalesInvoices'
  AND COLUMN_NAME IN ('SubTotal','TotalAmount','PaidAmount');
-- Expected: decimal, 18, 2 for all

-- Check quantity types
SELECT COLUMN_NAME, DATA_TYPE, NUMERIC_PRECISION, NUMERIC_SCALE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'WarehouseStocks'
  AND COLUMN_NAME = 'Quantity';
-- Expected: decimal, 18, 3

-- Check no varchar columns exist
SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'dbo'
  AND DATA_TYPE = 'varchar';
-- Expected: 0 rows (all should be nvarchar)
```

## 6. Success Criteria Checklist

- [ ] `dotnet build` passes with 0 errors
- [ ] Database has 22+ tables
- [ ] Admin user with BCrypt hash exists
- [ ] Default warehouse with IsDefault=true exists
- [ ] Cash customer exists
- [ ] 5+ measurement units exist
- [ ] 7 document sequences initialized
- [ ] All money columns are decimal(18,2)
- [ ] All quantity columns are decimal(18,3)
- [ ] All text columns are nvarchar
- [ ] WarehouseStocks has CHECK (Quantity >= 0)
- [ ] All FKs use ON DELETE NO ACTION
