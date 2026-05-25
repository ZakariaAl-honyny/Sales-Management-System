# Quickstart: Collapsible Tree Sidebar Navigation (Phase 17)

**Feature**: `017-sidebar-navigation`
**Date**: 2026-05-25
**Status**: Complete

---

## Developer Guide

This feature modernizes the Desktop WPF shell to use an accordion-style sidebar and Single Page Application (SPA) style navigation.

### 1. Architectural Rules to Follow
* **Never use `.Show()` or `.ShowDialog()` for main application screens anymore.** Popups and confirmation dialogs via `IDialogService` are fine, but major features (Sales, Purchases, Reports) MUST load into the `ContentControl` via ViewModel swapping.
* **Respect RTL**: Remember that margin bindings in XAML might need adjusting. `Padding="25,5,5,5"` on a Left-To-Right UI indents from the left. On an RTL UI, you indent from the right, so use `Padding="5,5,25,5"` appropriately to push sub-menu items inward.
* **Keep Styles Centralized**: Place the `SidebarSubMenuButtonStyle` in a resource dictionary, do not apply local button styling.

### 2. Implementation Steps

1. **Setup Navigation**: Modify `MainViewModel` to include a `CurrentViewModel` property. Create or inject a `NavigationService`.
2. **Update XAML Structure**: Open `MainWindow.xaml`. Replace the flat `StackPanel` of buttons with the structured `ScrollViewer` > `StackPanel` > `Expander` structure defined in the PRD.
3. **Apply Styles**: Create the transparent/borderless styles for the sub-menu buttons.
4. **Bind Commands**: Bind each new `Expander` sub-button to an `ICommand` in `MainViewModel` that changes the `CurrentViewModel`.
5. **Verify DataTemplates**: Ensure all targeted ViewModels have a corresponding `DataTemplate` in the Application resources so they render visually.

### 3. Testing the UI

Run the application and verify:
1. The Sidebar sits on the right side and text flows from right to left.
2. Clicking a main module (e.g., "المبيعات والتوزيع") animates smoothly to reveal its child options.
3. Clicking a child option instantly loads the respective View in the center of the screen without refreshing the entire application.
4. The sidebar scrollbar appears only when necessary (e.g., if you expand every category and resize the window).
