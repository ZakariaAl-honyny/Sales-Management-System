# API Contracts: Identifier Strategy & Validation

**Feature**: `013-identifier-validation`
**Date**: 2026-05-25

---

## DTO Modifications

The `Code` property MUST be removed from the following Data Transfer Objects (DTOs) in the `SalesSystem.Contracts` project. This includes both Request (Create/Update) and Response DTOs.

### Products
- `ProductDto`, `CreateProductRequest`, `UpdateProductRequest`

### Customers
- `CustomerDto`, `CreateCustomerRequest`, `UpdateCustomerRequest`

### Suppliers
- `SupplierDto`, `CreateSupplierRequest`, `UpdateSupplierRequest`

### Warehouses
- `WarehouseDto`, `CreateWarehouseRequest`, `UpdateWarehouseRequest`

### Categories & Units
- `CategoryDto`, `CreateCategoryRequest`, `UpdateCategoryRequest`
- `UnitDto`, `CreateUnitRequest`, `UpdateUnitRequest`

### Users
- `UserDto`, `CreateUserRequest`, `UpdateUserRequest`

---

## ErrorCode Modifications

- **Remove**: `ErrorCodes.DuplicateCode`
- **Keep**: `ErrorCodes.DuplicateBarcode` (Barcodes are still actively used and must be unique).

Any Application layer services that previously checked for duplicate codes (e.g., `_customerRepository.AnyAsync(x => x.Code == request.Code)`) must have that logic deleted entirely.
