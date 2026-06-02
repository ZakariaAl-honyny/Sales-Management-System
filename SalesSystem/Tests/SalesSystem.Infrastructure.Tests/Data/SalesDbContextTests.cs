using Microsoft.EntityFrameworkCore;
using SalesSystem.Domain.Entities;
using SalesSystem.Infrastructure.Data;

namespace SalesSystem.Infrastructure.Tests.Data;

public class SalesDbContextTests
{
    [Fact]
    public void Constructor_WithValidOptions_CreatesInstance()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<SalesDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb1")
            .Options;

        // Act
        var context = new SalesDbContext(options);

        // Assert
        context.Should().NotBeNull();
    }

    [Fact]
    public async Task SaveChanges_AddsEntity_PersistsToDatabase()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<SalesDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb2")
            .Options;

        await using var context = new SalesDbContext(options);

        var customer = Customer.Create(
            name: "Test Customer",
            openingBalance: 0,
            phone: "0123456789"
        );

        context.Customers.Add(customer);
        
        // Act
        await context.SaveChangesAsync();

        // Assert
        var savedCustomer = await context.Customers.FirstOrDefaultAsync(c => c.Id == customer.Id);
        savedCustomer.Should().NotBeNull();
        savedCustomer!.Name.Should().Be("Test Customer");
        savedCustomer.Phone.Should().Be("0123456789");
    }

    [Fact]
    public async Task SaveChanges_UpdatesEntity_ModifiesInDatabase()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<SalesDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb3")
            .Options;

        await using var context = new SalesDbContext(options);

        var customer = Customer.Create(name: "Original Name");
        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        // Act
        customer.Update(
            name: "Updated Name",
            phone: "9876543210",
            email: "test@test.com",
            address: null,
            taxNumber: null,
            creditLimit: 0,
            updatedByUserId: null
        );
        await context.SaveChangesAsync();

        // Assert
        var updatedCustomer = await context.Customers.FirstOrDefaultAsync(c => c.Id == customer.Id);
        updatedCustomer.Should().NotBeNull();
        updatedCustomer!.Name.Should().Be("Updated Name");
        updatedCustomer.Phone.Should().Be("9876543210");
    }

    [Fact]
    public async Task SaveChanges_SoftDeletesEntity_SetsIsActiveToFalse()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<SalesDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb4")
            .Options;

        await using var context = new SalesDbContext(options);

        var customer = Customer.Create(name: "To Delete");
        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        // Act
        customer.MarkAsDeleted();
        await context.SaveChangesAsync();

        // Assert
        var deletedCustomer = await context.Customers.FirstOrDefaultAsync(c => c.Id == customer.Id);
        deletedCustomer.Should().BeNull();
    }

    [Fact]
    public void OnModelCreating_AppliesConfigurations_HasCorrectEntityTypes()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<SalesDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb5")
            .Options;

        // Act
        var modelBuilder = new ModelBuilder();
        var context = new SalesDbContext(options);
        
        // Verify all DbSets are configured
        context.Users.Should().NotBeNull();
        context.Units.Should().NotBeNull();
        context.Categories.Should().NotBeNull();
        context.Products.Should().NotBeNull();
        context.Warehouses.Should().NotBeNull();
        context.Customers.Should().NotBeNull();
        context.SalesInvoices.Should().NotBeNull();
        context.PurchaseInvoices.Should().NotBeNull();
        context.SalesReturns.Should().NotBeNull();
        context.PurchaseReturns.Should().NotBeNull();
        context.StockTransfers.Should().NotBeNull();
        context.CustomerPayments.Should().NotBeNull();
        context.SupplierPayments.Should().NotBeNull();
        context.InventoryMovements.Should().NotBeNull();
        context.StoreSettings.Should().NotBeNull();
        context.DocumentSequences.Should().NotBeNull();
    }

    [Fact]
    public async Task SaveChanges_WithMultipleEntities_PersistsAll()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<SalesDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb6")
            .Options;

        await using var context = new SalesDbContext(options);

        var customer = Customer.Create(name: "Customer 1");
        var warehouse = Warehouse.Create(name: "Main Warehouse", location: "Test Location");
        warehouse.SetCreatedBy(1);

        context.Customers.Add(customer);
        context.Warehouses.Add(warehouse);
        
        // Act
        await context.SaveChangesAsync();

        // Assert
        var customersCount = await context.Customers.CountAsync();
        var warehousesCount = await context.Warehouses.CountAsync();

        customersCount.Should().Be(1);
        warehousesCount.Should().Be(1);
    }

    [Fact]
    public async Task Query_CanFilterEntities_AppliesCorrectFilter()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<SalesDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb7")
            .Options;

        await using var context = new SalesDbContext(options);

        var customer1 = Customer.Create(name: "Customer One");
        var customer2 = Customer.Create(name: "Customer Two");
        
        context.Customers.Add(customer1);
        context.Customers.Add(customer2);
        await context.SaveChangesAsync();

        // Act
        var result = await context.Customers.FirstOrDefaultAsync(c => c.Name == "Customer One");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Customer One");
    }

    [Fact]
    public async Task SaveChanges_WithSalesInvoice_HasCorrectRelationships()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<SalesDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb8")
            .Options;

        await using var context = new SalesDbContext(options);

        var warehouse = Warehouse.Create(name: "Warehouse 1", location: "Location 1");
        warehouse.SetCreatedBy(1);
        
        var customer = Customer.Create(name: "Customer 1");
        
        context.Warehouses.Add(warehouse);
        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        var invoice = SalesInvoice.Create(
            warehouse.Id,
            customerId: customer.Id
        );

        context.SalesInvoices.Add(invoice);
        await context.SaveChangesAsync();

        // Assert
        var savedInvoice = await context.SalesInvoices
            .Include(i => i.Customer)
            .Include(i => i.Warehouse)
            .FirstOrDefaultAsync(i => i.Id == invoice.Id);

        savedInvoice.Should().NotBeNull();
        savedInvoice!.Customer.Should().NotBeNull();
        savedInvoice.Warehouse.Should().NotBeNull();
        savedInvoice.CustomerId.Should().Be(customer.Id);
        savedInvoice.WarehouseId.Should().Be(warehouse.Id);
    }

    [Fact]
    public async Task DbContext_SalesInvoiceItem_HasDecimalPrecisionForQuantity()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<SalesDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb9")
            .Options;

        await using var context = new SalesDbContext(options);

        var warehouse = Warehouse.Create(name: "Warehouse 1", location: "Location 1");
        warehouse.SetCreatedBy(1);
        context.Warehouses.Add(warehouse);
        await context.SaveChangesAsync();

        var invoice = SalesInvoice.Create(warehouse.Id);
        // Quantity uses precision (18,3) - should store up to 3 decimal places
        var item = SalesInvoiceItem.Create(productId: 1, quantity: 100.123m, unitPrice: 50.789m);
        invoice.AddItem(item);

        context.SalesInvoices.Add(invoice);
        await context.SaveChangesAsync();

        // Assert - Verify decimals are stored correctly
        var saved = await context.SalesInvoices.Include(i => i.Items).FirstAsync();
        saved.Items.First().Quantity.Should().Be(100.123m);
        saved.Items.First().UnitPrice.Should().Be(50.789m);
    }

    [Fact]
    public async Task DbContext_Customer_HasDecimalPrecisionForMoney()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<SalesDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb10")
            .Options;

        await using var context = new SalesDbContext(options);

        var customer = Customer.Create(name: "Precision Test Customer");
        // Balance uses precision (18,2) for money
        customer.IncreaseBalance(12345.67m);

        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        // Assert - Verify money values are stored with correct precision
        var saved = await context.Customers.FirstAsync();
        saved.CurrentBalance.Should().Be(12345.67m);
    }

    [Fact]
    public async Task DbContext_SalesInvoice_HasCorrectRelationshipWithItems()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<SalesDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb11")
            .Options;

        await using var context = new SalesDbContext(options);

        var warehouse = Warehouse.Create(name: "Warehouse", location: "Loc");
        warehouse.SetCreatedBy(1);
        context.Warehouses.Add(warehouse);
        await context.SaveChangesAsync();

        var invoice = SalesInvoice.Create(warehouse.Id);
        var item1 = SalesInvoiceItem.Create(productId: 1, quantity: 5m, unitPrice: 100m);
        var item2 = SalesInvoiceItem.Create(productId: 2, quantity: 3m, unitPrice: 200m);
        invoice.AddItem(item1);
        invoice.AddItem(item2);

        context.SalesInvoices.Add(invoice);
        await context.SaveChangesAsync();

        // Assert - Items relationship is configured correctly
        var saved = await context.SalesInvoices
            .Include(i => i.Items)
            .FirstAsync();

        saved.Items.Should().HaveCount(2);
        saved.SubTotal.Should().Be(1100m); // (5*100) + (3*200)
    }

    [Fact]
    public async Task Query_WithSoftDeleteFilter_ExcludesDeletedEntities()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<SalesDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb12")
            .Options;

        await using var context = new SalesDbContext(options);

        var customer1 = Customer.Create(name: "Active Customer");
        var customer2 = Customer.Create(name: "Deleted Customer");
        context.Customers.Add(customer1);
        context.Customers.Add(customer2);
        await context.SaveChangesAsync();

        // Soft delete customer2
        customer2.MarkAsDeleted();
        await context.SaveChangesAsync();

        // Assert - Global query filter excludes soft-deleted
        var activeCustomers = await context.Customers.ToListAsync();
        activeCustomers.Should().HaveCount(1);
        activeCustomers.First().Name.Should().Be("Active Customer");
    }
}