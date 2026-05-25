# Research: Product Lifecycle & Media Management

**Feature**: `014-product-lifecycle`
**Date**: 2026-05-25
**Status**: Complete

---

## Decision Log

### D-001: Image Storage Strategy

**Decision**: Product images will be stored on the local file system at `%AppData%\SalesSystem\Images`. The database will only store the relative path or filename in `Product.ImagePath`.

**Rationale**: Storing images as `VARBINARY(MAX)` in SQL Server severely bloats the `.bak` files, slows down backup/restore operations (Rule-007), and increases RAM usage. The local file system is optimized for static media serving.

---

### D-002: WPF Lazy Loading for Images

**Decision**: Implement asynchronous image loading using `BitmapImage` with `CacheOption = BitmapCacheOption.OnLoad` combined with WPF `IsAsync=True` in XAML bindings.

**Rationale**: If the application tries to load hundreds of large JPEGs synchronously while rendering a list, the UI thread will freeze. Asynchronous loading ensures smooth scrolling in the Product Grid.

---

### D-003: Write-off Accounting Integration (DEFERRED)

**Decision**: The accounting integration (automatic `JournalEntry` creation) has been DEFERRED to a future phase because the `JournalEntry` and `IAccountingService` infrastructure does not exist in the current system state.

**Rationale**: Avoiding scope creep. We will only perform stock deduction and `StockWriteOff` history recording for Phase 14.
