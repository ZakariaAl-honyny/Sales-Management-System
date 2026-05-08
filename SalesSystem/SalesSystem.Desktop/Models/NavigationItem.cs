using SalesSystem.Contracts.Enums;

namespace SalesSystem.Desktop.Models;

public sealed class NavigationItem
{
    public string Label { get; init; } = string.Empty; // Arabic label
    public string IconKey { get; init; } = string.Empty; // Image resource key
    public Type ScreenType { get; init; } = null!;        // UserControl type to load
    public UserRole MinRole { get; init; }                 // Minimum required role
    
    public bool IsVisible(UserRole userRole)
    {
        // Admin (1) sees everything (MinRole 1, 2, or 3)
        // Manager (2) sees items where MinRole is 2 or 3
        // Cashier (3) sees items where MinRole is 3
        return (byte)userRole <= (byte)MinRole;
    }
}
