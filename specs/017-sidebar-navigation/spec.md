# Feature Specification: Collapsible Tree Sidebar Navigation (Phase 17)

**Feature Branch**: `017-sidebar-navigation`
**Created**: 2026-05-25
**Status**: Draft
**Input**: Phase 17 — Collapsible Tree Sidebar Navigation

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Organized and Scalable Navigation (Priority: P1)

As an active user of the system, I need a clean, structured, two-level navigation menu so that I can quickly find the module I want to access without being overwhelmed by a massive flat list of buttons.

**Independent Test**: Open the application main window. Observe that the sidebar displays only main module categories (e.g., Sales, Purchases, Finance, Reports, Settings) alongside intuitive icons. Click on "Sales & Distribution". The section expands smoothly to reveal the sub-screens (e.g., Quick POS, Drafts, Returns). Click it again to collapse.

**Acceptance Scenarios**:

1. **Given** the application is running, **When** the user looks at the right-side menu, **Then** they see a visually grouped, Right-To-Left (RTL) tree structure with clear parent modules.
2. **Given** a parent module is collapsed, **When** the user clicks on it, **Then** it expands instantly, displaying its respective sub-menu buttons.
3. **Given** a parent module is expanded, **When** the user clicks on the module header again, **Then** it collapses, hiding the sub-items and saving vertical screen space.

---

### User Story 2 — Dynamic Screen Switching (Priority: P1)

As a power user, I need to switch between different screens rapidly (e.g., from the POS screen to the Daily Journal) without the entire interface or sidebar reloading, causing lag or resetting my navigation state.

**Independent Test**: Expand the "Finance" group and click "Chart of Accounts". Ensure the Chart of Accounts screen loads into the main content area instantly. The sidebar remains visible and perfectly preserves its current expanded/collapsed state. Expand the "Reports" group and click a report. The main content area updates immediately to the new screen.

**Acceptance Scenarios**:

1. **Given** the user is viewing a specific module, **When** they click a sub-menu button in the sidebar, **Then** the central workspace immediately renders the target screen.
2. **Given** the user navigates between screens, **When** a new screen is loaded, **Then** the sidebar does NOT reload, and retains the exact expansion state the user left it in.
3. **Given** a sub-menu button is pressed, **Then** the system delegates the navigation logic entirely to the central ViewModel router, maintaining strict architectural separation.

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST replace the existing flat button sidebar with a hierarchical two-level collapsible menu.
- **FR-002**: The top level MUST represent Main Modules (e.g., Sales, Purchases, Finance) equipped with an icon and an expansion indicator.
- **FR-003**: The second level MUST represent concrete application screens/actions (e.g., POS, Add Supplier).
- **FR-004**: The sidebar UI MUST strictly enforce Right-To-Left (RTL) layout and Arabic text alignment.
- **FR-005**: The sidebar MUST be wrapped inside a scrollable container to ensure accessibility on small-resolution screens when multiple groups are expanded.
- **FR-006**: Sub-menu buttons MUST have a clean, modern aesthetic (borderless, transparent background) with slight internal padding (margin/indentation) to visually indicate they are child elements of the parent group.
- **FR-007**: The UI layer MUST NOT bind sub-menu button clicks to direct code-behind window-spawning events.
- **FR-008**: Navigation MUST be handled via a centralized `NavigationService` or `MainViewModel` utilizing an `ICommand` pattern.
- **FR-009**: The main application window MUST utilize a dynamic `ContentControl` region to host the active screen (ViewModel).

---

## Success Criteria *(mandatory)*

- **SC-001**: The navigation panel can comfortably support adding 50+ new screens in the future without cluttering the UI or requiring major redesigns.
- **SC-002**: Switching between any two active screens happens in under 100 milliseconds without visual flickering of the sidebar.
- **SC-003**: Code complexity in the main window view is reduced by strictly delegating navigation resolution to the ViewModel logic rather than event handlers.
- **SC-004**: The visual aesthetic aligns with modern web standards, eliminating legacy "Windows 95 style" heavy button borders in the navigation area.

---

## Assumptions

- The base architectural pattern strictly adheres to MVVM (Model-View-ViewModel) and supports dynamic ViewModel injection.
- The WPF components `Expander` and `ScrollViewer` provide sufficient built-in animation performance for smooth collapse/expand functionality.
