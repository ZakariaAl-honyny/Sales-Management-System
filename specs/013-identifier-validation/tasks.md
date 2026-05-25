---
description: "Task list for Identifier Strategy & Validation (v4.5.3–v4.6.2)"
---

# Tasks: Identifier Strategy & Validation (v4.5.3–v4.6.2)

**Input**: `specs/013-identifier-validation/`
**Prerequisites**: plan.md ✅ | spec.md ✅ | research.md ✅ | data-model.md ✅ | contracts/ ✅

> **Implementation Note**: Each task includes exact file paths and class names so it can be executed by any model without additional context. Tasks marked [P] can run in parallel with other [P]-marked tasks in the same phase.

---

## Phase 1: Setup

- [X] T001 Verify solution builds with 0 errors: `dotnet build SalesSystem/SalesSystem.slnx` — FILE: `SalesSystem/SalesSystem.slnx`

---

## Phase 2: US1 — Simplified Entity Identification (Priority: P1)

**Goal**: Eradicate the legacy `Code` property from non-document entities across the entire stack.

**Independent Test**: API requests to create/update Products, Customers, etc. no longer contain `Code`. The database tables no longer have `Code` columns.

- [X] T002 [P] [US1] Remove `string Code` property and related constructor parameters from Domain Entities: `Product`, `Customer`, `Supplier`, `Warehouse`, `Category`, `Unit`, `User`. — FILE: `SalesSystem/SalesSystem.Domain/Entities/Products/Product.cs` + 6 others

- [X] T003 [P] [US1] Remove `string Code` property from all related DTOs (Data Transfer Objects) and Request classes in the Contracts project. Remove `ErrorCodes.DuplicateCode`. — FILE: `SalesSystem/SalesSystem.Contracts/Requests/Products/CreateProductRequest.cs` + multiple others

- [X] T004 [US1] Remove database index configurations referencing `Code` from EF Core entity configurations. — FILE: `SalesSystem/SalesSystem.Infrastructure/Persistence/Configurations/ProductConfiguration.cs` + 6 others

- [X] T005 [US1] Remove any code-generation or duplicate-code-checking logic from Application Layer Services (e.g., `ProductService`, `CustomerService`). — FILE: `SalesSystem/SalesSystem.Application/Services/Products/ProductService.cs` + multiple others

- [X] T006 [US1] Generate and apply EF Core Migration to drop the `Code` columns. (N/A — Code was never in database) Command: `dotnet ef migrations add DropLegacyEntityCodes --project SalesSystem/SalesSystem.Infrastructure --startup-project SalesSystem/SalesSystem.Api`. — FILE: `SalesSystem/SalesSystem.Infrastructure/Persistence/SalesDbContextModelSnapshot.cs`

**Checkpoint**: Solution compiles. Migration drops `Code` columns successfully.

---

## Phase 3: US2 — Immediate Visual Validation Feedback (Priority: P1)

**Goal**: Implement the standard WPF `INotifyDataErrorInfo` framework and visual styling.

**Independent Test**: Forcing a validation error displays a red border and a `❗` icon on the specific text box.

- [X] T007 [P] [US2] Implement `INotifyDataErrorInfo` interface in `ViewModelBase`. Add `Dictionary<string, List<string>> _errors`, `HasErrors`, `GetErrors()`, `AddError()`, and `ClearErrors()` methods. — FILE: `SalesSystem/SalesSystem.DesktopPWF/ViewModels/Base/ViewModelBase.cs`

- [X] T008 [P] [US2] Define standard `Validation.ErrorTemplate` in global XAML resources. Create a style targeting `TextBox` and `ComboBox` that draws a red border and displays the `❗` icon with a tooltip bound to the error message. — FILE: `SalesSystem/SalesSystem.DesktopPWF/Views/Resources/Styles.xaml` (or `App.xaml`)

**Checkpoint**: `ViewModelBase` supports standard error reporting.

---

## Phase 4: US3 — Comprehensive Save Validation Dialogue (Priority: P1)

**Goal**: Update all editor ViewModels to use the new validation framework and intercept invalid saves with a dialogue summary.

**Independent Test**: Clicking "Save" on an empty form opens a DialogService warning listing exactly which fields are missing.

- [X] T009 [P] [US3] Refactor Product, Category, and Unit Editor ViewModels. Remove legacy manual boolean error flags (e.g., `HasNameError`). Implement `ValidateAll()` using `AddError` and `ClearErrors`. Remove `CanExecute` predicate from `SaveCommand`. Intercept save to show `_dialogService.ShowWarningAsync()` if invalid. — FILE: `SalesSystem/SalesSystem.DesktopPWF/ViewModels/Products/ProductEditorViewModel.cs` + 2 others

- [X] T010 [P] [US3] Refactor Customer and Supplier Editor ViewModels. Apply the same `ValidateAll()` and `SaveCommand` pattern. — FILE: `SalesSystem/SalesSystem.DesktopPWF/ViewModels/Customers/CustomerEditorViewModel.cs` + 1 other

- [X] T011 [P] [US3] Refactor Warehouse and User Editor ViewModels. Apply the same `ValidateAll()` and `SaveCommand` pattern. — FILE: `SalesSystem/SalesSystem.DesktopPWF/ViewModels/Inventory/WarehouseEditorViewModel.cs` + 1 other

- [X] T012 [P] [US3] Clean up XAML files for all updated editors. Remove any manually coded `<TextBlock Foreground="Red" />` error messages that were previously bound to `HasXError` booleans. Ensure input bindings use `UpdateSourceTrigger=PropertyChanged` and `ValidatesOnNotifyDataErrors=True`. — FILE: *Multiple XAML files in `SalesSystem.DesktopPWF/Views/`*

**Checkpoint**: All master data editors validate correctly, display red borders, and show an aggregated error dialogue on failed saves.

---

## Phase 5: Polish

- [X] T013 Update `docs/CHANGELOG.md` with v4.6 entry: Identifier Strategy & Validation (Removed legacy Code fields, migrated to auto-increment IDs, implemented standard INotifyDataErrorInfo WPF validation with dialog summaries). — FILE: `docs/CHANGELOG.md`

---

## Dependencies

- **Phase 1 (Setup)**: No dependencies
- **Phase 2 (US1 - Code Purge)**: Depends on Phase 1. Must be completed as a cohesive unit to maintain compilability.
- **Phase 3 (US2 - UI Framework)**: Depends on Phase 1.
- **Phase 4 (US3 - UI Refactor)**: Depends on Phase 3 (`ViewModelBase` support).
- **Phase 5 (Polish)**: Depends on all previous phases.

---

## Implementation Strategy

### MVP First (US1 + US2)
1. Completely purge the `Code` field from the backend (Phase 2). This touches many files and breaks compilation until fully completed.
2. Build the UI foundation (Phase 3) in `ViewModelBase` and XAML styles.
3. Iteratively refactor the UI ViewModels (Phase 4), testing one (e.g., `ProductEditorViewModel`) before rolling the pattern out to the remaining editors.
