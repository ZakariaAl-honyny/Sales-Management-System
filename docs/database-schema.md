# Database Schema Design
# Sales Management System — v4.6.7 (InvoiceNo Int Re-addition)
# Platform: SQL Server 2019+
# 30+ Tables | decimal-only financials | nvarchar text | Soft delete
# Platform: SQL Server 2019+
# 30+ Tables | decimal-only financials | nvarchar text | Soft delete

---

# 1) Important Design Rules Before Tables
These rules will save you many problems later:

## Data Types
- **Primary Keys**: `int IDENTITY(1,1)` — Named `Id` in all entities (BaseEntity pattern).
- **Texts**: `nvarchar` (to support Arabic/English).
- **Currency/Money**: `decimal(18,2)` — Precision 18, Scale 2.
- **Quantities**: `decimal(18,3)` — Precision 18, Scale 3.
- **Status and Types**: `tinyint` (for Enums).
- **Flags**: `bit` (0/1).
- **Dates and Times**: `datetime2`.
- **Audit Tracking**: Standardized across all entities:
    - `CreatedAt` datetime2
    - `CreatedByUserId` int null (FK to Users.Id)
    - `UpdatedAt` datetime2 null
    - `UpdatedByUserId` int null (FK to Users.Id)
    - `IsActive` bit (Soft delete flag)

---

# 2) Proposed Database
Database Name example: **`SalesSystemDb`**
Default schema: **`dbo`**

---

# 3) Core Tables

---

## A) Users
### Columns
- `Id` int PK
- `UserName` nvarchar(50) not null unique
- `PasswordHash` nvarchar(256) not null
- `FullName` nvarchar(150) not null
- `Role` tinyint not null (1=Admin, 2=Manager, 3=Cashier)
- `MustChangePassword` bit not null default 1
- `Status` tinyint not null default 1 (1=Active, 2=Locked, 3=Suspended)
- `LastLoginAt` datetime2 null
- `FailedLoginAttempts` int not null default 0
- `CreatedByUserId` int null FK
- `UpdatedByUserId` int null FK
- `IsActive` bit not null default 1
- `CreatedAt` datetime2 not null
- `UpdatedAt` datetime2 null

---

## B) Units
### Columns
- `Id` int PK
- `Name` nvarchar(50) not null
- `Symbol` nvarchar(20) null
- `CreatedByUserId` int null FK
- `UpdatedByUserId` int null FK
- `IsActive` bit not null default 1
- `CreatedAt` datetime2 not null
- `UpdatedAt` datetime2 null

---

## C) Categories
### Columns
- `Id` int PK
- `Name` nvarchar(100) not null
- `Description` nvarchar(250) null
- `CreatedByUserId` int null FK
- `UpdatedByUserId` int null FK
- `IsActive` bit not null default 1
- `CreatedAt` datetime2 not null
- `UpdatedAt` datetime2 null

---

## D) Products
### Columns
- `Id` int PK
- `Barcode` nvarchar(50) null unique (legacy — prefer UnitBarcodes)
- `Name` nvarchar(150) not null
- `CategoryId` int null FK
- `SupplierPrice` decimal(18,2) not null default 0 (catalog price for SupplierPrice costing method)
- `AvgCost` decimal(18,2) not null default 0 (computed by UpdateProductPricingService)
- `ReorderLevel` decimal(18,3) not null default 0
- `MinStock` decimal(18,3) not null default 0
- `TrackExpiry` bit not null default 0
- `IsExpirable` bit not null default 0
- `Description` nvarchar(500) null
- `CreatedByUserId` int null FK
- `UpdatedByUserId` int null FK
- `IsActive` bit not null default 1
- `CreatedAt` datetime2 not null
- `UpdatedAt` datetime2 null

> **v4.3 Change:** Pricing (RetailPrice, WholesalePrice) and ConversionFactor moved to `ProductUnits` table.
> Barcodes moved to `UnitBarcodes` table (one per product-unit combination).
> **v4.5.3 Change:** `Code` column removed — use auto-increment `Id` as sole identifier.

## D2) ProductUnits (v4.3 — Dynamic Unit of Measure)
### Columns
- `Id` int PK
- `ProductId` int not null FK → Products(Id)
- `UnitName` nvarchar(50) not null  -- e.g., "Piece", "Box", "Carton"
- `ConversionFactor` decimal(18,3) not null default 1  -- Base=1, Box=24, Carton=144
- `RetailPrice` decimal(18,2) not null default 0
- `WholesalePrice` decimal(18,2) not null default 0
- `IsBaseUnit` bit not null default 0  -- Exactly one per product
- `CreatedByUserId` int null FK
- `UpdatedByUserId` int null FK
- `IsActive` bit not null default 1
- `CreatedAt` datetime2 not null
- `UpdatedAt` datetime2 null

### Constraints
- `UNIQUE(ProductId, UnitName)` — no duplicate unit names per product
- `UNIQUE(ProductId, IsBaseUnit)` filter where IsBaseUnit=1 — exactly one base unit per product
- `CHECK (ConversionFactor > 0)`

## D3) UnitBarcodes (v4.3 — Replaces ProductBarcodes)
### Columns
- `Id` int PK
- `ProductUnitId` int not null FK → ProductUnits(Id)
- `BarcodeValue` nvarchar(100) not null unique
- `IsDefault` bit not null default 0
- `CreatedAt` datetime2 not null
- `IsActive` bit not null default 1

---

## E) Warehouses
### Columns
- `Id` int PK
- `Name` nvarchar(100) not null
- `Location` nvarchar(250) null
- `IsDefault` bit not null default 0
- `CreatedByUserId` int null FK
- `UpdatedByUserId` int null FK
- `IsActive` bit not null default 1
- `CreatedAt` datetime2 not null
- `UpdatedAt` datetime2 null

> **v4.5.3 Change:** `Code` column removed — use auto-increment `Id` as sole identifier.

---

## F) WarehouseStocks
### Columns
- `Id` int PK
- `WarehouseId` int not null FK
- `ProductId` int not null FK
- `Quantity` decimal(18,3) not null default 0
- `ReorderLevel` decimal(18,3) not null default 0
- `CreatedByUserId` int null FK
- `UpdatedByUserId` int null FK
- `IsActive` bit not null default 1
- `CreatedAt` datetime2 not null
- `UpdatedAt` datetime2 not null

### Important Constraints
- `UNIQUE(WarehouseId, ProductId)`
- `CHECK (Quantity >= 0)` — CRITICAL: prevents negative stock at DB level

---

## G) Suppliers
### Columns
- `Id` int PK
- `Name` nvarchar(150) not null
- `Phone` nvarchar(20) null
- `Email` nvarchar(100) null
- `Address` nvarchar(250) null
- `OpeningBalance` decimal(18,2) not null default 0
- `CurrentBalance` decimal(18,2) not null default 0
- `AccountId` int null FK → Accounts(Id)
- `CurrencyId` int null FK → Currencies(Id)
- `CreditLimit` decimal(18,2) not null default 0
- `CreatedByUserId` int null FK
- `UpdatedByUserId` int null FK
- `IsActive` bit not null default 1
- `CreatedAt` datetime2 not null
- `UpdatedAt` datetime2 null

> **v4.5.3 Change:** `Code` column removed — use auto-increment `Id` as sole identifier.

---

## H) Customers
### Columns
- `Id` int PK
- `Name` nvarchar(150) not null
- `Phone` nvarchar(20) null
- `Email` nvarchar(100) null
- `Address` nvarchar(250) null
- `OpeningBalance` decimal(18,2) not null default 0
- `CurrentBalance` decimal(18,2) not null default 0
- `AccountId` int null FK → Accounts(Id)
- `CurrencyId` int null FK → Currencies(Id)
- `CreditLimit` decimal(18,2) not null default 0
- `CustomerType` tinyint null (1=Retail, 2=Wholesale, 3=Corporate)
- `CreatedByUserId` int null FK
- `UpdatedByUserId` int null FK
- `IsActive` bit not null default 1
- `CreatedAt` datetime2 not null
- `UpdatedAt` datetime2 null

