namespace SalesSystem.Contracts.Requests;

public record AddProductUnitRequest(
    int UnitId,
    decimal ConversionFactor,
    bool IsBaseUnit
);
