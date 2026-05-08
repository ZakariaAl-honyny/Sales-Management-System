# Product Requirements Document
# Sales Management System — MVP v3.0
# Platform: .NET 10 LTS | Year: 2026

---

## 1. Executive Summary

A local desktop sales management system for small retail shops.
Built on Clean Architecture with WinForms Desktop + ASP.NET Core 10 API
+ SQL Server. Designed for future expansion to Web and Mobile.

---

## 2. Scope

### In Scope
- User authentication with role-based access (Admin/Manager/Cashier)
- Product management with categories and units
- Multi-warehouse inventory management
- Purchase invoices (Cash/Credit/Mixed)
- Sales invoices (Cash/Credit/Mixed)
- Sales returns and purchase returns
- Stock transfers between warehouses
- Customer and supplier balance tracking
- Customer and supplier payments
- Basic reports (daily sales, stock, balances)
- Invoice printing with store logo
- Database backup and restore
- Audit logging (who did what and when)

### Out of Scope (Future Phases)
- Web interface
- Mobile application
- Multi-branch management
- Full accounting system
- External integrations

---

## 3. Functional Requirements

### 3.1 Authentication
- Login with username + password
- JWT token issued on successful login (expires: 8 hours)
- Refresh token for session continuity
- Role stored in JWT claims: Admin=1, Manager=2, Cashier=3
- Failed login attempts logged
- Logout clears token from Desktop memory

### 3.2 Product Management
- Add / Edit / Deactivate products (no hard delete)
- Fields: Code, Barcode, Name, Category, Unit,
          PurchasePrice, SalePrice, MinStock, Description
- Search by: Name, Code, Barcode
- Filter by: Category, Active status
- Barcode scanner support (keyboard input simulation)

### 3.3 Warehouse Management
- Add / Edit / Deactivate warehouses
- One warehouse flagged as IsDefault
- View stock per warehouse per product
- Stock transfer between warehouses

### 3.4 Purchase Invoice
- Select supplier and destination warehouse
- Add multiple products with quantity and unit cost
- Apply line-level discount per item
- Apply invoice-level discount
- Tax calculation (optional, from settings)
- Payment type: Cash / Credit / Mixed
- If Mixed: enter paid amount, system calculates due amount
- Auto-generate invoice number (format: PUR-2026-000001)
- On Post: increase stock in selected warehouse
- On Post: update supplier balance if credit/mixed
- On Cancel: reverse stock and balance

### 3.5 Sales Invoice
- Select customer (or use default Cash customer)
- Select source warehouse
- Add multiple products with quantity and unit price
- Apply line-level and invoice-level discounts
- Payment type: Cash / Credit / Mixed
- Validate: quantity available in selected warehouse
- Auto-generate invoice number (format: INV-2026-000001)
- On Post: decrease stock from selected warehouse
- On Post: update customer balance if credit/mixed
- On Cancel: reverse stock and balance
- Print invoice after saving

### 3.6 Sales Return
- Reference original invoice (optional)
- If referenced: validate return quantity ≤ sold quantity
  minus previously returned quantity
- On Post: increase stock in selected warehouse
- On Post: decrease customer balance by return amount

### 3.7 Purchase Return
- Reference original purchase invoice (optional)
- On Post: decrease stock from selected warehouse
- On Post: decrease supplier balance by return amount

### 3.8 Stock Transfer
- Select source warehouse and destination warehouse
- Source ≠ Destination (enforced at DB and application level)
- Add products with quantities
- Validate: sufficient stock in source warehouse
- On Post: decrease source warehouse stock
- On Post: increase destination warehouse stock
- Record both movements in InventoryMovements

### 3.9 Payments
- Customer payment: record cash received from customer
  → decreases customer balance
- Supplier payment: record cash paid to supplier
  → decreases supplier balance
- Both linked optionally to specific invoice

### 3.10 Reports
- Daily sales report (by date range)
- Daily purchases report (by date range)
- Stock report (per warehouse or all warehouses)
- Customer balance report (all or specific customer)
- Supplier balance report (all or specific supplier)
- Product movement report (history for one product)
- Low stock alert report (below MinStock)

### 3.11 Settings
- Store name, phone, address
- Logo upload (stored as file path)
- Default tax rate and tax toggle
- Default warehouse selection

### 3.12 Backup
- Manual backup: creates .bak file with timestamp
- Restore from .bak file
- Backup location configurable

---

## 4. Non-Functional Requirements

### 4.1 Performance
- Invoice save time: < 2 seconds
- Report generation: < 5 seconds for up to 1 year of data
- Product search: < 500ms

### 4.2 Security
- JWT authentication on all API endpoints
- Role-based access at API level (policy-based authorization)
- Role-based UI element visibility at Desktop level
- FluentValidation on all incoming API requests
- BCrypt password hashing (work factor: 12)
- Connection string encrypted or in environment variable

### 4.3 Reliability
- All financial operations wrapped in database transactions
- Automatic rollback on any failure
- Serilog file logging for all errors and critical operations
- Database constraints as final validation layer

### 4.4 Deployment
- API runs as Windows Service (no console window)
- Desktop distributed as MSI installer
- .NET 10 runtime bundled in installer (self-contained)
- Setup wizard guides user through installation

---

## 5. Database Entities Summary
Users → Authentication and authorization
Units → Product measurement units
Categories → Product categories
Products → Product catalog
Warehouses → Storage locations
WarehouseStocks → Stock per product per warehouse ⚠️ Critical
Suppliers → Supplier master data with balance
Customers → Customer master data with balance
PurchaseInvoices → Purchase invoice headers
PurchaseInvoiceItems → Purchase invoice line items
SalesInvoices → Sales invoice headers
SalesInvoiceItems → Sales invoice line items
PurchaseReturns → Purchase return headers
PurchaseReturnItems → Purchase return line items
SalesReturns → Sales return headers
SalesReturnItems → Sales return line items
StockTransfers → Transfer headers
StockTransferItems → Transfer line items
CustomerPayments → Payments received from customers
SupplierPayments → Payments made to suppliers
InventoryMovements → Complete audit trail of stock changes ⚠️ Critical
StoreSettings → Store configuration (single row)
DocumentSequences → Auto-increment invoice numbers

text


---

## 6. Solution Architecture
SalesSystem/
├── SalesSystem.Contracts/
│ ├── DTOs/
│ ├── Requests/
│ ├── Responses/
│ └── Common/
│ ├── Result.cs
│ ├── PagedResult.cs
│ └── ErrorCodes.cs
│
├── SalesSystem.Domain/
│ ├── Entities/
│ ├── Enums/
│ ├── Exceptions/
│ └── Common/
│ └── BaseEntity.cs
│
├── SalesSystem.Application/
│ ├── Interfaces/
│ │ ├── Repositories/
│ │ ├── Services/
│ │ └── IUnitOfWork.cs
│ └── Services/
│ ├── ProductService.cs
│ ├── SalesService.cs ⚠️ Critical
│ ├── PurchaseService.cs ⚠️ Critical
│ ├── SalesReturnService.cs ⚠️ Critical
│ ├── PurchaseReturnService.cs ⚠️ Critical
│ ├── InventoryService.cs ⚠️ Critical
│ ├── StockTransferService.cs ⚠️ Critical
│ ├── PaymentService.cs
│ ├── ReportService.cs
│ ├── AuthService.cs
│ ├── BackupService.cs
│ └── DocumentSequenceService.cs ⚠️ Thread-safe
│
├── SalesSystem.Infrastructure/
│ ├── Data/
│ │ ├── SalesDbContext.cs
│ │ └── Configurations/
│ ├── Repositories/
│ ├── Migrations/
│ └── Services/
│ └── BackupService.cs
│
├── SalesSystem.Api/
│ ├── Controllers/
│ ├── Validators/ ← FluentValidation
│ ├── Middleware/
│ │ ├── ExceptionMiddleware.cs
│ │ └── RequestLoggingMiddleware.cs
│ └── Extensions/
│ ├── AuthExtensions.cs
│ └── ValidationExtensions.cs
│
└── SalesSystem.Desktop/
├── Forms/
├── Controls/
│ ├── Common/
│ ├── Products/
│ ├── Sales/
│ ├── Purchases/
│ ├── Returns/
│ ├── Warehouses/
│ ├── Customers/
│ ├── Suppliers/
│ └── Reports/
├── Services/
│ ├── Api/
│ ├── Navigation/
│ ├── Auth/
│ ├── Dialogs/
│ ├── Notifications/
│ └── Printing/
├── Messaging/
│ ├── EventBus.cs
│ └── Messages/
└── AppHost/
├── DependencyInjection.cs
└── AppSettings.cs

