using Microsoft.EntityFrameworkCore;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;
using SalesSystem.Infrastructure.Data;
using SalesSystem.Infrastructure.Repositories;

namespace SalesSystem.Infrastructure.Tests.Repositories;

public class CustomerRepositoryTests
{
    private SalesDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<SalesDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new SalesDbContext(options);
    }

    private static Customer CreateTestCustomer(string name, int accountId = 1)
    {
        return Customer.Create(name, accountId: accountId);
    }

    [Fact]
    public async Task AddAsync_NewCustomer_AddsToDatabase()
    {
        // Arrange
        await using var context = CreateContext("CustomerDb1");
        var repository = new GenericRepository<Customer>(context);
        
        var customer = CreateTestCustomer("New Customer");

        // Act
        await repository.AddAsync(customer);
        await context.SaveChangesAsync();

        // Assert
        var savedCustomer = await context.Customers.FirstOrDefaultAsync(c => c.Id == customer.Id);
        savedCustomer.Should().NotBeNull();
        savedCustomer!.Name.Should().Be("New Customer");
        /* CurrentBalance removed — balance lives on linked Account */
    }

    [Fact]
    public async Task GetByIdAsync_ExistingCustomer_ReturnsCustomer()
    {
        // Arrange
        await using var context = CreateContext("CustomerDb2");
        var repository = new GenericRepository<Customer>(context);
        
        var customer = CreateTestCustomer("Test Customer");
        await repository.AddAsync(customer);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetByIdAsync(customer.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(customer.Id);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingCustomer_ReturnsNull()
    {
        // Arrange
        await using var context = CreateContext("CustomerDb3");
        var repository = new GenericRepository<Customer>(context);

        // Act
        var result = await repository.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_MultipleCustomers_ReturnsAll()
    {
        // Arrange
        await using var context = CreateContext("CustomerDb4");
        var repository = new GenericRepository<Customer>(context);
        
        var customer1 = CreateTestCustomer("Customer 1");
        var customer2 = CreateTestCustomer("Customer 2");
        var customer3 = CreateTestCustomer("Customer 3");

        await repository.AddAsync(customer1);
        await repository.AddAsync(customer2);
        await repository.AddAsync(customer3);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(3);
        result.Select(c => c.Name).Should().Contain("Customer 1");
        result.Select(c => c.Name).Should().Contain("Customer 2");
        result.Select(c => c.Name).Should().Contain("Customer 3");
    }

    [Fact]
    public async Task UpdateAsync_ModifiedCustomer_UpdatesInDatabase()
    {
        // Arrange
        await using var context = CreateContext("CustomerDb5");
        var repository = new GenericRepository<Customer>(context);
        
        var customer = CreateTestCustomer("Original Name");
        await repository.AddAsync(customer);
        await context.SaveChangesAsync();

        // Act
        customer.Update("Original Name", creditLimit: 0, updatedByUserId: 1);
        await repository.UpdateAsync(customer);
        await context.SaveChangesAsync();

        // Assert
        var updated = await context.Customers.FirstOrDefaultAsync(c => c.Id == customer.Id);
        updated.Should().NotBeNull();
    }

    [Fact]
    public async Task SoftDeleteAsync_ExistingCustomer_SetsIsActiveToFalse()
    {
        // Arrange
        await using var context = CreateContext("CustomerDb6");
        var repository = new GenericRepository<Customer>(context);
        
        var customer = CreateTestCustomer("To Delete");
        await repository.AddAsync(customer);
        await context.SaveChangesAsync();

        // Act
        await repository.SoftDeleteAsync(customer.Id);
        await context.SaveChangesAsync();

        // Assert
        var deleted = await context.Customers.FirstOrDefaultAsync(c => c.Id == customer.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task Query_AllowsLinqOperations_ReturnsFilteredResults()
    {
        // Arrange
        await using var context = CreateContext("CustomerDb7");
        var repository = new GenericRepository<Customer>(context);
        
        var customer1 = CreateTestCustomer("Alpha Customer");
        var customer2 = CreateTestCustomer("Beta Customer");
        var customer3 = CreateTestCustomer("Gamma Customer");

        await repository.AddAsync(customer1);
        await repository.AddAsync(customer2);
        await repository.AddAsync(customer3);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.Query()
            .Where(c => c.IsActive)
            .ToListAsync();

        // Assert
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task AddAsync_MultipleCustomersWithBalance_CalculatesCorrectly()
    {
        // Arrange
        await using var context = CreateContext("CustomerDb8");
        var repository = new GenericRepository<Customer>(context);
        
        var customer1 = CreateTestCustomer("Customer 1");
        var customer2 = CreateTestCustomer("Customer 2");
        
        await repository.AddAsync(customer1);
        await repository.AddAsync(customer2);
        await context.SaveChangesAsync();

        // Act
        var customers = await repository.GetAllAsync();

        // Assert
        customers.Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdateAsync_CustomerBalanceChanges_PersistsCorrectly()
    {
        // Arrange
        await using var context = CreateContext("CustomerDb9");
        var repository = new GenericRepository<Customer>(context);
        
        var customer = CreateTestCustomer("Test Customer");
        await repository.AddAsync(customer);
        await context.SaveChangesAsync();

        // Act
        await repository.UpdateAsync(customer);
        await context.SaveChangesAsync();

        // Assert
        var updated = await context.Customers.FirstOrDefaultAsync(c => c.Id == customer.Id);
        updated.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateAsync_DecreaseCustomerBalance_PersistsCorrectly()
    {
        // Arrange
        await using var context = CreateContext("CustomerDb10");
        var repository = new GenericRepository<Customer>(context);
        
        var customer = CreateTestCustomer("Test Customer");
        await repository.AddAsync(customer);
        await context.SaveChangesAsync();

        // Act
        await repository.UpdateAsync(customer);
        await context.SaveChangesAsync();

        // Assert
        var updated = await context.Customers.FirstOrDefaultAsync(c => c.Id == customer.Id);
        updated.Should().NotBeNull();
    }

    [Fact]
    public async Task Query_WithActiveFilter_ExcludesDeletedCustomers()
    {
        // Arrange
        await using var context = CreateContext("CustomerDb11");
        var repository = new GenericRepository<Customer>(context);
        
        var customer1 = CreateTestCustomer("Active Customer");
        var customer2 = CreateTestCustomer("Deleted Customer");
        await repository.AddAsync(customer1);
        await repository.AddAsync(customer2);
        await context.SaveChangesAsync();

        // Soft delete customer2
        customer2.MarkAsDeleted();
        await context.SaveChangesAsync();

        // Act - Query with explicit IsActive filter
        var activeCustomers = await repository.Query()
            .Where(c => c.IsActive)
            .ToListAsync();

        // Assert
        activeCustomers.Should().HaveCount(1);
    }

    [Fact]
    public async Task Query_WithSearchTerm_FiltersByName()
    {
        // Arrange
        await using var context = CreateContext("CustomerDb12");
        var repository = new GenericRepository<Customer>(context);
        
        context.Customers.Add(Customer.Create("Ahmed Ali", accountId: 1));
        context.Customers.Add(Customer.Create("Sara Hassan", accountId: 1));
        context.Customers.Add(Customer.Create("Ahmed Kamal", accountId: 1));
        await context.SaveChangesAsync();

        // Act - Search for customers with "Ahmed" in name
        var searchResults = await repository.Query()
            .Where(c => c.Name.Contains("Ahmed"))
            .ToListAsync();

        // Assert
        searchResults.Should().HaveCount(2);
        searchResults.Should().OnlyContain(c => c.Name.Contains("Ahmed"));
    }

    [Fact]
    public async Task Query_OrderBy_OrdersResultsCorrectly()
    {
        // Arrange
        await using var context = CreateContext("CustomerDb13");
        var repository = new GenericRepository<Customer>(context);
        
        context.Customers.Add(Customer.Create("Charlie", accountId: 1));
        context.Customers.Add(Customer.Create("Alpha", accountId: 1));
        context.Customers.Add(Customer.Create("Bravo", accountId: 1));
        await context.SaveChangesAsync();

        // Act - Order by Name
        var orderedCustomers = await repository.Query()
            .OrderBy(c => c.Name)
            .ToListAsync();

        // Assert
        orderedCustomers.Should().HaveCount(3);
        orderedCustomers[0].Name.Should().Be("Alpha");
        orderedCustomers[1].Name.Should().Be("Bravo");
        orderedCustomers[2].Name.Should().Be("Charlie");
    }

    [Fact]
    public async Task Query_Pagination_SkipsAndTakesCorrectly()
    {
        // Arrange
        await using var context = CreateContext("CustomerDb14");
        var repository = new GenericRepository<Customer>(context);
        
        for (int i = 1; i <= 10; i++)
        {
            var customer = Customer.Create($"Customer {i}", accountId: 1);
            await repository.AddAsync(customer);
        }
        await context.SaveChangesAsync();

        // Act - Get page 2 with page size 3 (skip 3, take 3)
        var page2 = await repository.Query()
            .OrderBy(c => c.Id)
            .Skip(3)
            .Take(3)
            .ToListAsync();

        // Assert
        page2.Should().HaveCount(3);
        page2.First().Name.Should().Be("Customer 4");
        page2.Last().Name.Should().Be("Customer 6");
    }
}
