# Tasks: Desktop Shell

**Input**: specs/004-desktop-shell/ (plan.md, spec.md, data-model.md, contracts/ui-contracts.md, research.md, quickstart.md)
**Project**: SalesSystem/SalesSystem.Desktop/
**No tests requested** — implementation tasks only.

## Format: `[ID] [P?] [Story] Description`

---

## Phase 1: Setup

**Purpose**: Add Generic Host, appsettings.json, and create folder skeleton.

- [x] T001 Add `Microsoft.Extensions.Hosting` 10.x to `SalesSystem/SalesSystem.Desktop/SalesSystem.Desktop.csproj` via `dotnet add package`
- [x] T002 Create `SalesSystem/SalesSystem.Desktop/appsettings.json` with content `{ "ApiSettings": { "BaseUrl": "https://localhost:7001" } }` and set `<CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>` in the csproj
- [x] T003 [P] Create empty folder structure: `Configuration/`, `Models/`, `Messages/`, `Services/Interfaces/`, `Services/Http/`, `Forms/`, `Controls/Common/`, `Controls/Placeholders/` under `SalesSystem/SalesSystem.Desktop/`

---

## Phase 2: Foundational (Blocking — complete before any user story)

**Purpose**: All shared models, interfaces, DI composition root, and auth handler. Every user story depends on these.

- [x] T004 Create `SalesSystem/SalesSystem.Desktop/Configuration/ApiSettings.cs` — typed config class with `public string BaseUrl { get; set; } = "https://localhost:7001";`
- [x] T005 [P] Create `SalesSystem/SalesSystem.Desktop/Models/UserSession.cs` — sealed class with `UserId int`, `UserName string`, `FullName string`, `Role UserRole`, `Token string`; derived helpers `IsAdmin`, `IsManager` (see data-model.md §1.1)
- [x] T006 [P] Create `SalesSystem/SalesSystem.Desktop/Models/NavigationItem.cs` — sealed class with `Label string`, `IconKey string`, `ScreenType Type`, `MinRole UserRole`, method `IsVisible(UserRole)` (see data-model.md §1.2)
- [x] T007 [P] Create `SalesSystem/SalesSystem.Desktop/Models/Notification.cs` — enum `NotificationType { Success, Error, Warning }` and sealed class `Notification` with `Message`, `Type`, `Duration int = 3000` (see data-model.md §1.4)
- [x] T008 [P] Create `SalesSystem/SalesSystem.Desktop/Messages/Messages.cs` — abstract record `EntityChangedMessage(int EntityId)` and 7 concrete message records: `ProductChangedMessage`, `CustomerChangedMessage`, `SupplierChangedMessage`, `SalesInvoiceChangedMessage`, `PurchaseInvoiceChangedMessage`, `StockChangedMessage`, `SessionExpiredMessage` (see data-model.md §1.3)
- [x] T009 [P] Create `SalesSystem/SalesSystem.Desktop/Services/Interfaces/ISessionService.cs` — interface with `Current UserSession?`, `IsAuthenticated bool`, `SignIn(UserSession)`, `SignOut()` (see contracts/ui-contracts.md)
- [x] T010 [P] Create `SalesSystem/SalesSystem.Desktop/Services/Interfaces/IAuthApiService.cs` — interface with `LoginAsync(string, string, CancellationToken) Task<Result<UserSession>>` (see contracts/ui-contracts.md)
- [x] T011 [P] Create `SalesSystem/SalesSystem.Desktop/Services/Interfaces/INavigationService.cs` — interface with `SetContentPanel(Panel)`, `NavigateTo<TControl>() where TControl : UserControl`, `CurrentScreen Type?` (see contracts/ui-contracts.md)
- [x] T012 [P] Create `SalesSystem/SalesSystem.Desktop/Services/Interfaces/IEventBus.cs` — interface with `Subscribe<TMessage>(Action<TMessage>) IDisposable` and `Publish<TMessage>(TMessage)` (see contracts/ui-contracts.md)
- [x] T013 [P] Create `SalesSystem/SalesSystem.Desktop/Services/Interfaces/INotificationService.cs` — interface with `ShowSuccess`, `ShowError`, `ShowWarning` (string message each) (see contracts/ui-contracts.md)
- [x] T014 [P] Create `SalesSystem/SalesSystem.Desktop/Services/Interfaces/IDialogService.cs` — interface with `Confirm(string message, string title = "تأكيد") bool` (see contracts/ui-contracts.md)
- [x] T015 Create `SalesSystem/SalesSystem.Desktop/Controls/BaseModuleControl.cs` — abstract `UserControl` subclass with `List<IDisposable> _subscriptions`, abstract `RegisterSubscriptions()`, `AddSubscription(IDisposable)`, calls `RegisterSubscriptions()` in `OnLoad`, disposes all in `Dispose(bool)` (see contracts/ui-contracts.md §BaseModuleControl)
- [x] T016 Create `SalesSystem/SalesSystem.Desktop/Services/Http/AuthTokenHandler.cs` — `DelegatingHandler` subclass; injects `ISessionService` and `IEventBus`; in `SendAsync` attaches `Authorization: Bearer {token}` header; if response is 401, calls `sessionService.SignOut()` and publishes `new SessionExpiredMessage()`, then returns the response
- [x] T017 Rewrite `SalesSystem/SalesSystem.Desktop/Program.cs` — use `Host.CreateDefaultBuilder()` with `ConfigureServices`: bind `ApiSettings` from config, register `IHttpClientFactory` via `AddHttpClient<IAuthApiService, AuthApiService>` with `BaseAddress` from `ApiSettings`, add `AuthTokenHandler` as `AddHttpMessageHandler`, register all services as Singleton (`IEventBus→EventBus`, `ISessionService→SessionService`, `INavigationService→NavigationService`, `INotificationService→NotificationService`, `IDialogService→DialogService`), register `LoginForm` and `MainForm` as `Transient`; then `Application.Run(host.Services.GetRequiredService<LoginForm>())`