text


---

## 7. Implementation Phases

### Phase 1 — Foundation
Tasks:

Create Solution with 6 projects
Configure .NET 10 project references
Write all Domain Entities
Write all Contracts (DTOs, Requests, Result<T>)
Write all Custom Exceptions
Write SQL creation scripts with seed data
Configure SalesDbContext with all Configurations
Run initial EF Core Migration
Definition of Done:

Solution builds without errors
Database created from migration
Seed data inserted (admin user, default warehouse,
cash customer, units)
text


### Phase 2 — Backend Core
Tasks:

Implement IUnitOfWork and repositories
Implement AuthService with JWT
Implement ProductService (simplest — use as template)
Implement CustomerService, SupplierService
Implement WarehouseService
Implement DocumentSequenceService (thread-safe)
Wire up API Controllers (Products, Customers,
Suppliers, Warehouses, Auth)
Add FluentValidation for all request models
Add Serilog configuration
Add Global Exception Handler middleware
Add JWT authorization policies per role
Definition of Done:

All basic CRUD endpoints work via Swagger
Invalid requests rejected with clear error messages
All requests logged
Unauthorized requests return 401/403
text


### Phase 3 — Business Logic (Critical)
Tasks:

Implement InventoryService ⚠️
Implement PurchaseService ⚠️
Implement SalesService ⚠️
Implement SalesReturnService ⚠️
Implement PurchaseReturnService ⚠️
Implement StockTransferService ⚠️
Implement PaymentService
Wire up remaining API Controllers
Definition of Done:

Complete purchase flow works end-to-end
Complete sales flow works end-to-end
Stock deducted correctly on sale
Stock increased correctly on purchase
Returns reverse stock and balance correctly
All operations are transactional (rollback tested)
InventoryMovements populated on every stock change
text


### Phase 4 — Desktop Shell
Tasks:

Implement EventBus (with WeakReference and UI thread safety)
Implement NavigationService
Implement AuthApiService and token storage
Build MainForm with Sidebar
Build LoginForm
Implement role-based sidebar visibility
Build Common UserControls
(SearchBar, Loading, SummaryCard, PaymentPanel)
Implement NotificationService
Implement DialogService
Definition of Done:

Login works and retrieves JWT token
Role determines which sidebar items are visible
Navigation between screens works
Common controls render correctly
text


### Phase 5 — Desktop Modules
Order of implementation:

Products Module ← Use as template for others
Customers Module
Suppliers Module
Warehouses Module
Purchases Module ⚠️
Sales Module ⚠️ Most important
Returns Module ⚠️
Stock Transfer Module
Reports Module
Payments Module
Definition of Done per module:

List screen with search and filter
Add/Edit dialog
Delete/Deactivate with confirmation
EventBus messages published on changes
Other screens refresh automatically on change
text


### Phase 6 — Printing
Tasks:

Build InvoicePrinter class
Build ReceiptPrinter class (80mm thermal)
Support store logo in header
Support store name, phone, address in header
Support invoice details and totals
Print preview before printing
Definition of Done:

Full A4 invoice prints correctly
80mm receipt prints correctly
Logo appears in header when configured
text


### Phase 7 — Production Readiness
Tasks:

Implement BackupService (SQL Server .bak)
Build Settings screen (Admin only)
Build User Management screen (Admin only)
Configure API as Windows Service
Create Inno Setup installer script
Encrypt connection string
Final security review
Performance testing with 1 year of data
Definition of Done:

System installs on clean Windows machine
API starts automatically with Windows
Backup creates valid .bak file
Restore successfully restores data
All role restrictions working in both API and Desktop
text


---

## 8. Critical Business Rules Reference
SALES FLOW:

Validate all items have sufficient stock in warehouse
Generate invoice number (thread-safe)
BEGIN TRANSACTION
Create invoice (Draft status)
Add all items (Domain calculates LineTotal)
Domain validates: PaidAmount <= TotalAmount
Post invoice (status = Posted)
Save invoice to DB (get invoice ID)
For each item: decrease WarehouseStock
For each item: create InventoryMovement record
If DueAmount > 0: increase Customer.CurrentBalance
COMMIT TRANSACTION
→ Any failure: ROLLBACK ALL
PURCHASE FLOW: (mirror of sales with suppliers)
RETURN FLOW: (reverse of original operation)
TRANSFER FLOW: (decrease source, increase destination — same transaction)
PAYMENT FLOW: (decrease balance, no stock change)

Sales Management System — PRD MVP v3.0 (النسخة النهائية الشاملة)
1. معلومات المشروع
text

الاسم:     Sales Management System
الإصدار:   MVP v3.0
التاريخ:   2025
المعمارية: Clean Architecture + WinForms Desktop + ASP.NET Core API
قاعدة البيانات: SQL Server
2. وصف المشروع
نظام إدارة مبيعات لمحل صغير يعمل على Desktop محلياً عبر ASP.NET Core Web API وقاعدة بيانات SQL Server. يشمل إدارة المنتجات، المخازن المتع5. قاعدة البيانات — Schema الكامل
(Updated to match docs/database-schema.md)

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
- `UnitId` int null FK
- `PurchasePrice` decimal(18,2) not null default 0
- `SalePrice` decimal(18,2) not null default 0
- `MinStock` decimal(18,3) not null default 0
- `Description` nvarchar(500) null
- `CreatedByUserId` int null FK
- `UpdatedByUserId` int null FK
- `IsActive` bit not null default 1
- `CreatedAt` datetime2 not null
- `UpdatedAt` datetime2 null

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
    UnitId          INT           NULL REFERENCES dbo.Units(Id),
    PurchasePrice   DECIMAL(18,2) NOT NULL DEFAULT 0,
    SalePrice       DECIMAL(18,2) NOT NULL DEFAULT 0,
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

