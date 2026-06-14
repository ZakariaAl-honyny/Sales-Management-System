namespace SalesSystem.Contracts.Requests;

public record AddProductUnitRequest(
    int UnitId,
    decimal Factor,
    bool IsBaseUnit
);