> **v4.5.3 Change:** `Code` column removed — use auto-increment `Id` as sole identifier.

---

# 4) Purchases

## I) PurchaseInvoices
### Columns
- `Id` int PK
- `InvoiceNo` int not null (user-facing invoice number, UNIQUE per document type — generated via DocumentSequenceService.GetNextIntAsync)
- `SupplierId` int not null FK
- `WarehouseId` int not null FK
- `InvoiceDate` datetime2 not null
- `DueDate` date null
- `PaymentType` tinyint not null (1=Cash, 2=Credit, 3=Mixed)
- `SubTotal` decimal(18,2) not null default 0
- `DiscountAmount` decimal(18,2) not null default 0
- `TaxAmount` decimal(18,2) not null default 0
- `TotalAmount` decimal(18,2) not null default 0
- `PaidAmount` decimal(18,2) not null default 0
- `DueAmount` decimal(18,2) not null default 0
- `Notes` nvarchar(500) null
- `Status` tinyint not null (1=Draft, 2=Posted, 3=Cancelled)
- `CreatedByUserId` int null FK
- `UpdatedByUserId` int null FK
- `IsActive` bit not null default 1
- `CreatedAt` datetime2 not null
- `UpdatedAt` datetime2 null

---

# 5) Sales

## K) SalesInvoices
### Columns
- `Id` int PK
- `InvoiceNo` int not null (user-facing invoice number, UNIQUE per document type — generated via DocumentSequenceService.GetNextIntAsync)
- `CustomerId` int null FK
- `WarehouseId` int not null FK
- `InvoiceDate` datetime2 not null
- `DueDate` date null
- `PaymentType` tinyint not null (1=Cash, 2=Credit, 3=Mixed)
- `SubTotal` decimal(18,2) not null default 0
- `DiscountAmount` decimal(18,2) not null default 0
- `TaxAmount` decimal(18,2) not null default 0
- `TotalAmount` decimal(18,2) not null default 0
- `PaidAmount` decimal(18,2) not null default 0
- `DueAmount` decimal(18,2) not null default 0
- `Notes` nvarchar(500) null
- `Status` tinyint not null (1=Draft, 2=Posted, 3=Cancelled)
- `CreatedByUserId` int null FK
- `UpdatedByUserId` int null FK
- `IsActive` bit not null default 1
- `CreatedAt` datetime2 not null
- `UpdatedAt` datetime2 null
- `CashBoxId` int null FK → CashBoxes(Id)

---

## L) SalesInvoiceItems
### Columns
- `SalesInvoiceItemId` int PK
- `SalesInvoiceId` int not null FK
- `ProductId` int not null FK
- `Quantity` decimal(18,3) not null
- `UnitPrice` decimal(18,2) not null
- `DiscountAmount` decimal(18,2) not null default 0
- `LineTotal` decimal(18,2) not null
- `Notes` nvarchar(250) null
- `CreatedByUserId` int null FK
- `UpdatedByUserId` int null FK
- `IsActive` bit not null default 1
- `CreatedAt` datetime2 not null
- `UpdatedAt` datetime2 null

---

# 6) Returns

## M) PurchaseReturns
### Columns
- `Id` int PK
- `ReturnNo` nvarchar(30) not null unique
- `PurchaseInvoiceId` int null FK
- `SupplierId` int not null FK
- `WarehouseId` int not null FK
- `ReturnDate` datetime2 not null
- `Reason` nvarchar(250) null
- `SubTotal` decimal(18,2) not null default 0
- `TotalAmount` decimal(18,2) not null default 0
- `Status` tinyint not null
- `CreatedByUserId` int null FK
- `UpdatedByUserId` int null FK
- `IsActive` bit not null default 1
- `CreatedAt` datetime2 not null
- `UpdatedAt` datetime2 null

---

## N) PurchaseReturnItems
### Columns
- `PurchaseReturnItemId` int PK
- `PurchaseReturnId` int not null FK
- `ProductId` int not null FK
- `Quantity` decimal(18,3) not null
- `UnitCost` decimal(18,2) not null
- `LineTotal` decimal(18,2) not null
- `CreatedByUserId` int null FK
- `UpdatedByUserId` int null FK
- `IsActive` bit not null default 1
- `CreatedAt` datetime2 not null
- `UpdatedAt` datetime2 null

---

## O) SalesReturns
### Columns
- `Id` int PK
- `ReturnNo` nvarchar(30) not null unique
- `SalesInvoiceId` int null FK
- `CustomerId` int not null FK
- `WarehouseId` int not null FK
- `ReturnDate` datetime2 not null
- `Reason` nvarchar(250) null
- `SubTotal` decimal(18,2) not null default 0
- `TotalAmount` decimal(18,2) not null default 0
- `Status` tinyint not null
- `CreatedByUserId` int null FK
- `UpdatedByUserId` int null FK
- `IsActive` bit not null default 1
- `CreatedAt` datetime2 not null
- `UpdatedAt` datetime2 null

---

## P) SalesReturnItems
### Columns
- `SalesReturnItemId` int PK
- `SalesReturnId` int not null FK
- `ProductId` int not null FK
- `Quantity` decimal(18,3) not null
- `UnitPrice` decimal(18,2) not null
- `LineTotal` decimal(18,2) not null
- `CreatedByUserId` int null FK
- `UpdatedByUserId` int null FK
- `IsActive` bit not null default 1
- `CreatedAt` datetime2 not null
- `UpdatedAt` datetime2 null

---

# 7) Stock Transfer Between Warehouses

## Q) StockTransfers
### Columns
- `Id` int PK
- `TransferNo` nvarchar(30) not null unique
- `FromWarehouseId` int not null FK
- `ToWarehouseId` int not null FK
- `TransferDate` datetime2 not null
- `Notes` nvarchar(500) null
- `Status` tinyint not null
- `CreatedByUserId` int null FK
- `UpdatedByUserId` int null FK
- `IsActive` bit not null default 1
- `CreatedAt` datetime2 not null
- `UpdatedAt` datetime2 null

---

## R) StockTransferItems
### Columns
- `StockTransferItemId` int PK
- `StockTransferId` int not null FK
- `ProductId` int not null FK
- `Quantity` decimal(18,3) not null
- `Notes` nvarchar(250) null
- `CreatedByUserId` int null FK
- `UpdatedByUserId` int null FK
- `IsActive` bit not null default 1
- `CreatedAt` datetime2 not null
- `UpdatedAt` datetime2 null

---

# 8) Payments and Collections

## S) CustomerPayments
### Columns
- `Id` int PK
- `PaymentNo` nvarchar(30) not null unique
- `CustomerId` int not null FK
- `SalesInvoiceId` int null FK
- `PaymentDate` datetime2 not null
- `Amount` decimal(18,2) not null
- `PaymentMethod` tinyint not null (1=Cash, 2=Bank Transfer, 3=Card, 4=Other)
- `ReferenceNo` nvarchar(50) null
- `Notes` nvarchar(500) null
- `CreatedByUserId` int null FK
- `UpdatedByUserId` int null FK
- `IsActive` bit not null default 1
- `CreatedAt` datetime2 not null
- `UpdatedAt` datetime2 null

---

## T) SupplierPayments
### Columns
- `Id` int PK
- `PaymentNo` nvarchar(30) not null unique
- `SupplierId` int not null FK
- `PurchaseInvoiceId` int null FK
- `PaymentDate` datetime2 not null
- `Amount` decimal(18,2) not null
- `PaymentMethod` tinyint not null
- `ReferenceNo` nvarchar(50) null
- `Notes` nvarchar(500) null
- `CreatedByUserId` int null FK
- `UpdatedByUserId` int null FK
- `IsActive` bit not null default 1
- `CreatedAt` datetime2 not null
- `UpdatedAt` datetime2 null

