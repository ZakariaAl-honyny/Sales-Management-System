# Feature Specification: Desktop Shell

**Feature Branch**: `004-desktop-shell`  
**Created**: 2026-05-08  
**Status**: Draft  
**Input**: User description: "Phase 4 — Desktop Shell: Implement EventBus, NavigationService, AuthApiService, MainForm with Sidebar, LoginForm, role-based sidebar visibility, Common UserControls, NotificationService, DialogService"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - User Login (Priority: P1) 🎯

A staff member opens the application and is presented with a login screen. They enter their username and password. If the credentials are valid, the system authenticates them, stores their session token, and transitions to the main application window. The sidebar menu items shown depend on the staff member's role (Admin, Manager, or Cashier).

**Why this priority**: Login is the entry gate to the entire application. Without it, no other desktop functionality is accessible. This is the fundamental prerequisite for every other user story.

**Independent Test**: Can be fully tested by launching the application, entering valid/invalid credentials, and verifying that correct authentication results in the main window appearing with appropriate sidebar items while invalid credentials display an error message.

**Acceptance Scenarios**:

1. **Given** a user is not logged in, **When** they launch the application, **Then** the login screen is displayed as the initial view.
2. **Given** a user enters valid credentials, **When** they click the login button, **Then** the system authenticates against the backend API, stores the token securely in memory, and navigates to the main application screen.
3. **Given** a user enters invalid credentials, **When** they click the login button, **Then** a clear Arabic error message is displayed (e.g., "اسم المستخدم أو كلمة المرور غير صحيحة") and the login form remains active.
4. **Given** a user is authenticated, **When** their session token expires, **Then** the system returns them to the login screen with a message indicating the session has ended.
5. **Given** a user is logged in, **When** they click the logout button, **Then** the session token is cleared from memory and the login screen is displayed.

---

### User Story 2 - Role-Based Navigation (Priority: P1) 🎯

After logging in, the user sees a permanent, always-expanded sidebar with both icons and text labels. The visible items are determined by the user's role. An Admin sees all menu items. A Manager sees everything except administrative functions (User Management, Warehouses, Settings). A Cashier sees only Sales, Sales Returns, and Customer viewing.

**Why this priority**: Role-based navigation enforces the application's security model at the UI level, ensuring users only access authorized functionality. This is tightly coupled with login and defines the core shell experience.

**Independent Test**: Can be tested by logging in with accounts of each role and verifying that the sidebar shows only the permitted menu items per the permissions matrix.

**Acceptance Scenarios**:

1. **Given** a user logged in as Admin, **When** the main screen loads, **Then** all sidebar menu items are visible: Dashboard, Products, Customers, Suppliers, Warehouses, Purchases, Sales, Returns, Transfers, Payments, Reports, Settings, User Management.
2. **Given** a user logged in as Manager, **When** the main screen loads, **Then** the sidebar shows all items except: Warehouses management, Settings, User Management.
3. **Given** a user logged in as Cashier, **When** the main screen loads, **Then** the sidebar shows only: Sales, Sales Returns, and Customers (view-only).
4. **Given** any user, **When** they click a sidebar menu item, **Then** the corresponding content screen loads in the main content area.

---

### User Story 3 - Screen Navigation (Priority: P1)

A user clicks different sidebar items to navigate between modules. Each click loads the corresponding screen in the main content area, replacing the previous screen. The currently selected sidebar item is visually highlighted. Navigation is smooth and responsive.

**Why this priority**: Navigation is the mechanism through which every business module is accessed. A broken or confusing navigation renders all downstream screens inaccessible.

**Independent Test**: Can be tested by clicking through all sidebar items and verifying each loads the correct placeholder screen, previous screens are properly disposed, and the active item is highlighted.

**Acceptance Scenarios**:

1. **Given** a user is on the main screen, **When** they click a sidebar item, **Then** the main content area displays the corresponding module's screen within 500 milliseconds.
2. **Given** a user is viewing a module screen, **When** they click a different sidebar item, **Then** the previous screen is disposed of properly and the new screen is loaded.
3. **Given** a user is navigating, **When** a screen is loaded, **Then** the corresponding sidebar item is visually highlighted to indicate the active section.
4. **Given** a user navigates away from a screen, **When** that screen had active subscriptions, **Then** all subscriptions are properly unsubscribed to prevent memory leaks.

