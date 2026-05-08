# Data Model: Desktop Modules (005)

**Branch**: `005-desktop-modules` | **Date**: 2026-05-08

> This document covers the Desktop-layer data structures, API client models,
> EventBus messages, and service interfaces needed for Phase 5.
> Domain entities and DB schema are already established in Phase 2/3.

---

## 1. API Client DTOs (Desktop → API)

These are the C# records/classes the Desktop uses to send/receive data.
They live in `SalesSystem.Contracts` (already established).

### 1.1 Products

```csharp
// Contracts/Responses/ProductResponse.cs  (already exists from Phase 3)
public record ProductResponse(
    int Id, string Code, string Barcode, string Name,
    int CategoryId, string CategoryName,
    int UnitId, string UnitName,
    decimal PurchasePrice, decimal SalePrice, decimal MinStock,
    string? Description, bool IsActive
);

// Contracts/Requests/CreateProductRequest.cs  (already exists)
public record CreateProductRequest(
    string Code, string Barcode, string Name,
    int CategoryId, int UnitId,
    decimal PurchasePrice, decimal SalePrice,
    decimal MinStock, string? Description
);

// Contracts/Requests/UpdateProductRequest.cs  (already exists)
public record UpdateProductRequest(
    string Code, string Barcode, string Name,
    int CategoryId, int UnitId,
    decimal PurchasePrice, decimal SalePrice,
    decimal MinStock, string? Description
);
```

### 1.2 Categories & Units

```csharp
// Contracts/Responses/CategoryResponse.cs
public record CategoryResponse(int Id, string Name, bool IsActive);

// Contracts/Requests/CreateCategoryRequest.cs
public record CreateCategoryRequest(string Name);

// Contracts/Responses/UnitResponse.cs
public record UnitResponse(int Id, string Name, string? Symbol, bool IsActive);

// Contracts/Requests/CreateUnitRequest.cs
public record CreateUnitRequest(string Name, string? Symbol);
```

### 1.3 Customers & Suppliers

```csharp
// Contracts/Responses/CustomerResponse.cs  (already exists)
public record CustomerResponse(
    int Id, string Name, string? Phone, string? Address,
    decimal CurrentBalance, bool IsActive
);

// Contracts/Requests/CreateCustomerRequest.cs  (already exists)
public record CreateCustomerRequest(string Name, string? Phone, string? Address);

// Contracts/Requests/UpdateCustomerRequest.cs  (already exists)
public record UpdateCustomerRequest(string Name, string? Phone, string? Address);

// SupplierResponse / CreateSupplierRequest / UpdateSupplierRequest — mirror Customer
```

### 1.4 Warehouses

```csharp
// Contracts/Responses/WarehouseResponse.cs
public record WarehouseResponse(
    int Id, string Name, string? Address, bool IsDefault, bool IsActive
);

// Contracts/Responses/WarehouseStockSummaryResponse.cs
public record WarehouseStockSummaryResponse(
    int ProductId, string ProductName, string ProductCode,
    decimal Quantity
);

// Contracts/Requests/CreateWarehouseRequest.cs
public record CreateWarehouseRequest(string Name, string? Address, bool IsDefault);

// Contracts/Requests/UpdateWarehouseRequest.cs
public record UpdateWarehouseRequest(string Name, string? Address);
```

### 1.5 Sales Invoice

```csharp
// Contracts/Requests/CreateSalesInvoiceRequest.cs
public record CreateSalesInvoiceRequest(
    int CustomerId,
    int WarehouseId,
    PaymentType PaymentType,
    decimal PaidAmount,
    decimal InvoiceDiscount,
    decimal TaxRate,          // NEW: from clarification Q1
    bool IsTaxInclusive,      // NEW: from clarification Q1
    string? Notes,
    List<SalesInvoiceItemRequest> Items
);

public record SalesInvoiceItemRequest(
    int ProductId,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountAmount
);

// Contracts/Responses/SalesInvoiceResponse.cs
public record SalesInvoiceResponse(
    int Id, string InvoiceNumber,
    int CustomerId, string CustomerName,
    int WarehouseId, string WarehouseName,
    InvoiceStatus Status, PaymentType PaymentType,
    decimal SubTotal, decimal InvoiceDiscount,
    decimal TaxRate, decimal TaxAmount,   // NEW
    bool IsTaxInclusive,                  // NEW
    decimal TotalAmount, decimal PaidAmount, decimal DueAmount,
    DateTime InvoiceDate, string? Notes,
    List<SalesInvoiceItemResponse> Items
);

public record SalesInvoiceItemResponse(
    int Id, int ProductId, string ProductName, string ProductCode,
    decimal Quantity, decimal UnitPrice, decimal DiscountAmount, decimal LineTotal
);
```

### 1.6 Purchase Invoice

```csharp
// Mirror of Sales with Supplier instead of Customer,
// UnitCost instead of UnitPrice, and PurchaseIn movement type.
public record CreatePurchaseInvoiceRequest(
    int SupplierId, int WarehouseId,
    PaymentType PaymentType, decimal PaidAmount,
    decimal InvoiceDiscount, decimal TaxRate, bool IsTaxInclusive,
    string? Notes, List<PurchaseInvoiceItemRequest> Items
);
```

