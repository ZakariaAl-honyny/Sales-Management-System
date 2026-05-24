---
description: "Task list for Multi-Window & UI Polish (v4.5)"
---

# Tasks: Multi-Window & UI Polish (v4.5)

**Input**: `specs/012-multi-window-ui/`
**Prerequisites**: plan.md ✅ | spec.md ✅ | research.md ✅ | data-model.md ✅ | contracts/ ✅

> **Implementation Note**: Each task includes exact file paths and class names so it can be executed by any model without additional context. Tasks marked [P] can run in parallel with other [P]-marked tasks in the same phase.

---

## Phase 1: Setup

- [X] T001 Verify solution builds with 0 errors: `dotnet build SalesSystem/SalesSystem.slnx` — FILE: `SalesSystem/SalesSystem.slnx`

---

## Phase 2: Foundational (BLOCKS US1)

- [X] T002 [P] Create `IScreenWindowService.cs` interface with method `void OpenNonModal(ViewModelBase viewModel, string title, double width = 800, double height = 600)` — FILE: `SalesSystem/SalesSystem.DesktopPWF/Services/App/IScreenWindowService.cs`

---

## Phase 3: US1 — Non-Modal Multi-Window Multitasking (Priority: P1)

**Goal**: Users can open multiple non-modal editor windows that cascade, and closing them results in full memory GC (no memory leaks).

**Independent Test**: Open 5 editor windows → verify they cascade. Close them → memory profiler shows 0 active instances of the ViewModels.

- [X] T003 [US1] Create generic host `ScreenWindow.xaml`. Properties: `WindowStartupLocation="Manual"`, `FlowDirection="RightToLeft"`. Content: `<ContentControl Content="{Binding}" />`. In `.xaml.cs` hook the `Closed` event to cast `DataContext` to `IDisposable` and explicitly call `Dispose()`. — FILE: `SalesSystem/SalesSystem.DesktopPWF/Views/ScreenWindow.xaml` + `.cs`

- [X] T004 [US1] Implement `ScreenWindowService` implementing `IScreenWindowService`. Maintain `List<WeakReference<Window>> _openWindows`. In `OpenNonModal`, instantiate `ScreenWindow`, set `DataContext`, `Title`, `Width`, `Height`. Calculate offset: `count = _openWindows.Count(w => w.TryGetTarget(out _))`, `Left = App.Current.MainWindow.Left + 30 * (count % 10)`, `Top = App.Current.MainWindow.Top + 30 * (count % 10)`. Add WeakReference to list. Call `window.Show()`. — FILE: `SalesSystem/SalesSystem.DesktopPWF/Services/App/ScreenWindowService.cs`

- [X] T005 [US1] Update `App.xaml.cs` to register `IScreenWindowService` as a Singleton in DI. — FILE: `SalesSystem/SalesSystem.DesktopPWF/App.xaml.cs`

- [X] T006 [US1] Audit `ViewModelBase.cs` and all ViewModels using `IEventBus`. Ensure EventBus subscriptions are stored as `IDisposable` and are properly disposed within the `public virtual void Dispose()` method to prevent GC roots. — FILE: `SalesSystem/SalesSystem.DesktopPWF/ViewModels/Base/ViewModelBase.cs`

- [X] T007 [US1] Update list ViewModels (Product, Customer, Supplier, Category, Unit, User) to use `_screenWindowService.OpenScreen(...)` instead of `_dialogService.ShowDialog(...)` for all Entity Editors. — FILE: `SalesSystem/SalesSystem.DesktopPWF/ViewModels/Products/ProductListViewModel.cs` + 5 others

**Checkpoint**: Launch app, click "New Product", a non-modal window opens. Click "New Invoice", a second non-modal window opens cascaded over the first.

---

## Phase 4: US4 — Consistent and Safe Dialog Ownership (Priority: P2)

**Goal**: Fix "Cannot set Owner property" crashes and completely eradicate `MessageBox.Show` from the desktop project.

**Independent Test**: Trigger an error from a child window. The error dialog centers over the child window. Search codebase for `MessageBox.Show` = 0 results.

- [X] T008 [P] [US4] Update `DialogService.cs`. Owner resolution now uses `GetActiveWindow()` helper that finds the active window first, falling back to MainWindow. — FILE: `SalesSystem/SalesSystem.DesktopPWF/Services/Dialog/DialogService.cs`

- [X] T009 [P] [US4] Search and replace all instances of `System.Windows.MessageBox.Show` across the `DesktopPWF` codebase. — FILE: *Multiple Files in `SalesSystem.DesktopPWF`* — **Already 0 instances, no changes needed**

**Checkpoint**: Zero instances of `MessageBox` exist. Dialogs safely position themselves over the currently active window.

---

## Phase 5: US2 — Default Newest-First List Sorting (Priority: P2)

**Goal**: All list screens automatically show the newest items at the top without user interaction.

**Independent Test**: Open Products List. The most recently created product is the first item.

- [X] T010 [P] [US2] Update list ViewModels (e.g., `ProductsListViewModel`, `CustomersListViewModel`, `InvoicesListViewModel`) or the backend application services serving these lists to explicitly append `.OrderByDescending(x => x.Id)` or `.OrderByDescending(x => x.InvoiceDate)`. — FILE: `SalesSystem/SalesSystem.DesktopPWF/ViewModels/Inventory/InventoryViewModel.cs`, `SalesSystem/SalesSystem.DesktopPWF/ViewModels/Inventory/LowStockViewModel.cs`

**Checkpoint**: 100% of data lists sort descending by default.

---

## Phase 6: US3 — Arabic ToolTips for UI Discoverability (Priority: P2)

**Goal**: All primary interactive controls have Arabic ToolTips.

**Independent Test**: Hover over the Save button in any editor, see "حفظ البيانات المدخلة".

- [X] T011 [P] [US3] Perform a sweeping pass across all XAML files in the `Views` directory. Add appropriate Arabic `ToolTip="..."` to primary `<Button>` elements (e.g., Save, Delete, New, Print) and required `<TextBox>`/`<ComboBox>` elements. — FILE: *Multiple XAML files in `SalesSystem.DesktopPWF/Views/`* — 12 XAML files updated ~19 ToolTips added

**Checkpoint**: Hovering over buttons and inputs shows helpful Arabic context.

---

## Phase 7: Polish

- [X] T012 Update `docs/CHANGELOG.md` with v4.5 entry: Multi-Window & UI Polish (Non-modal editors, cascading windows, GC leak fixes, list sorting, Arabic tooltips, robust dialog ownership). — FILE: `docs/CHANGELOG.md`

---

## Dependencies

- **Phase 1 (Setup)**: No dependencies
- **Phase 2 (Foundational)**: Depends on Phase 1
- **Phase 3 (US1 - Multi-Window)**: Depends on Phase 2. Core architectural change.
- **Phase 4 (US4 - Dialog Fix)**: Independent of US1, but best done after to ensure dialogs work in the new multi-window context.
- **Phase 5 (US2 - Sorting)**: Independent UI polish.
- **Phase 6 (US3 - Tooltips)**: Independent UI polish.
- **Phase 7 (Polish)**: Depends on all previous phases.

---

## Implementation Strategy

### MVP First (US1 + US4)
1. Build the generic non-modal host and the `ScreenWindowService`.
2. Fix the `EventBus` GC memory leak constraints immediately.
3. Fix the `DialogService` owner resolution, as non-modal windows will immediately expose this bug if unpatched.
4. Replace `MessageBox.Show`.
5. Once the core multi-window architecture is stable, apply the global UX sweeps (Sorting and Tooltips) in parallel.
