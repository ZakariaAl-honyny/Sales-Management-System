namespace SalesSystem.Contracts.Requests;

public record UpdateProductUnitRequest(
    string UnitName,
    decimal RetailPrice,
    decimal WholesalePrice
);
