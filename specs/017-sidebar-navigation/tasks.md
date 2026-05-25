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

- [ ] T001 [P] Refactor `SalesSystem.DesktopPWF/ViewModels/MainViewModel.cs`:
  - Add a private field `_currentViewModel` of type `ViewModelBase`.
  - Add a public property `CurrentViewModel` that raises `OnPropertyChanged` when changed.
  - Add a generic `NavigateTo<T>()` method or `RelayCommand<Type>` that sets `CurrentViewModel = _serviceProvider.GetRequiredService<T>()`.
- [ ] T002 [US2] In `MainViewModel.cs`, implement specific navigation commands for the main screens (e.g., `PosCommand`, `DraftsCommand`, `SalesReturnsCommand`, `PurchasesCommand`, `JournalCommand`, etc.) bound to the respective ViewModel resolutions.

**Checkpoint**: MainViewModel can now safely host and switch active ViewModels in memory.

---

## Phase 2: Styling and Resource Dictionaries

**Purpose**: Define the modern web-like styles for the Expander and Sub-Menu buttons globally.

- [ ] T003 [US1] Open `SalesSystem.DesktopPWF/App.xaml` (or your main Resource dictionary). Add the `SidebarSubMenuButtonStyle` EXACTLY as defined in `specs/017-sidebar-navigation/contracts/ui-contracts.md`. Make sure to set `Padding="25,8,10,8"` to correctly indent the text from the right (RTL alignment).
- [ ] T004 [US1] In `App.xaml`, add a modern style for the WPF `Expander` control if needed, ensuring the `Foreground` is White, `FontSize` is 14, and there are no heavy default Windows borders.

**Checkpoint**: Styles are globally available to the entire application.

---

## Phase 3: MainWindow UI Restructuring

**Purpose**: Replace the flat sidebar with the collapsible tree and setup the `ContentControl`.

- [ ] T005 [US2] Open `SalesSystem.DesktopPWF/Views/MainWindow.xaml`. Locate the main content area (where screens were previously loaded or tabs were placed) and replace it with: `<ContentControl Content="{Binding CurrentViewModel}" Grid.Column="0" Margin="10" />`.
- [ ] T006 [US1] In `MainWindow.xaml`, locate the Sidebar area (usually `Grid.Column="1"` for RTL). Wrap the Sidebar content in a `<ScrollViewer VerticalScrollBarVisibility="Auto" HorizontalAlignment="Right" Width="260">`.
- [ ] T007 [US1] Inside the `ScrollViewer`, add a `<StackPanel Background="#1E293B">` (Dark Slate). 
- [ ] T008 [US1] Replace the old flat buttons with the 5 `Expander` groups explicitly listed in the PRD (Sales, Purchases, Finance, Reports, Settings). Inside each `Expander`, add a `StackPanel` containing the sub-menu `<Button>` elements.
- [ ] T009 [US1] Apply `Style="{StaticResource SidebarSubMenuButtonStyle}"` to ALL sub-menu buttons.
- [ ] T010 [US2] Bind the `Command` property of every sub-menu button to its corresponding navigation command in `MainViewModel` (e.g., `Command="{Binding PosCommand}"`).

**Checkpoint**: The visual layout is fully hierarchical and strictly conforms to RTL.

---

## Phase 4: DataTemplate Registration

**Purpose**: Tell WPF how to render the `CurrentViewModel` visually in the `ContentControl`.

- [ ] T011 [US2] In `MainWindow.xaml` (inside `<Window.Resources>`) or `App.xaml`, define `DataTemplate` bindings mapping every routable ViewModel to its View. Example:
  `<DataTemplate DataType="{x:Type viewmodels:PosViewModel}">`
      `<views:PosView />`
  `</DataTemplate>`
  *(Ensure you map all screens referenced by the new Sidebar buttons)*.

**Checkpoint**: Clicking a sidebar button successfully renders the target UserControl in the center of the screen instantly.

---

## Phase 5: Verification & Cleanup

- [ ] T012 Run the WPF application. Ensure `FlowDirection="RightToLeft"` works flawlessly. Verify that expanding an item works and switching views does not reload the sidebar or crash.
- [ ] T013 Ensure NO code-behind (e.g. `MainWindow.xaml.cs`) contains `.Show()` or `.ShowDialog()` for these main screens anymore. Delete legacy flat button click-event handlers.