### 1.7 Returns

```csharp
// Contracts/Requests/CreateSalesReturnRequest.cs
public record CreateSalesReturnRequest(
    int CustomerId, int WarehouseId,
    int? OriginalSalesInvoiceId,    // optional reference
    decimal TaxRate, bool IsTaxInclusive,
    string? Notes,
    List<SalesReturnItemRequest> Items
);

public record SalesReturnItemRequest(
    int ProductId, decimal Quantity, decimal UnitPrice, decimal DiscountAmount
);

// PurchaseReturn mirrors SalesReturn with SupplierId
```

### 1.8 Stock Transfer

```csharp
// Contracts/Requests/CreateStockTransferRequest.cs
public record CreateStockTransferRequest(
    int SourceWarehouseId, int DestinationWarehouseId,
    string? Notes,
    List<StockTransferItemRequest> Items
);

public record StockTransferItemRequest(int ProductId, decimal Quantity);

// Contracts/Responses/StockTransferResponse.cs
public record StockTransferResponse(
    int Id, string TransferNumber,
    int SourceWarehouseId, string SourceWarehouseName,
    int DestinationWarehouseId, string DestinationWarehouseName,
    InvoiceStatus Status, DateTime TransferDate, string? Notes,
    List<StockTransferItemResponse> Items
);
```

### 1.9 Payments

```csharp
// Contracts/Requests/CreateCustomerPaymentRequest.cs
public record CreateCustomerPaymentRequest(
    int CustomerId,
    decimal Amount,
    int? InvoiceId,       // optional link
    string? Notes
);

// Contracts/Requests/CreateSupplierPaymentRequest.cs
public record CreateSupplierPaymentRequest(
    int SupplierId,
    decimal Amount,
    int? InvoiceId,
    string? Notes
);

// Contracts/Responses/CustomerPaymentResponse.cs
public record CustomerPaymentResponse(
    int Id, string PaymentNumber,
    int CustomerId, string CustomerName,
    decimal Amount, int? InvoiceId,
    DateTime PaymentDate, string? Notes
);
```

### 1.10 Reports

```csharp
// Contracts/Requests/ReportFilterRequest.cs
public record ReportFilterRequest(
    DateTime? DateFrom, DateTime? DateTo,
    int? CustomerId, int? SupplierId,
    int? WarehouseId, int? ProductId
);

// Report responses are List<Dictionary<string,object>> for flexibility,
// serialized to DataTable on the Desktop for display + export.
```

---

## 2. EventBus Messages (Desktop)

All messages live in `SalesSystem.Desktop/Messaging/Messages/`.

```csharp
// Pattern: carry entity ID only — NO data payloads (RULE-034)

public record ProductChangedMessage(int ProductId);
public record CategoryChangedMessage(int CategoryId);
public record UnitChangedMessage(int UnitId);
public record CustomerChangedMessage(int CustomerId);
public record SupplierChangedMessage(int SupplierId);
public record WarehouseChangedMessage(int WarehouseId);
public record SaleInvoiceChangedMessage(int InvoiceId);
public record PurchaseInvoiceChangedMessage(int InvoiceId);
public record SalesReturnChangedMessage(int ReturnId);
public record PurchaseReturnChangedMessage(int ReturnId);
public record StockTransferChangedMessage(int TransferId);
public record CustomerPaymentChangedMessage(int PaymentId);
public record SupplierPaymentChangedMessage(int PaymentId);
public record StockChangedMessage(int ProductId, int WarehouseId);  // triggers Dashboard refresh
```

---

## 3. Desktop Service Interfaces

All interfaces live in `SalesSystem.Desktop/Services/Api/`.

