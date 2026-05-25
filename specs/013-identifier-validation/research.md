# Research: Identifier Strategy & Validation

**Feature**: `013-identifier-validation`
**Date**: 2026-05-25
**Status**: Complete

---

## Decision Log

### D-001: Entity Code Removal Strategy

**Decision**: Remove `Code` property from `Product`, `Customer`, `Supplier`, `Warehouse`, `Category`, `Unit`, and `User`. Generate a single EF Core migration `DropEntityCodes`. Remove `DocumentSequenceService` logic for these entities.

**Rationale**: Simplifies the domain. Users rely on `Name`, auto-increment `Id`, or `Barcode`. `Code` was redundant and caused sequence race conditions.

---

### D-002: WPF Validation Framework

**Decision**: Implement `INotifyDataErrorInfo` in `ViewModelBase`. Maintain a `Dictionary<string, List<string>> _errors` backing field. Provide protected methods `AddError(propertyName, error)` and `ClearErrors(propertyName)`. Remove all custom `HasNameError`, `NameError` properties from child ViewModels.

**Rationale**: `INotifyDataErrorInfo` is the native, standardized WPF validation interface. It automatically integrates with WPF bindings (`ValidatesOnNotifyDataErrors=True`) and drastically reduces boilerplate code in individual ViewModels.

---

### D-003: WPF Error Template Styling

**Decision**: Define a global `Validation.ErrorTemplate` in `App.xaml` or a shared resource dictionary. The template will draw a `Border` with `BorderBrush="Red"` around the `AdornedElement` and place a `TextBlock` or `Icon` (❗) on the right edge, bound to the `[0].ErrorContent` as a `ToolTip`.

**Rationale**: Provides immediate, standardized visual feedback across the entire application without needing to manually code error `TextBlock`s under every single `TextBox` (as required by the previous `HasError` boolean pattern).

---

### D-004: Save Validation Interception

**Decision**: As per RULE-059, the `SaveCommand` will always be `CanExecute = true`. The command execution will start with a call to `Validate()`. If `HasErrors` is true, it aggregates the dictionary values into a single string and calls `_dialogService.ShowWarningAsync()`, then returns without saving.

**Rationale**: Disabling the save button leaves users guessing why they can't save. Intercepting the save to show a comprehensive list of missing fields is much more user-friendly.
