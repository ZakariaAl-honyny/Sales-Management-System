<h1 align="center">🧾 Sales Management System</h1>

<p align="center">
  <strong>A comprehensive sales management platform for small-to-medium retail businesses</strong><br/>
  <em>Desktop Client + RESTful API Backend — Built with Clean Architecture</em>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-10%20LTS-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt=".NET 10"/>
  <img src="https://img.shields.io/badge/WPF-MVVM-0078D4?style=for-the-badge&logo=windows&logoColor=white" alt="WPF"/>
  <img src="https://img.shields.io/badge/SQL%20Server-2019+-CC2927?style=for-the-badge&logo=microsoftsqlserver&logoColor=white" alt="SQL Server"/>
  <img src="https://img.shields.io/badge/Architecture-Clean-2ECC71?style=for-the-badge" alt="Clean Architecture"/>
  <img src="https://img.shields.io/badge/API-ASP.NET%20Core%2010-512BD4?style=for-the-badge" alt="ASP.NET Core"/>
  <img src="https://img.shields.io/badge/Status-In%20Development-FFA500?style=for-the-badge" alt="Status"/>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/License-MIT-green.svg?style=flat-square" alt="License"/>
  <img src="https://img.shields.io/badge/Version-v4.0%20Expansion-blue.svg?style=flat-square" alt="Version"/>
  <img src="https://img.shields.io/badge/Language-Arabic%20%2B%20English-orange.svg?style=flat-square" alt="Language"/>
</p>

---

## 📋 Overview

A **comprehensive sales management platform** built with Clean Architecture + CQRS principles, designed for small-to-medium retail businesses. The current version delivers a **WPF Desktop client (MVVM)** backed by an **ASP.NET Core Web API**, handling sales, purchases, inventory, returns, and financial tracking with full Arabic/English bilingual support.

The API-first architecture is designed to support **future web and mobile clients** without any backend changes.

### Why This Project?

- 🏪 **Purpose-built for retail** — Sales invoices, purchase orders, returns, and stock transfers
- 🔄 **Wholesale & Retail Flex** — Sell by box or piece simultaneously with intelligent inventory conversion
  - Separate `WholesalePrice` and `RetailPrice` per product
  - `ConversionFactor` for box↔piece conversion (e.g., 1 box = 12 pieces)
  - `UnitType` enum (Retail=0, Wholesale=1) for pricing mode
- 🔍 **Multi-barcode support** — Multiple barcodes per product (retail, wholesale, custom)
  - `ProductBarcodes` table for additional barcodes
  - Auto-detect unit type from scanned barcode
  - Fallback to legacy `Products.Barcode` column
- 🔌 **Hardware Integration** — Built-in barcode scanner support (Keyboard Emulation) with mobile camera scanning planned
- 🔒 **Financial integrity** — All money calculations use `decimal` precision, never floating-point
- 📦 **Multi-warehouse & Low Stock AI** — Track inventory across branches with auto-calculated reorder suggestions
  - `ReorderLevel` on each product
  - Smart low-stock reporting (e.g., "1 box + 3 pieces")
- 👥 **Role-based access** — Admin, Manager, and Cashier with granular permissions
- 📊 **Full audit trail** — Every stock change and financial transaction is tracked
- 🌐 **API-first design** — RESTful API ready for web/mobile clients in future phases

---

## 🏗️ Architecture

The system follows **Clean Architecture** with a strict 6-project solution structure:

```
SalesSystem/
├── 📦 SalesSystem.Contracts/       ← DTOs, Requests, Responses, Result<T>
├── 🏛️ SalesSystem.Domain/          ← Entities, Business Rules, Exceptions
├── ⚙️ SalesSystem.Application/     ← Services, Interfaces, Use Cases
├── 🗄️ SalesSystem.Infrastructure/  ← EF Core, DbContext, Repositories
├── 🌐 SalesSystem.Api/             ← Controllers, Validation, Middleware
└── 🖥️ SalesSystem.DesktopPWF/      ← WPF UI, MVVM, EventBus
```

### Data Flow

```
Desktop → (HttpClient) → API → Application → Infrastructure → SQL Server
                                     ↓
                              Domain (ZERO dependencies)
```

> **Key Principle:** The Desktop app **never** connects to the database directly. All communication goes through the Web API.