```csharp
// IProductApiService.cs
public interface IProductApiService
{
    Task<List<ProductResponse>> GetAllAsync(string? search, int? categoryId, bool includeInactive = false);
    Task<ProductResponse?> GetByIdAsync(int id);
    Task<ProductResponse?> GetByBarcodeAsync(string barcode);
    Task<Result<ProductResponse>> CreateAsync(CreateProductRequest request);
    Task<Result<ProductResponse>> UpdateAsync(int id, UpdateProductRequest request);
    Task<Result> DeactivateAsync(int id);
    Task<Result> ReactivateAsync(int id);
}

// ICategoryApiService.cs
public interface ICategoryApiService
{
    Task<List<CategoryResponse>> GetAllAsync(bool includeInactive = false);
    Task<Result<CategoryResponse>> CreateAsync(CreateCategoryRequest request);
    Task<Result> DeleteAsync(int id);
}

// IUnitApiService.cs
public interface IUnitApiService
{
    Task<List<UnitResponse>> GetAllAsync(bool includeInactive = false);
    Task<Result<UnitResponse>> CreateAsync(CreateUnitRequest request);
    Task<Result> DeleteAsync(int id);
}

// ICustomerApiService.cs
public interface ICustomerApiService
{
    Task<List<CustomerResponse>> GetAllAsync(string? search, bool includeInactive = false);
    Task<CustomerResponse?> GetByIdAsync(int id);
    Task<Result<CustomerResponse>> CreateAsync(CreateCustomerRequest request);
    Task<Result<CustomerResponse>> UpdateAsync(int id, UpdateCustomerRequest request);
    Task<Result> DeactivateAsync(int id);
    Task<Result> ReactivateAsync(int id);
}

// ISupplierApiService.cs — mirrors ICustomerApiService

// IWarehouseApiService.cs
public interface IWarehouseApiService
{
    Task<List<WarehouseResponse>> GetAllAsync(bool includeInactive = false);
    Task<WarehouseResponse?> GetByIdAsync(int id);
    Task<List<WarehouseStockSummaryResponse>> GetStockAsync(int warehouseId);
    Task<Result<WarehouseResponse>> CreateAsync(CreateWarehouseRequest request);
    Task<Result<WarehouseResponse>> UpdateAsync(int id, UpdateWarehouseRequest request);
    Task<Result> SetDefaultAsync(int id);
}

// ISalesInvoiceApiService.cs
public interface ISalesInvoiceApiService
{
    Task<List<SalesInvoiceResponse>> GetAllAsync(DateTime? from, DateTime? to, InvoiceStatus? status);
    Task<SalesInvoiceResponse?> GetByIdAsync(int id);
    Task<Result<SalesInvoiceResponse>> CreateAsync(CreateSalesInvoiceRequest request);
    Task<Result<SalesInvoiceResponse>> PostAsync(int id);
    Task<Result> CancelAsync(int id);
}

// IPurchaseInvoiceApiService.cs — mirrors ISalesInvoiceApiService with Purchase types

// ISalesReturnApiService.cs / IPurchaseReturnApiService.cs — same pattern

// IStockTransferApiService.cs
public interface IStockTransferApiService
{
    Task<List<StockTransferResponse>> GetAllAsync(DateTime? from, DateTime? to);
    Task<StockTransferResponse?> GetByIdAsync(int id);
    Task<Result<StockTransferResponse>> CreateAsync(CreateStockTransferRequest request);
    Task<Result<StockTransferResponse>> PostAsync(int id);
    Task<Result> CancelAsync(int id);
}

// ICustomerPaymentApiService.cs
public interface ICustomerPaymentApiService
{
    Task<List<CustomerPaymentResponse>> GetAllAsync(int? customerId, DateTime? from, DateTime? to);
    Task<Result<CustomerPaymentResponse>> CreateAsync(CreateCustomerPaymentRequest request);
}

// ISupplierPaymentApiService.cs — mirrors ICustomerPaymentApiService

// IReportApiService.cs
public interface IReportApiService
{
    Task<DataTable> GetDailySalesReportAsync(ReportFilterRequest filter);
    Task<DataTable> GetDailyPurchasesReportAsync(ReportFilterRequest filter);
    Task<DataTable> GetStockReportAsync(int? warehouseId);
    Task<DataTable> GetCustomerBalanceReportAsync(int? customerId);
    Task<DataTable> GetSupplierBalanceReportAsync(int? supplierId);
    Task<DataTable> GetProductMovementReportAsync(int? productId, DateTime? from, DateTime? to);
    Task<DataTable> GetLowStockReportAsync();
}

// IDashboardApiService.cs
public interface IDashboardApiService
{
    Task<DashboardSummaryResponse> GetSummaryAsync();
}
```

---

## 4. State Transitions (UI Layer)

### Invoice Lifecycle (UI Buttons)

| Current Status | Available Actions |
|---------------|------------------|
| `Draft` | Save Draft, Post Invoice, Delete (soft cancel) |
| `Posted` | Cancel Invoice (shows confirmation + reversal warning) |
| `Cancelled` | View Only (all buttons disabled) |

### Invoice Button Visibility Rules

- **Save Draft**: Visible only when Status = Draft
- **Post Invoice**: Visible only when Status = Draft AND Items.Count > 0
- **Cancel Invoice**: Visible only when Status = Posted AND user role = ManagerAndAbove
- **Edit**: Disabled for Posted and Cancelled invoices

---

## 5. Re-activation Rules

Per clarification Q4, each list screen has a **"Show Deactivated" toggle checkbox**:

| Toggle State | API call |
|-------------|----------|
| OFF (default) | `GET /api/[entity]?includeInactive=false` |
| ON | `GET /api/[entity]?includeInactive=true` |

When a deactivated entity is selected in the list and the user presses **"Re-activate"** (visible only when "Show Deactivated" is ON), a `PUT /api/[entity]/{id}/reactivate` is called.

---

## 6. Decimal Precision Summary

| Field Type | C# Type | DB Type | Precision |
|-----------|---------|---------|-----------|
| Money (price, cost, discount, total) | `decimal` | `decimal(18,2)` | 2 dp |
| Quantity | `decimal` | `decimal(18,3)` | 3 dp |
| Tax Rate | `decimal` | `decimal(5,2)` | percentage |
| Balance | `decimal` | `decimal(18,2)` | 2 dp |