-- 6. WarehouseStocks
CREATE TABLE dbo.WarehouseStocks
(
    Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_WarehouseStocks PRIMARY KEY,
    WarehouseId     INT NOT NULL REFERENCES dbo.Warehouses(Id),
    ProductId       INT NOT NULL REFERENCES dbo.Products(Id),
    Quantity        DECIMAL(18,3) NOT NULL DEFAULT 0,
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
    LineTotal               DECIMAL(18,2) NOT NULL
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
    LineTotal            DECIMAL(18,2) NOT NULL
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
    DefaultTaxRate  DECIMAL(5,2)  NOT NULL CONSTRAINT DF_StoreSettings_TaxRate DEFAULT(0),
    IsTaxEnabled    BIT           NOT NULL DEFAULT(0),
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
    LineTotal            DECIMAL(18,2) NOT NULL
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
    LineTotal         DECIMAL(18,2) NOT NULL
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
    Notes               NVARCHAR(250) NULL
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

6. Domain Entities — C# Classes
6.1 Base Entity
csharp

// SalesSystem.Domain/Common/BaseEntity.cs
namespace SalesSystem.Domain.Common;

public abstract class BaseEntity
{
    public int Id { get; protected set; }
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; protected set; }
    public bool IsActive { get; protected set; } = true;

    protected void SetUpdated() => UpdatedAt = DateTime.UtcNow;
    protected void Deactivate() => IsActive = false;
}
6.2 Product Entity
csharp

// SalesSystem.Domain/Entities/Product.cs
namespace SalesSystem.Domain.Entities;

public class Product : BaseEntity
{
    public string? Code { get; private set; }
    public string? Barcode { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int? CategoryId { get; private set; }
    public int? UnitId { get; private set; }
    public decimal PurchasePrice { get; private set; }
    public decimal SalePrice { get; private set; }
    public decimal MinStock { get; private set; }
    public string? Description { get; private set; }

    // Navigation
    public Category? Category { get; private set; }
    public Unit? Unit { get; private set; }

    protected Product() { } // EF Core

    public static Product Create(
        string name,
        decimal purchasePrice,
        decimal salePrice,
        string? code = null,
        string? barcode = null,
        int? categoryId = null,
        int? unitId = null,
        decimal minStock = 0,
        string? description = null)
    {
        // ✅ Validation
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم المنتج مطلوب");

        if (salePrice < 0)
            throw new DomainException("سعر البيع لا يمكن أن يكون سالباً");

        if (purchasePrice < 0)
            throw new DomainException("سعر الشراء لا يمكن أن يكون سالباً");

        if (minStock < 0)
            throw new DomainException("الحد الأدنى للمخزون لا يمكن أن يكون سالباً");

        return new Product
        {
            Name = name.Trim(),
            PurchasePrice = purchasePrice,
            SalePrice = salePrice,
            Code = code?.Trim(),
            Barcode = barcode?.Trim(),
            CategoryId = categoryId,
            UnitId = unitId,
            MinStock = minStock,
            Description = description?.Trim()
        };
    }

    public void Update(
        string name,
        decimal purchasePrice,
        decimal salePrice,
        string? code,
        string? barcode,
        int? categoryId,
        int? unitId,
        decimal minStock,
        string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم المنتج مطلوب");
        if (salePrice < 0)
            throw new DomainException("سعر البيع لا يمكن أن يكون سالباً");

        Name = name.Trim();
        PurchasePrice = purchasePrice;
        SalePrice = salePrice;
        Code = code?.Trim();
        Barcode = barcode?.Trim();
        CategoryId = categoryId;
        UnitId = unitId;
        MinStock = minStock;
        Description = description?.Trim();
        SetUpdated();
    }

    public void Deactivate()
    {
        IsActive = false;
        SetUpdated();
    }
}
6.3 WarehouseStock Entity ⚠️ الأهم
csharp

// SalesSystem.Domain/Entities/WarehouseStock.cs
namespace SalesSystem.Domain.Entities;

public class WarehouseStock : BaseEntity
{
    public int WarehouseId { get; private set; }
    public int ProductId { get; private set; }
    public decimal Quantity { get; private set; }
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    // Navigation
    public Warehouse? Warehouse { get; private set; }
    public Product? Product { get; private set; }

    protected WarehouseStock() { }

    public static WarehouseStock Create(int warehouseId, int productId)
    {
        return new WarehouseStock
        {
            WarehouseId = warehouseId,
            ProductId = productId,
            Quantity = 0
        };
    }