---

# 8b) Cash Box Management (v4.3)

## U2) CashBoxes
### Columns
- `Id` int PK
- `Name` nvarchar(100) not null
- `OpeningBalance` decimal(18,2) not null default 0
- `CurrentBalance` decimal(18,2) not null default 0 (computed from CashTransaction sum)
- `IsDefault` bit not null default 0
- `AccountId` int null FK → Accounts(Id)
- `CurrencyId` int null FK → Currencies(Id)
- `CreatedByUserId` int null FK
- `UpdatedByUserId` int null FK
- `IsActive` bit not null default 1
- `CreatedAt` datetime2 not null
- `UpdatedAt` datetime2 null

### Constraints
- `CHECK (CurrentBalance >= 0)` — never negative

## U3) CashTransactions (v4.3)
### Columns
- `Id` int PK
- `CashBoxId` int not null FK → CashBoxes(Id)
- `TransactionType` tinyint not null (1=OpeningBalance, 2=SalesIncome, 3=Expense, 4=TransferOut, 5=TransferIn, 6=RefundOut, 7=SupplierPayment, 8=CustomerPayment)
- `Amount` decimal(18,2) not null (positive=IN, negative=OUT)
- `BalanceBefore` decimal(18,2) not null
- `BalanceAfter` decimal(18,2) not null
- `ReferenceType` nvarchar(30) null  -- e.g., "SalesInvoice", "PurchaseInvoice"
- `ReferenceId` int null
- `Notes` nvarchar(500) null
- `CreatedByUserId` int null FK
- `CreatedAt` datetime2 not null

### Constraints
- Immutable — no UPDATE, no DELETE. Cancellations use offsetting entries.

# 8c) Product Price History (v4.3)

## U4) ProductPriceHistory
### Columns
- `Id` int PK
- `ProductUnitId` int not null FK → ProductUnits(Id)
- `OldRetailPrice` decimal(18,2) not null
- `NewRetailPrice` decimal(18,2) not null
- `OldWholesalePrice` decimal(18,2) not null
- `NewWholesalePrice` decimal(18,2) not null
- `OldCost` decimal(18,2) not null
- `NewCost` decimal(18,2) not null
- `ChangeReason` nvarchar(250) null
- `ChangedByUserId` int not null FK → Users(Id)
- `CreatedAt` datetime2 not null

### Triggers
- Purchase invoice posting (cost update via WeightedAverage/LastPurchasePrice/SupplierPrice)
- Manual price adjustment in product editor
- Supplier price sync

# 9) Store Settings & System Settings

## U) StoreSettings
### Columns
- `Id` int PK
- `StoreName` nvarchar(150) not null
- `Phone` nvarchar(20) null
- `Address` nvarchar(250) null
- `LogoPath` nvarchar(255) null
- `CurrencyCode` nvarchar(10) not null default 'SAR'
- `DefaultTaxRate` decimal(18,2) not null default 0
- `IsTaxEnabled` bit not null default 0
- `TaxNumber` nvarchar(50) null
- `CreatedByUserId` int null FK
- `UpdatedByUserId` int null FK
- `IsActive` bit not null default 1
- `CreatedAt` datetime2 not null
- `UpdatedAt` datetime2 null

## U5) SystemSettings (v4.3)
### Columns
- `Id` int PK
- `Key` nvarchar(100) not null unique
- `Value` nvarchar(500) not null
- `Description` nvarchar(250) null
- `CreatedAt` datetime2 not null
- `UpdatedAt` datetime2 null

### Seed Data
| Key | Value | Description |
|-----|-------|-------------|
| `CostingMethod` | `1` | 1=WeightedAverage, 2=LastPurchasePrice, 3=SupplierPrice |
| `DefaultCashBoxId` | `1` | Default cash box for invoice payments |

---

# 10) Inventory Movement Log (Critical)
## V) InventoryMovements
### Columns
- `Id` int PK
- `ProductId` int not null FK
- `WarehouseId` int not null FK
- `MovementType` tinyint not null (1=PurchaseIn, 2=SaleOut, 3=SaleReturnIn, 4=PurchaseReturnOut, 5=TransferOut, 6=TransferIn, 7=Adjustment)
- `QuantityChange` decimal(18,3) not null — positive=IN, negative=OUT
- `QuantityBefore` decimal(18,3) not null — stock before this movement
- `QuantityAfter` decimal(18,3) not null — stock after (= Before + Change)
- `ReferenceType` nvarchar(30) not null
- `ReferenceId` int not null
- `UnitCost` decimal(18,2) null
- `MovementDate` datetime2 not null
- `Notes` nvarchar(500) null
- `CreatedByUserId` int null FK
- `UpdatedByUserId` int null FK
- `IsActive` bit not null default 1
- `CreatedAt` datetime2 not null
- `UpdatedAt` datetime2 null

---

# 11) Full SQL Server Implementation Script

