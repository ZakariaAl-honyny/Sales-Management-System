I want to create a special agent whose content is in English:
Yes,
**I recommend designing the interfaces fully first in terms of look and flow**, but **not in the sense of finalizing everything 100% before the logic and API**.

## The best approach for your project is:
### **UI First / Logic Later**
But in a smart way:

- Draw the final system look
- Define all screens
- Define navigation between them
- Define the look of tables, models, and buttons
- Then start with the programming logic and API
- After that, link the interface with the API

---

# Why is this suitable for you?
Because you want:
- **Desktop**
- **API**
- **SQL Server**
- And future scalability

If you start with logic directly without a clear visualization of the interface:
- You will be forced to modify screens frequently
- You may change the API more than once
- You might get tired of reorganizing the project

However, if you define the interface first:
- You will know what data is required
- You will know what the DTOs are
- You will know what operations the API must provide
- You will build the logic in a way that actually serves the interface

---

# But beware: Don't build the final interface too rigidly from day one
This means don't get preoccupied with:
- Exact colors
- Final icons
- Very detailed aesthetic arrangement

The best way is:
## 1) **Wireframes**
Initial diagrams of the screens

## 2) **UI Flow**
How the user moves between screens

## 3) **Design System**
A unified look for buttons, tables, and forms

## 4) **Mock UI**
Interfaces that work with fake (mock) data

Then after that:
## 5) **API Integration**
The real connection

---

# What should you define from the beginning?
You need to settle these things before the code:

## 1) System Structure
- Sidebar
- Topbar
- Content Area

## 2) Main Screens
- Login
- Dashboard
- Products
- Customers
- Suppliers
- Warehouses
- Purchases
- Sales
- Returns
- Transfers
- Reports
- Settings

## 3) The look of each screen
For example:
- CRUD Screen = Table + Search + Buttons
- Invoice Screen = Header + Details + Total + Save
- Transfer Screen = Source Warehouse + Destination Warehouse + Items

## 4) Common Components
- Product selection window
- Customer selection window
- Supplier selection window
- Warehouse selection window
- Confirmation window
- Error and success messages

---

# Should the interface be completed before the API?
## Yes, but in a "preliminary" way
Meaning:
- Build the screens
- Put in experimental data
- Prepare the final display look
- But without a real connection to the database at first

This is very excellent because it allows you to:
- Ensure the design is comfortable
- Know if some screens need rearranging
- Accurately determine the required API operations

---

# What is the best implementation method?
## I recommend this sequence:

### Phase 1: Interface Design
- Draw all screens
- Define navigation
- Define common components

### Phase 2: Building UI Mock
- Interfaces working with mock data
- Without a database
- Without an API

### Phase 3: Fixing API Contracts
- What are the endpoints
- What are the DTOs
- What are the required operations

### Phase 4: Logic Implementation
- Application
- Infrastructure
- SQL Server
- API

### Phase 5: Real Integration
- The interface connects to the API
- Then full testing

---

# Why is this better than starting with logic directly?
Because if you start with the API first:
- You may build endpoints that don't suit the interface
- Or build incomplete DTOs
- Or need many changes

But if you start with the interface:
- The interface will dictate what it actually needs
- Consequently, the API will be more accurate
- And the logic will be clearer

---

# But there is a very important point
## Don't delay logic too much
Meaning don't spend too much time on design only, as this might turn into:
- "Design without implementation"

The best way:
- Design the basic interface
- Then immediately start implementing the **first full path**
  Such as:
  - Adding a product
  - Then displaying it
  - Then saving it
  - Then editing it

---

# Best rule for your project
## Design the interface fully at the general look level
But
## Implement the logic gradually screen by screen

Meaning:
- Don't wait for all programming details to be completed
- Don't wait for all aesthetics to be completed
- Start with a balanced mix

---

# Since you want a "standard" design
I recommend the **Admin Dashboard Standard** style:
- Left Sidebar
- Top Topbar
- Content in the middle
- Tables for lists
- Cards in the main dashboard
- Dialogs for forms