    /// <summary>
    /// زيادة المخزون — للشراء ومرتجع البيع والتحويل الداخل
    /// </summary>
    public void Increase(decimal quantity)
    {
        if (quantity <= 0)
            throw new DomainException($"كمية الزيادة يجب أن تكون أكبر من صفر، القيمة المُدخلة: {quantity}");

        Quantity += quantity;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// تقليل المخزون — للبيع ومرتجع الشراء والتحويل الخارج
    /// </summary>
    public void Decrease(decimal quantity)
    {
        if (quantity <= 0)
            throw new DomainException($"كمية الخصم يجب أن تكون أكبر من صفر، القيمة المُدخلة: {quantity}");

        if (Quantity < quantity)
            throw new InsufficientStockException(
                ProductId, WarehouseId, quantity, Quantity);

        Quantity -= quantity;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// التحقق فقط بدون تغيير — للتحقق المسبق
    /// </summary>
    public bool HasSufficientStock(decimal requestedQuantity)
        => Quantity >= requestedQuantity;
}
6.4 SalesInvoice Entity ⚠️ أهم Entity في النظام
csharp

// SalesSystem.Domain/Entities/SalesInvoice.cs
namespace SalesSystem.Domain.Entities;

public class SalesInvoice : BaseEntity
{
    public string InvoiceNo { get; private set; } = string.Empty;
    public int CustomerId { get; private set; }
    public int WarehouseId { get; private set; }
    public DateTime InvoiceDate { get; private set; }
    public DateTime? DueDate { get; private set; }
    public PaymentType PaymentType { get; private set; }
    public decimal SubTotal { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public decimal PaidAmount { get; private set; }
    public decimal DueAmount { get; private set; }
    public string? Notes { get; private set; }
    public InvoiceStatus Status { get; private set; }
    public int? CreatedByUserId { get; private set; }

    // Navigation
    public Customer? Customer { get; private set; }
    public Warehouse? Warehouse { get; private set; }
    private readonly List<SalesInvoiceItem> _items = new();
    public IReadOnlyCollection<SalesInvoiceItem> Items => _items.AsReadOnly();

    protected SalesInvoice() { }

    public static SalesInvoice Create(
        string invoiceNo,
        int customerId,
        int warehouseId,
        PaymentType paymentType,
        decimal paidAmount,
        string? notes,
        int? createdByUserId)
    {
        if (string.IsNullOrWhiteSpace(invoiceNo))
            throw new DomainException("رقم الفاتورة مطلوب");
        if (customerId <= 0)
            throw new DomainException("العميل مطلوب");
        if (warehouseId <= 0)
            throw new DomainException("المخزن مطلوب");
        if (paidAmount < 0)
            throw new DomainException("المبلغ المدفوع لا يمكن أن يكون سالباً");

        return new SalesInvoice
        {
            InvoiceNo = invoiceNo,
            CustomerId = customerId,
            WarehouseId = warehouseId,
            InvoiceDate = DateTime.UtcNow,
            PaymentType = paymentType,
            PaidAmount = paidAmount,
            Notes = notes?.Trim(),
            Status = InvoiceStatus.Draft,
            CreatedByUserId = createdByUserId
        };
    }

    public void AddItem(int productId, decimal quantity, 
                        decimal unitPrice, decimal discountAmount = 0)
    {
        if (Status != InvoiceStatus.Draft)
            throw new DomainException("لا يمكن تعديل فاتورة مرحَّلة أو ملغاة");
        if (quantity <= 0)
            throw new DomainException("الكمية يجب أن تكون أكبر من صفر");
        if (unitPrice < 0)
            throw new DomainException("السعر لا يمكن أن يكون سالباً");
        if (discountAmount < 0)
            throw new DomainException("الخصم لا يمكن أن يكون سالباً");

        // ⚠️ LineTotal لا تُحسب في الـ UI — تُحسب هنا فقط
        var lineTotal = (quantity * unitPrice) - discountAmount;
        if (lineTotal < 0)
            throw new DomainException("إجمالي السطر لا يمكن أن يكون سالباً");

        var item = SalesInvoiceItem.Create(
            Id, productId, quantity, unitPrice, discountAmount, lineTotal);
        _items.Add(item);

        RecalculateTotals();
    }

    /// <summary>
    /// ⚠️ الحساب النهائي — يُنفذ عند كل إضافة/حذف عنصر
    /// </summary>
    private void RecalculateTotals()
    {
        // ✅ decimal arithmetic — لا float
        SubTotal = _items.Sum(i => i.LineTotal);
        
        // TaxAmount يُحسب على SubTotal بعد الخصم الكلي
        TaxAmount = 0; // يُحسب لاحقاً إذا كانت الضريبة مفعلة
        
        TotalAmount = SubTotal - DiscountAmount + TaxAmount;
        
        if (TotalAmount < 0)
            throw new DomainException("إجمالي الفاتورة لا يمكن أن يكون سالباً");

        if (PaidAmount > TotalAmount)
            throw new DomainException(
                $"المبلغ المدفوع ({PaidAmount}) أكبر من إجمالي الفاتورة ({TotalAmount})");

        DueAmount = TotalAmount - PaidAmount;
    }

    /// <summary>
    /// ترحيل الفاتورة — يُستدعى مرة واحدة فقط
    /// </summary>
    public void Post()
    {
        if (Status != InvoiceStatus.Draft)
            throw new DomainException($"لا يمكن ترحيل الفاتورة بحالة: {Status}");
        if (!_items.Any())
            throw new DomainException("لا يمكن ترحيل فاتورة فارغة");

        RecalculateTotals(); // تحقق أخير قبل الترحيل

        Status = InvoiceStatus.Posted;
        SetUpdated();
    }

    public void Cancel(string reason)
    {
        if (Status == InvoiceStatus.Cancelled)
            throw new DomainException("الفاتورة ملغاة بالفعل");

        Status = InvoiceStatus.Cancelled;
        Notes = string.IsNullOrEmpty(Notes)
            ? $"ملغاة: {reason}"
            : $"{Notes} | ملغاة: {reason}";
        SetUpdated();
    }
}
7. Contracts — DTOs و Requests
7.1 Common Result
csharp

// SalesSystem.Contracts/Common/Result.cs
namespace SalesSystem.Contracts.Common;

/// <summary>
/// ⚠️ كل العمليات ترجع Result<T> — لا exceptions مكشوفة للـ UI
/// </summary>
public class Result<T>
{
    public bool IsSuccess { get; private set; }
    public T? Value { get; private set; }
    public string? Error { get; private set; }
    public string? ErrorCode { get; private set; }

    private Result() { }

    public static Result<T> Success(T value) => new()
    {
        IsSuccess = true,
        Value = value
    };

    public static Result<T> Failure(string error, string? errorCode = null) => new()
    {
        IsSuccess = false,
        Error = error,
        ErrorCode = errorCode
    };
}

public class Result
{
    public bool IsSuccess { get; private set; }
    public string? Error { get; private set; }

    public static Result Success() => new() { IsSuccess = true };
    public static Result Failure(string error) => new() { IsSuccess = false, Error = error };
}

// أكواد الأخطاء القياسية
public static class ErrorCodes
{
    public const string NotFound              = "NOT_FOUND";
    public const string InsufficientStock     = "INSUFFICIENT_STOCK";
    public const string DuplicateInvoiceNo    = "DUPLICATE_INVOICE_NO";
    public const string InvalidAmount         = "INVALID_AMOUNT";
    public const string InvalidQuantity       = "INVALID_QUANTITY";
    public const string InvoiceAlreadyPosted  = "ALREADY_POSTED";
    public const string InvoiceAlreadyCancelled = "ALREADY_CANCELLED";
    public const string ValidationError       = "VALIDATION_ERROR";
}
7.2 DTOs الأساسية
csharp

// SalesSystem.Contracts/DTOs/ProductDto.cs
public record ProductDto(
    int ProductId,
    string? Code,
    string? Barcode,
    string Name,
    int? CategoryId,
    string? CategoryName,
    int? UnitId,
    string? UnitName,
    decimal PurchasePrice,
    decimal SalePrice,
    decimal MinStock,
    bool IsActive,
    // المخزون الإجمالي — يُحسب من WarehouseStocks
    decimal TotalStock
);

// SalesSystem.Contracts/DTOs/WarehouseStockDto.cs
public record WarehouseStockDto(
    int WarehouseId,
    string WarehouseName,
    int ProductId,
    string ProductName,
    decimal Quantity
);

// SalesSystem.Contracts/DTOs/SalesInvoiceDto.cs
public record SalesInvoiceDto(
    int SalesInvoiceId,
    string InvoiceNo,
    int CustomerId,
    string CustomerName,
    int WarehouseId,
    string WarehouseName,
    DateTime InvoiceDate,
    string PaymentType,
    decimal SubTotal,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal DueAmount,
    string Status,
    List<SalesInvoiceItemDto> Items
);

public record SalesInvoiceItemDto(
    int ProductId,
    string ProductName,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountAmount,
    decimal LineTotal
);
7.3 Requests مع Validation
csharp

// SalesSystem.Contracts/Requests/Sales/CreateSalesInvoiceRequest.cs
public record CreateSalesInvoiceRequest(
    int CustomerId,
    int WarehouseId,
    int PaymentType,           // 1=Cash, 2=Credit, 3=Mixed
    decimal PaidAmount,
    string? Notes,
    List<SalesInvoiceItemRequest> Items
);

public record SalesInvoiceItemRequest(
    int ProductId,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountAmount
);

// SalesSystem.Contracts/Requests/Products/CreateProductRequest.cs
public record CreateProductRequest(
    string Name,
    string? Code,
    string? Barcode,
    int? CategoryId,
    int? UnitId,
    decimal PurchasePrice,
    decimal SalePrice,
    decimal MinStock,
    string? Description
);
8. Application Services — منطق العمل
8.1 ISalesService Interface
csharp

// SalesSystem.Application/Interfaces/ISalesService.cs
namespace SalesSystem.Application.Interfaces;

public interface ISalesService
{
    Task<Result<SalesInvoiceDto>> CreateInvoiceAsync(
        CreateSalesInvoiceRequest request, int? userId,
        CancellationToken ct = default);

    Task<Result<SalesInvoiceDto>> GetByIdAsync(
        int invoiceId, CancellationToken ct = default);

    Task<Result<PagedResult<SalesInvoiceDto>>> GetAllAsync(
        SalesInvoiceFilter filter, CancellationToken ct = default);

    Task<Result> CancelInvoiceAsync(
        int invoiceId, string reason, int? userId,
        CancellationToken ct = default);
}
8.2 SalesService Implementation ⚠️ الأهم والأحرج
csharp

// SalesSystem.Application/Services/SalesService.cs
namespace SalesSystem.Application.Services;

public class SalesService : ISalesService
{
    private readonly IUnitOfWork _uow;
    private readonly IDocumentSequenceService _sequenceService;
    private readonly IInventoryService _inventoryService;
    private readonly ILogger<SalesService> _logger;

    public SalesService(
        IUnitOfWork uow,
        IDocumentSequenceService sequenceService,
        IInventoryService inventoryService,
        ILogger<SalesService> logger)
    {
        _uow = uow;
        _sequenceService = sequenceService;
        _inventoryService = inventoryService;
        _logger = logger;
    }

    public async Task<Result<SalesInvoiceDto>> CreateInvoiceAsync(
        CreateSalesInvoiceRequest request,
        int? userId,
        CancellationToken ct = default)
    {
        // === Step 1: Validate Request ===
        var validationResult = ValidateRequest(request);
        if (!validationResult.IsSuccess)
            return Result<SalesInvoiceDto>.Failure(
                validationResult.Error!, ErrorCodes.ValidationError);

        // === Step 2: التحقق من وجود العميل والمخزن ===
        var customer = await _uow.Customers.GetByIdAsync(request.CustomerId, ct);
        if (customer == null)
            return Result<SalesInvoiceDto>.Failure(
                "العميل غير موجود", ErrorCodes.NotFound);

        var warehouse = await _uow.Warehouses.GetByIdAsync(request.WarehouseId, ct);
        if (warehouse == null || !warehouse.IsActive)
            return Result<SalesInvoiceDto>.Failure(
                "المخزن غير موجود أو غير نشط", ErrorCodes.NotFound);

        // === Step 3: التحقق المسبق من المخزون لكل عنصر ===
        // ⚠️ مهم: نتحقق أولاً قبل فتح Transaction
        foreach (var item in request.Items)
        {
            var stock = await _uow.WarehouseStocks
                .GetByWarehouseAndProductAsync(request.WarehouseId, item.ProductId, ct);

            var available = stock?.Quantity ?? 0;
            if (available < item.Quantity)
            {
                var product = await _uow.Products.GetByIdAsync(item.ProductId, ct);
                return Result<SalesInvoiceDto>.Failure(
                    $"المخزون غير كافٍ للمنتج '{product?.Name}'. " +
                    $"المتوفر: {available}, المطلوب: {item.Quantity}",
                    ErrorCodes.InsufficientStock);
            }
        }

        // === Step 4: توليد رقم الفاتورة ===
        string invoiceNo;
        try
        {
            invoiceNo = await _sequenceService.GenerateAsync("INV", ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "فشل في توليد رقم الفاتورة");
            return Result<SalesInvoiceDto>.Failure(
                "فشل في توليد رقم الفاتورة");
        }

        // === Step 5: Database Transaction ===
        // ⚠️ كل العمليات التالية داخل Transaction واحدة
        await using var transaction = await _uow.BeginTransactionAsync(ct);
        try
        {
            // 5a. إنشاء الفاتورة
            var invoice = SalesInvoice.Create(
                invoiceNo,
                request.CustomerId,
                request.WarehouseId,
                (PaymentType)request.PaymentType,
                request.PaidAmount,
                request.Notes,
                userId);

            // 5b. إضافة العناصر (الحساب يتم في Domain)
            foreach (var itemRequest in request.Items)
            {
                invoice.AddItem(
                    itemRequest.ProductId,
                    itemRequest.Quantity,
                    itemRequest.UnitPrice,
                    itemRequest.DiscountAmount);
            }

            // 5c. التحقق من المبلغ المدفوع
            if (request.PaidAmount > invoice.TotalAmount)
                return Result<SalesInvoiceDto>.Failure(
                    $"المبلغ المدفوع ({request.PaidAmount:N2}) " +
                    $"أكبر من إجمالي الفاتورة ({invoice.TotalAmount:N2})",
                    ErrorCodes.InvalidAmount);

            // 5d. ترحيل الفاتورة
            invoice.Post();

            // 5e. حفظ الفاتورة في DB
            await _uow.SalesInvoices.AddAsync(invoice, ct);
            await _uow.SaveChangesAsync(ct);

            // 5f. خصم المخزون لكل عنصر
            // ⚠️ يتم بعد حفظ الفاتورة وتوليد ID
            foreach (var item in invoice.Items)
            {
                await _inventoryService.DecreaseStockAsync(
                    warehouseId: request.WarehouseId,
                    productId: item.ProductId,
                    quantity: item.Quantity,
                    movementType: MovementType.SaleOut,
                    referenceType: "SalesInvoice",
                    referenceId: invoice.Id,
                    unitCost: item.UnitPrice,
                    userId: userId,
                    ct: ct);
            }

            // 5g. تحديث رصيد العميل (إذا كان دين)
            if (invoice.DueAmount > 0)
            {
                customer.IncreaseBalance(invoice.DueAmount);
                await _uow.SaveChangesAsync(ct);
            }

            // 5h. Commit
            await transaction.CommitAsync(ct);

            _logger.LogInformation(
                "تم إنشاء فاتورة البيع {InvoiceNo} بإجمالي {Total}",
                invoice.InvoiceNo, invoice.TotalAmount);

            var dto = MapToDto(invoice);
            return Result<SalesInvoiceDto>.Success(dto);
        }
        catch (InsufficientStockException ex)
        {
            await transaction.RollbackAsync(ct);
            _logger.LogWarning(ex, "مخزون غير كافٍ أثناء البيع");
            return Result<SalesInvoiceDto>.Failure(
                ex.Message, ErrorCodes.InsufficientStock);
        }
        catch (DomainException ex)
        {
            await transaction.RollbackAsync(ct);
            _logger.LogWarning(ex, "خطأ في قواعد العمل أثناء البيع");
            return Result<SalesInvoiceDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            _logger.LogError(ex, "خطأ غير متوقع أثناء إنشاء فاتورة البيع");
            return Result<SalesInvoiceDto>.Failure(
                "حدث خطأ غير متوقع. يرجى المحاولة مرة أخرى.");
        }
    }

    private Result ValidateRequest(CreateSalesInvoiceRequest request)
    {
        if (request.Items == null || !request.Items.Any())
            return Result.Failure("يجب إضافة منتج واحد على الأقل");

        if (request.PaidAmount < 0)
            return Result.Failure("المبلغ المدفوع لا يمكن أن يكون سالباً");

        if (request.PaymentType is < 1 or > 3)
            return Result.Failure("نوع الدفع غير صحيح");

        foreach (var item in request.Items)
        {
            if (item.ProductId <= 0)
                return Result.Failure("معرّف المنتج غير صحيح");
            if (item.Quantity <= 0)
                return Result.Failure("الكمية يجب أن تكون أكبر من صفر");
            if (item.UnitPrice < 0)
                return Result.Failure("السعر لا يمكن أن يكون سالباً");
            if (item.DiscountAmount < 0)
                return Result.Failure("الخصم لا يمكن أن يكون سالباً");

            // ⚠️ التحقق من LineTotal منطقياً
            var lineTotal = (item.Quantity * item.UnitPrice) - item.DiscountAmount;
            if (lineTotal < 0)
                return Result.Failure(
                    $"إجمالي السطر سالب للمنتج {item.ProductId}. " +
                    "الخصم لا يمكن أن يتجاوز سعر البيع.");
        }

        return Result.Success();
    }
}
8.3 InventoryService ⚠️ حرج جداً
csharp

// SalesSystem.Application/Services/InventoryService.cs
namespace SalesSystem.Application.Services;

public class InventoryService : IInventoryService
{
    private readonly IUnitOfWork _uow;

    public InventoryService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    /// <summary>
    /// ⚠️ يُستدعى دائماً داخل Transaction خارجية
    /// </summary>
    public async Task DecreaseStockAsync(
        int warehouseId, int productId, decimal quantity,
        MovementType movementType, string referenceType,
        int referenceId, decimal? unitCost, int? userId,
        CancellationToken ct = default)
    {
        // جلب أو إنشاء سجل المخزون
        var stock = await _uow.WarehouseStocks
            .GetByWarehouseAndProductAsync(warehouseId, productId, ct);

        if (stock == null)
            throw new DomainException(
                $"لا يوجد سجل مخزون للمنتج {productId} في المخزن {warehouseId}");

        var quantityBefore = stock.Quantity;

        // ⚠️ هذا يرمي InsufficientStockException إذا لم تكن الكمية كافية
        stock.Decrease(quantity);

        // تسجيل الحركة
        var movement = new InventoryMovement
        {
            ProductId = productId,
            WarehouseId = warehouseId,
            MovementType = movementType,
            QuantityChange = -quantity,    // سالب = خروج
            QuantityBefore = quantityBefore,
            QuantityAfter = stock.Quantity,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            UnitCost = unitCost,
            MovementDate = DateTime.UtcNow,
            CreatedByUserId = userId
        };

        await _uow.InventoryMovements.AddAsync(movement, ct);
        await _uow.SaveChangesAsync(ct);
    }

    public async Task IncreaseStockAsync(
        int warehouseId, int productId, decimal quantity,
        MovementType movementType, string referenceType,
        int referenceId, decimal? unitCost, int? userId,
        CancellationToken ct = default)
    {
        var stock = await _uow.WarehouseStocks
            .GetByWarehouseAndProductAsync(warehouseId, productId, ct);

        // إذا لم يكن هناك سجل، نُنشئ واحداً
        if (stock == null)
        {
            stock = WarehouseStock.Create(warehouseId, productId);
            await _uow.WarehouseStocks.AddAsync(stock, ct);
            await _uow.SaveChangesAsync(ct); // للحصول على ID
        }

        var quantityBefore = stock.Quantity;
        stock.Increase(quantity);

        var movement = new InventoryMovement
        {
            ProductId = productId,
            WarehouseId = warehouseId,
            MovementType = movementType,
            QuantityChange = quantity,     // موجب = دخول
            QuantityBefore = quantityBefore,
            QuantityAfter = stock.Quantity,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            UnitCost = unitCost,
            MovementDate = DateTime.UtcNow,
            CreatedByUserId = userId
        };

        await _uow.InventoryMovements.AddAsync(movement, ct);
        await _uow.SaveChangesAsync(ct);
    }
}
8.4 DocumentSequenceService — توليد أرقام الوثائق
csharp

// SalesSystem.Application/Services/DocumentSequenceService.cs
namespace SalesSystem.Application.Services;

public class DocumentSequenceService : IDocumentSequenceService
{
    private readonly IUnitOfWork _uow;
    private static readonly SemaphoreSlim _lock = new(1, 1);

    public DocumentSequenceService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    /// <summary>
    /// ⚠️ Thread-safe — يُستخدم Lock لمنع الأرقام المكررة
    /// المخرج: INV-2025-000001
    /// </summary>
    public async Task<string> GenerateAsync(
        string prefix, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var sequence = await _uow.DocumentSequences
                .GetByPrefixAsync(prefix, ct);

            if (sequence == null)
                throw new InvalidOperationException(
                    $"Sequence prefix '{prefix}' غير موجود");

            sequence.LastNumber++;
            sequence.UpdatedAt = DateTime.UtcNow;
            await _uow.SaveChangesAsync(ct);

            // Format: INV-2025-000001
            return $"{prefix}-{DateTime.Now.Year}-{sequence.LastNumber:D6}";
        }
        finally
        {
            _lock.Release();
        }
    }
}
8.5 SalesReturn Service ⚠️ منطق المرتجع الحرج
csharp

// SalesSystem.Application/Services/SalesReturnService.cs
public async Task<Result<SalesReturnDto>> CreateReturnAsync(
    CreateSalesReturnRequest request, int? userId, CancellationToken ct = default)
{
    // === التحقق المسبق ===
    // إذا كان مرتجع لفاتورة محددة، تحقق من الكميات
    if (request.SalesInvoiceId.HasValue)
    {
        var originalInvoice = await _uow.SalesInvoices
            .GetByIdWithItemsAsync(request.SalesInvoiceId.Value, ct);

        if (originalInvoice == null)
            return Result<SalesReturnDto>.Failure("الفاتورة الأصلية غير موجودة");

        if (originalInvoice.Status == InvoiceStatus.Cancelled)
            return Result<SalesReturnDto>.Failure("لا يمكن إرجاع فاتورة ملغاة");

        // ⚠️ التحقق: الكمية المُرجعة لا تتجاوز الكمية الأصلية
        foreach (var returnItem in request.Items)
        {
            var originalItem = originalInvoice.Items
                .FirstOrDefault(i => i.ProductId == returnItem.ProductId);

            if (originalItem == null)
                return Result<SalesReturnDto>.Failure(
                    $"المنتج {returnItem.ProductId} غير موجود في الفاتورة الأصلية");

            // ⚠️ حساب ما تم إرجاعه مسبقاً
            var previouslyReturned = await _uow.SalesReturnItems
                .GetTotalReturnedQuantityAsync(
                    request.SalesInvoiceId.Value, returnItem.ProductId, ct);

            var maxReturnable = originalItem.Quantity - previouslyReturned;

            if (returnItem.Quantity > maxReturnable)
                return Result<SalesReturnDto>.Failure(
                    $"الكمية المطلوب إرجاعها ({returnItem.Quantity}) " +
                    $"أكبر من الكمية القابلة للإرجاع ({maxReturnable})");
        }
    }

    // === Transaction ===
    await using var transaction = await _uow.BeginTransactionAsync(ct);
    try
    {
        var returnNo = await _sequenceService.GenerateAsync("SR", ct);

        // حساب الإجمالي
        var totalAmount = request.Items
            .Sum(i => i.Quantity * i.UnitPrice);   // ✅ decimal

        // إنشاء المرتجع
        var salesReturn = new SalesReturn
        {
            ReturnNo = returnNo,
            SalesInvoiceId = request.SalesInvoiceId,
            CustomerId = request.CustomerId,
            WarehouseId = request.WarehouseId,
            ReturnDate = DateTime.UtcNow,
            Reason = request.Reason,
            TotalAmount = totalAmount,
            Status = InvoiceStatus.Posted,
            CreatedByUserId = userId
        };

        await _uow.SalesReturns.AddAsync(salesReturn, ct);
        await _uow.SaveChangesAsync(ct);

        // إضافة العناصر
        foreach (var item in request.Items)
        {
            var returnItem = new SalesReturnItem
            {
                SalesReturnId = salesReturn.SalesReturnId,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                LineTotal = item.Quantity * item.UnitPrice  // ✅ decimal
            };
            await _uow.SalesReturnItems.AddAsync(returnItem, ct);
        }

        await _uow.SaveChangesAsync(ct);

        // ⚠️ إعادة المخزون (مرتجع البيع يزيد المخزون)
        foreach (var item in request.Items)
        {
            await _inventoryService.IncreaseStockAsync(
                warehouseId: request.WarehouseId,
                productId: item.ProductId,
                quantity: item.Quantity,
                movementType: MovementType.SaleReturnIn,
                referenceType: "SalesReturn",
                referenceId: salesReturn.SalesReturnId,
                unitCost: item.UnitPrice,
                userId: userId,
                ct: ct);
        }

        // ⚠️ تخفيض رصيد العميل (المرتجع يُقلل الدين)
        var customer = await _uow.Customers.GetByIdAsync(request.CustomerId, ct);
        customer!.DecreaseBalance(totalAmount);
        await _uow.SaveChangesAsync(ct);

        await transaction.CommitAsync(ct);

        return Result<SalesReturnDto>.Success(MapToDto(salesReturn));
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync(ct);
        _logger.LogError(ex, "خطأ في إنشاء مرتجع البيع");
        return Result<SalesReturnDto>.Failure("حدث خطأ غير متوقع");
    }
}
9. Exceptions المخصصة
csharp

// SalesSystem.Domain/Exceptions/

// الاستثناء الأساسي لقواعد العمل
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}

// استثناء المخزون غير الكافي — الأكثر أهمية
public class InsufficientStockException : DomainException
{
    public int ProductId { get; }
    public int WarehouseId { get; }
    public decimal RequestedQuantity { get; }
    public decimal AvailableQuantity { get; }

