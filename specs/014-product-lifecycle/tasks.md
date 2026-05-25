# Implementation Tasks: Product Lifecycle & Media Management

**Feature**: `014-product-lifecycle`
**Spec**: [spec.md](./spec.md)
**Plan**: [plan.md](./plan.md)

## Implementation Strategy
To support execution by a smaller/cheaper LLM model, these tasks are written with extreme specificity. Each task includes exact file paths, class names, and detailed logic expectations. We will follow a strict horizontal slice per User Story after setting up the foundational schema.

## Dependencies & Execution Order
1. **Phase 1 (Foundational Schema)**: MUST be completed first.
2. **Phase 2 (US1)**: Depends on Phase 1.
3. **Phase 3 (US2)**: Depends on Phase 2 (needs the ExpirationDate field).
4. **Phase 4 (US3)**: Depends on Phase 1 (needs StockWriteOff entity). Can be done in parallel with Phase 3.

---

## Phase 1: Foundational Schema & Configuration (Blocking)
*Goal: Prepare the EF Core Database Schema for Expiration Dates, Images, and Stock Write-Offs.*

- [x] T001 Update `Product` entity in `SalesSystem.Domain/Entities/Products/Product.cs` to add `public DateTime? ExpirationDate { get; private set; }` and `public string? ImagePath { get; private set; }`. Add setter methods if appropriate for your domain pattern.
- [x] T002 Update `ProductConfiguration.cs` in `SalesSystem.Infrastructure/Configurations/Products/ProductConfiguration.cs` to add `.HasMaxLength(500)` for the `ImagePath` property.
- [x] T003 Create `StockWriteOff` entity in `SalesSystem.Domain/Entities/Inventory/StockWriteOff.cs`. Must include: `Id`, `ProductId`, `WarehouseId`, `Quantity` (decimal), `WriteOffDate` (DateTime), `Reason` (string), `CreatedByUserId`, `CreatedAt`. Add a constructor with Guard Clauses (Quantity > 0, Reason not empty).
- [x] T004 Create `StockWriteOffConfiguration` in `SalesSystem.Infrastructure/Configurations/Inventory/StockWriteOffConfiguration.cs` with `.HasPrecision(18, 3)` for `Quantity` and `DeleteBehavior.Restrict` on FKs (Product, Warehouse).
- [x] T005 Add `public DbSet<StockWriteOff> StockWriteOffs { get; set; }` to `SalesDbContext.cs` in `SalesSystem.Infrastructure/Data/SalesDbContext.cs`.
- [x] T006 Generate EF Core Migration and update database (e.g., `AddProductLifecycleAndWriteOff`).

---

## Phase 2: User Story 1 — Optional Media & Expiration Tracking (Priority P1)
*Goal: Allow users to specify expiration dates and upload images when creating/editing products.*

- [x] T007 [P] [US1] Update `CreateProductRequest`, `UpdateProductRequest`, and `ProductDto` in `SalesSystem.Contracts/Products/` to include `DateTime? ExpirationDate` and `string? ImagePath`.
- [x] T008 [P] [US1] Create `LocalImageStorageService.cs` (and `ILocalImageStorageService`) in `SalesSystem.Infrastructure/Services/`. It must save `byte[]` or `IFormFile` to `%AppData%\SalesSystem\Images`, validate extensions (.jpg, .png), restrict size to < 2MB, and return the saved string path. Register it in DI.
- [x] T009 [US1] Update `ProductService.cs` in `SalesSystem.Application/Services/Products/ProductService.cs`. Add validation so that if `ExpirationDate` is provided during creation, it cannot be a past date (`< DateTime.Today`). Ensure `ExpirationDate` and `ImagePath` map correctly to/from the domain entity.
- [x] T010 [US1] Update `ProductsController.cs` in `SalesSystem.Api/Controllers/` to handle the new fields. If image upload uses multipart/form-data, add a dedicated endpoint `POST /api/v1/products/{id}/image` that receives the image, calls `LocalImageStorageService`, and updates the product's `ImagePath`.
- [x] T011 [US1] Update `ProductEditorViewModel.cs` in `SalesSystem.DesktopPWF/ViewModels/Products/ProductEditorViewModel.cs`. Add a `bool HasExpirationDate` property, a `DateTime? ExpirationDate` property, and an `UploadImageCommand`.
- [x] T012 [US1] Update `ProductEditorView.xaml` in `SalesSystem.DesktopPWF/Views/Products/ProductEditorView.xaml`. Add a CheckBox bound to `HasExpirationDate`. Add a `DatePicker` whose `IsEnabled` is bound to `HasExpirationDate`. Add an `Image` control bound to the ImagePath with `IsAsync=True` (Lazy Loading), plus an "اختيار صورة" button to trigger the upload.

