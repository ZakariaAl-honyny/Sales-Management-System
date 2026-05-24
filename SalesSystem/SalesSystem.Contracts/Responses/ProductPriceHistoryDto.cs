namespace SalesSystem.Contracts.Responses;

public record ProductPriceHistoryDto(
    int Id,
    int ProductUnitId,
    string UnitName,
    decimal OldRetailPrice,
    decimal NewRetailPrice,
    decimal OldWholesalePrice,
    decimal NewWholesalePrice,
    decimal OldAvgCost,
    decimal NewAvgCost,
    string ChangeReason,
    string ChangedByUserName,
    DateTime ChangedAt
);
