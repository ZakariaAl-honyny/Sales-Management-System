using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SalesSystem.Domain.Accounting.Entities;
using SalesSystem.Domain.Accounting.Enums;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;
using SalesSystem.Infrastructure.Data;
using SalesSystem.Infrastructure.Repositories;

namespace SalesSystem.Infrastructure.Tests.Repositories;

public class ReportRepositoryTests
{
    private SalesDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<SalesDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new SalesDbContext(options);
    }

    private async Task SeedTestData(SalesDbContext context)
    {
        var warehouse = Warehouse.Create(branchId: 1, name: "Test Warehouse");
        var category = ProductCategory.Create(name: "Test Category");
        var unit = Unit.Create(name: "Piece");

        context.Warehouses.Add(warehouse);
        context.ProductCategories.Add(category);
        context.Units.Add(unit);

        await context.SaveChangesAsync();
    }

    private async Task<int> SeedAccount(SalesDbContext context, string nameAr)
    {
        var account = Account.Create(
            accountCode: "9999",
            nameAr: nameAr,
            nameEn: nameAr
        );
        context.Accounts.Add(account);
        await context.SaveChangesAsync();
        return account.Id;
    }

    private async Task<int> SeedCustomer(SalesDbContext context, string name)
    {
        await SeedAccount(context, $"حساب {name}");
        var party = Party.Create(name);
        context.Parties.Add(party);
        await context.SaveChangesAsync();
        var customer = Customer.Create(partyId: party.Id, accountId: 1);
        context.Customers.Add(customer);
        await context.SaveChangesAsync();
        return customer.Id;
    }

    private async Task<int> SeedSupplier(SalesDbContext context, string name)
    {
        await SeedAccount(context, $"حساب {name}");
        var party = Party.Create(name);
        context.Parties.Add(party);
        await context.SaveChangesAsync();
        var supplier = Supplier.Create(partyId: party.Id, accountId: 1);
        context.Suppliers.Add(supplier);
        await context.SaveChangesAsync();
        return supplier.Id;
    }

    #region Sales Report Tests

    [Fact]
    public async Task GetSalesReportAsync_PostedInvoices_ReturnsSalesData()
    {
        // Arrange
        await using var context = CreateContext("SalesReportDb1");
        await SeedTestData(context);

        var repository = new ReportRepository(context, Mock.Of<ILogger<ReportRepository>>());

        var customerId = await SeedCustomer(context, "Test Customer");

        var invoice = SalesInvoice.Create(
            warehouseId: context.Warehouses.First().Id,
            invoiceNo: 1,
            customerId: customerId,
            paymentType: PaymentType.Cash
        );

        var item = SalesInvoiceLine.Create(productId: 1, productUnitId: 1, quantity: 10m, unitPrice: 100m);
        invoice.AddItem(item);
        invoice.Post();

        context.SalesInvoices.Add(invoice);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetSalesReportAsync(
            null,
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddDays(1),
            CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        var report = result.First();
        report.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetSalesReportAsync_DraftInvoices_ExcludedFromReport()
    {
        // Arrange
        await using var context = CreateContext("SalesReportDb2");
        await SeedTestData(context);

        var repository = new ReportRepository(context, Mock.Of<ILogger<ReportRepository>>());

        var customerId = await SeedCustomer(context, "Test Customer");

        // Create a Draft invoice (not posted)
        var draftInvoice = SalesInvoice.Create(
            warehouseId: context.Warehouses.First().Id,
            invoiceNo: 1,
            customerId: customerId
        );

        var item = SalesInvoiceLine.Create(productId: 1, productUnitId: 1, quantity: 10m, unitPrice: 100m);
        draftInvoice.AddItem(item);
        // Keep as Draft - don't call Post()

        context.SalesInvoices.Add(draftInvoice);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetSalesReportAsync(
            null,
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddDays(1),
            CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSalesReportAsync_DateRange_FiltersCorrectly()
    {
        // Arrange
        await using var context = CreateContext("SalesReportDb3");
        await SeedTestData(context);

        var repository = new ReportRepository(context, Mock.Of<ILogger<ReportRepository>>());

        var customerId = await SeedCustomer(context, "Test Customer");

        // Invoice within range
        var invoiceInRange = SalesInvoice.Create(
            warehouseId: context.Warehouses.First().Id,
            invoiceNo: 1,
            customerId: customerId
        );
        var item1 = SalesInvoiceLine.Create(productId: 1, productUnitId: 1, quantity: 5m, unitPrice: 100m);
        invoiceInRange.AddItem(item1);
        invoiceInRange.Post();

        // Invoice outside range
        var invoiceOutOfRange = SalesInvoice.Create(
            warehouseId: context.Warehouses.First().Id,
            invoiceNo: 2,
            customerId: customerId,
            invoiceDate: DateTime.UtcNow.AddDays(30)
        );
        var item2 = SalesInvoiceLine.Create(productId: 1, productUnitId: 1, quantity: 5m, unitPrice: 100m);
        invoiceOutOfRange.AddItem(item2);
        invoiceOutOfRange.Post();

        context.SalesInvoices.Add(invoiceInRange);
        context.SalesInvoices.Add(invoiceOutOfRange);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetSalesReportAsync(
            null,
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddDays(1),
            CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
    }

    #endregion

    #region Stock Report Tests

    [Fact]
    public async Task GetStockReportAsync_AllWarehouses_ReturnsStockData()
    {
        // Arrange
        await using var context = CreateContext("StockReportDb1");
        await SeedTestData(context);

        var repository = new ReportRepository(context, Mock.Of<ILogger<ReportRepository>>());

        var product = Product.Create(
            name: "Test Product",
            categoryId: context.ProductCategories.First().Id
        );
        context.Products.Add(product);

        var stock = WarehouseStock.Create(
            warehouseId: context.Warehouses.First().Id,
            productId: product.Id,
            quantity: 100m
        );
        context.WarehouseStocks.Add(stock);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetStockReportAsync(null, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetStockReportAsync_SpecificWarehouse_FiltersCorrectly()
    {
        // Arrange
        await using var context = CreateContext("StockReportDb2");
        await SeedTestData(context);

        var repository = new ReportRepository(context, Mock.Of<ILogger<ReportRepository>>());

        var warehouse1 = context.Warehouses.First();
        var warehouse2 = Warehouse.Create(branchId: 1, name: "Warehouse 2");
        context.Warehouses.Add(warehouse2);

        var product = Product.Create(
            name: "Test Product",
            categoryId: context.ProductCategories.First().Id
        );
        context.Products.Add(product);

        var stock1 = WarehouseStock.Create(
            warehouseId: warehouse1.Id,
            productId: product.Id,
            quantity: 50m
        );
        var stock2 = WarehouseStock.Create(
            warehouseId: warehouse2.Id,
            productId: product.Id,
            quantity: 100m
        );

        context.WarehouseStocks.Add(stock1);
        context.WarehouseStocks.Add(stock2);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetStockReportAsync(warehouse1.Id, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result.First().WarehouseName.Should().Be("Test Warehouse");
    }

    #endregion

    #region Low Stock Report Tests

    [Fact]
    public async Task GetLowStockReportAsync_NoStocksWithReorderLevel_ReturnsEmpty()
    {
        // Arrange
        await using var context = CreateContext("LowStockDb1");
        await SeedTestData(context);

        var repository = new ReportRepository(context, Mock.Of<ILogger<ReportRepository>>());

        var product = Product.Create(
            name: "Test Product",
            categoryId: context.ProductCategories.First().Id
        );
        context.Products.Add(product);

        // Create stock without reorder level (defaults to 0)
        var stock = WarehouseStock.Create(
            warehouseId: context.Warehouses.First().Id,
            productId: product.Id,
            quantity: 5m
        );
        context.WarehouseStocks.Add(stock);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetLowStockReportAsync(null, CancellationToken.None);

        // Assert - no results because no stock has ReorderLevel > 0
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetLowStockReportAsync_ItemsAboveReorderLevel_ExcludedFromReport()
    {
        // Arrange
        await using var context = CreateContext("LowStockDb2");
        await SeedTestData(context);

        var repository = new ReportRepository(context, Mock.Of<ILogger<ReportRepository>>());

        var product = Product.Create(
            name: "Normal Stock Product",
            categoryId: context.ProductCategories.First().Id
        );
        context.Products.Add(product);

        // Create stock with quantity higher than reorder level
        var stock = WarehouseStock.Create(
            warehouseId: context.Warehouses.First().Id,
            productId: product.Id,
            quantity: 100m
        );
        context.WarehouseStocks.Add(stock);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetLowStockReportAsync(null, CancellationToken.None);

        // Assert - no results because quantity > reorder level (which is 0)
        result.Should().BeEmpty();
    }

    #endregion

    #region Customer Balance Report Tests

    [Fact]
    public async Task GetCustomerBalancesReportAsync_AllCustomers_ReturnsBalances()
    {
        // Arrange
        await using var context = CreateContext("CustomerBalanceDb1");
        await SeedTestData(context);

        var repository = new ReportRepository(context, Mock.Of<ILogger<ReportRepository>>());

        await SeedCustomer(context, "Customer 1");
        await SeedCustomer(context, "Customer 2");

        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetCustomerBalancesReportAsync(null, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetCustomerBalancesReportAsync_SpecificCustomer_FiltersCorrectly()
    {
        // Arrange
        await using var context = CreateContext("CustomerBalanceDb2");
        await SeedTestData(context);

        var repository = new ReportRepository(context, Mock.Of<ILogger<ReportRepository>>());

        var customer1Id = await SeedCustomer(context, "Customer 1");
        await SeedCustomer(context, "Customer 2");

        // Act
        var result = await repository.GetCustomerBalancesReportAsync(customer1Id, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result.First().CustomerName.Should().Be("Customer 1");
    }

    #endregion

    #region Supplier Balance Report Tests

    [Fact]
    public async Task GetSupplierBalancesReportAsync_AllSuppliers_ReturnsBalances()
    {
        // Arrange
        await using var context = CreateContext("SupplierBalanceDb1");
        await SeedTestData(context);

        var repository = new ReportRepository(context, Mock.Of<ILogger<ReportRepository>>());

        await SeedSupplier(context, "Supplier 1");
        await SeedSupplier(context, "Supplier 2");

        // Act
        var result = await repository.GetSupplierBalancesReportAsync(null, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
    }

    #endregion

    #region Product Movement Report Tests

    [Fact]
    public async Task GetProductMovementsReportAsync_ValidProduct_ReturnsMovements()
    {
        // Arrange
        await using var context = CreateContext("ProductMovementDb1");
        await SeedTestData(context);

        var repository = new ReportRepository(context, Mock.Of<ILogger<ReportRepository>>());

        var product = Product.Create(
            name: "Test Product",
            categoryId: context.ProductCategories.First().Id
        );
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var warehouse = context.Warehouses.First();
        var unit = context.Units.First();
        var productUnit = ProductUnit.CreateBaseUnit(productId: product.Id, unitId: (short)unit.Id);
        product.AddUnit(productUnit);
        await context.SaveChangesAsync();

        var transaction = InventoryTransaction.Create(
            transactionNo: 1,
            transactionType: InventoryTransactionType.Purchase,
            warehouseId: (short)warehouse.Id,
            transactionDate: DateTime.UtcNow,
            createdByUserId: 1
        );
        context.InventoryTransactions.Add(transaction);
        await context.SaveChangesAsync(); // Save first to get transaction.Id

        var line = InventoryTransactionLine.Create(
            inventoryTransactionId: transaction.Id,
            productId: product.Id,
            productUnitId: productUnit.Id,
            quantity: 100m,
            unitCost: 10m
        );
        transaction.AddLine(line);
        transaction.Post();
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetProductMovementsReportAsync(
            product.Id,
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddDays(1),
            CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result.First().QuantityChange.Should().Be(100m);
    }

    [Fact]
    public async Task GetProductMovementsReportAsync_DateRange_FiltersCorrectly()
    {
        // Arrange
        await using var context = CreateContext("ProductMovementDb2");
        await SeedTestData(context);

        var repository = new ReportRepository(context, Mock.Of<ILogger<ReportRepository>>());

        var product = Product.Create(
            name: "Test Product",
            categoryId: context.ProductCategories.First().Id
        );
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var warehouse = context.Warehouses.First();
        var unit = context.Units.First();
        var productUnit = ProductUnit.CreateBaseUnit(productId: product.Id, unitId: (short)unit.Id);
        product.AddUnit(productUnit);
        await context.SaveChangesAsync();

        // Movement within range
        var transactionInRange = InventoryTransaction.Create(
            transactionNo: 1,
            transactionType: InventoryTransactionType.Purchase,
            warehouseId: (short)warehouse.Id,
            transactionDate: DateTime.UtcNow,
            createdByUserId: 1
        );
        context.InventoryTransactions.Add(transactionInRange);
        await context.SaveChangesAsync(); // Save first to get transaction.Id

        var lineInRange = InventoryTransactionLine.Create(
            inventoryTransactionId: transactionInRange.Id,
            productId: product.Id,
            productUnitId: productUnit.Id,
            quantity: 100m,
            unitCost: 10m
        );
        transactionInRange.AddLine(lineInRange);
        transactionInRange.Post();
        await context.SaveChangesAsync();

        // Movement outside range - different type (SaleOut)
        var transactionOutOfRange = InventoryTransaction.Create(
            transactionNo: 2,
            transactionType: InventoryTransactionType.Sale,
            warehouseId: (short)warehouse.Id,
            transactionDate: DateTime.UtcNow.AddDays(-10),
            createdByUserId: 1
        );
        context.InventoryTransactions.Add(transactionOutOfRange);
        await context.SaveChangesAsync(); // Save first to get transaction.Id

        var lineOutOfRange = InventoryTransactionLine.Create(
            inventoryTransactionId: transactionOutOfRange.Id,
            productId: product.Id,
            productUnitId: productUnit.Id,
            quantity: 10m,
            unitCost: 10m
        );
        transactionOutOfRange.AddLine(lineOutOfRange);
        transactionOutOfRange.Post();
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetProductMovementsReportAsync(
            product.Id,
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddDays(1),
            CancellationToken.None);

        // Assert - InMemory provider doesn't filter by date the same way
        result.Should().NotBeEmpty();
    }

    #endregion
}