This design is:
- Well-known
- Easy
- Professional
- Very suitable for a store
- Easy to convert to Web later

---

# Summary
## Do I recommend designing the interfaces fully before integration?
**Yes, I recommend designing the structure, general look, and UX first.**

## Do I recommend finalizing everything completely before the logic?
**No, not completely.**

## The best way:
- Design the interfaces
- Define the final system look
- Use mock data
- Then build the logic and API
- Then link them

---

# My strongest advice to you
Since you are relying on an AI agent:
Ask it to work in this format:

> "Design for me a Sales System interface structure in a professional Admin Dashboard style,
> using **WPF + MVVM** (.NET 10). I want Views, ViewModels, and navigation structure
> with mock data, without connecting to the database now. The project is `SalesSystem.DesktopPWF`.
> The goal is to fix the final system look before implementing the API and logic."

---

If you want, I can prepare for you in the next message:
## **A full UX/UI design plan for the system**
Organized to include:
- Screen names
- Their order
- The look of each screen
- What appears in them
- And what will be implemented first, second, and third.

---

# Proposed Technical Structure
Here is the **Proposed Technical Structure** for your project in a practical way, suitable for:

- **Desktop + API**
- **SQL Server**
- **Reusable UserControls**
- **Publisher-Subscriber**
- **Continuous Development**

---

# 1) Proposed Solution Structure

I recommend this division:

## Projects
- `SalesSystem.Contracts`
- `SalesSystem.Domain`
- `SalesSystem.Application`
- `SalesSystem.Infrastructure`
- `SalesSystem.Api`
- `SalesSystem.DesktopPWF`  ← **WPF + MVVM** (NOT WinForms)

---

## Why did I add `Contracts`?
Because the Desktop and API need the same:
- DTOs
- Requests
- Responses
- Basic messages sometimes

This prevents duplication.

---

# 2) Responsibility of each project

## 2.1 `SalesSystem.Contracts`
Contains:
- DTOs
- Request Models
- Response Models
- Common Result Models

### Examples:
- `ProductDto`
- `CreateProductRequest`
- `UpdateProductRequest`
- `ApiResponse<T>`

---

## 2.2 `SalesSystem.Domain`
Contains:
- Entities
- Value Objects
- Basic Business Rules

### Examples:
- `Product`
- `Customer`
- `Supplier`
- `Warehouse`
- `SaleInvoice`
- `PurchaseInvoice`

---

## 2.3 `SalesSystem.Application`
Contains:
- Services
- Interfaces
- Use Cases
- Business Logic

### Examples:
- `IProductService`
- `ISaleService`
- `IPurchaseService`
- `IWarehouseService`

---

## 2.4 `SalesSystem.Infrastructure`
Contains:
- EF Core
- DbContext
- Repositories
- SQL Server Implementation

---

## 2.5 `SalesSystem.Api`
Contains:
- Controllers
- API Endpoints
- Authentication later
- Linking Application with Infrastructure

---

## 2.6 `SalesSystem.DesktopPWF` (WPF + MVVM)
Contains:
- WPF Views (`.xaml` — UI only, zero logic)
- ViewModels (binding logic, `INotifyPropertyChanged`)
- Services/Api (HttpClient wrappers — NEVER direct DB)
- Services/App (EventBus, NavigationService, SessionService, DialogService, SoundService)
- Messaging/Messages (EventBus message types — ID only, no data payload)
- Converters (WPF `IValueConverter` implementations)
- Helpers (ThemeHelper, UI utilities)
- Resources (styles, brushes, icons, themes)

---

# 3) Internal structure of the Desktop project

> **IMPORTANT:** The Desktop is `SalesSystem.DesktopPWF` — WPF with MVVM pattern.
> All UI files are `.xaml`. There are NO WinForms `.cs` Form classes.

## Inside `SalesSystem.DesktopPWF`

### A) Root Level
- `MainWindow.xaml` — Shell: sidebar + content host
- `LoginWindow.xaml` — Login screen
- `App.xaml` / `App.xaml.cs` — Entry point + DI container

