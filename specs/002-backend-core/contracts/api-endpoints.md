# API Endpoint Contracts: Backend Core (Phase 2)

**Base URL**: `https://localhost:5001/api`  
**Auth**: JWT Bearer token (except `/auth/login`)

## Authentication

| Method | Endpoint | Auth | Request | Response |
|--------|----------|------|---------|----------|
| POST | `/auth/login` | None | `LoginRequest` | `LoginResponse` |

**LoginRequest**: `{ userName: string, password: string }`  
**LoginResponse**: `{ token: string, userName: string, fullName: string, role: byte, expiresAt: datetime }`

## Products (ManagerAndAbove)

| Method | Endpoint | Request | Response |
|--------|----------|---------|----------|
| GET | `/products` | `?search=&categoryId=&page=&pageSize=` | `PagedResult<ProductDto>` |
| GET | `/products/{id}` | — | `ProductDto` |
| POST | `/products` | `CreateProductRequest` | `ProductDto` |
| PUT | `/products/{id}` | `UpdateProductRequest` | `ProductDto` |
| DELETE | `/products/{id}` | — | 204 No Content |

## Categories (ManagerAndAbove)

| Method | Endpoint | Request | Response |
|--------|----------|---------|----------|
| GET | `/categories` | — | `List<CategoryDto>` |
| GET | `/categories/{id}` | — | `CategoryDto` |
| POST | `/categories` | `CreateCategoryRequest` | `CategoryDto` |
| PUT | `/categories/{id}` | `UpdateCategoryRequest` | `CategoryDto` |
| DELETE | `/categories/{id}` | — | 204 No Content |

## Units (ManagerAndAbove)

| Method | Endpoint | Request | Response |
|--------|----------|---------|----------|
| GET | `/units` | — | `List<UnitDto>` |
| GET | `/units/{id}` | — | `UnitDto` |
| POST | `/units` | `CreateUnitRequest` | `UnitDto` |
| PUT | `/units/{id}` | `UpdateUnitRequest` | `UnitDto` |
| DELETE | `/units/{id}` | — | 204 No Content |

## Customers (AllStaff — Cashier: GET only)

| Method | Endpoint | Request | Response |
|--------|----------|---------|----------|
| GET | `/customers` | `?search=&page=&pageSize=` | `PagedResult<CustomerDto>` |
| GET | `/customers/{id}` | — | `CustomerDto` |
| POST | `/customers` | `CreateCustomerRequest` | `CustomerDto` |
| PUT | `/customers/{id}` | `UpdateCustomerRequest` | `CustomerDto` |
| DELETE | `/customers/{id}` | — | 204 No Content |

## Suppliers (ManagerAndAbove)

| Method | Endpoint | Request | Response |
|--------|----------|---------|----------|
| GET | `/suppliers` | `?search=&page=&pageSize=` | `PagedResult<SupplierDto>` |
| GET | `/suppliers/{id}` | — | `SupplierDto` |
| POST | `/suppliers` | `CreateSupplierRequest` | `SupplierDto` |
| PUT | `/suppliers/{id}` | `UpdateSupplierRequest` | `SupplierDto` |
| DELETE | `/suppliers/{id}` | — | 204 No Content |

## Warehouses (AdminOnly)

| Method | Endpoint | Request | Response |
|--------|----------|---------|----------|
| GET | `/warehouses` | — | `List<WarehouseDto>` |
| GET | `/warehouses/{id}` | — | `WarehouseDto` |
| POST | `/warehouses` | `CreateWarehouseRequest` | `WarehouseDto` |
| PUT | `/warehouses/{id}` | `UpdateWarehouseRequest` | `WarehouseDto` |
| DELETE | `/warehouses/{id}` | — | 204 No Content |

## Error Response Format (All Endpoints)

```json
{
  "error": "Human-readable error message",
  "errorCode": "VALIDATION_ERROR | NOT_FOUND | DUPLICATE_ENTRY | ...",
  "details": [ "Field-level error 1", "Field-level error 2" ]
}
```

| HTTP Status | Meaning |
|-------------|---------|
| 200 | Success |
| 201 | Created |
| 204 | Deleted (soft) |
| 400 | Validation error |
| 401 | Unauthenticated |
| 403 | Unauthorized (wrong role) |
| 404 | Entity not found |
| 500 | Internal server error |
