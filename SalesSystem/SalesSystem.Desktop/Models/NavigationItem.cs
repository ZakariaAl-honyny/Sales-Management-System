using SalesSystem.Contracts.Enums;

namespace SalesSystem.Desktop.Models;

public sealed class NavigationItem
{
    public string    Label       { get; init; } = string.Empty;
    public string    IconKey     { get; init; } = string.Empty;
    public Type      ScreenType  { get; init; } = null!;
    public UserRole  MinRole     { get; init; }
    
    public bool IsVisible(UserRole userRole)
    {
        // Admin (1) sees everything (MinRole 1, 2, or 3)
        // Manager (2) sees MinRole 2 or 3
        // Cashier (3) sees only MinRole 3
        return (byte)userRole <= (byte)MinRole;
    }
}
