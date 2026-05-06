# Quickstart: Backend Core (Phase 2)

## Prerequisites

- .NET 10 SDK installed
- SQL Server running locally
- Environment variable `SALESSYSTEM_DB_CONNECTION` set
- Environment variable `SALESSYSTEM_JWT_SECRET` set (min 32 chars)

## How to Run

```bash
cd SalesSystem/SalesSystem.Api
dotnet run
```

API starts at `https://localhost:5001`. Open Swagger at `https://localhost:5001/swagger`.

## Verification Steps

### 1. Login
```http
POST /api/auth/login
{ "userName": "admin", "password": "admin123" }
```
Expected: 200 with JWT token.

### 2. Create Product (use token from step 1)
```http
POST /api/products
Authorization: Bearer {token}
{ "name": "Test Product", "salePrice": 10.00, "purchasePrice": 5.00 }
```
Expected: 201 with ProductDto.

### 3. List Products
```http
GET /api/products
Authorization: Bearer {token}
```
Expected: 200 with paginated results including the test product.

### 4. Test Authorization (Cashier blocked)
Create a Cashier user, login, then try `POST /api/products`. Expected: 403 Forbidden.

### 5. Test Validation
```http
POST /api/products
Authorization: Bearer {token}
{ "name": "", "salePrice": -1 }
```
Expected: 400 with field-level validation errors.

## Definition of Done

- [ ] All CRUD endpoints work via Swagger
- [ ] Invalid requests rejected with clear error messages
- [ ] All requests logged (check `logs/` directory)
- [ ] Unauthorized requests return 401/403
- [ ] Solution builds with 0 errors, 0 warnings
