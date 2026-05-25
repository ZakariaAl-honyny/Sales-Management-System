# Research: Collapsible Tree Sidebar Navigation (Phase 17)

**Feature**: `017-sidebar-navigation`
**Date**: 2026-05-25
**Status**: Complete

---

## Decision Log

### D-001: Sidebar Component Choice

**Decision**: Use native WPF `Expander` controls nested within a `StackPanel` inside a `ScrollViewer`.

**Rationale**: The PRD explicitly requires this lightweight approach. While third-party libraries (like MaterialDesignInXAML or MahApps.Metro) offer tree controls, using native WPF `Expander` guarantees maximum control over styling (borderless, transparent backgrounds), zero external dependencies, and perfect RTL support out of the box.

---

### D-002: Sub-Menu Styling Strategy

**Decision**: Create a shared `SidebarSubMenuButtonStyle` in `App.xaml` or `Resources.xaml`.

**Rationale**: To achieve a "web-like" modern accordion menu, WPF's default button styles (which are heavy, with thick borders and gradients) must be overridden. The new style will set `Background="Transparent"`, `BorderThickness="0"`, use specific margins for indentation, and apply subtle hover effects.

---

### D-003: Navigation and Memory Management

**Decision**: Bind the main content area of `MainWindow.xaml` to a `ContentControl` whose `Content` is bound to a `CurrentViewModel` property in `MainViewModel`. 

**Rationale**: Directly opening new Windows (e.g., `new SalesWindow().Show()`) scatters state and consumes memory. By using a single `MainWindow` that hosts active ViewModels, we maintain a Single Page Application (SPA) feel, which is required by the PRD. Switching the `CurrentViewModel` allows WPF to automatically garbage collect the old ViewModel if no other references exist, provided we unsubscribe from EventBus handlers properly (Rule-012).
