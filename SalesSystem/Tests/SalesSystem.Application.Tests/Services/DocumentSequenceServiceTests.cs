using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Repositories;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Application.Services;
using SalesSystem.Domain.Common;
using SalesSystem.Domain.Entities;
using System.Linq.Expressions;
using Xunit.Abstractions;

namespace SalesSystem.Application.Tests.Services;

/// <summary>
/// Unit tests for DocumentSequenceService business logic.
/// Tests thread-safe sequence generation using the schema (DocumentType + NextNumber).
/// </summary>
public class DocumentSequenceServiceTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly TestDbContext _dbContext;
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<ILogger<DocumentSequenceService>> _mockLogger;

    private readonly DocumentSequenceService _sut;

    public DocumentSequenceServiceTests(ITestOutputHelper output)
    {
        _output = output;
        _output.WriteLine("[TEST] DocumentSequenceServiceTests initialized");

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new TestDbContext(options);

        _mockUow = new Mock<IUnitOfWork>();
        _mockLogger = new Mock<ILogger<DocumentSequenceService>>();

        _mockUow.Setup(u => u.DocumentSequences).Returns(new InMemoryEfCoreRepository<DocumentSequence>(_dbContext));

        _mockUow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                await _dbContext.SaveChangesAsync();
                return 1;
            });

        _sut = new DocumentSequenceService(_mockUow.Object, _mockLogger.Object);
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
    }

    #region GetNextNumberAsync Tests

    [Fact]
    public async Task GetNextNumberAsync_NewPrefix_CreatesSequenceAndReturnsNumber()
    {
        _output.WriteLine("[TEST] GetNextNumberAsync_NewPrefix_CreatesSequenceAndReturnsNumber");

        var result = await _sut.GetNextNumberAsync("INV", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().StartWith("INV-");
        result.Value.Should().Contain(DateTime.Now.Year.ToString());

        _output.WriteLine($"[DEBUG] Generated number: {result.Value}");
        _output.WriteLine("[PASS] New prefix creates sequence correctly");
    }

    [Fact]
    public async Task GetNextNumberAsync_ExistingPrefix_ReturnsNextSequence()
    {
        _output.WriteLine("[TEST] GetNextNumberAsync_ExistingPrefix_ReturnsNextSequence");

        // First call
        var result1 = await _sut.GetNextNumberAsync("INV", CancellationToken.None);
        result1.IsSuccess.Should().BeTrue();

        // Second call
        var result2 = await _sut.GetNextNumberAsync("INV", CancellationToken.None);
        result2.IsSuccess.Should().BeTrue();

        result1.Value.Should().NotBe(result2.Value);

        // Extract sequence numbers
        var seq1 = result1.Value!.Split('-').Last();
        var seq2 = result2.Value!.Split('-').Last();

        int.Parse(seq2).Should().BeGreaterThan(int.Parse(seq1));

        _output.WriteLine($"[DEBUG] Seq1: {seq1}, Seq2: {seq2}");
        _output.WriteLine("[PASS] Existing prefix returns next sequence");
    }

    [Fact]
    public async Task GetNextNumberAsync_DifferentPrefixes_ReturnIndependentSequences()
    {
        _output.WriteLine("[TEST] GetNextNumberAsync_DifferentPrefixes_ReturnIndependentSequences");

        var invResult = await _sut.GetNextNumberAsync("INV", CancellationToken.None);
        var purResult = await _sut.GetNextNumberAsync("PUR", CancellationToken.None);
        var srResult = await _sut.GetNextNumberAsync("SR", CancellationToken.None);

        invResult.IsSuccess.Should().BeTrue();
        purResult.IsSuccess.Should().BeTrue();
        srResult.IsSuccess.Should().BeTrue();

        invResult.Value.Should().StartWith("INV-");
        purResult.Value.Should().StartWith("PUR-");
        srResult.Value.Should().StartWith("SR-");

        _output.WriteLine($"[DEBUG] INV: {invResult.Value}, PUR: {purResult.Value}, SR: {srResult}");
        _output.WriteLine("[PASS] Different prefixes have independent sequences");
    }

    [Fact]
    public async Task GetNextNumberAsync_KnownPrefix_SetsCorrectDocumentType()
    {
        _output.WriteLine("[TEST] GetNextNumberAsync_KnownPrefix_SetsCorrectDocumentType");

        var result = await _sut.GetNextNumberAsync("INV", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        // DocumentType should be "SalesInvoice" (mapped from "INV")
        var sequence = await _dbContext.DocumentSequences.FirstOrDefaultAsync(s => s.DocumentType == "SalesInvoice");
        sequence.Should().NotBeNull();
        sequence!.DocumentType.Should().Be("SalesInvoice");

        _output.WriteLine($"[PASS] INV prefix mapped to SalesInvoice");
    }

    [Fact]
    public async Task GetNextNumberAsync_UnknownPrefix_UsesPrefixAsKey()
    {
        _output.WriteLine("[TEST] GetNextNumberAsync_UnknownPrefix_UsesPrefixAsKey");

        var result = await _sut.GetNextNumberAsync("XXX", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        // Unknown prefix maps to uppercase of itself
        var sequence = await _dbContext.DocumentSequences.FirstOrDefaultAsync(s => s.DocumentType == "XXX");
        sequence.Should().NotBeNull();
        sequence!.DocumentType.Should().Be("XXX");

        _output.WriteLine("[PASS] Unknown prefix uses itself as document type");
    }

    [Fact]
    public async Task GetNextNumberAsync_PadsSequenceNumberCorrectly()
    {
        _output.WriteLine("[TEST] GetNextNumberAsync_PadsSequenceNumberCorrectly");

        // Generate multiple numbers
        var results = new List<string>();
        for (int i = 0; i < 5; i++)
        {
            var result = await _sut.GetNextNumberAsync("TEST", CancellationToken.None);
            results.Add(result.Value!);
        }

        // Verify padding (6 digits)
        results.Should().OnlyContain(r => r.EndsWith("000001") || r.EndsWith("000002") ||
            r.EndsWith("000003") || r.EndsWith("000004") || r.EndsWith("000005"));

        results[0].Should().EndWith("000001");
        results[4].Should().EndWith("000005");

        _output.WriteLine($"[DEBUG] Generated sequences: {string.Join(", ", results)}");
        _output.WriteLine("[PASS] Sequence numbers are zero-padded correctly");
    }

    #endregion

    #region GetNextIntAsync Tests

    [Fact]
    public async Task GetNextIntAsync_NewKey_CreatesSequenceAndReturnsInt()
    {
        _output.WriteLine("[TEST] GetNextIntAsync_NewKey_CreatesSequenceAndReturnsInt");

        var result = await _sut.GetNextIntAsync("SalesInvoice", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(1);

        _output.WriteLine($"[DEBUG] Generated int: {result.Value}");
        _output.WriteLine("[PASS] New key creates sequence and returns 1");
    }

    [Fact]
    public async Task GetNextIntAsync_ExistingKey_ReturnsNextInt()
    {
        _output.WriteLine("[TEST] GetNextIntAsync_ExistingKey_ReturnsNextInt");

        // First call
        var result1 = await _sut.GetNextIntAsync("SalesInvoice", CancellationToken.None);
        result1.IsSuccess.Should().BeTrue();
        result1.Value.Should().Be(1);

        // Second call
        var result2 = await _sut.GetNextIntAsync("SalesInvoice", CancellationToken.None);
        result2.IsSuccess.Should().BeTrue();
        result2.Value.Should().Be(2);

        _output.WriteLine($"[DEBUG] Seq1: {result1.Value}, Seq2: {result2.Value}");
        _output.WriteLine("[PASS] Existing key returns next int");
    }

    [Fact]
    public async Task GetNextIntAsync_DifferentKeys_ReturnIndependentSequences()
    {
        _output.WriteLine("[TEST] GetNextIntAsync_DifferentKeys_ReturnIndependentSequences");

        var salesResult = await _sut.GetNextIntAsync("SalesInvoice", CancellationToken.None);
        var purchaseResult = await _sut.GetNextIntAsync("PurchaseInvoice", CancellationToken.None);

        salesResult.IsSuccess.Should().BeTrue();
        purchaseResult.IsSuccess.Should().BeTrue();

        salesResult.Value.Should().Be(1);
        purchaseResult.Value.Should().Be(1);

        _output.WriteLine($"[DEBUG] Sales: {salesResult.Value}, Purchase: {purchaseResult.Value}");
        _output.WriteLine("[PASS] Different keys have independent sequences");
    }

    [Fact]
    public async Task GetNextIntAsync_PrefixAndIntUseSameSequence()
    {
        _output.WriteLine("[TEST] GetNextIntAsync_PrefixAndIntUseSameSequence");

        // GetNextNumberAsync with "INV" maps to "SalesInvoice"
        var strResult = await _sut.GetNextNumberAsync("INV", CancellationToken.None);
        strResult.IsSuccess.Should().BeTrue();

        // GetNextIntAsync with "SalesInvoice" shares the same sequence
        var intResult = await _sut.GetNextIntAsync("SalesInvoice", CancellationToken.None);
        intResult.IsSuccess.Should().BeTrue();
        intResult.Value.Should().Be(2); // Second call after GetNextNumberAsync

        _output.WriteLine($"[DEBUG] Str result: {strResult.Value}, Int result: {intResult.Value}");
        _output.WriteLine("[PASS] Prefix and int use same sequence");
    }

    #endregion

    #region Helper Classes

    private class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

        public DbSet<DocumentSequence> DocumentSequences => Set<DocumentSequence>();
    }

    private class InMemoryEfCoreRepository<T> : IGenericRepository<T> where T : Entity
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

        public Task HardDeleteAsync(int id, CancellationToken ct = default)
            => throw new NotImplementedException();

        public void DeleteRange(IEnumerable<T> entities)
            => throw new NotImplementedException();

        public Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default, params string[] includePaths)
            => Task.FromResult(_context.Set<T>().FirstOrDefault(predicate));

        public Task<T?> FirstOrDefaultIgnoreFiltersAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default, params string[] includePaths)
            => Task.FromResult(_context.Set<T>().IgnoreQueryFilters().FirstOrDefault(predicate));

        public Task<List<T>> ToListAsync(CancellationToken ct = default, params string[] includePaths)
            => Task.FromResult(_context.Set<T>().ToList());

        public Task<List<T>> ToListAsync(Expression<Func<T, bool>>? predicate, Func<IQueryable<T>, IQueryable<T>>? queryConfig = null, CancellationToken ct = default, bool ignoreQueryFilters = false, params string[] includePaths)
        {
            IQueryable<T> query = _context.Set<T>();
            if (ignoreQueryFilters) query = query.IgnoreQueryFilters();
            if (predicate != null) query = query.Where(predicate);
            if (queryConfig != null) query = queryConfig(query);
            return Task.FromResult(query.ToList());
        }

        public Task<(List<T> Items, int TotalCount)> GetPagedAsync(Expression<Func<T, bool>>? predicate, Func<IQueryable<T>, IQueryable<T>>? orderConfig, int page, int pageSize, CancellationToken ct = default, bool ignoreQueryFilters = false, params string[] includePaths)
        {
            IQueryable<T> query = _context.Set<T>();
            if (ignoreQueryFilters) query = query.IgnoreQueryFilters();
            if (predicate != null) query = query.Where(predicate);
            var totalCount = query.Count();
            if (orderConfig != null) query = orderConfig(query);
            var items = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return Task.FromResult((items, totalCount));
        }

        public Task<List<T>> ToListIgnoreFiltersAsync(CancellationToken ct = default, params string[] includePaths)
            => Task.FromResult(_context.Set<T>().IgnoreQueryFilters().ToList());

        public Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default)
            => Task.FromResult(predicate == null ? _context.Set<T>().Count() : _context.Set<T>().Count(predicate));

        public Task<int> CountIgnoreFiltersAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default)
            => Task.FromResult(predicate == null ? _context.Set<T>().IgnoreQueryFilters().Count() : _context.Set<T>().IgnoreQueryFilters().Count(predicate));

        public Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
            => Task.FromResult(_context.Set<T>().Any(predicate));

        public Task<bool> AnyIgnoreFiltersAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
            => Task.FromResult(_context.Set<T>().IgnoreQueryFilters().Any(predicate));

        public IQueryable<T> Query() => _context.Set<T>().AsQueryable();
    }

    #endregion
}