**Checkpoint**: All interfaces, models, messages, DI wiring complete — user stories can begin.

---

## Phase 3: US1 — User Login (P1) 🎯 MVP

**Goal**: Authentication flow: LoginForm → API → session → MainForm.
**Independent Test**: Launch app, enter valid/invalid credentials, verify transition or error message.

- [x] T018 [US1] Implement `SalesSystem/SalesSystem.Desktop/Services/SessionService.cs` — `ISessionService` implementation; stores `UserSession?` in private field; `SignIn` sets field; `SignOut` sets field to null; thread-safe with `lock`
- [x] T019 [US1] Implement `SalesSystem/SalesSystem.Desktop/Services/AuthApiService.cs` — `IAuthApiService` implementation; inject `HttpClient`; `LoginAsync` posts `LoginRequest { UserName, Password }` as JSON to `/api/v1/auth/login`; on 200 deserialize response to `UserSession`; on non-200 return `Result<UserSession>.Failure(rawErrorMessage)`; on `HttpRequestException` return `Result.Failure("لا يمكن الاتصال بالخادم. تأكد من تشغيل الخدمة.")`
- [x] T020 [US1] Create `SalesSystem/SalesSystem.Desktop/Forms/LoginForm.cs` and `LoginForm.Designer.cs` — WinForms `Form`, Size=480x320, StartPosition=CenterScreen, FormBorderStyle=FixedDialog, MaximizeBox=false; contains: `lblTitle` (Arabic store name), `txtUserName`, `txtPassword` (UseSystemPasswordChar=true), `btnLogin`, `lblError` (red, hidden by default); wire `btnLogin.Click` to `LoginAsync()`
- [x] T021 [US1] Implement `LoginForm.LoginAsync()` — disable `btnLogin`, show loading text; call `_authApiService.LoginAsync(txtUserName.Text, txtPassword.Text)`; on success: `_sessionService.SignIn(session)`, create and show `MainForm` via DI (`_serviceProvider.GetRequiredService<MainForm>()`), then `this.Hide()`; on failure: show `result.Error` in `lblError`; re-enable `btnLogin` in `finally`

**Checkpoint**: Login flow fully functional — valid creds → MainForm, invalid → Arabic error.

---

## Phase 4: US2 — Role-Based Navigation (P1) 🎯

**Goal**: Sidebar shows correct items per role (Admin=13, Manager=10, Cashier=3).
**Independent Test**: Log in as each role, verify sidebar item count and labels match permissions matrix.

