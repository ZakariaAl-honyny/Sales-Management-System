# Database Schema Design
# Sales Management System — MVP v3.0
# Platform: SQL Server 2019+
# 22 Tables | decimal-only financials | nvarchar text | Soft delete

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
- `Code` nvarchar(30) null unique
- `Barcode` nvarchar(50) null unique
- `Name` nvarchar(150) not null
- `CategoryId` int null FK
- `WholesaleUnitId` int null FK
- `RetailUnitId` int null FK
- `ConversionFactor` decimal(18,3) not null default 1
- `PurchasePrice` decimal(18,2) not null default 0
- `WholesalePrice` decimal(18,2) not null default 0
- `RetailPrice` decimal(18,2) not null default 0
- `ReorderLevel` decimal(18,3) not null default 0  -- Minimum stock level for low-stock alerts
- `MinStock` decimal(18,3) not null default 0
- `Description` nvarchar(500) null
- `CreatedByUserId` int null FK
- `UpdatedByUserId` int null FK
- `IsActive` bit not null default 1
- `CreatedAt` datetime2 not null
- `UpdatedAt` datetime2 null

## D2) ProductBarcodes (NEW - Multi-barcode support)
### Columns
- `Id` int PK
- `ProductId` int not null FK → Products(Id)
- `BarcodeValue` nvarchar(100) not null unique
- `UnitType` tinyint not null default 0  -- 0=Retail, 1=Wholesale
- `IsDefault` bit not null default 0
- `CreatedAt` datetime2 not null
- `IsActive` bit not null default 1

---

## E) Warehouses
### Columns
- `Id` int PK
- `Code` nvarchar(30) null unique
- `Name` nvarchar(100) not null
- `Location` nvarchar(250) null
- `IsDefault` bit not null default 0
- `CreatedByUserId` int null FK
- `UpdatedByUserId` int null FK
- `IsActive` bit not null default 1
- `CreatedAt` datetime2 not null
- `UpdatedAt` datetime2 null

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
- `Code` nvarchar(30) null unique
- `Name` nvarchar(150) not null
- `Phone` nvarchar(20) null
- `Email` nvarchar(100) null
- `Address` nvarchar(250) null
- `OpeningBalance` decimal(18,2) not null default 0
- `CurrentBalance` decimal(18,2) not null default 0
- `CreatedByUserId` int null FK
- `UpdatedByUserId` int null FK
- `IsActive` bit not null default 1
- `CreatedAt` datetime2 not null
- `UpdatedAt` datetime2 null

---

## H) Customers
### Columns
- `Id` int PK
- `Code` nvarchar(30) null unique
- `Name` nvarchar(150) not null
- `Phone` nvarchar(20) null
- `Email` nvarchar(100) null
- `Address` nvarchar(250) null
- `OpeningBalance` decimal(18,2) not null default 0
- `CurrentBalance` decimal(18,2) not null default 0
- `CreatedByUserId` int null FK
- `UpdatedByUserId` int null FK
- `IsActive` bit not null default 1
- `CreatedAt` datetime2 not null
- `UpdatedAt` datetime2 null

---

# 4) Purchases

## I) PurchaseInvoices
### Columns
- `Id` int PK
- `InvoiceNo` nvarchar(30) not null unique
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

## J) PurchaseInvoiceItems
### Columns
- `PurchaseInvoiceItemId` int PK
- `PurchaseInvoiceId` int not null FK
- `ProductId` int not null FK
- `Quantity` decimal(18,3) not null
- `UnitCost` decimal(18,2) not null
- `DiscountAmount` decimal(18,2) not null default 0
- `LineTotal` decimal(18,2) not null
- `Notes` nvarchar(250) null
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
- `InvoiceNo` nvarchar(30) not null unique
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

# 9) Store Settings

