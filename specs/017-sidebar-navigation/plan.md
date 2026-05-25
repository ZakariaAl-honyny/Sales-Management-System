# Implementation Plan: Collapsible Tree Sidebar Navigation (Phase 17)

**Branch**: `017-sidebar-navigation` | **Date**: 2026-05-25 | **Spec**: [spec.md](./spec.md)

---

## Summary

This phase upgrades the primary navigation of the Desktop WPF application from a flat button list to a structured, two-level hierarchical accordion menu using `Expander` controls. It heavily enforces MVVM principles by removing direct window-spawning code and centralizing navigation via `ContentControl` injection and `NavigationService`.

---

## Technical Context

**Language/Version**: C# 13 / WPF (.NET 10 LTS)
**Architecture Scope**: Desktop UI Layer (`SalesSystem.DesktopPWF`)
**Constraints**:
- Must enforce RTL (`FlowDirection="RightToLeft"`).
- `MainViewModel` or `NavigationService` must handle all ViewModel switching.
- Zero business logic changes. This is purely a Presentation Layer restructuring.

---

## Constitution Check

| # | Principle | Status | Notes |
|---|-----------|--------|-------|
| I | Decimal Precision | N/A | UI only |
| II | Domain Formulas | N/A | UI only |
| III | Transactional Integrity | N/A | UI only |
| IV | Invoice Lifecycle | N/A | UI only |
| VII| Architecture Boundaries | ✅ PASS | Ensures UI (WPF) strictly uses Commands instead of code-behind for navigation. |
| XII| EventBus / Thread Safety | ✅ PASS | Centralized navigation ensures memory safety and unloads unused ViewModels cleanly. |

**Gate Result**: ✅ ALL CLEAR — This phase enhances the UI layer while explicitly enforcing the MVVM architecture boundary.

---

## Project Structure

### Source Code (affected paths)

```text
SalesSystem/
└── SalesSystem.DesktopPWF/
    ├── App.xaml / Resources.xaml   ← UPDATE (Add Expander/Sidebar button styles)
    ├── Views/
    │   └── MainWindow.xaml         ← REFACTOR (Replace flat StackPanel with ScrollViewer + Expanders)
    ├── ViewModels/
    │   └── MainViewModel.cs        ← UPDATE (Add CurrentViewModel property and Navigation Commands)
    └── Services/
        └── NavigationService.cs    ← CREATE/UPDATE (If not already centralized)
```
