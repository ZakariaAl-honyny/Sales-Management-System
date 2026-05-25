# Implementation Plan: Touch-Optimized Quick POS Interface

**Branch**: `015-touch-pos-interface` | **Date**: 2026-05-25 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `specs/015-touch-pos-interface/spec.md`

## Summary

This phase introduces a dual-mode sales interface, adding a fast, touch-friendly "Restaurant-Style" POS layout alongside the standard retail grid. It leverages WPF `UniformGrid` for category/product selection and ensures sub-50ms cart interactions. The new UI will route all business logic through the exact same backend API endpoints and Domain entities as the standard UI to guarantee architectural consistency.

## Technical Context

**Language/Version**: C# 13 / .NET 10 LTS (WPF + ASP.NET Core)  
**Primary Dependencies**: WPF UI components (VirtualizingStackPanel, UniformGrid), MVVM Toolkit, EventBus  
**Storage**: N/A (Reuses existing SQL Server DB / EF Core schema)  
**Testing**: WPF manual UI testing + Existing API unit tests  
**Target Platform**: Windows Desktop (WPF Desktop App)
**Project Type**: Desktop WPF Client  
**Performance Goals**: < 50ms cart addition latency, 60fps smooth scrolling with lazy-loaded images  
**Constraints**: Fully responsive Right-To-Left (RTL) layout, optimized for minimum 1024x768 touch screens  

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| # | Principle | Status | Notes |
|---|-----------|--------|-------|
| II | Financial Formulas | ✅ PASS | Cart calculates totals exactly as defined in `SalesInvoice` entity. |
| IV | Invoice Lifecycle | ✅ PASS | Reuses `Draft` (1) and `Posted` (2) states via "Cash/Card" and "Draft" buttons. |
| VII | Architecture Boundaries | ✅ PASS | Touch UI is purely WPF Desktop. Connects to API via `HttpClient` (never direct DB). |
| XII | EventBus (Desktop) | ✅ PASS | UI view switching or cart updates will use standard MVVM/EventBus. |

## Project Structure

### Documentation (this feature)

```text
specs/015-touch-pos-interface/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output (empty/reused)
└── tasks.md             # Phase 2 output (/speckit-tasks)
```

### Source Code (affected paths)

```text
SalesSystem/
└── SalesSystem.DesktopPWF/
    ├── ViewModels/
    │   ├── Sales/
    │   │   ├── SalesInvoiceEditorViewModel.cs ← UPDATE (Add ViewMode toggle logic)
    │   │   ├── TouchPosViewModel.cs          ← CREATE (New dedicated VM for touch)
    │   │   └── TouchPosCartViewModel.cs      ← CREATE (Sub-VM for active cart)
    └── Views/
        ├── Sales/
        │   ├── SalesInvoiceEditor.xaml       ← UPDATE (Add ContentControl for mode switching)
        │   ├── TouchPosView.xaml             ← CREATE (Right panel: Categories/Products)
        │   └── TouchPosCartView.xaml         ← CREATE (Left panel: Active cart + Numpad)
        └── Controls/
            └── NumericKeypadControl.xaml     ← CREATE (Reusable on-screen numpad)
```

**Structure Decision**: The Touch POS UI will be implemented as modular UserControls within the Desktop project. The main `SalesInvoiceEditorViewModel` will orchestrate switching between the standard grid view and the new touch view.
