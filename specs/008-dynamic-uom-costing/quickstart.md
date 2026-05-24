# Quickstart: Dynamic UOM & Costing

1. Use EF Core migrations to create the new `ProductUnits`, `UnitBarcodes`, and `ProductPriceHistory` tables. The migration must seed a base `ProductUnit` (factor = 1) for every existing `Product`, moving the prices and costs to the unit level.
2. Update the `Product` entity in the Domain layer to remove scalar price properties and rely on the `Units` collection.
3. Implement `UpdateProductPricingService` to handle cost recalculation logic based on the `SystemSettings.CostingMethod`.
4. Update the Purchase Invoice posting workflow to call `UpdateProductPricingService` after a successful invoice save.
5. Create the API controllers and WPF ViewModels/Views to manage Product Units and view Price History.