- [x] T022 [US2] Create all 13 placeholder `UserControl` files in `SalesSystem/SalesSystem.Desktop/Controls/Placeholders/`: `DashboardControl`, `ProductsControl`, `CustomersControl`, `SuppliersControl`, `WarehousesControl`, `PurchasesControl`, `SalesControl`, `ReturnsControl`, `TransfersControl`, `PaymentsControl`, `ReportsControl`, `SettingsControl`, `UsersControl` — each extends `BaseModuleControl`, displays an Arabic label (e.g. `لوحة التحكم — قريباً`) centered, overrides `RegisterSubscriptions()` as no-op
- [x] T023 [US2] Create `SalesSystem/SalesSystem.Desktop/Forms/MainForm.cs` and `MainForm.Designer.cs` — `Form`, Size=1280x800, StartPosition=CenterScreen; layout: left `pnlSidebar` (Width=220, Dock=Left, BackColor=dark), right `pnlContent` (Dock=Fill); top strip `pnlTopBar` (Height=48, Dock=Top) containing `lblUserName`, `btnLogout`
- [x] T024 [US2] Implement `MainForm.BuildSidebar()` — define the 13 `NavigationItem` entries with correct `ScreenType` and `MinRole` per permissions matrix (AGENTS.md §6); filter by `item.IsVisible(session.Role)`; for each visible item create a `Button` styled as a nav item (FlatStyle, full width, Arabic text + icon key); add to `FlowLayoutPanel` in `pnlSidebar`
- [x] T025 [US2] Implement `MainForm.OnLoad` — call `_sessionService.Current` to populate `lblUserName` with `FullName (Role)`; call `BuildSidebar()`; subscribe to `SessionExpiredMessage` via `_eventBus` to call `HandleSessionExpired()`; call `_navigationService.SetContentPanel(pnlContent)`; navigate to Dashboard by default

**Checkpoint**: Correct sidebar item count and labels per role verified.

---

## Phase 5: US3 — Screen Navigation (P1)

**Goal**: Clicking sidebar items swaps screens in content panel, disposes previous, highlights active.
**Independent Test**: Click all sidebar items sequentially, verify each loads, previous is disposed, active item highlighted.

- [x] T026 [US3] Implement `SalesSystem/SalesSystem.Desktop/Services/NavigationService.cs` — `INavigationService` implementation; holds `Panel? _panel`, `UserControl? _current`, `Type? _currentScreen`; `SetContentPanel` stores panel ref; `NavigateTo<TControl>`: dispose `_current`, clear `_panel.Controls`, resolve `new TControl` via `IServiceProvider`, set `Dock=Fill`, add to `_panel.Controls`, set `_currentScreen`; all on UI thread
- [x] T027 [US3] Register all 13 placeholder `UserControl` types as `Transient` in `Program.cs` DI registration block (T017 update)
- [x] T028 [US3] Wire sidebar button `Click` handlers in `MainForm` to call `_navigationService.NavigateTo<TScreenType>()` (use generic method dispatch via a dictionary `Type→Action`)
- [x] T029 [US3] Implement active sidebar item highlight in `MainForm` — on each navigation, reset all sidebar buttons to default `BackColor`; set clicked button `BackColor` to accent color; store reference to active button

**Checkpoint**: Full navigation cycle working, breakpoint in `BaseModuleControl.Dispose` confirms cleanup.

---

## Phase 6: US4 — Application Event Communication (P2)

**Goal**: In-process pub/sub; UI thread marshaling; auto-cleanup on dispose.
**Independent Test**: Publish a message, verify subscriber receives it on UI thread; dispose subscriber, publish again, verify no invocation.