    public InsufficientStockException(
        int productId, int warehouseId,
        decimal requested, decimal available)
        : base($"المخزون غير كافٍ. المنتج: {productId}, " +
               $"المخزن: {warehouseId}, " +
               $"المطلوب: {requested}, المتوفر: {available}")
    {
        ProductId = productId;
        WarehouseId = warehouseId;
        RequestedQuantity = requested;
        AvailableQuantity = available;
    }
}

// استثناء الفاتورة المكررة
public class DuplicateInvoiceNoException : DomainException
{
    public string InvoiceNo { get; }

    public DuplicateInvoiceNoException(string invoiceNo)
        : base($"رقم الفاتورة '{invoiceNo}' موجود بالفعل")
    {
        InvoiceNo = invoiceNo;
    }
}
10. Infrastructure — EF Core
10.1 DbContext
csharp

// SalesSystem.Infrastructure/DbContext/SalesDbContext.cs
namespace SalesSystem.Infrastructure.Data;

public class SalesDbContext : DbContext
{
    public SalesDbContext(DbContextOptions<SalesDbContext> options) 
        : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<WarehouseStock> WarehouseStocks => Set<WarehouseStock>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<PurchaseInvoice> PurchaseInvoices => Set<PurchaseInvoice>();
    public DbSet<PurchaseInvoiceItem> PurchaseInvoiceItems => Set<PurchaseInvoiceItem>();
    public DbSet<SalesInvoice> SalesInvoices => Set<SalesInvoice>();
    public DbSet<SalesInvoiceItem> SalesInvoiceItems => Set<SalesInvoiceItem>();
    public DbSet<SalesReturn> SalesReturns => Set<SalesReturn>();
    public DbSet<SalesReturnItem> SalesReturnItems => Set<SalesReturnItem>();
    public DbSet<PurchaseReturn> PurchaseReturns => Set<PurchaseReturn>();
    public DbSet<PurchaseReturnItem> PurchaseReturnItems => Set<PurchaseReturnItem>();
    public DbSet<StockTransfer> StockTransfers => Set<StockTransfer>();
    public DbSet<StockTransferItem> StockTransferItems => Set<StockTransferItem>();
    public DbSet<CustomerPayment> CustomerPayments => Set<CustomerPayment>();
    public DbSet<SupplierPayment> SupplierPayments => Set<SupplierPayment>();
    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();
    public DbSet<StoreSettings> StoreSettings => Set<StoreSettings>();
    public DbSet<DocumentSequence> DocumentSequences => Set<DocumentSequence>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // تطبيق كل الـ Configurations
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(SalesDbContext).Assembly);
    }
}
10.2 WarehouseStock Configuration ⚠️
csharp

