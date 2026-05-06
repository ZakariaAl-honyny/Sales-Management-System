# Contracts: Foundation Setup

**Date**: 2026-05-06
**Scope**: Phase 1 defines data contracts only (no API endpoints yet)

## Result<T> Contract

The shared result wrapper used by ALL service methods.

```csharp
public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }
    public string? ErrorCode { get; }

    public static Result<T> Success(T value);
    public static Result<T> Failure(string error, string? errorCode = null);
}

public class Result
{
    public bool IsSuccess { get; }
    public string? Error { get; }
    public string? ErrorCode { get; }

    public static Result Success();
    public static Result Failure(string error, string? errorCode = null);
}
```

## PagedResult<T> Contract

```csharp
public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; }
    public int TotalCount { get; }
    public int Page { get; }
    public int PageSize { get; }
    public int TotalPages { get; }
    public bool HasNext { get; }
    public bool HasPrevious { get; }
}
```

## ErrorCodes Contract

```csharp
public static class ErrorCodes
{
    public const string NotFound = "NOT_FOUND";
    public const string ValidationError = "VALIDATION_ERROR";
    public const string DuplicateEntry = "DUPLICATE_ENTRY";
    public const string InsufficientStock = "INSUFFICIENT_STOCK";
    public const string InvalidOperation = "INVALID_OPERATION";
    public const string Unauthorized = "UNAUTHORIZED";
    public const string Forbidden = "FORBIDDEN";
}
```

## DTO Contracts (representative examples)

### ProductDto
```csharp
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
    string? Description,
    bool IsActive,
    DateTime CreatedAt
);
```

### SalesInvoiceDto
```csharp
public record SalesInvoiceDto(
    int SalesInvoiceId,
    string InvoiceNo,
    int? CustomerId,
    string? CustomerName,
    int WarehouseId,
    string WarehouseName,
    DateTime InvoiceDate,
    DateOnly? DueDate,
    byte PaymentType,
    decimal SubTotal,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal DueAmount,
    string? Notes,
    byte Status,
    DateTime CreatedAt,
    IReadOnlyList<SalesInvoiceItemDto> Items
);
```

## Request Contracts (representative examples)

### CreateProductRequest
```csharp
public record CreateProductRequest(
    string? Code,
    string? Barcode,
    string Name,
    int? CategoryId,
    int? UnitId,
    decimal PurchasePrice,
    decimal SalePrice,
    decimal MinStock,
    string? Description
);
```

### LoginRequest
```csharp
public record LoginRequest(
    string UserName,
    string Password
);
```

### LoginResponse
```csharp
public record LoginResponse(
    string Token,
    string UserName,
    string FullName,
    byte Role,
    DateTime ExpiresAt
);
```

## IUnitOfWork Contract

```csharp
public interface IUnitOfWork : IDisposable
{
    // Repository properties (added in Phase 2)
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default);
}
```

## IGenericRepository<T> Contract

```csharp
public interface IGenericRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    void Update(T entity);
    void SoftDelete(T entity);
}
```

---

**Note**: Full DTO and Request classes for all 22 entities will be created
during implementation. The patterns above serve as the contract template
that all DTOs/Requests MUST follow.