### New Services (v4.2)
- `DialogService` - Modal dialogs (Error, Success, Warning, Confirm, Delete)
- `ToastNotificationService` - Auto-dismissing notifications

---

## ✨ Features

### 💰 Sales & Purchases
- Sales invoices with multi-item support
- Purchase invoices with supplier management
- Mixed payment types (Cash / Credit / Mixed)
- Invoice lifecycle: Draft → Posted → Cancelled

### 📦 Inventory Management
- Multi-warehouse stock tracking
- Stock transfers between warehouses
- Real-time stock availability checking
- Negative stock prevention (DB-level CHECK constraints)
- Complete movement audit trail (`InventoryMovements`)

### 🔄 Returns
- Sales returns with original invoice reference
- Purchase returns with supplier tracking
- Automatic stock reversal on return posting

### 👥 User Management
- Three roles: **Admin**, **Manager**, **Cashier**
- JWT Bearer authentication
- Role-based API policies (`AdminOnly`, `ManagerAndAbove`, `AllStaff`)
- BCrypt password hashing (work factor: 12)

### 📊 Reports
- Daily sales reports
- Stock level reports
- Customer debt tracking
- Supplier payment tracking

### 🖨️ Printing
- A4 invoice printing with store branding
- 80mm thermal receipt printing
- Print preview support

### 🔒 Defensive Programming (v4.2)
- Guard Clauses on ALL Domain entities
- Arabic error messages for validation failures
- Real-time form validation with red border indicators
- Save buttons disabled until form is valid

### 🗑️ Conditional Delete Strategy (v4.2)
- Three-option delete dialog: Cancel / Deactivate / Permanent Delete
- Soft delete keeps entity history for audit trails
- Hard delete with reference validation (prevents breaking FKs)
- Applied to: Products, Customers, Suppliers, Categories, Units, Warehouses

### 💬 Modern Dialogs (v4.2)
- Styled modal dialogs (Error, Success, Warning, Confirmation)
- RTL Arabic interface with custom themes
- Delete confirmation with 3 options
- Toast notifications for minor actions (auto-dismiss)

### ✨ Real-Time Validation (v4.2)
- INotifyDataErrorInfo implementation
- Dynamic validation feedback as user types
- Visual red borders on invalid fields
- Arabic error messages: "الاسم مطلوب", "الكمية يجب أن تكون أكبر من صفر"

### 🚫 Out of Scope (Future Phases)

The following features are **not included** in the current MVP but are planned for future development:

| Feature | Phase | Notes |
|---------|-------|-------|
| 🌐 Web interface | Future | API is ready — only a frontend client is needed |
| 📱 Mobile application | Future | API supports any HTTP client |
| 🏢 Multi-branch management | Future | Current scope is single-branch with multi-warehouse |
| 📒 Full accounting system | Future | Current MVP handles sales/purchase ledgers only |
| 🔗 External integrations | Future | E-commerce, payment gateways, tax authority APIs |
| 📧 Email / SMS notifications | Future | Customer alerts and payment reminders |
| 📈 Advanced analytics | Future | BI dashboards and trend analysis |

> **Note:** The Clean Architecture and API-first design make adding these features straightforward without modifying existing business logic.

---

## 🛠️ Tech Stack

| Layer | Technology | Version |
|-------|-----------|---------|
| **Runtime** | .NET | 10 LTS |
| **API** | ASP.NET Core Web API | 10 |
| **Desktop UI** | WPF (MVVM) + INotifyDataErrorInfo | .NET 10 |
| **Database** | SQL Server | 2019+ |
| **ORM** | Entity Framework Core | 10 |
| **Authentication** | JWT Bearer | — |
| **Password Hashing** | BCrypt.Net-Next | 4.x |
| **Validation** | FluentValidation | 11.x |
| **Logging** | Serilog | 8.x |
| **API Docs** | Swashbuckle (Swagger) | 6.x |

---

## 📐 Database Schema

**24 tables** covering the full retail domain:

| Category | Tables |
|----------|--------|
| **Core** | Users, Units, Categories, Products, Warehouses, WarehouseStocks |
| **Products** | ProductBarcodes (multi-barcode support) |
| **Trading Partners** | Customers, Suppliers |
| **Purchases** | PurchaseInvoices, PurchaseInvoiceItems |
| **Sales** | SalesInvoices, SalesInvoiceItems |
| **Returns** | PurchaseReturns, PurchaseReturnItems, SalesReturns, SalesReturnItems |
| **Transfers** | StockTransfers, StockTransferItems |
| **Payments** | CustomerPayments, SupplierPayments |
| **System** | StoreSettings, DocumentSequences, InventoryMovements |

### Key Constraints
- `decimal(18,2)` for all money fields — **never** `float` or `double`
- `decimal(18,3)` for all quantities
- `nvarchar` for all text (Arabic + English support)
- `CHECK (Quantity >= 0)` on warehouse stock
- Soft delete pattern (`IsActive` flag) — no hard deletes

---

## 🔐 Security

| Feature | Implementation |
|---------|---------------|
| Authentication | JWT Bearer (8-hour expiry) |
| Password Storage | BCrypt hash (work factor: 12) |
| Authorization | Policy-based (`AdminOnly`, `ManagerAndAbove`, `AllStaff`) |
| Token Storage | In-memory only (Desktop) — never persisted to disk |
| Connection Strings | Environment variables — never in source code |
| User Deletion | Soft delete only — FK integrity preserved |

---

## 📂 Project Documentation

| Document | Purpose |
|----------|---------|
| [`AGENTS.md`](AGENTS.md) | Master rules for AI-assisted development |
| [`docs/CONSTITUTION.md`](docs/CONSTITUTION.md) | Non-negotiable architectural rules |
| [`docs/PRD-MVP-v3.0.md`](docs/PRD-MVP-v3.0.md) | Full product requirements document |
| [`docs/database-schema.md`](docs/database-schema.md) | SQL Server schema (22 tables) |
| [`docs/ui-screens.md`](docs/ui-screens.md) | UI/UX flows and EventBus patterns |

---

## 🚀 Getting Started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [SQL Server 2019+](https://www.microsoft.com/en-us/sql-server) (or SQL Server Express)
- Visual Studio 2022+ (recommended)

### Setup

```bash
# 1. Clone the repository
git clone https://github.com/ZakariaAl-honyny/Sales-Management-System.git

# 2. Navigate to the project
cd Sales-Management-System

# 3. Create the database
# Run the SQL script from docs/database-schema.md against your SQL Server

# 4. Configure connection string
# Set environment variable:
set SALESSYSTEM_DB_CONNECTION="Server=.;Database=SalesSystemDb;Trusted_Connection=true;TrustServerCertificate=true;"

# 5. Run the API
cd SalesSystem.Api
dotnet run

# 6. Run the Desktop app
cd SalesSystem.DesktopPWF
dotnet run
```

### Default Login
| Username | Password | Role |
|----------|----------|------|
| `admin` | `admin123` | Admin |

---

## 📈 Implementation Phases

| Phase | Description | Status |
|-------|-------------|--------|
| Phase 1-5 | Foundation → Desktop Modules | ✅ Completed |
| **Phase 6** | Printing — A4 Invoices, 80mm Thermal Receipts | 🔲 In Progress |
| **Phase 7** | Production — Backup, Windows Service, Installer | 🔲 Planned |

---

## 🆕 What's New in v4.2

| Feature | Description |
|---------|-------------|
| **DeleteStrategy** | Conditional soft/hard delete with 3-option dialog |
| **Guard Clauses** | Domain entities protected from invalid states |
| **Styled Dialogs** | Modern RTL modal dialogs with Arabic text |
| **Toast Notifications** | Non-blocking success/error notifications |
| **Real-Time Validation** | INotifyDataErrorInfo with red border feedback |
| **CanExecute Logic** | Save buttons disabled until form is valid |

---

## 🤝 Contributing

This project uses AI-assisted development with strict architectural rules. Before contributing:

1. Read [`AGENTS.md`](AGENTS.md) — all 44+ non-negotiable rules
2. Read [`docs/CONSTITUTION.md`](docs/CONSTITUTION.md) — financial and transaction rules
3. Follow the pre-submission checklist in AGENTS.md §9

---

## 📄 License

This project is licensed under the MIT License — see the [LICENSE](LICENSE) file for details.

---

<p align="center">
  Made with ❤️ for small retail shops
</p>