- [x] T030 [US4] Implement `SalesSystem/SalesSystem.Desktop/Services/EventBus.cs` — `IEventBus` implementation; use `ConcurrentDictionary<Type, List<WeakReference<Delegate>>>` for handler registry; `Subscribe<TMessage>`: add `WeakReference<Action<TMessage>>` entry, return `Subscription` disposable token that removes the entry; `Publish<TMessage>`: iterate live references, marshal to UI thread via `Application.OpenForms` first form `Invoke` if `InvokeRequired`, invoke handler, remove dead references; thread-safe with `lock` on the list
- [x] T031 [US4] Update `MainForm.Dispose(bool)` in `MainForm.cs` — ensure `_eventBusSubscription?.Dispose()` is called for the `SessionExpiredMessage` subscription registered in `OnLoad` (T025)
- [x] T032 [US4] Implement `MainForm.HandleSessionExpired()` — on `SessionExpiredMessage`: call `_sessionService.SignOut()`, create new `LoginForm` via DI, show it, close `MainForm`

**Checkpoint**: Event published from any control propagates to subscribers; disposed controls do not receive events.

---

## Phase 7: US5 — User Feedback and Notifications (P2)

**Goal**: Toast notifications bottom-right auto-dismiss 3s; confirmation dialogs in Arabic.
**Independent Test**: Call `ShowSuccess/ShowError/ShowWarning`, verify toast appears bottom-right and auto-dismisses; call `Confirm`, verify dialog shows Arabic text and returns correct bool.

