# Implementation Plan: Collapsible Multi-Level Sidebar Navigation (Phase 17)

**Branch**: `017-sidebar-navigation` | **Date**: 2026-06-13 | **Spec**: [spec.md](./spec.md)

---

## Summary

This phase codifies and enhances the existing sidebar navigation architecture from a flat 2-level Expander→Button layout into a fully hierarchical multi-level accordion menu that supports arbitrary nesting depth. Level 1 `Expander` controls (styled with `SidebarExpanderStyle`) represent major modules (المبيعات, المشتريات, المالية, التقارير, الأصناف, الإعدادات). Level 2 `Expander` controls (new `SidebarNestedExpanderStyle`) represent sub-groups within a module — e.g., within "التقارير": "التقارير المالية" → [قائمة الدخل, التدفق النقدي], "تقارير المبيعات" → [حسب العميل, حسب المنتج]. Level 3+ `Button` controls (existing `SidebarSubMenuButtonStyle`) represent leaf navigation targets. All navigation commands route through `MainViewModel`'s `RelayCommand` properties, which call `NavigateTo<T>()` — the generic method resolves the target ViewModel via DI, cleans up the previous ViewModel, applies permission checks, and sets `CurrentViewModel` on a `ContentControl` in MainWindow's central workspace. The sidebar preserves its expand/collapse state across navigation, supports unlimited future screens via simple ICommand additions, and enforces strict MVVM separation (no code-behind navigation logic in the sidebar).

---

## Technical Context

**Language/Version**: C# 13 / .NET 10 LTS (WPF Desktop only)
**Architecture Scope**: Entirely within `SalesSystem.DesktopPWF` — no Domain, Application, Infrastructure, or API changes
**Target**: All users — the sidebar is the primary navigation interface for the entire application
**Current State**: The sidebar already uses `Expander`→`Button` 2-level hierarchy with `SidebarExpanderStyle` (Level 1 headers) and `SidebarSubMenuButtonStyle` (Level 2 buttons) defined in `Styles.xaml`. `MainViewModel` already has 40+ `NavigateTo*Command` properties and a `NavigateTo<T>()` generic method. Permission checking via `CanNavigate()` is already implemented.
**Constraints**:
- Navigation state (which Expanders are expanded/collapsed) MUST persist during screen switching — the sidebar `ScrollViewer` MUST NOT reload
- Zero code-behind in sidebar navigation — all click handlers MUST route through `ICommand` bindings to `MainViewModel`
- New nested Expander style (`SidebarNestedExpanderStyle`) MUST visually differentiate Level 2 headers from Level 1 (smaller font, indented padding, muted background, no expand/collapse animation)
- Adding a new screen MUST only require: (1) add ICommand property in MainViewModel, (2) add DataTemplate in MainWindow.xaml Resources, (3) add Button in sidebar
- Permission filtering MUST work at both the module (Level 1) and screen (Level 2+) level — invisible items for unauthorized roles
- The sidebar MUST support right-to-left (RTL) layout with `FlowDirection="RightToLeft"`
- All interactive sidebar elements MUST have Arabic ToolTips explaining the destination screen

---

## Constitution Check

| # | Principle | Status | Notes |
|---|-----------|--------|-------|
| I–VI | Financial/Security/Data | ✅ N/A | Pure UI — no business logic changes |
| VII | Architecture Boundaries | ✅ PASS | All navigation stays within DesktopPWF; MVVM is strictly enforced |
| XII | EventBus | ✅ PASS | ViewModel lifecycle includes `Cleanup()` on navigation away, EventBus subscriptions disposed |
| XV | WPF Dialogs | ✅ PASS | Permission-denied messages use `IDialogService.ShowWarningAsync` — never silent failure |
| XVII | Real-Time UI Validation | ✅ N/A | No validation changes |
| RULE-160–169 | ScreenWindowService | ✅ PASS | New-window commands route through `IScreenWindowService.OpenWindow` — no direct Window creation |
| RULE-185–190 | Arabic ToolTips | ✅ PASS | All sidebar buttons have ToolTips describing the destination screen |

**Gate Result**: ✅ ALL CLEAR — No violations.

---

## Multi-Level Hierarchy Architecture

### Current Structure (2-Level)