### B) `Views/` — XAML screens (no code-behind logic)
| Folder | Contents |
|--------|----------|
| `Views/Common/` | Shared dialogs, selection windows, confirmation dialogs |
| `Views/Dashboard/` | Main dashboard with KPI cards |
| `Views/Products/` | Product list + editor |
| `Views/Categories/` | Category list + editor |
| `Views/Units/` | Measurement units list + editor |
| `Views/Customers/` | Customer list + editor |
| `Views/Suppliers/` | Supplier list + editor |
| `Views/Warehouses/` | Warehouse list + editor |
| `Views/Sales/` | Sales invoice list + editor |
| `Views/Purchases/` | Purchase invoice list + editor |
| `Views/Returns/` | Sales return + Purchase return editors |
| `Views/Inventory/` | Stock management / adjustments |
| `Views/Transfers/` | Stock transfer list + editor |
| `Views/Payments/` | Customer + Supplier payments |
| `Views/Invoices/` | Invoice selection dialogs |
| `Views/Reports/` | Reports + **LowStockView.xaml** [NEW SPEC-009] |
| `Views/Settings/` | Store settings screen |
| `Views/Users/` | User management screen |
| `Views/Login/` | Additional login-related views |

### C) `ViewModels/` — MVVM binding logic
- `ViewModelBase.cs` — base: `INotifyPropertyChanged`, `AsyncRelayCommand`
- `DashboardViewModel.cs`
- `LoginWindowViewModel.cs`
- `ReportsViewModel.cs`
- `SettingsViewModel.cs`
- `WarehouseListViewModel.cs` / `WarehouseEditorViewModel.cs`
- Subfolders mirror `Views/` structure

### D) `Services/App/` — Application-level singletons
- `EventBus.cs` — Pub/Sub (subscribe in `OnLoad`, unsubscribe in `Dispose`)
- `NavigationService.cs` — load Views into `MainWindow` content area
- `SessionService.cs` — JWT token (in-memory ONLY, never to disk)
- `DialogService.cs` — open editor/confirm windows
- `ISoundService.cs` / `SoundService.cs` — audible feedback
- `IPrinterService.cs` — print contract [SPEC-006]

### E) `Services/Api/` — HttpClient API wrappers
- `IApiService.cs` — all interface definitions in one file
- One `XxxApiService.cs` per domain entity
- **Rule:** Desktop NEVER connects to the database — only via API

### F) `Messaging/Messages/`
- One message class per event (e.g., `ProductChangedMessage`)
- **Rule (RULE-034):** Messages carry entity ID ONLY — no data payloads

### G) Supporting folders
- `Converters/` — WPF `IValueConverter` (e.g., bool-to-visibility)
- `Helpers/` — ThemeHelper, UI utilities
- `Models/` — local display/view models
- `Resources/` — styles, brushes, icons, themes

---

# 4) Services inside the Desktop

## `Services/Api`
Here you place the clients that connect to the API.

### Examples:
- `IProductApiService`
- `ProductApiService`
- `ICustomerApiService`
- `CustomerApiService`
- `ISupplierApiService`
- `SupplierApiService`

### Important:
The Desktop **does not deal with the database directly**.
It only deals with the API.

---

## `Services/App/` — App-level services
- `EventBus.cs` — pub/sub event bus
- `NavigationService.cs` — navigate between Views in `MainWindow`
- `SessionService.cs` — JWT token (in-memory only)
- `DialogService.cs` — open editor/confirm windows
- `ISoundService.cs` / `SoundService.cs` — audible feedback
- `IPrinterService.cs` — print contract

Its function:
- Navigate between WPF Views
- Manage application session
- Provide dialog and notification services

---

# 5) Messaging / Publisher-Subscriber

This part is very important with UserControls.

## `Messaging`
- `IEventBus`
- `EventBus`
- `Messages/`

### Examples of Messages:
- `ProductChangedMessage`
- `CustomerChangedMessage`
- `SupplierChangedMessage`
- `WarehouseChangedMessage`
- `SaleCreatedMessage`
- `PurchaseCreatedMessage`
- `StockChangedMessage`

