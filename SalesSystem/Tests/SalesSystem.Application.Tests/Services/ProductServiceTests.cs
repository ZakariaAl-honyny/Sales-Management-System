using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Repositories;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Application.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Domain.Common;
using SalesSystem.Domain.Entities;
using Xunit.Abstractions;

namespace SalesSystem.Application.Tests.Services;

/// <summary>
/// Unit tests for ProductService business logic.
/// </summary>
public class ProductServiceTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly TestDbContext _dbContext;
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<ILogger<ProductService>> _mockLogger;

    private readonly ProductService _sut;

    public ProductServiceTests(ITestOutputHelper output)
    {
        _output = output;
        _output.WriteLine("[TEST] ProductServiceTests initialized");

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new TestDbContext(options);

        _mockUow = new Mock<IUnitOfWork>();
        _mockLogger = new Mock<ILogger<ProductService>>();

        _mockUow.Setup(u => u.Products).Returns(new InMemoryEfCoreRepository<Product>(_dbContext));
        _mockUow.Setup(u => u.Categories).Returns(new InMemoryEfCoreRepository<Category>(_dbContext));

        _mockUow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                await _dbContext.SaveChangesAsync();
                return 1;
            });

        _sut = new ProductService(_mockUow.Object, _mockLogger.Object);
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingProduct_ReturnsDto()
    {
        _output.WriteLine("[TEST] GetByIdAsync_ExistingProduct_ReturnsDto");

        var product = Product.Create("Test Product", purchasePrice: 10m, salePrice: 100m, minStock: 5, code: "P001", barcode: null, categoryId: null, unitId: null, description: null, createdByUserId: null);
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.GetByIdAsync(product.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Test Product");
        result.Value.SalePrice.Should().Be(100m);

        _output.WriteLine("[PASS] GetByIdAsync returns product dto");
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentProduct_ReturnsNotFound()
    {
        _output.WriteLine("[TEST] GetByIdAsync_NonExistentProduct_ReturnsNotFound");

        var result = await _sut.GetByIdAsync(999, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("المنتج غير موجود");

        _output.WriteLine("[PASS] Non-existent product returns NotFound");
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidRequest_CreatesProduct()
    {
        _output.WriteLine("[TEST] CreateAsync_ValidRequest_CreatesProduct");

        var request = new SalesSystem.Contracts.Requests.CreateProductRequest
        {
            Name = "New Product",
            PurchasePrice = 50m,
            SalePrice = 100m,
            MinStock = 10,
            Code = "NP001",
            Barcode = "1234567890123"
        };

        var result = await _sut.CreateAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("New Product");
        result.Value.SalePrice.Should().Be(100m);
        result.Value.PurchasePrice.Should().Be(50m);

        _output.WriteLine("[PASS] CreateAsync creates product correctly");
    }

    [Fact]
    public async Task CreateAsync_DuplicateCode_ReturnsFailure()
    {
        _output.WriteLine("[TEST] CreateAsync_DuplicateCode_ReturnsFailure");

        var existing = Product.Create("Existing", purchasePrice: 10m, salePrice: 100m, minStock: 0, code: "P001", barcode: null, categoryId: null, unitId: null, description: null, createdByUserId: null);
        _dbContext.Products.Add(existing);
        await _dbContext.SaveChangesAsync();

        var request = new SalesSystem.Contracts.Requests.CreateProductRequest
        {
            Name = "New Product",
            PurchasePrice = 50m,
            SalePrice = 100m,
            Code = "P001" // Duplicate
        };

        var result = await _sut.CreateAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("كود المنتج مستخدم بالفعل");

        _output.WriteLine("[PASS] Duplicate code returns failure");
    }

    [Fact]
    public async Task CreateAsync_DuplicateBarcode_ReturnsFailure()
    {
        _output.WriteLine("[TEST] CreateAsync_DuplicateBarcode_ReturnsFailure");

        var existing = Product.Create("Existing", purchasePrice: 10m, salePrice: 100m, minStock: 0, code: "P001", barcode: "1234567890", categoryId: null, unitId: null, description: null, createdByUserId: null);
        _dbContext.Products.Add(existing);
        await _dbContext.SaveChangesAsync();

        var request = new SalesSystem.Contracts.Requests.CreateProductRequest
        {
            Name = "New Product",
            PurchasePrice = 50m,
            SalePrice = 100m,
            Code = "P002",
            Barcode = "1234567890" // Duplicate
        };

        var result = await _sut.CreateAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("باركود المنتج مستخدم بالفعل");

        _output.WriteLine("[PASS] Duplicate barcode returns failure");
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ValidRequest_UpdatesProduct()
    {
        _output.WriteLine("[TEST] UpdateAsync_ValidRequest_UpdatesProduct");

        var product = Product.Create("Original", purchasePrice: 10m, salePrice: 100m, minStock: 5, code: "P001", barcode: null, categoryId: null, unitId: null, description: null, createdByUserId: null);
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var request = new SalesSystem.Contracts.Requests.UpdateProductRequest
        {
            Name = "Updated Product",
            PurchasePrice = 20m,
            SalePrice = 200m,
            MinStock = 10,
            Code = null,
            Barcode = null,
            IsActive = true
        };

        var result = await _sut.UpdateAsync(product.Id, request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Updated Product");
        result.Value.PurchasePrice.Should().Be(20m);
        result.Value.SalePrice.Should().Be(200m);

        _output.WriteLine("[PASS] UpdateAsync updates product correctly");
    }

    [Fact]
    public async Task UpdateAsync_NonExistentProduct_ReturnsNotFound()
    {
        _output.WriteLine("[TEST] UpdateAsync_NonExistentProduct_ReturnsNotFound");

        var request = new SalesSystem.Contracts.Requests.UpdateProductRequest
        {
            Name = "Updated",
            IsActive = true
        };

        var result = await _sut.UpdateAsync(999, request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("المنتج غير موجود");

        _output.WriteLine("[PASS] Update non-existent product returns NotFound");
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ExistingProduct_SoftDeletes()
    {
        _output.WriteLine("[TEST] DeleteAsync_ExistingProduct_SoftDeletes");

        var product = Product.Create("Test Product", purchasePrice: 10m, salePrice: 100m, minStock: 0, code: "P001", barcode: null, categoryId: null, unitId: null, description: null, createdByUserId: null);
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.DeleteAsync(product.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        _output.WriteLine("[PASS] DeleteAsync soft deletes product");
    }

    [Fact]
    public async Task DeleteAsync_NonExistentProduct_ReturnsNotFound()
    {
        _output.WriteLine("[TEST] DeleteAsync_NonExistentProduct_ReturnsNotFound");

        var result = await _sut.DeleteAsync(999, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("المنتج غير موجود");

        _output.WriteLine("[PASS] Delete non-existent product returns NotFound");
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_WithCategoryFilter_FiltersResults()
    {
        _output.WriteLine("[TEST] GetAllAsync_WithCategoryFilter_FiltersResults");

        var category = Category.Create("Electronics", null, null);
        _dbContext.Categories.Add(category);

        var product1 = Product.Create("Product 1", 10m, 100m, 0, "P001", null, 1, null, null, null);
        var product2 = Product.Create("Product 2", 10m, 100m, 0, "P002", null, null, null, null);
        _dbContext.Products.Add(product1);
        _dbContext.Products.Add(product2);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.GetAllAsync(null, 1, 1, 10, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(1);
        result.Value.Items.First().Name.Should().Be("Product 1");

        _output.WriteLine("[PASS] Category filter works correctly");
    }

    [Fact]
    public async Task GetAllAsync_WithSearch_FiltersResults()
    {
        _output.WriteLine("[TEST] GetAllAsync_WithSearch_FiltersResults");

        var product1 = Product.Create("Laptop", 10m, 100m, 0, "P001", null, null, null, null);
        var product2 = Product.Create("Mouse", 10m, 100m, 0, "P002", null, null, null, null);
        _dbContext.Products.Add(product1);
        _dbContext.Products.Add(product2);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.GetAllAsync("Laptop", null, 1, 10, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(1);
        result.Value.Items.First().Name.Should().Be("Laptop");

        _output.WriteLine("[PASS] Search filters products correctly");
    }

    #endregion

    #region Helper Classes

    private class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

        public DbSet<Product> Products => Set<Product>();
        public DbSet<Category> Categories => Set<Category>();
    }

    private class InMemoryEfCoreRepository<T> : IGenericRepository<T> where T : BaseEntity
    {
        private readonly DbContext _context;

        public InMemoryEfCoreRepository(DbContext context)
        {
            _context = context;
        }

        public async Task<T?> GetByIdAsync(int id, CancellationToken ct = default)
            => await _context.Set<T>().FindAsync(new object[] { id }, ct);

        public Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<T>>(_context.Set<T>().ToList());

        public async Task<T> AddAsync(T entity, CancellationToken ct = default)
        {
            await _context.Set<T>().AddAsync(entity, ct);
            return entity;
        }

        public Task UpdateAsync(T entity, CancellationToken ct = default)
        {
            _context.Set<T>().Update(entity);
            return Task.CompletedTask;
        }

        public Task SoftDeleteAsync(int id, CancellationToken ct = default)
            => throw new NotImplementedException();

        public IQueryable<T> Query() => _context.Set<T>().AsQueryable();
    }

    #endregion
}