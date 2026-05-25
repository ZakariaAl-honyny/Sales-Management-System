# Implementation Plan: Multi-Window & UI Polish (v4.5)

**Branch**: `012-multi-window-ui` | **Date**: 2026-05-25 | **Spec**: [spec.md](./spec.md)

---

## Summary

This feature focuses purely on the WPF Desktop client. It introduces non-modal multi-window multitasking by implementing a `ScreenWindowService` and a generic `ScreenWindow` host. It prevents memory leaks using `WeakReference<Window>` and strict `EventBus` disposal. It standardizes list sorting to descending, adds Arabic ToolTips to interactive controls, and permanently replaces all raw `MessageBox.Show` calls with a fully integrated `IDialogService` that safely resolves window ownership.

---

## Technical Context

**Language/Version**: C# 13 / .NET 10 LTS (Desktop WPF)
**Primary Dependencies**: None (native WPF)
**Architecture Scope**: Entirely within `SalesSystem.DesktopPWF`
**Constraints**:
- Must not introduce memory leaks (non-modal windows + EventBus are high risk).
- Must resolve `PositionOverOwner` self-ownership crashes in dialogs.
- Must not use `MessageBox.Show`.
- Arabic text must be embedded in XAML inline.

---

## Constitution Check

| # | Principle | Status | Notes |
|---|-----------|--------|-------|
| I | Decimal Precision | ✅ N/A | No domain data changes |
| II | Domain Formulas | ✅ N/A | No domain logic changes |
| III | Transactional Integrity | ✅ N/A | No DB writes |
| IV | Invoice Lifecycle | ✅ N/A | No invoice logic changes |
| V | Stock Integrity | ✅ N/A | No stock logic changes |
| VI | Result Pattern | ✅ N/A | Feature is purely UI layer |
| VII | Architecture Boundaries | ✅ PASS | Desktop UI code remains strictly in DesktopPWF |
| VIII | Security | ✅ N/A | No security changes |
| IX | Four-Layer Validation | ✅ N/A | No validation changes |
| X | Logging | ✅ N/A | No new backend logging |
| XI | EF Core Conventions | ✅ N/A | No EF Core changes |
| XII | Audit Trail | ✅ N/A | No audit changes |
| XIII | Delete Strategy | ✅ N/A | No delete logic changes |
| XIV | Defensive Programming | ✅ PASS | Dialog owner resolution defends against invalid active windows |
| XV | WPF Dialogs | ✅ PASS | Explicitly mandates 100% IDialogService usage |
| XVI | Toast Notifications | ✅ N/A | No changes to toasts |
| XVII | Real-Time UI Validation | ✅ N/A | No changes to validation UI |

**Gate Result**: ✅ ALL CLEAR — no violations.

---

## Project Structure

### Source Code (affected paths)

```text
SalesSystem/
└── SalesSystem.DesktopPWF/
    ├── Services/
    │   ├── App/
    │   │   └── ScreenWindowService.cs    ← NEW (Manages non-modal windows)
    │   └── Dialog/
    │       └── DialogService.cs          ← UPDATE (Fix owner resolution)
    ├── Views/
    │   └── ScreenWindow.xaml             ← NEW (Generic host window)
    └── ViewModels/
        └── Base/
            └── ViewModelBase.cs          ← UPDATE (EventBus disposal cleanup)
```