---

## Phase 3: User Story 2 — Proactive Expiration Dashboard Notifications (Priority P2)
*Goal: Alert the user automatically upon system launch if products are expiring.*

- [x] T013 [P] [US2] Update `IProductService` and `ProductService.cs` to add `GetExpiringProductsAsync(int thresholdDays)`. It should query `_uow.Products.Where(p => p.ExpirationDate.HasValue && p.ExpirationDate.Value <= DateTime.Today.AddDays(thresholdDays))`.
- [x] T014 [US2] Add endpoint `GET /api/v1/products/expiring?thresholdDays=30` in `ProductsController.cs` that calls the new service method and returns a list of DTOs.
- [x] T015 [US2] Add expiring products startup notification in `App.xaml.cs`. Since no `MainViewModel` exists, `ScheduleExpirationNotificationCheckAsync()` runs as a fire-and-forget background task 5 seconds after startup — follows the same pattern as `ScheduleBackgroundUpdateCheckAsync()`. Calls `IProductApiService.GetExpiringProductsAsync(30)`, shows styled warning dialog via `IDialogService` if count > 0.

---

## Phase 4: User Story 3 — Expired Stock Management & Accounting Write-Offs (Priority P1)
*Goal: Report expired goods and safely write them off with automatic accounting journal entries.*

- [ ] T016 [P] [US3] Create `CreateStockWriteOffRequest.cs` and `StockWriteOffDto.cs` in `SalesSystem.Contracts/Inventory/`. Ensure they include `UnitId`. Also ensure AutoMapper profiles are updated to map the DTOs where appropriate.
- [ ] T017 [US3] Create `IInventoryWriteOffService` and `InventoryWriteOffService.cs` in `SalesSystem.Application/Services/Inventory/`. Implement `WriteOffExpiredStockAsync(request)`: Open transaction, fetch Product to get `ConversionFactor` via `ProductUnit.ConvertToUnit()` (Rule-060) to convert `Quantity` to base unit if necessary. Validate stock >= converted quantity. Call `_inventoryService.DecreaseStockAsync` (creates InventoryMovement), create `StockWriteOff` entity, and Commit transaction.
- [ ] T018 [US3] Create `InventoryWriteOffController.cs` in `SalesSystem.Api/Controllers/` with `POST /api/v1/inventory/writeoff` that calls the service and returns `Result`.
- [ ] T019 [US3] Create `ExpiredProductsReportViewModel.cs` in `SalesSystem.DesktopPWF/ViewModels/Reports/`. Include a `ThresholdDays` property (combobox: 0, 30, 60), a list of products, and a `WriteOffCommand` that takes a selected product, unit (UnitId), and quantity to execute the write-off API call.
- [ ] T020 [US3] Create `ExpiredProductsReportView.xaml` in `SalesSystem.DesktopPWF/Views/Reports/`. Implement a DataGrid displaying the products. Include a "ترحيل كحذف/إتلاف" button on each row that triggers the `WriteOffCommand`.

---

## Phase 5: Polish & Cross-Cutting Concerns

- [ ] T021 Run application and thoroughly test the WPF Image Lazy Loading by opening a product list with multiple images to ensure zero UI freezing (SC-003).
- [ ] T022 Verify all new exceptions use Arabic `DomainException` messages (e.g., "تاريخ الانتهاء لا يمكن أن يكون في الماضي").
- [ ] T023 Ensure `ProductEditorViewModel` adheres to `INotifyDataErrorInfo` for all new fields, matching the Phase 13 standard.
- [ ] T024 Add a navigation button/link in the Main Menu (`MainViewModel.cs` and `MainWindow.xaml`) to open the `ExpiredProductsReportView`, ensuring users can access the new report.
