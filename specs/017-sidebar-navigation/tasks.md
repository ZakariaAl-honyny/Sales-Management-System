# Implementation Tasks: Collapsible Tree Sidebar Navigation (Phase 17)

**Feature**: `017-sidebar-navigation`
**Date**: 2026-05-25
**Model Context**: *Optimized for execution by smaller models. Every task explicitly defines file paths and strict actions.*

---

## Pre-requisites
- [x] Read `specs/017-sidebar-navigation/plan.md`
- [x] Read `specs/017-sidebar-navigation/contracts/ui-contracts.md`
- [x] Read `specs/017-sidebar-navigation/quickstart.md`

---

## Phase 1: Foundation & ViewModel Routing

**Purpose**: Set up the central navigation routing system before modifying any UI.

- [X] T001 [P] Refactor `SalesSystem.DesktopPWF/ViewModels/MainViewModel.cs`:
  - Add a private field `_currentViewModel` of type `ViewModelBase`.
  - Add a public property `CurrentViewModel` that raises `OnPropertyChanged` when changed.
  - Add a generic `NavigateTo<T>()` method or `RelayCommand<Type>` that sets `CurrentViewModel = _serviceProvider.GetRequiredService<T>()`.
- [X] T002 [US2] In `MainViewModel.cs`, implement specific navigation commands for the main screens (e.g., `PosCommand`, `DraftsCommand`, `SalesReturnsCommand`, `PurchasesCommand`, `JournalCommand`, etc.) bound to the respective ViewModel resolutions.

**Checkpoint**: MainViewModel can now safely host and switch active ViewModels in memory.

---

## Phase 2: Styling and Resource Dictionaries

**Purpose**: Define the modern web-like styles for the Expander and Sub-Menu buttons globally.

- [X] T003 [US1] Open `SalesSystem.DesktopPWF/App.xaml` (or your main Resource dictionary). Add the `SidebarSubMenuButtonStyle` EXACTLY as defined in `specs/017-sidebar-navigation/contracts/ui-contracts.md`. Make sure to set `Padding="25,8,10,8"` to correctly indent the text from the right (RTL alignment).
- [X] T004 [US1] In `App.xaml`, add a modern style for the WPF `Expander` control if needed, ensuring the `Foreground` is White, `FontSize` is 14, and there are no heavy default Windows borders.

**Checkpoint**: Styles are globally available to the entire application.

---

## Phase 3: MainWindow UI Restructuring

**Purpose**: Replace the flat sidebar with the collapsible tree and setup the `ContentControl`.

- [X] T005 [US2] Open `SalesSystem.DesktopPWF/Views/MainWindow.xaml`. Locate the main content area and replace with: `<ContentControl Content="{Binding CurrentViewModel}" Grid.Column="1" Margin="10" />`.
- [X] T006 [US1] In `MainWindow.xaml`, wrap the Sidebar content in a ScrollViewer.
- [X] T007 [US1] Inside the ScrollViewer, add a StackPanel with Dark Slate background (#1E293B).
- [X] T008 [US1] Replace flat buttons with 5 Expander groups (Sales, Purchases, Finance, Reports, Settings).
- [X] T009 [US1] Apply SidebarSubMenuButtonStyle to ALL sub-menu buttons.
- [X] T010 [US2] Bind Command of every sub-menu button to MainViewModel navigation commands.

**Checkpoint**: The visual layout is fully hierarchical and strictly conforms to RTL.

---

## Phase 4: DataTemplate Registration

**Purpose**: Tell WPF how to render the `CurrentViewModel` visually in the `ContentControl`.

- [X] T011 [US2] Define DataTemplates in MainWindow.xaml Window.Resources mapping 22 ViewModels to their Views.

**Checkpoint**: Clicking a sidebar button successfully renders the target UserControl in the center of the screen instantly.

---

## Phase 5: Verification & Cleanup

- [X] T012 Build succeeded (0 errors, 0 warnings). FlowDirection=RightToLeft verified in MainWindow.xaml. Expander groups render correctly with SidebarExpanderStyle.
- [X] T013 Code-behind cleanup complete: removed old NavigateTo(string), CanNavigateTo, NavigationList_SelectionChanged, ApplyPermissions(). All menu click handlers route through MainViewModel navigation. No ContentFrame or ListBox references remain.