```sql
IF DB_ID(N'SalesSystemDb') IS NULL
BEGIN
    CREATE DATABASE SalesSystemDb;
END
GO

USE SalesSystemDb;
GO

-- 1. Users
CREATE TABLE dbo.Users
(
    Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
    UserName        NVARCHAR(50)  NOT NULL,
    PasswordHash    NVARCHAR(256) NOT NULL,
    FullName        NVARCHAR(150) NOT NULL,
    Role            TINYINT       NOT NULL, -- 1=Admin, 2=Manager, 3=Cashier
    CreatedByUserId INT           NULL REFERENCES dbo.Users(Id),
    UpdatedByUserId INT           NULL REFERENCES dbo.Users(Id),
    IsActive        BIT           NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT(1),
    CreatedAt       DATETIME2     NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT(SYSDATETIME()),
    UpdatedAt       DATETIME2     NULL,

    CONSTRAINT UQ_Users_UserName UNIQUE (UserName),
    CONSTRAINT CK_Users_Role CHECK (Role IN (1,2,3))
);

-- 2. Units
CREATE TABLE dbo.Units
(
    Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Units PRIMARY KEY,
    Name            NVARCHAR(50)  NOT NULL,
    Symbol          NVARCHAR(20)  NULL,
    CreatedByUserId INT           NULL REFERENCES dbo.Users(Id),
    UpdatedByUserId INT           NULL REFERENCES dbo.Users(Id),
    IsActive        BIT           NOT NULL CONSTRAINT DF_Units_IsActive DEFAULT(1),
    CreatedAt       DATETIME2     NOT NULL CONSTRAINT DF_Units_CreatedAt DEFAULT(SYSDATETIME()),
    UpdatedAt       DATETIME2     NULL
);

-- 3. Categories
CREATE TABLE dbo.Categories
(
    Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Categories PRIMARY KEY,
    Name            NVARCHAR(100) NOT NULL,
    Description     NVARCHAR(250) NULL,
    CreatedByUserId INT           NULL REFERENCES dbo.Users(Id),
    UpdatedByUserId INT           NULL REFERENCES dbo.Users(Id),
    IsActive        BIT           NOT NULL CONSTRAINT DF_Categories_IsActive DEFAULT(1),
    CreatedAt       DATETIME2     NOT NULL CONSTRAINT DF_Categories_CreatedAt DEFAULT(SYSDATETIME()),
    UpdatedAt       DATETIME2     NULL
);

-- 4. Warehouses
CREATE TABLE dbo.Warehouses
(
    Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Warehouses PRIMARY KEY,
    Name            NVARCHAR(100) NOT NULL,
    Location        NVARCHAR(250) NULL,
    IsDefault       BIT           NOT NULL CONSTRAINT DF_Warehouses_IsDefault DEFAULT(0),
    CreatedByUserId INT           NULL REFERENCES dbo.Users(Id),
    UpdatedByUserId INT           NULL REFERENCES dbo.Users(Id),
    IsActive        BIT           NOT NULL CONSTRAINT DF_Warehouses_IsActive DEFAULT(1),
    CreatedAt       DATETIME2     NOT NULL CONSTRAINT DF_Warehouses_CreatedAt DEFAULT(SYSDATETIME()),
    UpdatedAt       DATETIME2     NULL
);

-- 5. Products
CREATE TABLE dbo.Products
(
    Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Products PRIMARY KEY,
    Barcode         NVARCHAR(50)  NULL,  -- Legacy — prefer UnitBarcodes
    Name            NVARCHAR(150) NOT NULL,
    CategoryId      INT           NULL REFERENCES dbo.Categories(Id),
    SupplierPrice   DECIMAL(18,2) NOT NULL DEFAULT 0,  -- Catalog price for SupplierPrice costing
    AvgCost         DECIMAL(18,2) NOT NULL DEFAULT 0,  -- Computed by UpdateProductPricingService
    ReorderLevel    DECIMAL(18,3) NOT NULL DEFAULT 0,
    MinStock        DECIMAL(18,3) NOT NULL DEFAULT 0,
    Description     NVARCHAR(500) NULL,
    CreatedByUserId INT           NULL REFERENCES dbo.Users(Id),
    UpdatedByUserId INT           NULL REFERENCES dbo.Users(Id),
    IsActive        BIT           NOT NULL CONSTRAINT DF_Products_IsActive DEFAULT(1),
    CreatedAt       DATETIME2     NOT NULL CONSTRAINT DF_Products_CreatedAt DEFAULT(SYSDATETIME()),
    UpdatedAt       DATETIME2     NULL
);

-- 5b. ProductUnits (v4.3 — Dynamic Unit of Measure)
CREATE TABLE dbo.ProductUnits
(
    Id                INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ProductUnits PRIMARY KEY,
    ProductId         INT           NOT NULL REFERENCES dbo.Products(Id),
    UnitName          NVARCHAR(50)  NOT NULL,
    ConversionFactor  DECIMAL(18,3) NOT NULL CONSTRAINT DF_ProductUnits_ConversionFactor DEFAULT(1),
    RetailPrice       DECIMAL(18,2) NOT NULL CONSTRAINT DF_ProductUnits_RetailPrice DEFAULT(0),
    WholesalePrice    DECIMAL(18,2) NOT NULL CONSTRAINT DF_ProductUnits_WholesalePrice DEFAULT(0),
    IsBaseUnit        BIT           NOT NULL CONSTRAINT DF_ProductUnits_IsBaseUnit DEFAULT(0),
    CreatedByUserId   INT           NULL REFERENCES dbo.Users(Id),
    UpdatedByUserId   INT           NULL REFERENCES dbo.Users(Id),
    IsActive          BIT           NOT NULL CONSTRAINT DF_ProductUnits_IsActive DEFAULT(1),
    CreatedAt         DATETIME2     NOT NULL CONSTRAINT DF_ProductUnits_CreatedAt DEFAULT(SYSDATETIME()),
    UpdatedAt         DATETIME2     NULL,

    CONSTRAINT UQ_ProductUnits_Product_UnitName UNIQUE (ProductId, UnitName),
    CONSTRAINT CK_ProductUnits_ConversionFactor CHECK (ConversionFactor > 0)
);
CREATE INDEX IX_ProductUnits_ProductId ON dbo.ProductUnits(ProductId);

-- 5c. UnitBarcodes (v4.3 — Replaces ProductBarcodes)
CREATE TABLE dbo.UnitBarcodes
(
    Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_UnitBarcodes PRIMARY KEY,
    ProductUnitId   INT NOT NULL REFERENCES dbo.ProductUnits(Id),
    BarcodeValue    NVARCHAR(100) NOT NULL,
    IsDefault       BIT NOT NULL DEFAULT 0,
    CreatedAt       DATETIME2 NOT NULL CONSTRAINT DF_UnitBarcodes_CreatedAt DEFAULT(SYSDATETIME()),
    IsActive        BIT NOT NULL CONSTRAINT DF_UnitBarcodes_IsActive DEFAULT(1),

    CONSTRAINT UQ_UnitBarcodes_BarcodeValue UNIQUE (BarcodeValue)
);
CREATE INDEX IX_UnitBarcodes_ProductUnitId ON dbo.UnitBarcodes(ProductUnitId);

-- 6. WarehouseStocks
CREATE TABLE dbo.WarehouseStocks
(
    Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_WarehouseStocks PRIMARY KEY,
    WarehouseId     INT NOT NULL REFERENCES dbo.Warehouses(Id),
    ProductId       INT NOT NULL REFERENCES dbo.Products(Id),
    Quantity        DECIMAL(18,3) NOT NULL DEFAULT 0,
    ReorderLevel    DECIMAL(18,3) NOT NULL DEFAULT 0,
    CreatedByUserId INT           NULL REFERENCES dbo.Users(Id),
    UpdatedByUserId INT           NULL REFERENCES dbo.Users(Id),
    IsActive        BIT           NOT NULL CONSTRAINT DF_WarehouseStocks_IsActive DEFAULT(1),
    CreatedAt       DATETIME2     NOT NULL CONSTRAINT DF_WarehouseStocks_CreatedAt DEFAULT(SYSDATETIME()),
    UpdatedAt       DATETIME2     NOT NULL CONSTRAINT DF_WarehouseStocks_UpdatedAt DEFAULT(SYSDATETIME()),

    CONSTRAINT UQ_WarehouseStocks_Warehouse_Product UNIQUE (WarehouseId, ProductId),
    CONSTRAINT CK_WarehouseStocks_Qty CHECK (Quantity >= 0)
);

-- 7. Suppliers
CREATE TABLE dbo.Suppliers
(
    Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Suppliers PRIMARY KEY,
    Name            NVARCHAR(150) NOT NULL,
    Phone           NVARCHAR(20)  NULL,
    Email           NVARCHAR(100) NULL,
    Address         NVARCHAR(250) NULL,
    OpeningBalance  DECIMAL(18,2) NOT NULL DEFAULT 0,
    CurrentBalance  DECIMAL(18,2) NOT NULL DEFAULT 0,
    CreatedByUserId INT           NULL REFERENCES dbo.Users(Id),
    UpdatedByUserId INT           NULL REFERENCES dbo.Users(Id),
    IsActive        BIT           NOT NULL CONSTRAINT DF_Suppliers_IsActive DEFAULT(1),
    CreatedAt       DATETIME2     NOT NULL CONSTRAINT DF_Suppliers_CreatedAt DEFAULT(SYSDATETIME()),
    UpdatedAt       DATETIME2     NULL
);

-- 8. Customers
CREATE TABLE dbo.Customers
(
    Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Customers PRIMARY KEY,
    Name            NVARCHAR(150) NOT NULL,
    Phone           NVARCHAR(20)  NULL,
    Email           NVARCHAR(100) NULL,
    Address         NVARCHAR(250) NULL,
    OpeningBalance  DECIMAL(18,2) NOT NULL DEFAULT 0,
    CurrentBalance  DECIMAL(18,2) NOT NULL DEFAULT 0,
    CreatedByUserId INT           NULL REFERENCES dbo.Users(Id),
    UpdatedByUserId INT           NULL REFERENCES dbo.Users(Id),
    IsActive        BIT           NOT NULL CONSTRAINT DF_Customers_IsActive DEFAULT(1),
    CreatedAt       DATETIME2     NOT NULL CONSTRAINT DF_Customers_CreatedAt DEFAULT(SYSDATETIME()),
    UpdatedAt       DATETIME2     NULL
);

-- 9. PurchaseInvoices
CREATE TABLE dbo.PurchaseInvoices
(
    Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PurchaseInvoices PRIMARY KEY,
    InvoiceNo       INT           NOT NULL DEFAULT 0,
    SupplierId      INT           NOT NULL REFERENCES dbo.Suppliers(Id),
    WarehouseId     INT           NOT NULL REFERENCES dbo.Warehouses(Id),
    InvoiceDate     DATETIME2     NOT NULL,
    DueDate         DATE          NULL,
    PaymentType     TINYINT       NOT NULL,
    SubTotal        DECIMAL(18,2) NOT NULL DEFAULT 0,
    DiscountAmount  DECIMAL(18,2) NOT NULL DEFAULT 0,
    TaxAmount       DECIMAL(18,2) NOT NULL DEFAULT 0,
    TotalAmount     DECIMAL(18,2) NOT NULL DEFAULT 0,
    PaidAmount      DECIMAL(18,2) NOT NULL DEFAULT 0,
    DueAmount       DECIMAL(18,2) NOT NULL DEFAULT 0,
    Notes           NVARCHAR(500) NULL,
    Status          TINYINT       NOT NULL,
    CreatedByUserId INT           NULL REFERENCES dbo.Users(Id),
    UpdatedByUserId INT           NULL REFERENCES dbo.Users(Id),
    IsActive        BIT           NOT NULL DEFAULT(1),
    CreatedAt       DATETIME2     NOT NULL DEFAULT SYSDATETIME(),
    UpdatedAt       DATETIME2     NULL,

    CONSTRAINT UQ_PurchaseInvoices_InvoiceNo UNIQUE (InvoiceNo)
);

-- 10. PurchaseInvoiceItems
CREATE TABLE dbo.PurchaseInvoiceItems
(
    PurchaseInvoiceItemId   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PurchaseInvoiceItems PRIMARY KEY,
    PurchaseInvoiceId       INT NOT NULL REFERENCES dbo.PurchaseInvoices(Id),
    ProductId               INT NOT NULL REFERENCES dbo.Products(Id),
    Quantity                DECIMAL(18,3) NOT NULL,
    UnitCost                DECIMAL(18,2) NOT NULL,
    DiscountAmount          DECIMAL(18,2) NOT NULL DEFAULT 0,
    LineTotal               DECIMAL(18,2) NOT NULL,
    Notes                   NVARCHAR(250) NULL,
    CreatedByUserId         INT           NULL REFERENCES dbo.Users(Id),
    UpdatedByUserId         INT           NULL REFERENCES dbo.Users(Id),
    IsActive                BIT           NOT NULL DEFAULT(1),
    CreatedAt               DATETIME2     NOT NULL DEFAULT SYSDATETIME(),
    UpdatedAt               DATETIME2     NULL
);

-- 11. SalesInvoices
CREATE TABLE dbo.SalesInvoices
(
    Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SalesInvoices PRIMARY KEY,
    InvoiceNo       INT           NOT NULL DEFAULT 0,
    CustomerId      INT           NULL REFERENCES dbo.Customers(Id),
    WarehouseId     INT           NOT NULL REFERENCES dbo.Warehouses(Id),
    InvoiceDate     DATETIME2     NOT NULL,
    DueDate         DATE          NULL,
    PaymentType     TINYINT       NOT NULL,
    SubTotal        DECIMAL(18,2) NOT NULL DEFAULT 0,
    DiscountAmount  DECIMAL(18,2) NOT NULL DEFAULT 0,
    TaxAmount       DECIMAL(18,2) NOT NULL DEFAULT 0,
    TotalAmount     DECIMAL(18,2) NOT NULL DEFAULT 0,
    PaidAmount      DECIMAL(18,2) NOT NULL DEFAULT 0,
    DueAmount       DECIMAL(18,2) NOT NULL DEFAULT 0,
    Notes           NVARCHAR(500) NULL,
    Status          TINYINT       NOT NULL,
    CreatedByUserId INT           NULL REFERENCES dbo.Users(Id),
    UpdatedByUserId INT           NULL REFERENCES dbo.Users(Id),
    IsActive        BIT           NOT NULL DEFAULT(1),
    CreatedAt       DATETIME2     NOT NULL DEFAULT SYSDATETIME(),
    UpdatedAt       DATETIME2     NULL,

    CONSTRAINT UQ_SalesInvoices_InvoiceNo UNIQUE (InvoiceNo)
);

-- 12. SalesInvoiceItems
CREATE TABLE dbo.SalesInvoiceItems
(
    SalesInvoiceItemId   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SalesInvoiceItems PRIMARY KEY,
    SalesInvoiceId       INT NOT NULL REFERENCES dbo.SalesInvoices(Id),
    ProductId            INT NOT NULL REFERENCES dbo.Products(Id),
    Quantity             DECIMAL(18,3) NOT NULL,
    UnitPrice            DECIMAL(18,2) NOT NULL,
    DiscountAmount       DECIMAL(18,2) NOT NULL DEFAULT 0,
    LineTotal            DECIMAL(18,2) NOT NULL,
    Notes                NVARCHAR(250) NULL,
    CreatedByUserId      INT           NULL REFERENCES dbo.Users(Id),
    UpdatedByUserId      INT           NULL REFERENCES dbo.Users(Id),
    IsActive             BIT           NOT NULL DEFAULT(1),
    CreatedAt            DATETIME2     NOT NULL DEFAULT SYSDATETIME(),
    UpdatedAt            DATETIME2     NULL
);

-- 13. InventoryMovements
CREATE TABLE dbo.InventoryMovements
(
    Id                  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_InventoryMovements PRIMARY KEY,
    ProductId           INT NOT NULL REFERENCES dbo.Products(Id),
    WarehouseId         INT NOT NULL REFERENCES dbo.Warehouses(Id),
    MovementType        TINYINT NOT NULL,
    QuantityChange      DECIMAL(18,3) NOT NULL,
    QuantityBefore      DECIMAL(18,3) NOT NULL,
    QuantityAfter       DECIMAL(18,3) NOT NULL,
    ReferenceType       NVARCHAR(30) NOT NULL,
    ReferenceId         INT NOT NULL,
    UnitCost            DECIMAL(18,2) NULL,
    MovementDate        DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    Notes               NVARCHAR(500) NULL,
    CreatedByUserId     INT NULL REFERENCES dbo.Users(Id),
    UpdatedByUserId     INT NULL REFERENCES dbo.Users(Id),
    IsActive            BIT NOT NULL DEFAULT(1),
    CreatedAt           DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    UpdatedAt           DATETIME2 NULL
);

CREATE INDEX IX_InventoryMovements_Product ON dbo.InventoryMovements(ProductId, MovementDate DESC);
CREATE INDEX IX_InventoryMovements_Reference ON dbo.InventoryMovements(ReferenceType, ReferenceId);

-- 14. StoreSettings
CREATE TABLE dbo.StoreSettings
(
    Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_StoreSettings PRIMARY KEY,
    StoreName       NVARCHAR(150) NOT NULL,
    Phone           NVARCHAR(20)  NULL,
    Address         NVARCHAR(250) NULL,
    LogoPath        NVARCHAR(255) NULL,
    CurrencyCode    NVARCHAR(10)  NOT NULL CONSTRAINT DF_StoreSettings_Currency DEFAULT(N'SAR'),
    DefaultTaxRate  DECIMAL(18,2) NOT NULL CONSTRAINT DF_StoreSettings_TaxRate DEFAULT(0),
    IsTaxEnabled    BIT           NOT NULL CONSTRAINT DF_StoreSettings_TaxEnabled DEFAULT(0),
    TaxNumber       NVARCHAR(50)  NULL,
    CreatedByUserId INT           NULL REFERENCES dbo.Users(Id),
    UpdatedByUserId INT           NULL REFERENCES dbo.Users(Id),
    IsActive        BIT           NOT NULL CONSTRAINT DF_StoreSettings_IsActive DEFAULT(1),
    CreatedAt       DATETIME2     NOT NULL CONSTRAINT DF_StoreSettings_CreatedAt DEFAULT(SYSDATETIME()),
    UpdatedAt       DATETIME2     NULL
);

-- 15. PurchaseReturns
CREATE TABLE dbo.PurchaseReturns
(
    Id                INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PurchaseReturns PRIMARY KEY,
    ReturnNo          NVARCHAR(30)  NOT NULL UNIQUE,
    PurchaseInvoiceId INT           NULL REFERENCES dbo.PurchaseInvoices(Id),
    SupplierId        INT           NOT NULL REFERENCES dbo.Suppliers(Id),
    WarehouseId       INT           NOT NULL REFERENCES dbo.Warehouses(Id),
    ReturnDate        DATETIME2     NOT NULL,
    Reason            NVARCHAR(250) NULL,
    SubTotal          DECIMAL(18,2) NOT NULL DEFAULT 0,
    TotalAmount       DECIMAL(18,2) NOT NULL DEFAULT 0,
    Status            TINYINT       NOT NULL, -- 1=Draft, 2=Posted, 3=Cancelled
    CreatedByUserId   INT           NULL REFERENCES dbo.Users(Id),
    UpdatedByUserId   INT           NULL REFERENCES dbo.Users(Id),
    IsActive          BIT           NOT NULL DEFAULT(1),
    CreatedAt         DATETIME2     NOT NULL DEFAULT SYSDATETIME(),
    UpdatedAt         DATETIME2     NULL
);

-- 16. PurchaseReturnItems
CREATE TABLE dbo.PurchaseReturnItems
(
    PurchaseReturnItemId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PurchaseReturnItems PRIMARY KEY,
    PurchaseReturnId     INT NOT NULL REFERENCES dbo.PurchaseReturns(Id),
    ProductId            INT NOT NULL REFERENCES dbo.Products(Id),
    Quantity             DECIMAL(18,3) NOT NULL,
    UnitCost             DECIMAL(18,2) NOT NULL,
    LineTotal            DECIMAL(18,2) NOT NULL,
    CreatedByUserId      INT           NULL REFERENCES dbo.Users(Id),
    UpdatedByUserId      INT           NULL REFERENCES dbo.Users(Id),
    IsActive             BIT           NOT NULL DEFAULT(1),
    CreatedAt            DATETIME2     NOT NULL DEFAULT SYSDATETIME(),
    UpdatedAt            DATETIME2     NULL
);

-- 17. SalesReturns
CREATE TABLE dbo.SalesReturns
(
    Id                INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SalesReturns PRIMARY KEY,
    ReturnNo          NVARCHAR(30)  NOT NULL UNIQUE,
    SalesInvoiceId    INT           NULL REFERENCES dbo.SalesInvoices(Id),
    CustomerId        INT           NOT NULL REFERENCES dbo.Customers(Id),
    WarehouseId       INT           NOT NULL REFERENCES dbo.Warehouses(Id),
    ReturnDate        DATETIME2     NOT NULL,
    Reason            NVARCHAR(250) NULL,
    SubTotal          DECIMAL(18,2) NOT NULL DEFAULT 0,
    TotalAmount       DECIMAL(18,2) NOT NULL DEFAULT 0,
    Status            TINYINT       NOT NULL, -- 1=Draft, 2=Posted, 3=Cancelled
    CreatedByUserId   INT           NULL REFERENCES dbo.Users(Id),
    UpdatedByUserId   INT           NULL REFERENCES dbo.Users(Id),
    IsActive          BIT           NOT NULL DEFAULT(1),
    CreatedAt         DATETIME2     NOT NULL DEFAULT SYSDATETIME(),
    UpdatedAt         DATETIME2     NULL
);

-- 18. SalesReturnItems
CREATE TABLE dbo.SalesReturnItems
(
    SalesReturnItemId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SalesReturnItems PRIMARY KEY,
    SalesReturnId     INT NOT NULL REFERENCES dbo.SalesReturns(Id),
    ProductId         INT NOT NULL REFERENCES dbo.Products(Id),
    Quantity          DECIMAL(18,3) NOT NULL,
    UnitPrice         DECIMAL(18,2) NOT NULL,
    LineTotal         DECIMAL(18,2) NOT NULL,
    CreatedByUserId   INT           NULL REFERENCES dbo.Users(Id),
    UpdatedByUserId   INT           NULL REFERENCES dbo.Users(Id),
    IsActive          BIT           NOT NULL DEFAULT(1),
    CreatedAt         DATETIME2     NOT NULL DEFAULT SYSDATETIME(),
    UpdatedAt         DATETIME2     NULL
);

-- 19. StockTransfers
CREATE TABLE dbo.StockTransfers
(
    Id                INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_StockTransfers PRIMARY KEY,
    TransferNo        NVARCHAR(30)  NOT NULL UNIQUE,
    FromWarehouseId   INT           NOT NULL REFERENCES dbo.Warehouses(Id),
    ToWarehouseId     INT           NOT NULL REFERENCES dbo.Warehouses(Id),
    TransferDate      DATETIME2     NOT NULL,
    Notes             NVARCHAR(500) NULL,
    Status            TINYINT       NOT NULL, -- 1=Draft, 2=Posted, 3=Cancelled
    CreatedByUserId   INT           NULL REFERENCES dbo.Users(Id),
    UpdatedByUserId   INT           NULL REFERENCES dbo.Users(Id),
    IsActive          BIT           NOT NULL DEFAULT(1),
    CreatedAt         DATETIME2     NOT NULL DEFAULT SYSDATETIME(),
    UpdatedAt         DATETIME2     NULL
);

-- 20. StockTransferItems
CREATE TABLE dbo.StockTransferItems
(
    StockTransferItemId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_StockTransferItems PRIMARY KEY,
    StockTransferId     INT NOT NULL REFERENCES dbo.StockTransfers(Id),
    ProductId           INT NOT NULL REFERENCES dbo.Products(Id),
    Quantity            DECIMAL(18,3) NOT NULL,
    Notes               NVARCHAR(250) NULL,
    CreatedByUserId     INT           NULL REFERENCES dbo.Users(Id),
    UpdatedByUserId     INT           NULL REFERENCES dbo.Users(Id),
    IsActive            BIT           NOT NULL DEFAULT(1),
    CreatedAt           DATETIME2     NOT NULL DEFAULT SYSDATETIME(),
    UpdatedAt           DATETIME2     NULL
);

-- 21. CustomerPayments
CREATE TABLE dbo.CustomerPayments
(
    Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CustomerPayments PRIMARY KEY,
    PaymentNo       NVARCHAR(30)  NOT NULL UNIQUE,
    CustomerId      INT           NOT NULL REFERENCES dbo.Customers(Id),
    SalesInvoiceId  INT           NULL REFERENCES dbo.SalesInvoices(Id),
    PaymentDate     DATETIME2     NOT NULL,
    Amount          DECIMAL(18,2) NOT NULL,
    PaymentMethod   TINYINT       NOT NULL, -- 1=Cash, 2=Bank Transfer, 3=Card, 4=Other
    ReferenceNo     NVARCHAR(50)  NULL,
    Notes           NVARCHAR(500) NULL,
    CreatedByUserId INT           NULL REFERENCES dbo.Users(Id),
    UpdatedByUserId INT           NULL REFERENCES dbo.Users(Id),
    IsActive        BIT           NOT NULL DEFAULT(1),
    CreatedAt       DATETIME2     NOT NULL DEFAULT SYSDATETIME(),
    UpdatedAt       DATETIME2     NULL
);

-- 22. SupplierPayments
CREATE TABLE dbo.SupplierPayments
(
    Id                INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SupplierPayments PRIMARY KEY,
    PaymentNo         NVARCHAR(30)  NOT NULL UNIQUE,
    SupplierId        INT           NOT NULL REFERENCES dbo.Suppliers(Id),
    PurchaseInvoiceId INT           NULL REFERENCES dbo.PurchaseInvoices(Id),
    PaymentDate       DATETIME2     NOT NULL,
    Amount            DECIMAL(18,2) NOT NULL,
    PaymentMethod     TINYINT       NOT NULL,
    ReferenceNo       NVARCHAR(50)  NULL,
    Notes             NVARCHAR(500) NULL,
    CreatedByUserId   INT           NULL REFERENCES dbo.Users(Id),
    UpdatedByUserId   INT           NULL REFERENCES dbo.Users(Id),
    IsActive          BIT           NOT NULL DEFAULT(1),
    CreatedAt         DATETIME2     NOT NULL DEFAULT SYSDATETIME(),
    UpdatedAt         DATETIME2     NULL
);

-- 23. DocumentSequences
CREATE TABLE dbo.DocumentSequences
(
    Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DocumentSequences PRIMARY KEY,
    Prefix          NVARCHAR(10)  NOT NULL UNIQUE,
    LastNumber      INT           NOT NULL CONSTRAINT DF_DocumentSequences_LastNumber DEFAULT(0),
    UpdatedAt       DATETIME2     NOT NULL CONSTRAINT DF_DocumentSequences_UpdatedAt DEFAULT(SYSDATETIME())
);

-- 24. CashBoxes (v4.3)
CREATE TABLE dbo.CashBoxes
(
    Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CashBoxes PRIMARY KEY,
    Name            NVARCHAR(100) NOT NULL,
    OpeningBalance  DECIMAL(18,2) NOT NULL CONSTRAINT DF_CashBoxes_OpeningBalance DEFAULT(0),
    CurrentBalance  DECIMAL(18,2) NOT NULL CONSTRAINT DF_CashBoxes_CurrentBalance DEFAULT(0),
    IsDefault       BIT           NOT NULL CONSTRAINT DF_CashBoxes_IsDefault DEFAULT(0),
    CreatedByUserId INT           NULL REFERENCES dbo.Users(Id),
    UpdatedByUserId INT           NULL REFERENCES dbo.Users(Id),
    IsActive        BIT           NOT NULL CONSTRAINT DF_CashBoxes_IsActive DEFAULT(1),
    CreatedAt       DATETIME2     NOT NULL CONSTRAINT DF_CashBoxes_CreatedAt DEFAULT(SYSDATETIME()),
    UpdatedAt       DATETIME2     NULL,

    CONSTRAINT CK_CashBoxes_CurrentBalance CHECK (CurrentBalance >= 0)
);

-- 25. CashTransactions (v4.3)
CREATE TABLE dbo.CashTransactions
(
    Id                INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CashTransactions PRIMARY KEY,
    CashBoxId         INT           NOT NULL REFERENCES dbo.CashBoxes(Id),
    TransactionType   TINYINT       NOT NULL,
    Amount            DECIMAL(18,2) NOT NULL,
    BalanceBefore     DECIMAL(18,2) NOT NULL,
    BalanceAfter      DECIMAL(18,2) NOT NULL,
    ReferenceType     NVARCHAR(30)  NULL,
    ReferenceId       INT           NULL,
    Notes             NVARCHAR(500) NULL,
    CreatedByUserId   INT           NULL REFERENCES dbo.Users(Id),
    CreatedAt         DATETIME2     NOT NULL CONSTRAINT DF_CashTransactions_CreatedAt DEFAULT(SYSDATETIME()),

    CONSTRAINT CK_CashTransactions_TransactionType CHECK (TransactionType IN (1,2,3,4,5,6,7,8))
);
CREATE INDEX IX_CashTransactions_CashBoxId ON dbo.CashTransactions(CashBoxId, CreatedAt DESC);

-- 26. ProductPriceHistory (v4.3)
CREATE TABLE dbo.ProductPriceHistory
(
    Id                  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ProductPriceHistory PRIMARY KEY,
    ProductUnitId       INT           NOT NULL REFERENCES dbo.ProductUnits(Id),
    OldRetailPrice      DECIMAL(18,2) NOT NULL,
    NewRetailPrice      DECIMAL(18,2) NOT NULL,
    OldWholesalePrice   DECIMAL(18,2) NOT NULL,
    NewWholesalePrice   DECIMAL(18,2) NOT NULL,
    OldCost             DECIMAL(18,2) NOT NULL,
    NewCost             DECIMAL(18,2) NOT NULL,
    ChangeReason        NVARCHAR(250) NULL,
    ChangedByUserId     INT           NOT NULL REFERENCES dbo.Users(Id),
    CreatedAt           DATETIME2     NOT NULL CONSTRAINT DF_ProductPriceHistory_CreatedAt DEFAULT(SYSDATETIME())
);
CREATE INDEX IX_ProductPriceHistory_ProductUnitId ON dbo.ProductPriceHistory(ProductUnitId, CreatedAt DESC);

-- 27. SystemSettings (v4.3)
CREATE TABLE dbo.SystemSettings
(
    Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SystemSettings PRIMARY KEY,
    [Key]           NVARCHAR(100) NOT NULL,
    [Value]         NVARCHAR(500) NOT NULL,
    [Description]   NVARCHAR(250) NULL,
    CreatedAt       DATETIME2     NOT NULL CONSTRAINT DF_SystemSettings_CreatedAt DEFAULT(SYSDATETIME()),
    UpdatedAt       DATETIME2     NULL,

    CONSTRAINT UQ_SystemSettings_Key UNIQUE ([Key])
);

-- 28. SystemLog (v4.3)
CREATE TABLE dbo.SystemLog
(
    Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SystemLog PRIMARY KEY,
    [Level]         NVARCHAR(20)  NOT NULL,
    [Source]        NVARCHAR(100) NULL,
    [Message]       NVARCHAR(MAX) NOT NULL,
    [Exception]     NVARCHAR(MAX) NULL,
    CreatedAt       DATETIME2     NOT NULL CONSTRAINT DF_SystemLog_CreatedAt DEFAULT(SYSDATETIME())
);
CREATE INDEX IX_SystemLog_Level ON dbo.SystemLog([Level], CreatedAt DESC);

-- ============================================================================
-- 29+ New Tables (v4.7+ — Accounting Foundation)
-- ============================================================================

-- 29. Currencies
CREATE TABLE dbo.Currencies
(
    Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Currencies PRIMARY KEY,
    Code            NVARCHAR(10)  NOT NULL,
    Name            NVARCHAR(100) NOT NULL,
    Symbol          NVARCHAR(10)  NULL,
    ExchangeRate    DECIMAL(18,6) NOT NULL CONSTRAINT DF_Currencies_ExchangeRate DEFAULT(1),
    IsDefault       BIT           NOT NULL CONSTRAINT DF_Currencies_IsDefault DEFAULT(0),
    IsActive        BIT           NOT NULL CONSTRAINT DF_Currencies_IsActive DEFAULT(1),
    CreatedAt       DATETIME2     NOT NULL CONSTRAINT DF_Currencies_CreatedAt DEFAULT(SYSDATETIME()),
    UpdatedAt       DATETIME2     NULL,

    CONSTRAINT UQ_Currencies_Code UNIQUE (Code)
);

-- 30. Accounts (Chart of Accounts)
CREATE TABLE dbo.Accounts
(
    Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Accounts PRIMARY KEY,
    AccountCode     NVARCHAR(20)  NOT NULL,
    AccountName     NVARCHAR(150) NOT NULL,
    AccountType     TINYINT       NOT NULL, -- 1=Asset, 2=Liability, 3=Equity, 4=Income, 5=Expense
    ParentAccountId INT           NULL REFERENCES dbo.Accounts(Id),
    IsActive        BIT           NOT NULL CONSTRAINT DF_Accounts_IsActive DEFAULT(1),
    CreatedAt       DATETIME2     NOT NULL CONSTRAINT DF_Accounts_CreatedAt DEFAULT(SYSDATETIME()),
    UpdatedAt       DATETIME2     NULL,

    CONSTRAINT UQ_Accounts_AccountCode UNIQUE (AccountCode)
);

-- 31. JournalEntries
CREATE TABLE dbo.JournalEntries
(
    Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_JournalEntries PRIMARY KEY,
    EntryDate       DATETIME2     NOT NULL,
    ReferenceType   NVARCHAR(30)  NOT NULL, -- 'SalesInvoice', 'PurchaseInvoice', 'Payment', etc.
    ReferenceId     INT           NOT NULL,
    Description     NVARCHAR(500) NULL,
    IsPosted        BIT           NOT NULL CONSTRAINT DF_JournalEntries_IsPosted DEFAULT(1),
    CreatedByUserId INT           NULL REFERENCES dbo.Users(Id),
    CreatedAt       DATETIME2     NOT NULL CONSTRAINT DF_JournalEntries_CreatedAt DEFAULT(SYSDATETIME()),
    PostedAt        DATETIME2     NULL
);
CREATE INDEX IX_JournalEntries_Reference ON dbo.JournalEntries(ReferenceType, ReferenceId);
CREATE INDEX IX_JournalEntries_EntryDate ON dbo.JournalEntries(EntryDate DESC);

-- 32. JournalEntryLines
CREATE TABLE dbo.JournalEntryLines
(
    Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_JournalEntryLines PRIMARY KEY,
    JournalEntryId  INT           NOT NULL REFERENCES dbo.JournalEntries(Id),
    AccountId       INT           NOT NULL REFERENCES dbo.Accounts(Id),
    DebitAmount     DECIMAL(18,2) NOT NULL CONSTRAINT DF_JournalEntryLines_Debit DEFAULT(0),
    CreditAmount    DECIMAL(18,2) NOT NULL CONSTRAINT DF_JournalEntryLines_Credit DEFAULT(0),
    Description     NVARCHAR(250) NULL,

    CONSTRAINT CK_JournalEntryLines_Amounts CHECK (DebitAmount >= 0 AND CreditAmount >= 0)
);
CREATE INDEX IX_JournalEntryLines_JournalEntryId ON dbo.JournalEntryLines(JournalEntryId);
CREATE INDEX IX_JournalEntryLines_AccountId ON dbo.JournalEntryLines(AccountId);

-- 33. PurchaseLots (FIFO/FEFO tracking)
CREATE TABLE dbo.PurchaseLots
(
    Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PurchaseLots PRIMARY KEY,
    ProductId       INT           NOT NULL REFERENCES dbo.Products(Id),
    PurchaseInvoiceId INT         NOT NULL REFERENCES dbo.PurchaseInvoices(Id),
    LotNo           NVARCHAR(50)  NULL,
    ManufactureDate DATE          NULL,
    ExpiryDate      DATE          NULL,
    Quantity        DECIMAL(18,3) NOT NULL,
    RemainingQty    DECIMAL(18,3) NOT NULL,
    UnitCost        DECIMAL(18,2) NOT NULL,
    CreatedAt       DATETIME2     NOT NULL CONSTRAINT DF_PurchaseLots_CreatedAt DEFAULT(SYSDATETIME()),

    CONSTRAINT CK_PurchaseLots_RemainingQty CHECK (RemainingQty >= 0)
);
CREATE INDEX IX_PurchaseLots_ProductId ON dbo.PurchaseLots(ProductId, ExpiryDate);
CREATE INDEX IX_PurchaseLots_PurchaseInvoiceId ON dbo.PurchaseLots(PurchaseInvoiceId);

-- 34. FiscalYears
CREATE TABLE dbo.FiscalYears
(
    Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_FiscalYears PRIMARY KEY,
    YearName        NVARCHAR(20)  NOT NULL, -- e.g., '2026', '2026-2027'
    StartDate       DATE          NOT NULL,
    EndDate         DATE          NOT NULL,
    IsClosed        BIT           NOT NULL CONSTRAINT DF_FiscalYears_IsClosed DEFAULT(0),
    CreatedAt       DATETIME2     NOT NULL CONSTRAINT DF_FiscalYears_CreatedAt DEFAULT(SYSDATETIME()),

    CONSTRAINT CK_FiscalYears_EndAfterStart CHECK (EndDate > StartDate)
);

-- 35. Taxes
CREATE TABLE dbo.Taxes
(
    Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Taxes PRIMARY KEY,
    TaxName         NVARCHAR(100) NOT NULL,
    TaxRate         DECIMAL(18,2) NOT NULL,
    TaxType         TINYINT       NOT NULL, -- 1=Inclusive, 2=Exclusive, 3=Withholding
    IsDefault       BIT           NOT NULL CONSTRAINT DF_Taxes_IsDefault DEFAULT(0),
    IsActive        BIT           NOT NULL CONSTRAINT DF_Taxes_IsActive DEFAULT(1),
    CreatedAt       DATETIME2     NOT NULL CONSTRAINT DF_Taxes_CreatedAt DEFAULT(SYSDATETIME()),
    UpdatedAt       DATETIME2     NULL
);

-- ============================================================================
-- ALTER TABLE Statements for New Columns (v4.7+)
-- ============================================================================

-- Customers: New columns
ALTER TABLE dbo.Customers ADD
    AccountId       INT NULL REFERENCES dbo.Accounts(Id),
    CurrencyId      INT NULL REFERENCES dbo.Currencies(Id),
    CreditLimit     DECIMAL(18,2) NOT NULL CONSTRAINT DF_Customers_CreditLimit DEFAULT(0),
    CustomerType    TINYINT NULL; -- 1=Retail, 2=Wholesale, 3=Corporate

-- Suppliers: New columns
ALTER TABLE dbo.Suppliers ADD
    AccountId       INT NULL REFERENCES dbo.Accounts(Id),
    CurrencyId      INT NULL REFERENCES dbo.Currencies(Id),
    CreditLimit     DECIMAL(18,2) NOT NULL CONSTRAINT DF_Suppliers_CreditLimit DEFAULT(0);

-- Users: New columns
ALTER TABLE dbo.Users ADD
    MustChangePassword BIT NOT NULL CONSTRAINT DF_Users_MustChangePassword DEFAULT(1),
    [Status]           TINYINT NOT NULL CONSTRAINT DF_Users_Status DEFAULT(1), -- 1=Active, 2=Locked, 3=Suspended
    LastLoginAt        DATETIME2 NULL,
    FailedLoginAttempts INT NOT NULL CONSTRAINT DF_Users_FailedLoginAttempts DEFAULT(0);

-- Products: New columns
ALTER TABLE dbo.Products ADD
    AvgCost         DECIMAL(18,2) NOT NULL CONSTRAINT DF_Products_AvgCost DEFAULT(0),
    TrackExpiry     BIT NOT NULL CONSTRAINT DF_Products_TrackExpiry DEFAULT(0),
    IsExpirable     BIT NOT NULL CONSTRAINT DF_Products_IsExpirable DEFAULT(0);

-- CashBoxes: New columns
ALTER TABLE dbo.CashBoxes ADD
    AccountId       INT NULL REFERENCES dbo.Accounts(Id),
    CurrencyId      INT NULL REFERENCES dbo.Currencies(Id);

-- ============================================================================
-- Seed Data for SystemSettings
-- ============================================================================
INSERT INTO dbo.SystemSettings ([Key], [Value], [Description])
VALUES
    (N'CostingMethod', N'1', N'1=WeightedAverage, 2=LastPurchasePrice, 3=SupplierPrice'),
    (N'DefaultCashBoxId', N'1', N'Default cash box for invoice payments');
GO

```