---

### User Story 4 - Application Event Communication (Priority: P2)

When a user performs a data-changing action (such as creating a product), the system notifies all other open screens that may display related data. Those screens automatically refresh their data from the backend API. This ensures all views remain consistent without manual refresh.

**Why this priority**: The EventBus is the cross-cutting concern enabling real-time data consistency across all modules. Without it, screens would show stale data leading to user confusion and potential data entry errors.

**Independent Test**: Can be tested by having two screens open that display related data, performing a data change on one, and verifying the other refreshes automatically.

**Acceptance Scenarios**:

1. **Given** a screen subscribes to a data change event, **When** another screen publishes that event, **Then** the subscribing screen reloads its data from the API.
2. **Given** a screen is disposed, **When** an event is published, **Then** the disposed screen's handler is NOT invoked (no memory leaks or errors).
3. **Given** an event is published from a background operation, **When** the subscribing screen handles it, **Then** the UI update is marshaled to the UI thread safely.
4. **Given** multiple screens are subscribed to the same event, **When** the event is published, **Then** all subscribed screens receive the notification.

---

### User Story 5 - User Feedback and Notifications (Priority: P2)

After performing operations (saving, deleting, errors), the user receives clear visual feedback. Success operations show a brief success message (toast notification). Errors show a descriptive error message. Destructive operations (like delete) prompt the user with a confirmation dialog before proceeding.

**Why this priority**: User feedback provides essential usability — without it users cannot confirm whether operations succeeded or failed, leading to duplicate data entry or unnoticed errors.

**Independent Test**: Can be tested by triggering success, error, and confirmation scenarios and verifying the appropriate message type appears.

**Acceptance Scenarios**:

1. **Given** a user performs a successful operation, **When** the operation completes, **Then** a brief success notification appears (auto-dismissing after 3 seconds).
2. **Given** an operation fails, **When** the error is caught, **Then** a descriptive error message is displayed to the user in Arabic.
3. **Given** a user attempts a destructive action, **When** they click delete, **Then** a confirmation dialog appears asking "هل أنت متأكد؟" with confirm and cancel buttons.
4. **Given** a user cancels a confirmation dialog, **When** they click cancel, **Then** the destructive action is NOT performed.

---

### User Story 6 - Common Reusable Controls (Priority: P2)

The application provides a consistent set of reusable interface elements across all modules: a search bar for filtering lists, a loading indicator for async operations, summary cards for displaying key metrics, and a formatted money input field that enforces decimal precision. These ensure a uniform user experience.

**Why this priority**: Common controls eliminate visual inconsistency and reduce development effort for future module screens. They are building blocks required by all Phase 5 modules.

**Independent Test**: Can be tested by rendering each common control independently and verifying visual appearance and behavior.

**Acceptance Scenarios**:

1. **Given** a search bar is displayed, **When** the user types a search term, **Then** the search event fires after a brief debounce period (300ms).
2. **Given** an async operation is in progress, **When** the loading indicator is activated, **Then** a visual loading spinner is displayed in the content area.
3. **Given** a summary card is configured with a title, value, and optional icon, **When** rendered, **Then** it displays the data clearly in a compact card format.
4. **Given** a money input field, **When** the user enters a value, **Then** only valid decimal numbers are accepted and the display is formatted to two decimal places.

---

### Edge Cases