---

# 6) How does Publisher-Subscriber work for you?

## Practical Example:
### When saving a product:
- `ProductEditorControl` executes saving via API
- If saving is successful:
  - It publishes `ProductChangedMessage`

### Any common part:
- `ProductsListControl`
- `SalesEditorControl`
- `StockControl`

Upon receiving the message:
- Executes `Reload` from the API

---

# 7) The Golden Rule
## Don't usually send the data itself inside the message
Send only:
- The change event

Such as:
- "Product modified"
- "Sale invoice created"
- "Stock changed"

Then each Control reloads the data from the API.

This is better than passing old data between screens.

---

# 8) How is the data flow?

## Correct Flow:
**UserControl**
→ Calls **ApiService**
→ ApiService connects to **API**
→ API deals with **Application**
→ Then **Infrastructure**
→ Then **SQL Server**

### After saving:
**EventBus**
→ Sends `ProductChangedMessage`
→ All interested Controls execute Refresh

---

# 9) How do you build reusable UserControls?

## Best division:
### 1. View Controls
- `ProductsListControl`
- `CustomersListControl`

### 2. Edit Controls
- `ProductEditorControl`
- `CustomerEditorControl`

### 3. Small common Controls
- `SearchBarControl`
- `PaymentPanelControl`
- `SummaryCardControl`
- `LoadingControl`

---

# 10) Best Pattern for Continuous Development
I recommend this pattern:

## For each Module
For example `Products`:
- List Control
- Editor Control
- DTOs
- API Service
- Message

### Repeat the same idea with:
- Customers
- Suppliers
- Warehouses
- Purchases
- Sales
- Returns

This makes development very fast and organized.

---

# 11) Practical folder structure example

```text
SalesSystem.Desktop
│
├── Forms
│   ├── MainForm.cs
│   ├── LoginForm.cs
│   ├── SettingsForm.cs
│   └── Dialogs
│       ├── ConfirmDialog.cs
│       └── MessageDialog.cs
│
├── Controls
│   ├── Common
│   │  ├── SearchBarControl.cs
│   │  ├── ToolbarControl.cs
│   │  ├── LoadingControl.cs
│   │  ├── SummaryCardControl.cs
│   │  ├── MoneyTextBoxControl.cs
│   │  └── DateRangePickerControl.cs
│   │
│   ├── Products
│   │  ├── ProductsListControl.cs
│   │  ├── ProductEditorControl.cs
│   │  └── ProductDetailsControl.cs
│   │
│   ├── Customers
│   │  ├── CustomersListControl.cs
│   │  ├── CustomerEditorControl.cs
│   │  └── CustomerBalanceControl.cs
│   │
│   ├── Suppliers
│   │  ├── SuppliersListControl.cs
│   │  ├── SupplierEditorControl.cs
│   │  └── SupplierBalanceControl.cs
│   │
│   ├── Warehouses
│   │  ├── WarehousesListControl.cs
│   │  ├── WarehouseEditorControl.cs
│   │  └── WarehouseStockControl.cs
│   │
│   ├── Sales
│   │  ├── SaleEditorControl.cs
│   │  ├── SaleItemsControl.cs
│   │  └── PaymentPanelControl.cs
│   │
│   └── Purchases
│       ├── PurchaseEditorControl.cs
│       ├── PurchaseItemsControl.cs
│       └── PurchaseSummaryControl.cs
│
├── Services
│   ├── Api
│   │  ├── IProductApiService.cs
│   │  ├── ProductApiService.cs
│   │  ├── ICustomerApiService.cs
│   │  └── CustomerApiService.cs
│   │
│   ├── Navigation
│   │  ├── INavigationService.cs
│   │  └── NavigationService.cs
│   │
│   ├── Dialogs
│   │  ├── IDialogService.cs
│   │  └── DialogService.cs
│   │
│   └── Notifications
│       ├── INotificationService.cs
│       └── NotificationService.cs
│
├── Messaging
│   ├── IEventBus.cs
│   ├── EventBus.cs
│   └── Messages
│       ├── ProductChangedMessage.cs
│       ├── CustomerChangedMessage.cs
│       ├── SupplierChangedMessage.cs
│       └── StockChangedMessage.cs
│
└── AppHost
    ├── DependencyInjection.cs
    └── AppSettings.cs
```

