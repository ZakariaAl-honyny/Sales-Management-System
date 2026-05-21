using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;

namespace SalesSystem.Application.Services;

public class BarcodeLookupService : IBarcodeLookupService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<BarcodeLookupService> _logger;

    public BarcodeLookupService(IUnitOfWork uow, ILogger<BarcodeLookupService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<BarcodeSearchResult?> LookupAsync(
        string barcode,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(barcode)) return null;

        var normalized = barcode.Trim().ToUpperInvariant();

        var result = await _uow.UnitBarcodes.Query()
            .Include(b => b.ProductUnit)
                .ThenInclude(pu => pu.Product)
            .Where(b => b.BarcodeValue == normalized)
            .Select(b => new BarcodeSearchResult(
                b.ProductUnit.ProductId,
                b.ProductUnit.Product.Name,
                b.ProductUnitId,
                b.ProductUnit.UnitName,
                b.ProductUnit.BaseConversionFactor,
                b.ProductUnit.IsBaseUnit,
                b.ProductUnit.SalesPrice,
                b.ProductUnit.PurchaseCost,
                0m
            ))
            .FirstOrDefaultAsync(ct);

        if (result == null)
            _logger.LogWarning("Barcode not found: {Barcode}", normalized);

        return result;
    }
}