```
Expander Level 1 (SidebarExpanderStyle)
  ├── Button Level 2 (SidebarSubMenuButtonStyle)
  ├── Button Level 2
  └── Button Level 2
```

### Target Structure (N-Level)

```
Expander Level 1 (SidebarExpanderStyle)
  ├── Button Level 2 (side menu leaf)
  ├── Expander Level 2 (SidebarNestedExpanderStyle) ← NEW
  │     ├── Button Level 3 (leaf — uses same SidebarSubMenuButtonStyle with deeper indent)
  │     ├── Button Level 3
  │     └── Expander Level 3 ← (future use, same style as Level 2)
  │           └── Button Level 4
  └── Button Level 2
```

This is achieved by nesting Expander controls inside the `StackPanel` of a parent Expander's content. Each nested Expander uses `SidebarNestedExpanderStyle` which provides visual differentiation:

| Property | SidebarExpanderStyle (L1) | SidebarNestedExpanderStyle (L2+) |
|----------|--------------------------|----------------------------------|
| Header FontSize | 14 | 12 |
| Header FontWeight | SemiBold | Normal |
| Header Padding | 16,0 | 28,0 (more indent) |
| Header Foreground | White | #CBD5E1 (lighter) |
| Background (hover) | — | #334155 |
| Toggle Button template | Arrow ▶ | Smaller arrow or no icon |
| MinHeight | 40 | 32 |

### Nested Groups in Reports Module Example

The Reports module is the primary beneficiary of this new structure. Current flat list (17 buttons) becomes:

```
التقارير (Expander L1)
  ├── التقارير المالية (Expander L2)
  │     ├── قائمة الدخل (Button L3)
  │     ├── التدفق النقدي (Button L3)
  │     ├── ضريبة القيمة المضافة (Button L3)
  │     └── كشف حساب (Button L3)
  ├── تقارير المبيعات (Expander L2)
  │     ├── المبيعات حسب العميل (Button L3)
  │     ├── المبيعات حسب المنتج (Button L3)
  │     ├── المبيعات حسب الفئة (Button L3)
  │     └── ملخص المبيعات اليومي (Button L3)
  ├── تقارير المشتريات (Expander L2)
  │     ├── المشتريات حسب المورد (Button L3)
  │     └── المشتريات حسب المنتج (Button L3)
  ├── تقارير الخزينة (Expander L2)
  │     ├── ملخص الخزينة (Button L3)
  │     └── الإغلاق اليومي (Button L3)
  ├── تقارير المخزون (Expander L2)
  │     ├── كشف رصيد المخازن (Button L3)
  │     ├── حركة المخازن (Button L3)
  │     ├── نواقص المخزون (Button L3)
  │     └── المنتجات المنتهية (Button L3)
  ├── تقارير النشاط (Expander L2)
  │     ├── نشاط المستخدمين (Button L3)
  │     └── سجل الدخول (Button L3)
  └── التقرير الشامل (Button L2 — flat, no expander needed)
```

Similarly, the الإعدادات module could be grouped:

```
الإعدادات (Expander L1)
  ├── الجهات (Expander L2)
  │     ├── العملاء (Button L3)
  │     ├── الموردين (Button L3)
  │     └── المستخدمين (Button L3)
  ├── المخزون (Expander L2)
  │     ├── المستودعات (Button L3)
  │     ├── العمليات المخزنية (Button L3)
  │     ├── نقل المخزون (Button L3)
  │     └── المخزون (Button L3)
  ├── الحسابات (Expander L2)
  │     ├── دليل الحسابات (Button L3)
  │     ├── القيود اليومية (Button L3)
  │     └── السنوات المالية (Button L3)
  ├── الإعدادات العامة (Expander L2)
  │     ├── الإعدادات (Button L3)
  │     ├── إعدادات النظام (Button L3)
  │     ├── الضرائب (Button L3)
  │     ├── العملات (Button L3)
  │     └── أسعار العملات (Button L3)
  └── الصيانة (Expander L2)
        ├── النسخ الاحتياطي (Button L3)
        └── سجل الأحداث (Button L3)
```

---

## SidebarNestedExpanderStyle

### Definition in Styles.xaml

The new style is added alongside the existing `SidebarExpanderStyle`:

```text
SidebarNestedExpanderStyle (TargetType="Expander"):
  - Background: Transparent (default), #334155 (on hover)
  - Foreground: #CBD5E1 (light blue-gray)
  - FontSize: 12
  - Padding: 28,0 (indented from L1's 16px padding)
  - BorderThickness: 0
  - Template: Same as SidebarExpanderStyle but:
    - ToggleButton MinHeight: 32 (vs 40)
    - ToggleButton FontSize: 12 (vs 14)
    - ToggleButton FontWeight: Normal (vs SemiBold)
    - No arrow icon in toggle (optional — can use a smaller "▸")
    - ContentPresenter Margin: 4px left margin for sub-group indent
```

The existing `SidebarSubMenuButtonStyle` continues to be used for leaf buttons at ANY level (L2, L3, L4). The only difference is nesting depth — deeper buttons get more `Padding` indent via a `SidebarSubMenuButtonNestedStyle` variant or by adjusting the Margin on the containing StackPanel.

### Visually Distinguishing Levels

| Level | Element | Style | Font | Indent | Background |
|-------|---------|-------|------|--------|------------|
| 1 | Expander Header | SidebarExpanderStyle | 14pt SemiBold White | 16px | Transparent |
| 2 | Expander Header | SidebarNestedExpanderStyle | 12pt Normal #CBD5E1 | 28px | Hover: #334155 |
| 2 | Button Leaf | SidebarSubMenuButtonStyle | 11pt #E2E8F0 | 20px | Hover: #475569 |
| 3 | Button Leaf | SidebarSubMenuButtonStyle | 11pt #E2E8F0 | 32px | Hover: #475569 |

---

## Navigation Architecture

### ViewModel Resolution Chain

```
User clicks sidebar Button
  → Button.Command executes (bound to MainViewModel e.g., NavigateToIncomeStatementCommand)
  → MainViewModel.NavigateTo<IncomeStatementViewModel>()
    1. Checks permission: IsCurrentUserAuthorized("Reports")
    2. If denied: show dialog "ليس لديك صلاحية للوصول إلى هذه الشاشة"
    3. Calls CurrentViewModel?.Cleanup() — disposes EventBus subscriptions, cancels pending operations
    4. Creates new ViewModel instance via App.GetService<T>(DI container)
    5. Sets CurrentViewModel = newInstance
  → ContentControl.DataContext binding updates
  → DataTemplate in MainWindow.Resources resolves ViewModel type → renders corresponding View
  → New screen is displayed in the central workspace
```

### ViewModel-to-View Mapping (DataTemplates)

All ViewModel-to-View mappings are registered in `MainWindow.xaml` Resources as `DataTemplate` entries:

```xml
<DataTemplate DataType="{x:Type vmReports:IncomeStatementViewModel}">
    <viewsReports:IncomeStatementView />
</DataTemplate>
```

This is the ONLY mechanism for View resolution — no reflection, no convention-based lookup, no code-behind. Each ViewModel type maps to exactly one View. When a new screen is added, three things must happen:
1. A new `NavigateTo*Command` property in `MainViewModel`
2. A new `Button` (or nested Expander) in the sidebar XAML
3. A new `DataTemplate` entry in `MainWindow.xaml` Resources

### Expand/Collapse State Preservation

The sidebar uses WPF's native `Expander.IsExpanded` binding with `TwoWay` mode. Since the sidebar is part of the `MainWindow` visual tree and the `ContentControl` switching replaces only the central workspace (Grid.Column="1"), the sidebar `ScrollViewer` (Grid.Column="0") is never destroyed during navigation. Therefore, all `Expander.IsExpanded` states are preserved automatically by WPF's visual tree management. No custom state serialization is needed.

```
MainWindow Grid
  ├── Grid.Column=0: Sidebar Border ← NEVER replaced during navigation
  │     └── ScrollViewer > StackPanel > Expanders ← state persists
  └── Grid.Column=1: ContentControl ← Content replaced on each navigation
        └── Content = CurrentViewModel → DataTemplate resolves → renders View
```

---

## Permission-Aware Visibility

### Module-Level Hiding

Entire Expander sections can be hidden from unauthorized roles by binding `Visibility` to a `MainViewModel` property:

```xml
<Expander Header="المالية" Visibility="{Binding IsFinanceVisible, Converter={StaticResource BoolToVisibility}}" ...>
```

The `MainViewModel` computes these visibility flags from `ISessionService` permissions on construction:

