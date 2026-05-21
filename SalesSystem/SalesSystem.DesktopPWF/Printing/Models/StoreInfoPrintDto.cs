namespace SalesSystem.DesktopPWF.Printing.Models;

/// <summary>
/// Store information for printed documents
/// </summary>
public record StoreInfoPrintDto(
    string Name,
    string? Address,
    string? Phone,
    string? Email,
    string? TaxNumber,
    string? LogoPath
);
