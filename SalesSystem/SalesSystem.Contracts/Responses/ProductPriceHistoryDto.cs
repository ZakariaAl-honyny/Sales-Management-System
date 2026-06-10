namespace SalesSystem.Contracts.Responses;

public record ProductPriceHistoryDto(
    int Id,
    int ProductUnitId,
    decimal OldCost,
    decimal NewCost,
    string ChangeReason,
    string ChangedByUserName,
    DateTime ChangedAt
);