- [x] T033 [US5] Create `SalesSystem/SalesSystem.Desktop/Forms/ToastForm.cs` — frameless `Form` subclass; `FormBorderStyle=None`, `ShowInTaskbar=false`, `TopMost=true`; override `ShowWithoutActivation => true`; override `CreateParams` to add `WS_EX_NOACTIVATE` extended style; constructor takes `Notification`; sets `BackColor` by `NotificationType` (Success=#2ECC71, Error=#E74C3C, Warning=#E67E22); contains Arabic `lblMessage`; uses `System.Windows.Forms.Timer` (Interval=Duration) to call `this.Close()` on tick; `Show(IWin32Window owner)` calculates bottom-right position from `Screen.FromControl(owner).WorkingArea`
- [x] T034 [US5] Implement `SalesSystem/SalesSystem.Desktop/Services/NotificationService.cs` — `INotificationService` implementation; inject `IServiceProvider`; `ShowSuccess/ShowError/ShowWarning` create `ToastForm` with appropriate `Notification` and call `.Show(Application.OpenForms[0])`; marshal to UI thread if needed
- [x] T035 [US5] Implement `SalesSystem/SalesSystem.Desktop/Services/DialogService.cs` — `IDialogService` implementation; `Confirm` shows `MessageBox.Show(message, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2)` RTL-aware; returns `true` if result is `DialogResult.Yes`

**Checkpoint**: Toast appears bottom-right, correct color, auto-dismisses in 3s. Dialog returns correct bool.

---

## Phase 8: US6 — Common Reusable Controls (P2)

**Goal**: 4 reusable controls: SearchBar (debounce 300ms), LoadingOverlay, SummaryCard, MoneyTextBox.
**Independent Test**: Render each control on a test form, verify behavior independently.

- [x] T036 [P] [US6] Create `SalesSystem/SalesSystem.Desktop/Controls/Common/SearchBarControl.cs` — `UserControl` with `TextBox txtSearch` and `Label lblIcon`; public `event EventHandler<string>? SearchChanged`; public `string Placeholder { get; set; }` (sets placeholder via GotFocus/LostFocus); `System.Windows.Forms.Timer _debounce` (Interval=300, one-shot); on `txtSearch.TextChanged` restart timer; on timer `Tick` fire `SearchChanged`; public `string SearchText => txtSearch.Text`
- [x] T037 [P] [US6] Create `SalesSystem/SalesSystem.Desktop/Controls/Common/LoadingOverlayControl.cs` — `UserControl`, `Dock=Fill`, semi-transparent panel effect via `OnPaint` override drawing `Color.FromArgb(180, Color.White)` rectangle; contains centered `Label lblLoading` with Arabic text default "جاري التحميل..."; public `void Show()` and `void Hide()` (sets `Visible`); public `string LoadingText { get; set; }`
- [x] T038 [P] [US6] Create `SalesSystem/SalesSystem.Desktop/Controls/Common/SummaryCardControl.cs` — `UserControl`; contains `Label lblTitle`, `Label lblValue`, `PictureBox picIcon`; public properties `Title`, `Value` (string), `Icon` (Image?), `AccentColor` (Color) — `AccentColor` draws a 4px left border via `OnPaint`; fixed size 160×80
- [x] T039 [P] [US6] Create `SalesSystem/SalesSystem.Desktop/Controls/Common/MoneyTextBox.cs` — `TextBox` subclass; override `OnKeyPress` to allow only digits, single `.`, and backspace (`e.Handled = true` for others); override `OnLeave` to format `Text` to 2 decimal places (`decimal.TryParse` → `ToString("F2")`); public property `decimal DecimalValue => decimal.TryParse(Text, out var v) ? v : 0m`

**Checkpoint**: All 4 controls render and behave per spec acceptance scenarios.

---

## Phase 9: Polish & Cross-Cutting Concerns

- [ ] T040 Update `SalesSystem/SalesSystem.Desktop/Forms/LoginForm.Designer.cs` — set `RightToLeft=Yes`, `RightToLeftLayout=true` for full RTL Arabic layout
- [ ] T041 Update `SalesSystem/SalesSystem.Desktop/Forms/MainForm.Designer.cs` — set `RightToLeft=Yes`, `RightToLeftLayout=true`; sidebar docked `Right` in RTL mode
- [ ] T042 Run `dotnet build SalesSystem/SalesSystem.Desktop/SalesSystem.Desktop.csproj` and resolve all compiler errors
- [ ] T043 Execute the verification checklist in `specs/004-desktop-shell/quickstart.md` — manually test all 12 scenarios; document any failures as GitHub issues

---

## Dependencies & Execution Order

- **Phase 1 (Setup)**: Start immediately — T001→T002→T003 sequential
- **Phase 2 (Foundational)**: After Phase 1 — T004-T016 mostly parallel, T017 last (needs all interfaces)
- **Phase 3 (US1)**: After Phase 2 complete — T018→T019→T020→T021 sequential
- **Phase 4 (US2)**: After Phase 2 complete — T022→T023→T024→T025 sequential
- **Phase 5 (US3)**: After Phase 4 complete — T026→T027→T028→T029 sequential
- **Phase 6 (US4)**: After Phase 2 complete — T030→T031→T032 sequential
- **Phase 7 (US5)**: After Phase 2 complete — T033→T034→T035 sequential
- **Phase 8 (US6)**: After Phase 2 complete — T036-T039 all parallel
- **Phase 9 (Polish)**: After all phases complete

### Parallel Opportunities

```
Phase 2 complete
       │
       ├──→ US1 (T018-T021)     Login flow
       ├──→ US2+US3 (T022-T029) Sidebar + Navigation
       ├──→ US4 (T030-T032)     EventBus
       ├──→ US5 (T033-T035)     Notifications
       └──→ US6 (T036-T039)     Common Controls (all 4 fully parallel)
```

---

## Implementation Strategy

### MVP First (US1 + US2 + US3)

1. Phase 1: Setup
2. Phase 2: Foundational (CRITICAL — blocks all)
3. Phase 3: US1 Login → **validate login works**
4. Phase 4: US2 Role-Based Sidebar → **validate sidebar per role**
5. Phase 5: US3 Navigation → **validate screen swap + dispose**
6. **STOP — running shell is complete. Demo-ready.**

### Full Delivery

6. Phase 6: EventBus
7. Phase 7: Notifications & Dialogs
8. Phase 8: Common Controls
9. Phase 9: Polish + Verification checklist

---

## Summary

| Phase | Stories | Tasks | Parallel |
|-------|---------|-------|----------|
| Phase 1: Setup | — | T001-T003 | 1 |
| Phase 2: Foundational | — | T004-T017 | 11 |
| Phase 3: US1 Login | US1 | T018-T021 | 0 |
| Phase 4: US2 Sidebar | US2 | T022-T025 | 0 |
| Phase 5: US3 Navigation | US3 | T026-T029 | 0 |
| Phase 6: US4 EventBus | US4 | T030-T032 | 0 |
| Phase 7: US5 Feedback | US5 | T033-T035 | 0 |
| Phase 8: US6 Controls | US6 | T036-T039 | 4 |
| Phase 9: Polish | — | T040-T043 | 1 |
| **Total** | **6 stories** | **43 tasks** | **17 parallel** |

**MVP scope**: T001–T029 (29 tasks) = running authenticated shell with role-based navigation.
