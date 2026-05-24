# Feature Specification: Multi-Window & UI Polish (v4.5)

**Feature Branch**: `012-multi-window-ui`
**Created**: 2026-05-25
**Status**: Draft
**Input**: Phase 12 — Multi-Window & UI Polish (v4.5)

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Non-Modal Multi-Window Multitasking (Priority: P1)

A cashier is in the middle of creating a complex sales invoice when a supplier arrives. The cashier needs to quickly open the Purchase Invoice editor, receive the goods, and then return to the ongoing sales invoice without losing their place. Both editors need to be open simultaneously on the screen in separate, movable windows.

**Independent Test**: Open the "Sales Invoice" editor → Window opens → Keep it open and switch to the main dashboard → Click "Purchase Invoice" editor → Second window opens, offset slightly from the first (cascading) → User can type in both windows independently → Close both windows. Memory profiler confirms both ViewModels are garbage collected.

**Acceptance Scenarios**:

1. **Given** the user is on the main dashboard, **When** they launch an editor (e.g., Product Editor, Invoice Editor), **Then** the editor opens in a non-modal generic window that allows interaction with the rest of the application.
2. **Given** one editor window is already open, **When** the user launches a second editor, **Then** the new window appears slightly offset (cascaded) from the first window, making both title bars visible.
3. **Given** a user opens and then closes an editor window, **When** garbage collection occurs, **Then** the window and its associated ViewModel are fully released from memory (no lingering EventBus subscriptions or hard references).

---

### User Story 2 — Default Newest-First List Sorting (Priority: P2)

A manager adds a new product or creates a new invoice. When they view the corresponding list screen, they want to immediately see the item they just created at the very top of the list without having to scroll or manually click column headers to sort.

**Independent Test**: Create a new Product → Navigate to the Products List → The newly created product is the first item in the list.

**Acceptance Scenarios**:

1. **Given** a user navigates to any list screen (Products, Invoices, Customers, etc.), **When** the data loads, **Then** the list is automatically sorted with the newest records appearing at the top.
2. **Given** a user adds a new record, **When** the list refreshes, **Then** the new record appears at the top of the list.

---

### User Story 3 — Arabic ToolTips for UI Discoverability (Priority: P2)

A new employee is learning the system. When they hover their mouse over a button, icon, or input field, a helpful Arabic ToolTip appears explaining what the control does or what data is expected.

**Independent Test**: Hover over the "Save" button in any editor → A tooltip reading (for example) "حفظ البيانات المدخلة" appears → Hover over a required text field → A tooltip explaining the requirement appears.

**Acceptance Scenarios**:

1. **Given** the user hovers over an interactive button, **When** the tooltip delay elapses, **Then** a descriptive Arabic ToolTip appears explaining the action.
2. **Given** the user hovers over a data input field, **When** the tooltip delay elapses, **Then** an Arabic ToolTip appears providing guidance on the expected input (e.g., format, requirement).

---

### User Story 4 — Consistent and Safe Dialog Ownership (Priority: P2)

When an error or confirmation dialog appears, it is always centered over the active window. It never accidentally hides behind the main application, and it never causes a "self-ownership" application crash. The raw, unstyled Windows `MessageBox` is entirely eliminated from the system in favor of the branded, Arabic-first `IDialogService`.

**Independent Test**: Trigger a validation error in a non-modal editor window → The error dialog appears modal *only* to that specific editor window, centered perfectly over it → Trigger a global app error → The dialog appears over the main application window. Search the codebase for `MessageBox.Show` and find zero results.

**Acceptance Scenarios**:

1. **Given** an action triggers a dialog from within a child window, **When** the dialog opens, **Then** it correctly sets the child window as its owner and centers over it.
2. **Given** the system needs to display a message to the user, **When** the code is executed, **Then** it uses `IDialogService` exclusively, never falling back to `MessageBox.Show`.

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST provide a generic non-modal window host (`ScreenWindow`) capable of displaying any registered View/ViewModel pair.
- **FR-002**: The system MUST implement a `ScreenWindowService` to manage the lifecycle, opening, and tracking of these non-modal windows.
- **FR-003**: The `ScreenWindowService` MUST position newly opened non-modal windows using a cascading pattern (e.g., offset by 30 pixels down and right relative to the previously opened window).
- **FR-004**: The system MUST track open non-modal windows using `WeakReference<Window>` to ensure the window manager does not prevent garbage collection of closed windows.
- **FR-005**: All ViewModels that subscribe to the `EventBus` MUST actively unsubscribe in their `Dispose` method, and Views MUST call `Dispose` on their ViewModels when the View is unloaded or the window is closed to prevent memory leaks.
- **FR-006**: All data retrieval queries backing list screens (Products, Customers, Invoices, etc.) MUST apply a default descending sort order based on creation date or primary key ID, ensuring newest items appear first.
- **FR-007**: All interactive UI controls (buttons, inputs, comboboxes) across all screens MUST include descriptive `ToolTip` attributes written in Arabic.
- **FR-008**: The `IDialogService` implementation MUST include a robust window ownership resolution mechanism that correctly identifies the active window as the owner and prevents `InvalidOperationException` (self-ownership) errors.
- **FR-009**: The native `System.Windows.MessageBox.Show` API MUST NOT be used anywhere in the `DesktopPWF` project.

---

## Success Criteria *(mandatory)*

- **SC-001**: Users can open and interact with at least 5 different non-modal editor windows simultaneously without application UI blocking.
- **SC-002**: Closing a non-modal window results in its associated View and ViewModel being fully garbage collected (verifiable via memory profiler, proving no memory leaks exist).
- **SC-003**: 100% of data grids/lists display their items in newest-first order by default.
- **SC-004**: 100% of primary interactive buttons and input fields feature an Arabic ToolTip.
- **SC-005**: 0 instances of `MessageBox.Show` exist in the Desktop application codebase.
- **SC-006**: 0 crashes related to invalid window ownership occur when displaying dialogs.

---

## Assumptions

- The `EventBus` implementation provides an explicit `Unsubscribe` or `Dispose` mechanism for active subscriptions.
- The cascading window logic resets to the default origin if the offset pushes the new window off the visible bounds of the primary screen.
- ToolTip content will be managed inline in XAML files rather than extracted to a localized resource dictionary, matching the current Arabic-first approach of the project.
