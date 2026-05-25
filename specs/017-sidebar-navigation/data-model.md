# Data Model: Collapsible Tree Sidebar Navigation (Phase 17)

**Feature**: `017-sidebar-navigation`
**Date**: 2026-05-25
**Status**: Complete

---

> [!NOTE]
> This feature deals exclusively with the WPF Presentation Layer. There are no Database, Domain Entity, or EF Core changes required.

## Presentation Layer (MVVM) Models

While there are no database entities, the navigation system relies on strongly typed commands and view models to function correctly.

### Navigation Target (Conceptual)

In the `MainViewModel`, the system must track the currently active screen.

```csharp
// Inside MainViewModel.cs

// The currently active screen/module presented in the center area
private ViewModelBase _currentViewModel;
public ViewModelBase CurrentViewModel 
{
    get => _currentViewModel;
    set => SetProperty(ref _currentViewModel, value);
}

// Commands bound to the Sidebar buttons
public ICommand NavigateToPosCommand { get; }
public ICommand NavigateToDraftsCommand { get; }
public ICommand NavigateToReturnsCommand { get; }
public ICommand NavigateToPurchasesCommand { get; }
public ICommand NavigateToJournalCommand { get; }
public ICommand NavigateToChartOfAccountsCommand { get; }
// ... etc.
```

### DataTemplates (UI Contract)

To render the `CurrentViewModel` correctly in the `ContentControl`, WPF uses `DataTemplate` mapping. These will need to be registered in `App.xaml` or `MainWindow.xaml.Resources`.

```xml
<!-- Example of View-to-ViewModel mapping required for dynamic navigation -->
<DataTemplate DataType="{x:Type viewmodels:PosViewModel}">
    <views:PosView />
</DataTemplate>

<DataTemplate DataType="{x:Type viewmodels:DraftsViewModel}">
    <views:DraftsView />
</DataTemplate>
```
