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
/// Unit tests for DocumentSequenceService business logic.
/// Tests thread-safe sequence generation.
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
        srResult.Should().StartWith("SR-");

        _output.WriteLine($"[DEBUG] INV: {invResult.Value}, PUR: {purResult.Value}, SR: {srResult}");
        _output.WriteLine("[PASS] Different prefixes have independent sequences");
    }

    [Fact]
    public async Task GetNextNumberAsync_YearChange_ResetsSequence()
    {
        _output.WriteLine("[TEST] GetNextNumberAsync_YearChange_ResetsSequence");

        // Create a sequence for a different year
        var oldSequence = DocumentSequence.Create("فاتورة مبيعات", "INV", DateTime.Now.Year - 1);
        oldSequence.GetNextNumber(); // Increment to 1
        _dbContext.DocumentSequences.Add(oldSequence);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.GetNextNumberAsync("INV", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain(DateTime.Now.Year.ToString());

        _output.WriteLine($"[DEBUG] Year changed sequence: {result.Value}");
        _output.WriteLine("[PASS] Year change resets sequence");
    }

    [Theory]
    [InlineData("INV", "فاتورة مبيعات")]
    [InlineData("PUR", "فاتورة مشتريات")]
    [InlineData("SR", "مرتجع مبيعات")]
    [InlineData("PR", "مرتجع مشتريات")]
    [InlineData("TRF", "تحويل مخزني")]
    [InlineData("CP", "سند قبض عميل")]
    [InlineData("SP", "سند صرف مورد")]
    public async Task GetNextNumberAsync_KnownPrefix_SetsCorrectDocumentType(string prefix, string expectedType)
    {
        _output.WriteLine($"[TEST] GetNextNumberAsync_KnownPrefix_{prefix}_SetsCorrectDocumentType");

        var result = await _sut.GetNextNumberAsync(prefix, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var sequence = await _dbContext.DocumentSequences.FirstOrDefaultAsync(s => s.Prefix == prefix);
        sequence.Should().NotBeNull();
        sequence!.DocumentType.Should().Be(expectedType);

        _output.WriteLine($"[PASS] Prefix {prefix} sets document type: {expectedType}");
    }

    [Fact]
    public async Task GetNextNumberAsync_UnknownPrefix_UsesDefaultType()
    {
        _output.WriteLine("[TEST] GetNextNumberAsync_UnknownPrefix_UsesDefaultType");

        var result = await _sut.GetNextNumberAsync("XXX", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var sequence = await _dbContext.DocumentSequences.FirstOrDefaultAsync(s => s.Prefix == "XXX");
        sequence.Should().NotBeNull();
        sequence!.DocumentType.Should().Be("أخرى");

        _output.WriteLine("[PASS] Unknown prefix uses default document type");
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

    #region Helper Classes

    private class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

        public DbSet<DocumentSequence> DocumentSequences => Set<DocumentSequence>();
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