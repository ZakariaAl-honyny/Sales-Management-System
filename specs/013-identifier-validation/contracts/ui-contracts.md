# UI Contracts: Identifier Strategy & Validation

**Feature**: `013-identifier-validation`
**Date**: 2026-05-25

---

## `ViewModelBase` Implementation

`ViewModelBase` MUST implement `INotifyDataErrorInfo`.

```csharp
public abstract class ViewModelBase : ObservableObject, INotifyDataErrorInfo
{
    private readonly Dictionary<string, List<string>> _errors = new();

    public bool HasErrors => _errors.Any();

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    public IEnumerable GetErrors(string? propertyName)
    {
        if (string.IsNullOrEmpty(propertyName) || !_errors.ContainsKey(propertyName))
            return null!;
        return _errors[propertyName];
    }

    protected void AddError(string propertyName, string error)
    {
        if (!_errors.ContainsKey(propertyName))
            _errors[propertyName] = new List<string>();

        if (!_errors[propertyName].Contains(error))
        {
            _errors[propertyName].Add(error);
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
            OnPropertyChanged(nameof(HasErrors));
        }
    }

    protected void ClearErrors(string propertyName)
    {
        if (_errors.Remove(propertyName))
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
            OnPropertyChanged(nameof(HasErrors));
        }
    }
}
```

---

## Global Error Template (XAML)

A global style MUST be added to `App.xaml` or `Styles.xaml` to target `TextBox` and `ComboBox` when `Validation.HasError="True"`.

```xml
<ControlTemplate x:Key="StandardErrorTemplate">
    <DockPanel LastChildFill="True">
        <!-- Error Icon with ToolTip on the left (RTL) -->
        <Border DockPanel.Dock="Left" Margin="5,0,0,0" ToolTip="{Binding AdornedElement.(Validation.Errors)[0].ErrorContent, ElementName=adornedElement}">
            <TextBlock Text="❗" Foreground="Red" VerticalAlignment="Center" FontWeight="Bold"/>
        </Border>
        <!-- The original control with a red border -->
        <Border BorderBrush="Red" BorderThickness="1" CornerRadius="2">
            <AdornedElementPlaceholder Name="adornedElement" />
        </Border>
    </DockPanel>
</ControlTemplate>

<!-- Apply automatically to all TextBoxes -->
<Style TargetType="TextBox" BasedOn="{StaticResource {x:Type TextBox}}">
    <Setter Property="Validation.ErrorTemplate" Value="{StaticResource StandardErrorTemplate}"/>
</Style>
```

---

## Editor ViewModel Validation Pattern

All 14 editor ViewModels MUST adopt this save pattern:

```csharp
// Constructor
SaveCommand = new AsyncRelayCommand(SaveAsync); // Notice: NO CanExecute delegate!

private bool ValidateAll()
{
    ClearErrors(nameof(Name));
    ClearErrors(nameof(Phone));

    if (string.IsNullOrWhiteSpace(Name))
        AddError(nameof(Name), "اسم العميل مطلوب");

    // ... other validations

    if (HasErrors)
    {
        var errorMessages = string.Join("\n• ", _errors.SelectMany(x => x.Value));
        _ = _dialogService.ShowWarningAsync("بيانات غير مكتملة", $"يرجى إكمال البيانات الإلزامية التالية:\n\n• {errorMessages}");
        return false;
    }

    return true;
}

private async Task SaveAsync()
{
    if (!ValidateAll()) return;

    // Proceed with API call...
}
```
