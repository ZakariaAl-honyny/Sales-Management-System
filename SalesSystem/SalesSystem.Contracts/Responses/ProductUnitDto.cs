namespace SalesSystem.Contracts.Responses;

public record ProductUnitDto(
    int Id,
    int ProductId,
    string UnitName,
    decimal ConversionFactor,
    decimal RetailPrice,
    decimal WholesalePrice,
    decimal AvgCost,
    bool IsBaseUnit,
    bool IsActive
)
{
    public List<string> Barcodes { get; set; } = new();
}
