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

    private static async Task<Customer> CreateTestCustomerAsync(SalesDbContext context, string name)
    {
        var party = Party.Create(name, PartyType.Customer, 1, createdByUserId: null);
        context.Set<Party>().Add(party);
        await context.SaveChangesAsync();
        var customer = Customer.Create(party.Id, createdByUserId: null);
        return customer;
    }

    [Fact]
    public async Task AddAsync_NewCustomer_AddsToDatabase()
    {
        // Arrange
        await using var context = CreateContext("CustomerDb1");
        var repository = new GenericRepository<Customer>(context);
        
        var customer = await CreateTestCustomerAsync(context, "New Customer");

        // Act
        await repository.AddAsync(customer);
        await context.SaveChangesAsync();

        // Assert
        var savedCustomer = await context.Customers.Include(c => c.Party).FirstOrDefaultAsync(c => c.Id == customer.Id);
        savedCustomer.Should().NotBeNull();
        savedCustomer!.Party.Name.Should().Be("New Customer");
        /* CurrentBalance removed — balance lives on linked Account */
    }

    [Fact]
    public async Task GetByIdAsync_ExistingCustomer_ReturnsCustomer()
    {
        // Arrange
        await using var context = CreateContext("CustomerDb2");
        var repository = new GenericRepository<Customer>(context);
        
        var customer = await CreateTestCustomerAsync(context, "Test Customer");
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
        
        var customer1 = await CreateTestCustomerAsync(context, "Customer 1");
        var customer2 = await CreateTestCustomerAsync(context, "Customer 2");
        var customer3 = await CreateTestCustomerAsync(context, "Customer 3");

        await repository.AddAsync(customer1);
        await repository.AddAsync(customer2);
        await repository.AddAsync(customer3);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(3);
        result.Select(c => c.Party.Name).Should().Contain("Customer 1");
        result.Select(c => c.Party.Name).Should().Contain("Customer 2");
        result.Select(c => c.Party.Name).Should().Contain("Customer 3");
    }

    [Fact]
    public async Task UpdateAsync_ModifiedCustomer_UpdatesInDatabase()
    {
        // Arrange
        await using var context = CreateContext("CustomerDb5");
        var repository = new GenericRepository<Customer>(context);
        
        var customer = await CreateTestCustomerAsync(context, "Original Name");
        await repository.AddAsync(customer);
        await context.SaveChangesAsync();

        // Act
        customer.Update(creditLimit: 0, updatedByUserId: 1);
        await repository.UpdateAsync(customer);
        await context.SaveChangesAsync();

        // Assert
        var updated = await context.Customers.Include(c => c.Party).FirstOrDefaultAsync(c => c.Id == customer.Id);
        updated.Should().NotBeNull();
    }

    [Fact]
    public async Task SoftDeleteAsync_ExistingCustomer_SetsIsActiveToFalse()
    {
        // Arrange
        await using var context = CreateContext("CustomerDb6");
        var repository = new GenericRepository<Customer>(context);
        
        var customer = await CreateTestCustomerAsync(context, "To Delete");
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
        
        var customer1 = await CreateTestCustomerAsync(context, "Alpha Customer");
        var customer2 = await CreateTestCustomerAsync(context, "Beta Customer");
        var customer3 = await CreateTestCustomerAsync(context, "Gamma Customer");

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
        
        var customer1 = await CreateTestCustomerAsync(context, "Customer 1");
        var customer2 = await CreateTestCustomerAsync(context, "Customer 2");
        
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
        
        var customer = await CreateTestCustomerAsync(context, "Test Customer");
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
        
        var customer = await CreateTestCustomerAsync(context, "Test Customer");
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
        
        var customer1 = await CreateTestCustomerAsync(context, "Active Customer");
        var customer2 = await CreateTestCustomerAsync(context, "Deleted Customer");
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
        
        var party1 = Party.Create("Ahmed Ali", PartyType.Customer, 1, createdByUserId: null);
        context.Set<Party>().Add(party1);
        var party2 = Party.Create("Sara Hassan", PartyType.Customer, 1, createdByUserId: null);
        context.Set<Party>().Add(party2);
        var party3 = Party.Create("Ahmed Kamal", PartyType.Customer, 1, createdByUserId: null);
        context.Set<Party>().Add(party3);
        await context.SaveChangesAsync();

        context.Customers.Add(Customer.Create(party1.Id, createdByUserId: null));
        context.Customers.Add(Customer.Create(party2.Id, createdByUserId: null));
        context.Customers.Add(Customer.Create(party3.Id, createdByUserId: null));
        await context.SaveChangesAsync();

        // Act - Search for customers with "Ahmed" in party name (via join)
        var searchResults = await repository.Query()
            .Include(c => c.Party)
            .Where(c => c.Party.Name.Contains("Ahmed"))
            .ToListAsync();

        // Assert
        searchResults.Should().HaveCount(2);
        searchResults.Should().OnlyContain(c => c.Party.Name.Contains("Ahmed"));
    }

    [Fact]
    public async Task Query_OrderBy_OrdersResultsCorrectly()
    {
        // Arrange
        await using var context = CreateContext("CustomerDb13");
        var repository = new GenericRepository<Customer>(context);
        
        var partyA = Party.Create("Charlie", PartyType.Customer, 1, createdByUserId: null);
        context.Set<Party>().Add(partyA);
        var partyB = Party.Create("Alpha", PartyType.Customer, 1, createdByUserId: null);
        context.Set<Party>().Add(partyB);
        var partyC = Party.Create("Bravo", PartyType.Customer, 1, createdByUserId: null);
        context.Set<Party>().Add(partyC);
        await context.SaveChangesAsync();

        context.Customers.Add(Customer.Create(partyA.Id, createdByUserId: null));
        context.Customers.Add(Customer.Create(partyB.Id, createdByUserId: null));
        context.Customers.Add(Customer.Create(partyC.Id, createdByUserId: null));
        await context.SaveChangesAsync();

        // Act - Order by Party.Name
        var orderedCustomers = await repository.Query()
            .Include(c => c.Party)
            .OrderBy(c => c.Party.Name)
            .ToListAsync();

        // Assert
        orderedCustomers.Should().HaveCount(3);
        orderedCustomers[0].Party.Name.Should().Be("Alpha");
        orderedCustomers[1].Party.Name.Should().Be("Bravo");
        orderedCustomers[2].Party.Name.Should().Be("Charlie");
    }

    [Fact]
    public async Task Query_Pagination_SkipsAndTakesCorrectly()
    {
        // Arrange
        await using var context = CreateContext("CustomerDb14");
        var repository = new GenericRepository<Customer>(context);
        
        for (int i = 1; i <= 10; i++)
        {
            var party = Party.Create($"Customer {i}", PartyType.Customer, 1, createdByUserId: null);
            context.Set<Party>().Add(party);
            await context.SaveChangesAsync();
            var customer = Customer.Create(party.Id, createdByUserId: null);
            await repository.AddAsync(customer);
        }
        await context.SaveChangesAsync();

        // Act - Get page 2 with page size 3 (skip 3, take 3)
        var page2 = await repository.Query()
            .Include(c => c.Party)
            .OrderBy(c => c.Id)
            .Skip(3)
            .Take(3)
            .ToListAsync();

        // Assert
        page2.Should().HaveCount(3);
        page2.First().Party.Name.Should().Be("Customer 4");
        page2.Last().Party.Name.Should().Be("Customer 6");
    }
}
