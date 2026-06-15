namespace SalesSystem.Contracts.Requests;

public record AddProductUnitRequest(
    short UnitId,
    decimal Factor,
    bool IsBaseUnit
);