// SalesSystem.Infrastructure/Configurations/WarehouseStockConfiguration.cs
public class WarehouseStockConfiguration 
    : IEntityTypeConfiguration<WarehouseStock>
{
    public void Configure(EntityTypeBuilder<WarehouseStock> builder)
    {
        builder.ToTable("WarehouseStocks");
        builder.HasKey(x => x.WarehouseStockId);

        // ⚠️ decimal(18,3) للكمية — إلزامي
        builder.Property(x => x.Quantity)
            .HasColumnType("decimal(18,3)")
            .HasDefaultValue(0m);

        // ⚠️ Unique Constraint حرج
        builder.HasIndex(x => new { x.WarehouseId, x.ProductId })
            .IsUnique();

        // CHECK Constraint
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_WarehouseStocks_Qty", "[Quantity] >= 0"));

        builder.Property(x => x.UpdatedAt)
            .HasColumnType("datetime2");
    }
}
10.3 SalesInvoice Configuration
csharp

public class SalesInvoiceConfiguration 
    : IEntityTypeConfiguration<SalesInvoice>
{
    public void Configure(EntityTypeBuilder<SalesInvoice> builder)
    {
        builder.ToTable("SalesInvoices");
        builder.HasKey(x => x.SalesInvoiceId);

        builder.Property(x => x.InvoiceNo)
            .HasMaxLength(30)
            .IsRequired();
        builder.HasIndex(x => x.InvoiceNo).IsUnique();

        // ⚠️ كل الأموال decimal(18,2)
        builder.Property(x => x.SubTotal)
            .HasColumnType("decimal(18,2)").HasDefaultValue(0m);
        builder.Property(x => x.DiscountAmount)
            .HasColumnType("decimal(18,2)").HasDefaultValue(0m);
        builder.Property(x => x.TaxAmount)
            .HasColumnType("decimal(18,2)").HasDefaultValue(0m);
        builder.Property(x => x.TotalAmount)
            .HasColumnType("decimal(18,2)").HasDefaultValue(0m);
        builder.Property(x => x.PaidAmount)
            .HasColumnType("decimal(18,2)").HasDefaultValue(0m);
        builder.Property(x => x.DueAmount)
            .HasColumnType("decimal(18,2)").HasDefaultValue(0m);

        // CHECK Constraints
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_SI_Total",
                "[TotalAmount] >= 0");
            t.HasCheckConstraint("CK_SI_Paid",
                "[PaidAmount] >= 0 AND [PaidAmount] <= [TotalAmount]");
            t.HasCheckConstraint("CK_SI_Due",
                "[DueAmount] >= 0");
        });

        builder.HasMany(x => x.Items)
            .WithOne()
            .HasForeignKey(i => i.SalesInvoiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
11. API Controllers
11.1 SalesController
csharp

// SalesSystem.Api/Controllers/SalesController.cs
[ApiController]
[Route("api/[controller]")]
public class SalesController : ControllerBase
{
    private readonly ISalesService _salesService;

    public SalesController(ISalesService salesService)
    {
        _salesService = salesService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateSalesInvoiceRequest request,
        CancellationToken ct)
    {
        // userId من JWT لاحقاً — الآن null
        var result = await _salesService.CreateInvoiceAsync(request, null, ct);

        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                ErrorCodes.InsufficientStock => 
                    BadRequest(new { error = result.Error, code = result.ErrorCode }),
                ErrorCodes.NotFound => 
                    NotFound(new { error = result.Error }),
                ErrorCodes.ValidationError => 
                    BadRequest(new { error = result.Error }),
                _ => 
                    StatusCode(500, new { error = result.Error })
            };
        }

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Value!.SalesInvoiceId },
            result.Value);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _salesService.GetByIdAsync(id, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound();
    }

    [HttpDelete("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(
        int id, [FromBody] CancelRequest request, CancellationToken ct)
    {
        var result = await _salesService.CancelInvoiceAsync(
            id, request.Reason, null, ct);

        return result.IsSuccess
            ? Ok(new { message = "تم إلغاء الفاتورة بنجاح" })
            : BadRequest(new { error = result.Error });
    }
}
12. Desktop — EventBus
csharp

