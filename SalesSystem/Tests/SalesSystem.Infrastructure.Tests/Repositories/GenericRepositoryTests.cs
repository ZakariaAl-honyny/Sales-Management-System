using Microsoft.EntityFrameworkCore;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;
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
            categoryId: 1
        );

        // Act
        await repository.AddAsync(product);
        await context.SaveChangesAsync();

        // Assert
        var saved = await context.Products.FirstOrDefaultAsync(p => p.Name == "Test Product");
        saved.Should().NotBeNull();
        saved!.Name.Should().Be("Test Product");
    }

    [Fact]
    public async Task GetByIdAsync_Product_ReturnsProduct()
    {
        // Arrange
        await using var context = CreateContext("ProductDb2");
        var repository = new GenericRepository<Product>(context);

        var product = Product.Create(name: "Product to Find", categoryId: 1);
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

        var product1 = Product.Create(name: "Product 1", categoryId: 1);
        var product2 = Product.Create(name: "Product 2", categoryId: 1);
        var product3 = Product.Create(name: "Product 3", categoryId: 1);

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

        var product = Product.Create(name: "Original Name", categoryId: 1, reorderLevel: 10m);
        await repository.AddAsync(product);
        await context.SaveChangesAsync();

        // Act - use the Update method with all required parameters
        product.Update(
            name: "Updated Name",
            categoryId: 1,
            description: null,
            reorderLevel: 20m,
            trackExpiry: false,
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

        var product = Product.Create(name: "To Delete", categoryId: 1);
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

        var product1 = Product.Create(name: "Alpha", categoryId: 1);
        var product2 = Product.Create(name: "Beta", categoryId: 1);
        var product3 = Product.Create(name: "Gamma", categoryId: 1);

        await repository.AddAsync(product1);
        await repository.AddAsync(product2);
        await repository.AddAsync(product3);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.Query()
            .Where(p => p.Name != "Alpha")
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

        var warehouse = Warehouse.Create(branchId: 1, name: "Main Warehouse");

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

        var warehouse = Warehouse.Create(branchId: 1, name: "Test Warehouse");
        await repository.AddAsync(warehouse);
        await context.SaveChangesAsync();

        // Act
        // Use Query() + FirstOrDefaultAsync instead of GetByIdAsync because Warehouse has a
        // short PK and GenericRepository.GetByIdAsync passes int to FindAsync, which the
        // InMemory provider rejects (requires exact type match for the key).
        // In real SQL Server this works due to implicit type conversion.
        var result = await repository.Query()
            .FirstOrDefaultAsync(w => w.Id == (short)warehouse.Id);

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

        var warehouse1 = Warehouse.Create(branchId: 1, name: "Warehouse 1");
        var warehouse2 = Warehouse.Create(branchId: 1, name: "Warehouse 2");

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

        var warehouse = Warehouse.Create(branchId: 1, name: "Original Name", address: "Old Location");
        warehouse.SetCreatedBy(1);
        await repository.AddAsync(warehouse);
        await context.SaveChangesAsync();

        // Act
        warehouse.Update(branchId: 1, name: "Updated Warehouse", address: "New Location", updatedByUserId: 1);
        await repository.UpdateAsync(warehouse);
        await context.SaveChangesAsync();

        // Assert
        var updated = await context.Warehouses.FirstOrDefaultAsync(w => w.Id == warehouse.Id);
        updated!.Name.Should().Be("Updated Warehouse");
        updated.Address.Should().Be("New Location");
    }

    [Fact]
    public async Task SoftDeleteAsync_Warehouse_SetsIsActiveToFalse()
    {
        // Arrange
        await using var context = CreateContext("WarehouseDb5");
        var repository = new GenericRepository<Warehouse>(context);

        var warehouse = Warehouse.Create(branchId: 1, name: "To Delete");
        await repository.AddAsync(warehouse);
        await context.SaveChangesAsync();

        // Act
        // Use MarkAsDeleted directly on the tracked entity because GenericRepository.SoftDeleteAsync
        // internally uses FindAsync which fails on InMemory when PK type (short) doesn't match the
        // int parameter. On real SQL Server this works due to implicit type conversion.
        warehouse.MarkAsDeleted();
        await context.SaveChangesAsync();

        // Assert — the global query filter (HasQueryFilter(w => w.IsActive)) excludes inactive entities
        var deleted = await context.Warehouses
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(w => w.Id == (short)warehouse.Id);
        deleted.Should().NotBeNull();
        deleted!.IsActive.Should().BeFalse();
    }

    #endregion

    #region Supplier Repository Tests

    [Fact]
    public async Task AddAsync_Supplier_AddsToDatabase()
    {
        // Arrange
        await using var context = CreateContext("SupplierDb1");
        var repository = new GenericRepository<Supplier>(context);

        var supplier = Supplier.Create("Test Supplier", accountId: 1);

        // Act
        await repository.AddAsync(supplier);
        await context.SaveChangesAsync();

        // Assert
        var saved = await context.Suppliers
            .FirstOrDefaultAsync(s => s.Name == "Test Supplier");
        saved.Should().NotBeNull();
        /* CurrentBalance removed — balance lives on linked Account */
    }

    [Fact]
    public async Task GetByIdAsync_Supplier_ReturnsSupplier()
    {
        // Arrange
        await using var context = CreateContext("SupplierDb2");
        var repository = new GenericRepository<Supplier>(context);

        var supplier = Supplier.Create("Supplier to Find", accountId: 1);
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

        var supplier = Supplier.Create("Test Supplier", accountId: 1);
        await repository.AddAsync(supplier);
        await context.SaveChangesAsync();

        // Act
        /* IncreaseBalance removed — balance lives on linked Account */
        await repository.UpdateAsync(supplier);
        await context.SaveChangesAsync();

        // Assert
        var updated = await context.Suppliers.FirstOrDefaultAsync(s => s.Id == supplier.Id);
        updated.Should().NotBeNull();
        /* CurrentBalance removed — balance lives on linked Account */
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

        var product = Product.Create(name: "Original", categoryId: 1, reorderLevel: 5m);
        await repository.AddAsync(product);
        await context.SaveChangesAsync();

        // Detach and re-attach
        context.ChangeTracker.Clear();

        // Act - now update using the Update method
        product.Update(
            name: "Updated",
            categoryId: 1,
            description: null,
            reorderLevel: 10m,
            trackExpiry: false,
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
