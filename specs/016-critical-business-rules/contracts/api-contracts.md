# API Contracts: Critical Business Rules Reference (Phase 16)

**Feature**: `016-critical-business-rules`

*(Note: API routing and DTO schemas remain unchanged. The updates involve standardizing HTTP responses based on the `Result<T>` pattern).*

## Contract Standardization

### Controller Response Pattern
All transactional endpoints (`POST /api/v1/sales/invoices`, `POST /api/v1/inventory/transfers`, etc.) MUST strictly map the Application layer `Result` object to appropriate HTTP status codes:

- **Success (`result.IsSuccess`)**: Returns `200 OK` or `201 Created` with the expected DTO.
- **Domain Validation Failure (`!result.IsSuccess` with `DomainException` or specific ErrorCode)**: Returns `400 Bad Request` containing the Arabic error message (e.g., "المخزون غير كافٍ").
- **Concurrency/Locking Failure**: Returns `409 Conflict`.
- **System Exception**: Caught by middleware, logs the error, and returns `500 Internal Server Error` safely without exposing stack traces.

### Example Contract (Sales Post)

```json
// POST /api/v1/sales/invoices/post
// Request
{
  "invoiceId": 123,
  "paidAmount": 500.00
}

// Response (Success - 200 OK)
{
  "isSuccess": true,
  "value": {
    "invoiceId": 123,
    "status": "Posted",
    "invoiceNumber": "INV-2026-00123"
  }
}

// Response (Failure - 400 Bad Request)
{
  "isSuccess": false,
  "error": {
    "code": "DomainValidation",
    "message": "المبلغ المدفوع أكبر من الإجمالي"
  }
}
```