// SalesSystem.Desktop/Messaging/EventBus.cs
namespace SalesSystem.Desktop.Messaging;

public interface IEventBus
{
    IDisposable Subscribe<T>(Action<T> handler);
    void Publish<T>(T message);
}

public class EventBus : IEventBus
{
    private readonly Dictionary<Type, List<WeakReference<Delegate>>> 
        _subscribers = new();
    private readonly object _lock = new();

    public IDisposable Subscribe<T>(Action<T> handler)
    {
        lock (_lock)
        {
            var type = typeof(T);
            if (!_subscribers.ContainsKey(type))
                _subscribers[type] = new List<WeakReference<Delegate>>();

            var weakRef = new WeakReference<Delegate>(handler);
            _subscribers[type].Add(weakRef);

            return new Subscription(() =>
            {
                lock (_lock)
                    _subscribers[type].Remove(weakRef);
            });
        }
    }

    public void Publish<T>(T message)
    {
        List<WeakReference<Delegate>> handlers;
        lock (_lock)
        {
            if (!_subscribers.TryGetValue(typeof(T), out var list))
                return;
            handlers = list.ToList();
        }

        foreach (var weakRef in handlers)
        {
            if (weakRef.TryGetTarget(out var del) && del is Action<T> handler)
            {
                // ⚠️ تنفيذ على UI Thread
                if (Application.OpenForms.Count > 0)
                {
                    var form = Application.OpenForms[0]!;
                    if (form.InvokeRequired)
                        form.BeginInvoke(handler, message);
                    else
                        handler(message);
                }
            }
        }
    }

