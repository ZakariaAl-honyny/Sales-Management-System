using Microsoft.EntityFrameworkCore;
using SalesSystem.Domain.Entities;
using SalesSystem.Infrastructure.Data;
using SalesSystem.Infrastructure.Repositories;

namespace SalesSystem.Infrastructure.Tests.Repositories;

public class GenericRepositoryTests
{
    private SalesDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<SalesDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new SalesDbContext(options);
    }

    #region Product Repository Tests

    [Fact]
    public async Task AddAsync_Product_AddsToDatabase()
    {
        // Arrange
        await using var context = CreateContext("ProductDb1");
        var repository = new GenericRepository<Product>(context);

        var product = Product.Create(
            name: "Test Product",
            retailPrice: 100m,
            wholesalePrice: 900m,
            purchasePrice: 50m,
            retailUnitId: 1,
            wholesaleUnitId: 2,
            conversionFactor: 10m
        );

        // Act
        await repository.AddAsync(product);
        await context.SaveChangesAsync();

        // Assert
        var saved = await context.Products.FirstOrDefaultAsync(p => p.Name == "Test Product");
        saved.Should().NotBeNull();
        saved!.RetailPrice.Should().Be(100m);
    }

    [Fact]
    public async Task GetByIdAsync_Product_ReturnsProduct()
    {
        // Arrange
        await using var context = CreateContext("ProductDb2");
        var repository = new GenericRepository<Product>(context);

        var product = Product.Create(name: "Product to Find", retailPrice: 100m, wholesalePrice: 900m, purchasePrice: 50m, retailUnitId: 1, wholesaleUnitId: 2, conversionFactor: 10m);
        await repository.AddAsync(product);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetByIdAsync(product.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Product to Find");
    }

    [Fact]
    public async Task GetAllAsync_MultipleProducts_ReturnsAll()
    {
        // Arrange
        await using var context = CreateContext("ProductDb3");
        var repository = new GenericRepository<Product>(context);

        var product1 = Product.Create(name: "Product 1", retailPrice: 100m, wholesalePrice: 900m, purchasePrice: 50m, retailUnitId: 1, wholesaleUnitId: 2, conversionFactor: 10m);
        var product2 = Product.Create(name: "Product 2", retailPrice: 200m, wholesalePrice: 1800m, purchasePrice: 100m, retailUnitId: 1, wholesaleUnitId: 2, conversionFactor: 10m);
        var product3 = Product.Create(name: "Product 3", retailPrice: 300m, wholesalePrice: 2700m, purchasePrice: 150m, retailUnitId: 1, wholesaleUnitId: 2, conversionFactor: 10m);

        await repository.AddAsync(product1);
        await repository.AddAsync(product2);
        await repository.AddAsync(product3);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(3);
    }

[Fact]
    public async Task UpdateAsync_Product_UpdatesInDatabase()
    {
        // Arrange
        await using var context = CreateContext("ProductDb4");
        var repository = new GenericRepository<Product>(context);

        var product = Product.Create(name: "Original Name", retailPrice: 100m, wholesalePrice: 900m, purchasePrice: 80m, retailUnitId: 1, wholesaleUnitId: 2, conversionFactor: 10m, code: "P001", minStock: 10m);
        await repository.AddAsync(product);
        await context.SaveChangesAsync();

        // Act - use the Update method with all required parameters
        product.Update(
            name: "Updated Name",
            retailPrice: 150m,
            wholesalePrice: 1300m,
            purchasePrice: 90m,
            retailUnitId: 1,
            wholesaleUnitId: 2,
            conversionFactor: 10m,
            minStock: 20m,
            code: "P001",
            barcode: null,
            categoryId: null,
            description: null,
            updatedByUserId: 1
        );
        await repository.UpdateAsync(product);
        await context.SaveChangesAsync();

        // Assert
        var updated = await context.Products.FirstOrDefaultAsync(p => p.Id == product.Id);
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("Updated Name");
    }

    [Fact]
    public async Task SoftDeleteAsync_Product_SetsIsActiveToFalse()
    {
        // Arrange
        await using var context = CreateContext("ProductDb5");
        var repository = new GenericRepository<Product>(context);

        var product = Product.Create(name: "To Delete", retailPrice: 100m, wholesalePrice: 900m, purchasePrice: 50m, retailUnitId: 1, wholesaleUnitId: 2, conversionFactor: 10m);
        await repository.AddAsync(product);
        await context.SaveChangesAsync();

        // Act
        await repository.SoftDeleteAsync(product.Id);
        await context.SaveChangesAsync();

        // Assert
        var deleted = await context.Products.FirstOrDefaultAsync(p => p.Id == product.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task Query_Product_AllowsLinqOperations()
    {
        // Arrange
        await using var context = CreateContext("ProductDb6");
        var repository = new GenericRepository<Product>(context);

        var product1 = Product.Create(name: "Alpha", retailPrice: 100m, wholesalePrice: 900m, purchasePrice: 50m, retailUnitId: 1, wholesaleUnitId: 2, conversionFactor: 10m);
        var product2 = Product.Create(name: "Beta", retailPrice: 200m, wholesalePrice: 1800m, purchasePrice: 100m, retailUnitId: 1, wholesaleUnitId: 2, conversionFactor: 10m);
        var product3 = Product.Create(name: "Gamma", retailPrice: 300m, wholesalePrice: 2700m, purchasePrice: 150m, retailUnitId: 1, wholesaleUnitId: 2, conversionFactor: 10m);

        await repository.AddAsync(product1);
        await repository.AddAsync(product2);
        await repository.AddAsync(product3);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.Query()
            .Where(p => p.RetailPrice >= 150m)
            .ToListAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    #endregion

    #region Warehouse Repository Tests

    [Fact]
    public async Task AddAsync_Warehouse_AddsToDatabase()
    {
        // Arrange
        await using var context = CreateContext("WarehouseDb1");
        var repository = new GenericRepository<Warehouse>(context);

        var warehouse = Warehouse.Create(name: "Main Warehouse");

        // Act
        await repository.AddAsync(warehouse);
        await context.SaveChangesAsync();

        // Assert
        var saved = await context.Warehouses.FirstOrDefaultAsync(w => w.Name == "Main Warehouse");
        saved.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByIdAsync_Warehouse_ReturnsWarehouse()
    {
        // Arrange
        await using var context = CreateContext("WarehouseDb2");
        var repository = new GenericRepository<Warehouse>(context);

        var warehouse = Warehouse.Create(name: "Test Warehouse");
        await repository.AddAsync(warehouse);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetByIdAsync(warehouse.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test Warehouse");
    }

    [Fact]
    public async Task GetAllAsync_MultipleWarehouses_ReturnsAll()
    {
        // Arrange
        await using var context = CreateContext("WarehouseDb3");
        var repository = new GenericRepository<Warehouse>(context);

        var warehouse1 = Warehouse.Create(name: "Warehouse 1");
        var warehouse2 = Warehouse.Create(name: "Warehouse 2");

        await repository.AddAsync(warehouse1);
        await repository.AddAsync(warehouse2);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdateAsync_Warehouse_UpdatesInDatabase()
    {
        // Arrange
        await using var context = CreateContext("WarehouseDb4");
        var repository = new GenericRepository<Warehouse>(context);

        var warehouse = Warehouse.Create(name: "Original Name", location: "Old Location");
        warehouse.SetCreatedBy(1);
        await repository.AddAsync(warehouse);
        await context.SaveChangesAsync();

        // Act
        warehouse.Update(name: "Updated Warehouse", code: null, location: "New Location", isDefault: false, updatedByUserId: 1);
        await repository.UpdateAsync(warehouse);
        await context.SaveChangesAsync();

        // Assert
        var updated = await context.Warehouses.FirstOrDefaultAsync(w => w.Id == warehouse.Id);
        updated!.Name.Should().Be("Updated Warehouse");
        updated.Location.Should().Be("New Location");
    }

    [Fact]
    public async Task SoftDeleteAsync_Warehouse_SetsIsActiveToFalse()
    {
        // Arrange
        await using var context = CreateContext("WarehouseDb5");
        var repository = new GenericRepository<Warehouse>(context);

        var warehouse = Warehouse.Create(name: "To Delete");
        await repository.AddAsync(warehouse);
        await context.SaveChangesAsync();

        // Act
        await repository.SoftDeleteAsync(warehouse.Id);
        await context.SaveChangesAsync();

        // Assert
        var deleted = await context.Warehouses.FirstOrDefaultAsync(w => w.Id == warehouse.Id);
        deleted.Should().BeNull();
    }

    #endregion

    #region Supplier Repository Tests

    [Fact]
    public async Task AddAsync_Supplier_AddsToDatabase()
    {
        // Arrange
        await using var context = CreateContext("SupplierDb1");
        var repository = new GenericRepository<Supplier>(context);

        var supplier = Supplier.Create(
            name: "Test Supplier",
            phone: "0123456789",
            openingBalance: 500m
        );

        // Act
        await repository.AddAsync(supplier);
        await context.SaveChangesAsync();

        // Assert
        var saved = await context.Suppliers.FirstOrDefaultAsync(s => s.Name == "Test Supplier");
        saved.Should().NotBeNull();
        saved!.CurrentBalance.Should().Be(500m);
    }

    [Fact]
    public async Task GetByIdAsync_Supplier_ReturnsSupplier()
    {
        // Arrange
        await using var context = CreateContext("SupplierDb2");
        var repository = new GenericRepository<Supplier>(context);

        var supplier = Supplier.Create(name: "Supplier to Find");
        await repository.AddAsync(supplier);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetByIdAsync(supplier.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Supplier to Find");
    }

    [Fact]
    public async Task UpdateAsync_SupplierBalanceChanges_PersistsCorrectly()
    {
        // Arrange
        await using var context = CreateContext("SupplierDb3");
        var repository = new GenericRepository<Supplier>(context);

        var supplier = Supplier.Create(name: "Test Supplier", openingBalance: 1000m);
        await repository.AddAsync(supplier);
        await context.SaveChangesAsync();

        // Act
        supplier.IncreaseBalance(500m);
        await repository.UpdateAsync(supplier);
        await context.SaveChangesAsync();

        // Assert
        var updated = await context.Suppliers.FirstOrDefaultAsync(s => s.Id == supplier.Id);
        updated!.CurrentBalance.Should().Be(1500m);
    }

    #endregion

    #region Category Repository Tests

    [Fact]
    public async Task AddAsync_Category_AddsToDatabase()
    {
        // Arrange
        await using var context = CreateContext("CategoryDb1");
        var repository = new GenericRepository<Category>(context);

        var category = Category.Create(name: "Electronics");

        // Act
        await repository.AddAsync(category);
        await context.SaveChangesAsync();

        // Assert
        var saved = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Electronics");
        saved.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAllAsync_MultipleCategories_ReturnsAll()
    {
        // Arrange
        await using var context = CreateContext("CategoryDb2");
        var repository = new GenericRepository<Category>(context);

        var category1 = Category.Create(name: "Category 1");
        var category2 = Category.Create(name: "Category 2");

        await repository.AddAsync(category1);
        await repository.AddAsync(category2);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    #endregion

    #region Edge Cases and Error Handling

    [Fact]
    public async Task GetByIdAsync_NonExistentEntity_ReturnsNull()
    {
        // Arrange
        await using var context = CreateContext("EdgeDb1");
        var repository = new GenericRepository<Product>(context);

        // Act
        var result = await repository.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

[Fact]
    public async Task UpdateAsync_NonTrackedEntity_UpdatesSuccessfully()
    {
        // Arrange
        await using var context = CreateContext("EdgeDb2");
        var repository = new GenericRepository<Product>(context);

        var product = Product.Create(name: "Original", retailPrice: 100m, wholesalePrice: 900m, purchasePrice: 50m, retailUnitId: 1, wholesaleUnitId: 2, conversionFactor: 10m, code: "P001", minStock: 5m);
        await repository.AddAsync(product);
        await context.SaveChangesAsync();

        // Detach and re-attach
        context.ChangeTracker.Clear();

        // Act - now update using the Update method
        product.Update(
            name: "Updated",
            retailPrice: 120m,
            wholesalePrice: 1100m,
            purchasePrice: 60m,
            retailUnitId: 1,
            wholesaleUnitId: 2,
            conversionFactor: 10m,
            minStock: 10m,
            code: "P001",
            barcode: null,
            categoryId: null,
            description: null,
            updatedByUserId: 1
        );
        await repository.UpdateAsync(product);
        await context.SaveChangesAsync();

        // Assert
        var updated = await context.Products.FirstOrDefaultAsync(p => p.Id == product.Id);
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("Updated");
    }

    [Fact]
    public async Task SoftDeleteAsync_NonExistentId_DoesNotThrow()
    {
        // Arrange
        await using var context = CreateContext("EdgeDb3");
        var repository = new GenericRepository<Product>(context);

        // Act
        var act = async () => await repository.SoftDeleteAsync(999);

        // Assert
        await act.Should().NotThrowAsync();
    }

    #endregion
}
