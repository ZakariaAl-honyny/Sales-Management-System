# UI Contracts: Collapsible Tree Sidebar Navigation (Phase 17)

**Feature**: `017-sidebar-navigation`
**Date**: 2026-05-25
**Status**: Complete

---

## WPF Component Contracts

Because this feature focuses on Desktop UI restructuring, the relevant contracts are the XAML structural guarantees and styling requirements.

### 1. The Main Window Host Contract

The `MainWindow.xaml` must expose a single central hosting area using a `ContentControl`.

```xml
<!-- Main Content Area (Dynamic ViewModel Host) -->
<ContentControl Content="{Binding CurrentViewModel}" 
                Margin="10" 
                Grid.Column="0" /> <!-- Assuming Sidebar is Column 1 in RTL -->
```

### 2. The Sidebar Sub-Menu Button Style Contract

To ensure the sub-menu buttons look like modern web links rather than clunky desktop buttons, the following resource dictionary style contract must be implemented:

```xml
<Style x:Key="SidebarSubMenuButtonStyle" TargetType="Button">
    <Setter Property="Background" Value="Transparent" />
    <Setter Property="BorderThickness" Value="0" />
    <Setter Property="Foreground" Value="#E2E8F0" />
    <Setter Property="FontSize" Value="13" />
    <Setter Property="Padding" Value="25,8,10,8" /> <!-- High right padding for RTL indentation -->
    <Setter Property="HorizontalContentAlignment" Value="Right" />
    <Setter Property="Cursor" Value="Hand" />
    
    <!-- Template overrides to remove default button chrome -->
    <Setter Property="Template">
        <Setter.Value>
            <ControlTemplate TargetType="Button">
                <Border Background="{TemplateBinding Background}" Padding="{TemplateBinding Padding}">
                    <ContentPresenter HorizontalAlignment="{TemplateBinding HorizontalContentAlignment}" 
                                      VerticalAlignment="Center"/>
                </Border>
                <ControlTemplate.Triggers>
                    <!-- Hover Effect -->
                    <Trigger Property="IsMouseOver" Value="True">
                        <Setter Property="Background" Value="#475569" />
                        <Setter Property="Foreground" Value="White" />
                    </Trigger>
                </ControlTemplate.Triggers>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</Style>
```

### 3. Navigation Service Interface

If utilizing a decoupled service (recommended for testability and DI), the contract should look like this:

```csharp
public interface INavigationService
{
    ViewModelBase CurrentViewModel { get; }
    void NavigateTo<TViewModel>() where TViewModel : ViewModelBase;
    
    // Event fired when navigation completes, allowing UI to update (if needed)
    event Action StateChanged;
}
```