## U) StoreSettings
### Columns
- `Id` int PK
- `StoreName` nvarchar(150) not null
- `Phone` nvarchar(20) null
- `Address` nvarchar(250) null
- `LogoPath` nvarchar(255) null
- `CurrencyCode` nvarchar(10) not null default 'SAR'
- `DefaultTaxRate` decimal(5,2) not null default 0
- `IsTaxEnabled` bit not null default 0
- `TaxNumber` nvarchar(50) null
- `CreatedByUserId` int null FK
- `UpdatedByUserId` int null FK
- `IsActive` bit not null default 1
- `CreatedAt` datetime2 not null
- `UpdatedAt` datetime2 null

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
    Code            NVARCHAR(30)  NULL,
    Name            NVARCHAR(100) NOT NULL,
    Location        NVARCHAR(250) NULL,
    IsDefault       BIT           NOT NULL CONSTRAINT DF_Warehouses_IsDefault DEFAULT(0),
    CreatedByUserId INT           NULL REFERENCES dbo.Users(Id),
    UpdatedByUserId INT           NULL REFERENCES dbo.Users(Id),
    IsActive        BIT           NOT NULL CONSTRAINT DF_Warehouses_IsActive DEFAULT(1),
    CreatedAt       DATETIME2     NOT NULL CONSTRAINT DF_Warehouses_CreatedAt DEFAULT(SYSDATETIME()),
    UpdatedAt       DATETIME2     NULL,

    CONSTRAINT UQ_Warehouses_Code UNIQUE (Code)
);

-- 5. Products
CREATE TABLE dbo.Products
(
    Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Products PRIMARY KEY,
    Code            NVARCHAR(30)  NULL,
    Barcode         NVARCHAR(50)  NULL,
    Name            NVARCHAR(150) NOT NULL,
    CategoryId      INT           NULL REFERENCES dbo.Categories(Id),
    WholesaleUnitId INT           NULL REFERENCES dbo.Units(Id),
    RetailUnitId    INT           NULL REFERENCES dbo.Units(Id),
    ConversionFactor DECIMAL(18,3) NOT NULL DEFAULT 1,
    PurchasePrice   DECIMAL(18,2) NOT NULL DEFAULT 0,
    WholesalePrice  DECIMAL(18,2) NOT NULL DEFAULT 0,
    RetailPrice     DECIMAL(18,2) NOT NULL DEFAULT 0,
    ReorderLevel    DECIMAL(18,3) NOT NULL DEFAULT 0,
    MinStock        DECIMAL(18,3) NOT NULL DEFAULT 0,
    Description     NVARCHAR(500) NULL,
    CreatedByUserId INT           NULL REFERENCES dbo.Users(Id),
    UpdatedByUserId INT           NULL REFERENCES dbo.Users(Id),
    IsActive        BIT           NOT NULL CONSTRAINT DF_Products_IsActive DEFAULT(1),
    CreatedAt       DATETIME2     NOT NULL CONSTRAINT DF_Products_CreatedAt DEFAULT(SYSDATETIME()),
    UpdatedAt       DATETIME2     NULL,

    CONSTRAINT UQ_Products_Code UNIQUE (Code),
    CONSTRAINT UQ_Products_Barcode UNIQUE (Barcode)
);

-- 5b. ProductBarcodes (Multi-barcode support)
CREATE TABLE dbo.ProductBarcodes
(
    Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ProductBarcodes PRIMARY KEY,
    ProductId       INT NOT NULL REFERENCES dbo.Products(Id),
    BarcodeValue    NVARCHAR(100) NOT NULL,
    UnitType        TINYINT NOT NULL DEFAULT 0,  -- 0=Retail, 1=Wholesale
    IsDefault       BIT NOT NULL DEFAULT 0,
    CreatedAt       DATETIME2 NOT NULL CONSTRAINT DF_ProductBarcodes_CreatedAt DEFAULT(SYSDATETIME()),
    IsActive        BIT NOT NULL CONSTRAINT DF_ProductBarcodes_IsActive DEFAULT(1),

    CONSTRAINT UQ_ProductBarcodes_BarcodeValue UNIQUE (BarcodeValue)
);
CREATE INDEX IX_ProductBarcodes_ProductId ON dbo.ProductBarcodes(ProductId);

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
    Code            NVARCHAR(30)  NULL,
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
    UpdatedAt       DATETIME2     NULL,

    CONSTRAINT UQ_Suppliers_Code UNIQUE (Code)
);

-- 8. Customers
CREATE TABLE dbo.Customers
(
    Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Customers PRIMARY KEY,
    Code            NVARCHAR(30)  NULL,
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
    UpdatedAt       DATETIME2     NULL,

    CONSTRAINT UQ_Customers_Code UNIQUE (Code)
);

-- 9. PurchaseInvoices
CREATE TABLE dbo.PurchaseInvoices
(
    Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PurchaseInvoices PRIMARY KEY,
    InvoiceNo       NVARCHAR(30)  NOT NULL UNIQUE,
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
    UpdatedAt       DATETIME2     NULL
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
    InvoiceNo       NVARCHAR(30)  NOT NULL UNIQUE,
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
    UpdatedAt       DATETIME2     NULL
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

```