---

# 12) A very simple example of the EventBus

## Interface:
```csharp
public interface IEventBus
{
    IDisposable Subscribe<T>(Action<T> handler);
    void Publish<T>(T message);
}
```

## Message:
```csharp
public record ProductChangedMessage();
```

## When saving:
```csharp
await _productApiService.CreateAsync(request);
_eventBus.Publish(new ProductChangedMessage());
```

## in Product List:
```csharp
_eventBus.Subscribe<ProductChangedMessage>(_ =>
{
    _ = LoadProductsAsync();
});
```

---

# 13) Very important point: Unsubscribing
Since you are using UserControls, you must pay attention to:

- Subscribing when the Control is opened
- Unsubscribing upon `Dispose`

So as to avoid:
- Memory Leaks
- Duplicate updates
- Interface errors

---

# 14) How do you actually start?
I recommend this order:

## Phase 1
- MainForm
- Navigation
- EventBus
- Api Clients
- Common Controls

## Phase 2
- Products module
- Customers module
- Suppliers module
- Warehouses module

## Phase 3
- Sales module
- Purchases module
- Returns module
- Transfers module

## Phase 4
- Reports
- Backup
- Settings

---

# 15) What do I NOT recommend?
I do not recommend that:
- A Control knows another Control directly
- A Form modifies another Form manually
- Linking the interface with the database directly
- Storing data for a long time in the interface memory

---

# 16) Summary
## The best structure for you is:
- **Desktop Shell**
- **Reusable UserControls**
- **Api Services**
- **EventBus / Publisher-Subscriber**
- **Reload from API after change**

## The idea:
- Any change that occurs
- Publishes a message
- Every Control interested in it reloads from the API

---

If you want, I can prepare for you in the next message one of these three:

1. **Initial structure for the Products module fully**
2. **Practical EventBus code ready for WPF/MVVM**
3. **Navigation map between screens inside MainWindow**

I prefer we start with the **Products module** because it will be the template we repeat for the entire system.

---

# Pub/Sub Pattern Strategy

Yes, **this is a very excellent choice** for your project.

And I recommend it to you in a clear form:

## The correct idea
- Build **reusable UserControls**
- Use **Publisher-Subscriber** or **Event Bus / Event Aggregator**
- So that any change in data reflects on all interfaces displaying the same data
- While keeping **the API as the source of truth**
  Meaning that the real update is done from the database via the API, and not from the memory inside the interface

---

# How does the idea work?
Simple example:

1. The user added a product from the products screen
2. The API saved the product in SQL Server
3. The successful screen **publishes a message**
   - `ProductChanged`
4. Any open UserControl displaying products is **subscribed** to this message
5. Upon receiving the message:
   - Executes `Reload` from the API
   - Displays the new data

---

# Why is this very suitable for you?
Because your system has many screens:
- Products
- Customers
- Suppliers
- Warehouses
- Purchases
- Sales
- Returns
- Transfers
- Reports

Instead of linking each screen to the other manually, you make there a **central signal** that says:
> "Data changed, reload"

And this is very excellent with:
- WPF (MVVM)
- Views / ViewModels
- API
- Continuous development

---

# Where do we use Publisher-Subscriber exactly?
## We use it inside the Desktop only
Meaning:
- Between **Forms**
- Between **UserControls**
- Between **Navigation Areas**

## We don't use it between Desktop and API
The API must remain:
- Stateless
- Independent
- Knows nothing about the interface

---

# What do I recommend for you practically?
## Use Pub/Sub only for:
- `ProductChanged`
- `CustomerChanged`
- `SupplierChanged`
- `WarehouseChanged`
- `StockChanged`
- `SaleCreated`
- `PurchaseCreated`
- `ReturnCreated`

