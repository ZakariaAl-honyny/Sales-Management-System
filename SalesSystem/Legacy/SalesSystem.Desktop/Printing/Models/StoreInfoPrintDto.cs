namespace SalesSystem.Desktop.Printing.Models;

public class StoreInfoPrintDto
{
    public string StoreName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string TaxNumber { get; set; } = string.Empty;
    public string? LogoPath { get; set; }
}