- What happens when the API server is unreachable during login? → Display a clear connection error message: "لا يمكن الاتصال بالخادم. تأكد من تشغيل الخدمة."
- What happens when the token expires mid-operation? → Redirect to the login screen with a session-expired message.
- What happens when the user rapidly clicks multiple sidebar items? → Only the last-clicked screen should load; intermediate navigations should be cancelled or ignored.
- What happens when the EventBus receives an event while the UI is still loading? → Events should be queued and processed after the UI is fully loaded.
- What happens when the user closes the application without logging out? → Token is discarded from memory (not persisted to disk), so next launch requires fresh login.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST display a login screen as the initial view on application launch.
- **FR-002**: System MUST authenticate users by sending credentials to the backend API and receiving a JWT token.
- **FR-003**: System MUST store the JWT token securely in memory for the duration of the session (not persisted to disk).
- **FR-004**: System MUST include the JWT token in all subsequent API calls as a Bearer authorization header.
- **FR-005**: System MUST display a main application window (starting windowed at 1280x800 by default) with a persistent sidebar after successful login.
- **FR-006**: System MUST show or hide sidebar menu items based on the authenticated user's role according to the Permissions Matrix.
- **FR-007**: System MUST load the appropriate module screen in the content area when a sidebar item is clicked.
- **FR-008**: System MUST properly dispose of the previous screen (including unsubscribing from events) when navigating to a new screen.
- **FR-009**: System MUST provide an EventBus that allows screens to publish and subscribe to data change messages.
- **FR-010**: System MUST ensure EventBus message handlers execute on the UI thread to prevent cross-thread exceptions.
- **FR-011**: System MUST provide a notification service for displaying success, error, and warning toast messages in the bottom-right corner of the application window.
- **FR-012**: System MUST provide a dialog service for confirmation prompts on destructive actions.
- **FR-013**: System MUST provide reusable common controls: SearchBar, Loading indicator, SummaryCard, and MoneyTextBox.
- **FR-014**: System MUST redirect the user to the login screen when the session token expires or becomes invalid.
- **FR-015**: System MUST display the logged-in user's name and role in the main window header or topbar area.
- **FR-016**: System MUST provide a logout function that clears the session and returns to the login screen.
- **FR-017**: System MUST display all user-facing text in Arabic.
- **FR-018**: System MUST handle API connection failures gracefully by displaying the raw error message returned by the API to the user.
- **FR-019**: EventBus messages MUST carry only entity identifiers — NOT data payloads (per AGENTS.md RULE-034).
- **FR-020**: EventBus subscribers MUST unsubscribe in their Dispose method (per AGENTS.md RULE-012).

### Key Entities

- **Session**: Represents the current user's authentication state, including their identity, role, and token.
- **Navigation Item**: A sidebar menu entry with a label, icon, target screen, and required role level.
- **Event Message**: A lightweight notification object carrying only an entity identifier and an event type (created, updated, deleted).
- **Notification**: A transient visual message with a type (success, error, warning), text content, and auto-dismiss duration.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can log in and reach the main screen in under 3 seconds from entering credentials.
- **SC-002**: Sidebar correctly shows permitted items for all 3 roles with 100% accuracy per the Permissions Matrix.
- **SC-003**: Screen-to-screen navigation completes in under 500 milliseconds.
- **SC-004**: Event-driven screen refresh occurs within 1 second of the triggering data change.
- **SC-005**: 100% of common controls render correctly and behave consistently across all modules.
- **SC-006**: Zero memory leaks from event subscriptions after navigating through all screens sequentially.
- **SC-007**: All user-facing messages are displayed in Arabic.
- **SC-008**: The application gracefully handles API unavailability without crashing, displaying the exact API error message to the user.

## Clarifications

### Session 2026-05-08
- Q: API Base URL Configuration → A: Config file (`appsettings.json`) — pre-configured by installer, editable by admin.
- Q: Notification Display Location → A: Bottom-right corner of the application window.
- Q: Error Message Level of Detail → A: Raw error only — Display the exact message returned by the API.
- Q: Sidebar Interaction Mode → A: Fixed/Always Expanded — Permanent sidebar with both icon and text labels.
- Q: Main Window Startup State → A: Windowed (1280x800) by default.

## Assumptions

- The backend API (Phases 1–3) is fully operational and accessible at a base URL configured in `appsettings.json` (e.g., `https://localhost:7001`).
- The JWT authentication endpoint (`/api/v1/auth/login`) is already implemented and tested.
- The user roles (Admin=1, Manager=2, Cashier=3) and the Permissions Matrix defined in AGENTS.md are the source of truth for sidebar visibility.
- The Desktop application targets Windows only and uses the existing WinForms project (`SalesSystem.Desktop`).
- The application connects to the API via HTTP; it does NOT access the database directly (per RULE-007).
- All text content is in Arabic to match the target audience (small retail shops in Arabic-speaking regions).
- The common controls built in this phase will serve as the foundation for all module screens in Phase 5.
- The Desktop project already has a reference to `SalesSystem.Contracts` for shared DTOs and request models.
