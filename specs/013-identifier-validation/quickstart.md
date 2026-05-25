# Quickstart: Identifier Strategy & Validation

## Implementation Order

1. **Domain & Contracts Purge**: Rip out the `Code` property from Domain entities (`Product`, `Customer`, etc.) and `SalesSystem.Contracts` DTOs.
2. **Application Layer Purge**: Remove duplicate code checks and code generation logic from Application Services. Update Mapping profiles (e.g., Mapster) if they referenced `Code`.
3. **Database Migration**: Run `Add-Migration DropLegacyEntityCodes` and `Update-Database`.
4. **ViewModel Base & WPF Setup**: Implement `INotifyDataErrorInfo` in `ViewModelBase`. Add the global `Validation.ErrorTemplate` to `App.xaml` or the central resource dictionary.
5. **ViewModel Refactoring**: Update all Editor ViewModels one by one. Remove legacy `HasError` booleans. Implement the `ValidateAll()` method and ensure `SaveCommand` has no `CanExecute` predicate.
6. **View Cleanup**: Remove manual error `TextBlock`s from the XAML files, as the new `ErrorTemplate` handles visualization automatically. Ensure `UpdateSourceTrigger=PropertyChanged` is set on bindings.

## Key Invariants to Verify

- **Database Integrity**: The database scheme no longer contains `Code` columns in the 7 specified master tables.
- **Save Feedback**: Opening an empty form and clicking Save instantly triggers a dialog listing all required fields.
- **Red Border Tooltip**: Typing invalid data and tabbing away instantly shows a red border and an exclamation mark with the exact Arabic error message.
- **Always Enabled Save**: The Save/Post buttons are absolutely never disabled or "greyed out" during active editing.