    private class Subscription : IDisposable
    {
        private readonly Action _unsubscribe;
        public Subscription(Action unsubscribe) => _unsubscribe = unsubscribe;
        public void Dispose() => _unsubscribe();
    }
}

// Messages
public record ProductChangedMessage(int ProductId);
public record SaleCreatedMessage(int InvoiceId, string InvoiceNo, decimal Total);
public record PurchaseCreatedMessage(int InvoiceId);
public record StockChangedMessage(int ProductId, int WarehouseId, decimal NewQuantity);
public record CustomerBalanceChangedMessage(int CustomerId, decimal NewBalance);
public record SupplierBalanceChangedMessage(int SupplierId, decimal NewBalance);
13. قواعد العمل الحرجة — مرجع سريع
text

╔══════════════════════════════════════════════════════════════════╗
║          قواعد العمل الحرجة — يجب تطبيقها في كل مكان           ║
╠══════════════════════════════════════════════════════════════════╣
║                                                                  ║
║  FINANCIAL CALCULATIONS                                          ║
║  ─────────────────────                                          ║
║  ✅ LineTotal = (Quantity * UnitPrice) - DiscountAmount          ║
║  ✅ SubTotal  = Sum(LineTotal)                                   ║
║  ✅ Total     = SubTotal - InvoiceDiscount + TaxAmount           ║
║  ✅ Due       = Total - PaidAmount                               ║
║  ❌ لا تحسب في الـ UI — الحساب في Domain Layer فقط             ║
║  ❌ لا float/double في أي حساب مالي                             ║
║                                                                  ║
║  STOCK RULES                                                     ║
║  ───────────                                                     ║
║  ✅ تحقق من المخزون قبل فتح Transaction                         ║
║  ✅ اخصم المخزون بعد حفظ الفاتورة وتوليد ID                    ║
║  ✅ سجل في InventoryMovements مع كل تغيير                       ║
║  ❌ لا بيع بكمية تتجاوز المتوفر في المخزن المحدد               ║
║  ❌ لا كميات سالبة في WarehouseStocks                           ║
║                                                                  ║
║  BALANCE RULES                                                   ║
║  ─────────────                                                   ║
║  Customer Balance موجب = العميل مدين لنا                        ║
║  Customer Balance سالب = لدينا رصيد للعميل                     ║
║  Supplier Balance موجب = علينا للمورد                           ║
║  Supplier Balance سالب = المورد مدين لنا                        ║
║                                                                  ║
║  TRANSACTION RULES                                               ║
║  ─────────────────                                               ║
║  ✅ كل فاتورة (بيع/شراء/مرتجع/تحويل) في Transaction واحدة     ║
║  ✅ إذا فشل أي خطوة → Rollback كامل                             ║
║  ❌ لا Partial Commits                                           ║
║                                                                  ║
║  INVOICE RULES                                                   ║
║  ─────────────                                                   ║
║  ✅ الفاتورة تبدأ Draft ثم تُرحَّل Posted                       ║
║  ✅ المخزون يتأثر عند Posted فقط                                ║
║  ✅ الإلغاء يعكس أثر المخزون والرصيد                           ║
║  ❌ لا حذف نهائي للفواتير                                       ║
║  ❌ لا تعديل على فاتورة Posted                                  ║
║                                                                  ║
╚══════════════════════════════════════════════════════════════════╝
14. خطة التنفيذ المرحلية
text

Phase 1 — Foundation (الأساس)
├── إنشاء الـ 6 مشاريع في Solution
├── Domain Entities كاملة
├── Contracts (DTOs + Requests + Result<T>)
├── Custom Exceptions
└── SQL Script للجداول كاملة مع Seed Data

Phase 2 — Infrastructure
├── SalesDbContext + جميع الـ Configurations
├── EF Core Migrations
├── IUnitOfWork + UnitOfWork
├── Repositories الأساسية
└── اختبار الاتصال بقاعدة البيانات

Phase 3 — Application Services
├── ProductService (البداية — الأبسط)
├── CustomerService + SupplierService
├── WarehouseService
├── DocumentSequenceService ⚠️ مهم
├── InventoryService ⚠️ حرج
├── PurchaseService ⚠️ حرج
├── SalesService ⚠️ الأحرج
├── SalesReturnService ⚠️
└── PurchaseReturnService ⚠️

Phase 4 — API
├── جميع الـ Controllers
├── Global Exception Handler
└── Swagger Setup

Phase 5 — Desktop Shell
├── EventBus
├── NavigationService
├── MainForm + Sidebar
├── LoginForm
└── Common Controls

Phase 6 — Desktop Modules
├── Products Module (القالب)
├── Customers + Suppliers
├── Warehouses
├── Purchases Module
├── Sales Module ⚠️ الأهم
├── Returns Module
└── Reports

Phase 7 — Polish
├── BackupService
├── Settings Screen
└── Error Handling UI
15. تعليمات للـ AI Agent
text

⚠️ تعليمات إلزامية لكل طلب برمجي:

1. DECIMAL ONLY
   - جميع الحسابات المالية: decimal وليس float/double
   - أمثلة: decimal total = items.Sum(i => i.LineTotal);

2. TRANSACTIONS ALWAYS
   - كل عملية تؤثر على أكثر من جدول: داخل BeginTransactionAsync
   - عند أي استثناء: RollbackAsync

3. RESULT PATTERN
   - كل Service Method ترجع: Result<T> أو Result
   - لا رمي Exception للـ Controllers مباشرة

4. DOMAIN VALIDATION
   - التحقق من المنطق في Domain Entities
   - التحقق من البيانات في Application Services
   - الـ Controller يُعيد HTTP Response فقط

5. STOCK INTEGRITY
   - تحقق من المخزون قبل Transaction
   - اخصم بعد حفظ الفاتورة
   - سجل دائماً في InventoryMovements

6. NO DIRECT DB
   - Desktop لا يصل لـ DB مباشرة
   - كل طلب عبر HttpClient → API

7. BALANCE DIRECTION
   - Customer.IncreaseBalance() عند البيع الآجل
   - Customer.DecreaseBalance() عند الدفع أو مرتجع البيع
   - Supplier.IncreaseBalance() عند الشراء الآجل
   - Supplier.DecreaseBalance() عند دفعنا للمورد