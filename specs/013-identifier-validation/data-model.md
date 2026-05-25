# Data Model: Identifier Strategy & Validation

**Feature**: `013-identifier-validation`
**Date**: 2026-05-25

---

## Entity Modifications (Domain Layer)

The following entities MUST have their `string Code` property removed entirely. Any constructor parameters or factory methods setting `Code` must be updated.

| Entity | Location | Action |
|--------|----------|--------|
| `Product` | `SalesSystem.Domain/Entities/Products/` | Remove `Code` |
| `Customer` | `SalesSystem.Domain/Entities/Customers/` | Remove `Code` |
| `Supplier` | `SalesSystem.Domain/Entities/Suppliers/` | Remove `Code` |
| `Warehouse` | `SalesSystem.Domain/Entities/Inventory/` | Remove `Code` |
| `Category` | `SalesSystem.Domain/Entities/Products/` | Remove `Code` |
| `Unit` | `SalesSystem.Domain/Entities/Products/` | Remove `Code` |
| `User` | `SalesSystem.Domain/Entities/Identity/` | Remove `Code` |

### Unaffected Entities

The following entities retain their `Code` or `SequenceNumber` as they represent financial/transactional documents:
- `SalesInvoice`
- `PurchaseInvoice`
- `SalesReturn`
- `PurchaseReturn`
- `StockTransfer`
- `CustomerPayment`
- `SupplierPayment`

## Database Migrations (Infrastructure Layer)

- **Migration Name**: `DropLegacyEntityCodes`
- **Actions**:
  - `migrationBuilder.DropColumn(name: "Code", table: "Products");`
  - `migrationBuilder.DropColumn(name: "Code", table: "Customers");`
  - `migrationBuilder.DropColumn(name: "Code", table: "Suppliers");`
  - `migrationBuilder.DropColumn(name: "Code", table: "Warehouses");`
  - `migrationBuilder.DropColumn(name: "Code", table: "Categories");`
  - `migrationBuilder.DropColumn(name: "Code", table: "Units");`
  - `migrationBuilder.DropColumn(name: "Code", table: "Users");`

**Note**: Ensure any unique index configurations for `Code` in `SalesSystem.Infrastructure/Persistence/Configurations/` are also deleted before generating the migration.