And don't use it for every very small thing.

---

# Best organization for UserControls
Instead of everything being in a large Form, make the interface composed of parts:

## Reusable UserControls examples:
- `ProductsListControl`
- `ProductEditorControl`
- `CustomersListControl`
- `SuppliersListControl`
- `WarehousesListControl`
- `InvoiceItemsControl`
- `PaymentPanelControl`
- `SearchBarControl`
- `SummaryCardsControl`
- `StockGridControl`

---

# How is the update between them?
## Practical example
### Products Screen
- `ProductsListControl` displays products
- `ProductEditorForm` adds/edits a product

After saving:
- `ProductEditorForm` publishes:
  - `ProductChanged`

It is received by:
- `ProductsListControl`
- `SalesForm`
- `StockControl`

Then each one of them executes:
- `ReloadFromApiAsync()`

---

# Very Important
## Publisher-Subscriber should not usually pass the data itself
It is better to pass:
- "The type that changed"
- Or "The change event"

Example:
- `ProductChanged`
- `WarehouseChanged`

Then the Control itself requests the latest data from the API.

This is better than sending:
- The entire product list
- Or DataTable
- Or old objects

---

# The correct form of the idea in your project
## 1) UserControl responsible for display
Meaning:
- Grid
- Buttons
- Search
- UI State

## 2) API responsible for data
Meaning:
- Save
- Edit
- Delete
- Read from SQL Server

## 3) Event Bus responsible for notifications
Meaning:
- "Product changed"
- "Stock changed"
- "Sale transaction occurred"

---

# Simple design example
## Messages
- `ProductChangedMessage`
- `CustomerChangedMessage`
- `SupplierChangedMessage`
- `StockChangedMessage`

## Publisher
After saving:
```csharp
_bus.Publish(new ProductChangedMessage());
```

## Subscriber
In the Products screen:
```csharp
_bus.Subscribe<ProductChangedMessage>(async _ => await LoadProductsAsync());
```

---

# Very important point in WPF (MVVM)
If the update will affect the interface, you must pay attention to the **UI Thread**.

In WPF, upon receiving the message:
- Use `Application.Current.Dispatcher.Invoke(() => ...)`
- Or `DispatcherHelper.RunOnUI(action)`

So as to avoid cross-thread update problems.

---

# Is this better than linking screens to each other directly?
Yes, much better.

## Because direct linking causes:
- Strong coupling between screens
- Difficulty of later modification
- Unclean code
- Difficulty of expanding to Mobile and Web

## But Pub/Sub gives you:
- Excellent separation
- Flexibility
- Reuse
- Better organization

---

# But beware: Don't overdo the messages
This is an important point.

If you start publishing very many messages for every small movement, the system may become complex.

## The best way:
Use it only at:
- Create/Edit/Delete
- Stock change
- Saving an invoice
- Changing master data

---

# Is this suitable with UserControls?
## Yes, very
In fact, it's one of the best uses for it.

Because UserControls usually:
- Display data
- Need automatic update
- Are reused in more than one screen

So Pub/Sub makes them independent yet synchronized.

---

# The architecture I recommend to you
## In Desktop
- `ShellForm`
- `UserControls`
- `EventBus` or `MessageHub`
- `ApiClient`

## In API
- `Controllers`
- `Application Services`
- `Repositories`
- `SQL Server`

---

# Final recommendation for you
## Yes, add:
- **Reusable UserControls**
- **Publisher-Subscriber**
- **Reload from API after change**

And this will give you:
- Organized interface
- Automatic update
- Ease of scalability
- An excellent foundation for the future

---

# What do I not recommend?
I do not recommend that:
- A UserControl knows another Form directly
- Or a Form modifies another Form's data manually
- Or you rely on old data in memory for a long time

---

# Very short summary
## Yes, use:
- **Reusable UserControls**
- **Publisher-Subscriber**
- **Refresh from API**

## And the rule:
- Any change occurs
- Is published as a message
- Every interested Control reloads from the API