```text
IsFinanceVisible = session.CanAccess(Permission.CustomerPayment) 
                || session.CanAccess(Permission.SupplierPayment)
                || session.CanAccess(Permission.CashBoxView)
                || session.CanAccess(Permission.JournalEntryView)
```

A user with no finance permissions sees the entire المالية module collapsed/hidden. This prevents UI clutter and reduces the cognitive load of seeing irrelevant navigation items.

### Screen-Level Hiding

Individual sidebar buttons can also be hidden based on granular permissions:

```xml
<Button Content="الميزانية العمومية"
        Visibility="{Binding IsBalanceSheetVisible, Converter={StaticResource BoolToVisibility}}"
        Command="{Binding NavigateToBalanceSheetCommand}" .../>
```

When a module Expander contains only hidden items (all children invisible), the Expander itself should be hidden. This is handled by a `HasVisibleChildren` computed property that aggregates child visibility.

---

## Adding New Screens (Scalability)

To add a new screen to the navigation at any level:

1. **ViewModel**: Create the ViewModel class (extends `ViewModelBase`). Optionally add permission check constants.
2. **MainViewModel**: Add `ICommand NavigateToXxxCommand { get; }` property initialized as `new RelayCommand(() => NavigateTo<XxxViewModel>())`.
3. **MainWindow.xaml Resources**: Add `<DataTemplate DataType="{x:Type vmNamespace:XxxViewModel}"><viewsNamespace:XxxView /></DataTemplate>`.
4. **Sidebar XAML**: Add a `Button` inside the appropriate Expander's `StackPanel`, bound to the new command.
5. **Permission**: Add the tag-to-permission mapping in `CanNavigate()` method and `GetTagForViewModel()` if the screen is restricted.
6. **Testing**: Verify the new screen appears in navigation, is reachable, and respects permission settings.

This process adds ~10 lines of code and 2 XAML lines for a new leaf screen. Adding a new nested group (Expander) requires ~5-15 XAML lines but no additional ViewModel changes if existing commands are reused.

The architecture supports 50+ future screens without any structural changes. The sidebar `ScrollViewer` with `VerticalScrollBarVisibility="Auto"` handles overflow naturally. Nested Expanders prevent the sidebar from becoming an overwhelming flat list.

---

## Project Structure

```
SalesSystem.DesktopPWF/
├── Resources/
│   └── Styles.xaml                              ← UPDATE: add SidebarNestedExpanderStyle
├── ViewModels/
│   └── MainViewModel.cs                         ← UPDATE: add computed visibility properties per module (IsFinanceVisible, IsReportsVisible, etc.)
├── Views/
│   └── MainWindow.xaml                          ← UPDATE: restructure Expander content to use nested Expanders; add visibility bindings; add DataTemplates for new screens
└── Converters/
    └── (existing BoolToVisibilityConverter)     ← VERIFY: already exists
```

The `NavigationService.cs` (Frame-based) is NOT changed or used by this architecture — the sidebar uses `ContentControl` + `DataTemplate` binding for View switching, which is the correct MVVM pattern.

---

## Verification Checklist

- [ ] Sidebar supports arbitrary nesting depth (L1 Expander → L2 Expander → L3 Button) via nested `<Expander>` XAML
- [ ] `SidebarNestedExpanderStyle` added to Styles.xaml with visual differentiation (smaller font, indented, muted color)
- [ ] Reports module reorganized: 6 sub-groups (مالية, مبيعات, مشتريات, خزينة, مخزون, نشاط) with leaf buttons at L3
- [ ] Settings module reorganized: 5 sub-groups (جهات, مخزون, حسابات, إعدادات عامة, صيانة)
- [ ] Expand/collapse state preserved across all navigation — sidebar never reloads
- [ ] All sidebar buttons bound via `ICommand` (relay to MainViewModel) — zero Click event handlers in sidebar
- [ ] Permission-aware visibility: entire modules hide when user lacks all child permissions
- [ ] `CanNavigate()` tag mapping covers all 40+ ViewModel types
- [ ] Adding a new screen requires only: (1) ICommand, (2) DataTemplate, (3) Button XAML
- [ ] All sidebar buttons and expanders have Arabic ToolTips describing the destination
- [ ] RTL layout enforced (`FlowDirection="RightToLeft"`)
- [ ] Build: 0 errors, 0 warnings
