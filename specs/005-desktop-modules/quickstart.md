# Quickstart: Desktop Modules (Phase 5)

**Branch**: `005-desktop-modules`

---

## Prerequisites

Before working on any module, ensure the following are running:

1. **SQL Server** — database `SalesSystemDb` created and migrated (Phase 2 migrations applied).
2. **SalesSystem.Api** — running on `http://localhost:5000` with a valid user seeded.
3. **SalesSystem.Desktop** — shell functional (Phase 4 complete).

---

## Setup Checklist

```powershell
# 1. Start API
cd SalesSystem\SalesSystem.Api
dotnet run

# 2. Verify API health
curl http://localhost:5000/api/health   # should return 200

# 3. Install ClosedXML (AFTER approval added to AGENTS.md §5)
cd SalesSystem\SalesSystem.Desktop
dotnet add package ClosedXML --version 0.102.*

# 4. Run Desktop
cd SalesSystem\SalesSystem.Desktop
dotnet run
```

---

## Standard Module Implementation Template

When implementing each new module, follow these exact steps:

### Step 1 — Create API Service Interface

```csharp
// SalesSystem.Desktop/Services/Api/Interfaces/I[Entity]ApiService.cs
public interface I[Entity]ApiService
{
    Task<List<[Entity]Response>> GetAllAsync(string? search, bool includeInactive = false);
    Task<Result<[Entity]Response>> CreateAsync(Create[Entity]Request request);
    Task<Result<[Entity]Response>> UpdateAsync(int id, Update[Entity]Request request);
    Task<Result> DeactivateAsync(int id);
    Task<Result> ReactivateAsync(int id);
}
```

### Step 2 — Implement API Service

```csharp
// SalesSystem.Desktop/Services/Api/[Entity]ApiService.cs
public class [Entity]ApiService : I[Entity]ApiService
{
    private readonly HttpClientService _http;
    
    public [Entity]ApiService(HttpClientService http) => _http = http;
    
    public async Task<List<[Entity]Response>> GetAllAsync(string? search, bool includeInactive = false)
    {
        var query = $"?search={search}&includeInactive={includeInactive}";
        return await _http.GetListAsync<[Entity]Response>($"/api/[entities]{query}")
               ?? new List<[Entity]Response>();
    }
    
    // ... other methods
}
```

### Step 3 — Create EventBus Message

```csharp
// SalesSystem.Desktop/Messaging/Messages/[Entity]ChangedMessage.cs
public record [Entity]ChangedMessage(int [Entity]Id);
```

### Step 4 — Build List Control

```csharp
// SalesSystem.Desktop/Controls/[Entity]s/[Entity]sListControl.cs
public partial class [Entity]sListControl : UserControl
{
    private readonly I[Entity]ApiService _api;
    private readonly IEventBus _eventBus;
    private IDisposable? _subscription;
    
    public [Entity]sListControl(I[Entity]ApiService api, IEventBus eventBus)
    {
        InitializeComponent();
        _api = api;
        _eventBus = eventBus;
        RightToLeft = RightToLeft.Yes;
    }
    
    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        _subscription = _eventBus.Subscribe<[Entity]ChangedMessage>(OnChanged);
        _ = LoadDataAsync();
    }
    
    private void OnChanged([Entity]ChangedMessage msg)
    {
        if (InvokeRequired) Invoke(() => _ = LoadDataAsync());
        else _ = LoadDataAsync();
    }
    
    private async Task LoadDataAsync()
    {
        var items = await _api.GetAllAsync(
            txtSearch.Text,
            chkShowInactive.Checked);
        // bind to DataGridView
    }
    
    protected override void Dispose(bool disposing)
    {
        if (disposing) _subscription?.Dispose();
        base.Dispose(disposing);
    }
}
```

### Step 5 — Build Editor Form

```csharp
// SalesSystem.Desktop/Controls/[Entity]s/[Entity]EditorForm.cs
public partial class [Entity]EditorForm : Form
{
    private readonly I[Entity]ApiService _api;
    private readonly IEventBus _eventBus;
    private readonly int? _editId;  // null = create mode
    
    public [Entity]EditorForm(I[Entity]ApiService api, IEventBus eventBus, int? editId = null)
    {
        InitializeComponent();
        _api = api;
        _eventBus = eventBus;
        _editId = editId;
        RightToLeft = RightToLeft.Yes;
        RightToLeftLayout = true;
        Text = editId.HasValue ? "تعديل" : "إضافة جديد";
    }
    
    private async void btnSave_Click(object sender, EventArgs e)
    {
        // validate
        if (string.IsNullOrWhiteSpace(txtName.Text)) { /* show error */ return; }
        
        Result<[Entity]Response> result;
        if (_editId.HasValue)
            result = await _api.UpdateAsync(_editId.Value, BuildUpdateRequest());
        else
            result = await _api.CreateAsync(BuildCreateRequest());
        
        if (result.IsSuccess)
        {
            _eventBus.Publish(new [Entity]ChangedMessage(result.Value!.Id));
            DialogResult = DialogResult.OK;
            Close();
        }
        else
        {
            MessageBox.Show(result.Error, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
```

### Step 6 — Register in Program.cs

```csharp
// SalesSystem.Desktop/Program.cs
services.AddTransient<I[Entity]ApiService, [Entity]ApiService>();
services.AddTransient<[Entity]sListControl>();
```

---

## Invoice Module Template Differences

Invoice forms (Sales, Purchases, Returns, Transfers) differ from CRUD modules:

1. **No separate editor dialog** — the invoice IS the form.
2. **Line items grid** — `DataGridView` with editable Qty/Price/Discount columns.
3. **Tax toggle** — `CheckBox` ("شامل الضريبة") + `NumericUpDown` (tax rate %).
4. **Real-time totals** — update on every `CellEndEdit` event.
5. **Action buttons** change based on invoice status:
   - Draft: `[حفظ مسودة]` + `[ترحيل الفاتورة]`
   - Posted: `[إلغاء الفاتورة]` (ManagerAndAbove only)
   - Cancelled: all buttons disabled

---

## Role-Based UI Gating

Apply in each control's `OnLoad`:

```csharp
protected override void OnLoad(EventArgs e)
{
    base.OnLoad(e);
    var role = _authService.CurrentUserRole;
    
    // Hide/disable based on role
    btnAdd.Visible = role >= UserRole.Manager;
    btnEdit.Visible = role >= UserRole.Manager;
    btnDeactivate.Visible = role >= UserRole.Manager;
}
```

---

## RTL Requirements

Every `UserControl` and `Form` MUST have:

```csharp
this.RightToLeft = RightToLeft.Yes;
this.RightToLeftLayout = true;   // for Forms only
```

All `Label`, `TextBox`, `DataGridView`, and `Button` controls inherit RTL from parent. Exception: numeric/money fields should have `RightToLeft = No` for correct cursor behavior.

---

## Key File Paths

| What | Path |
|------|------|
| API Base URL config | `appsettings.json` → `ApiSettings:BaseUrl` |
| JWT token storage | `AuthService.CurrentToken` (in-memory only) |
| EventBus | `SalesSystem.Desktop/Services/EventBus.cs` |
| HttpClientService | `SalesSystem.Desktop/Services/HttpClientService.cs` (create in Phase A2) |
| DI registrations | `SalesSystem.Desktop/Program.cs` |
