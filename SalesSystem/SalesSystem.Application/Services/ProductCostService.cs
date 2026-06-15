using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Application.Services;

/// <summary>
/// Derives product cost from InventoryBatches using WeightedAverage, FIFO, or LatestCost methods.
/// This replaces any denormalized Product.Cost column — cost is always computed from batch data.
/// </summary>
public class ProductCostService : IProductCostService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ProductCostService> _logger;

    public ProductCostService(IUnitOfWork uow, ILogger<ProductCostService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<decimal>> GetAverageCostAsync(int productId, CancellationToken ct = default)
    {
        try
        {
            if (productId <= 0)
                return Result<decimal>.Failure("معرف المنتج مطلوب");

            var batches = await _uow.InventoryBatches.ToListAsync(
                b => b.ProductId == productId && b.QuantityRemaining > 0,
                q => q.OrderBy(b => b.CreatedAt),
                ct);

            if (batches.Count == 0)
            {
                _logger.LogDebug("No non-zero batches found for product {ProductId}, returning 0", productId);
                return Result<decimal>.Success(0m);
            }

            var totalCost = batches.Sum(b => b.QuantityRemaining * b.UnitCost);
            var totalQuantity = batches.Sum(b => b.QuantityRemaining);

            if (totalQuantity == 0)
                return Result<decimal>.Success(0m);

            var averageCost = Math.Round(totalCost / totalQuantity, 2);

            _logger.LogDebug(
                "Weighted average cost for product {ProductId}: {AverageCost} " +
                "(from {BatchCount} batches, total qty: {TotalQuantity}, total cost: {TotalCost})",
                productId, averageCost, batches.Count, totalQuantity, totalCost);

            return Result<decimal>.Success(averageCost);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error computing average cost for product {ProductId}", productId);
            return Result<decimal>.Failure("حدث خطأ أثناء حساب متوسط التكلفة.");
        }
    }

    /// <inheritdoc />
    public async Task<Result<List<FifoLayerDto>>> GetFifoLayersAsync(int productId, decimal quantity, CancellationToken ct = default)
    {
        try
        {
            if (productId <= 0)
                return Result<List<FifoLayerDto>>.Failure("معرف المنتج مطلوب");

            if (quantity <= 0)
                return Result<List<FifoLayerDto>>.Failure("الكمية يجب أن تكون أكبر من الصفر");

            var batches = await _uow.InventoryBatches.ToListAsync(
                b => b.ProductId == productId && b.QuantityRemaining > 0,
                q => q.OrderBy(b => b.CreatedAt),
                ct);

            if (batches.Count == 0)
            {
                _logger.LogDebug("No non-zero batches found for product {ProductId}, returning empty layers", productId);
                return Result<List<FifoLayerDto>>.Success(new List<FifoLayerDto>());
            }

            var layers = new List<FifoLayerDto>();
            var remaining = quantity;

            foreach (var batch in batches)
            {
                if (remaining <= 0)
                    break;

                var consumed = Math.Min(batch.QuantityRemaining, remaining);
                var totalCost = Math.Round(consumed * batch.UnitCost, 2);

                layers.Add(new FifoLayerDto(
                    BatchId: batch.Id,
                    BatchNo: batch.BatchNo.ToString(),
                    QuantityConsumed: consumed,
                    UnitCost: batch.UnitCost,
                    TotalCost: totalCost
                ));

                remaining -= consumed;
            }

            if (remaining > 0)
            {
                _logger.LogWarning(
                    "Insufficient stock for product {ProductId}: requested {RequestedQuantity}, " +
                    "available {AvailableQuantity}, short by {ShortQuantity}",
                    productId, quantity, quantity - remaining, remaining);
            }

            _logger.LogDebug(
                "FIFO layers for product {ProductId}: requested {RequestedQuantity}, " +
                "consumed {ConsumedQuantity} across {LayerCount} layers",
                productId, quantity, quantity - remaining, layers.Count);

            return Result<List<FifoLayerDto>>.Success(layers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error computing FIFO layers for product {ProductId}, quantity {Quantity}",
                productId, quantity);
            return Result<List<FifoLayerDto>>.Failure("حدث خطأ أثناء حساب طبقات FIFO.");
        }
    }

    /// <inheritdoc />
    public async Task<Result<decimal>> GetLatestCostAsync(int productId, CancellationToken ct = default)
    {
        try
        {
            if (productId <= 0)
                return Result<decimal>.Failure("معرف المنتج مطلوب");

            // Check if we have filtered queries on Id order — use AnyAsync first for fast check
            var hasBatches = await _uow.InventoryBatches.AnyAsync(
                b => b.ProductId == productId && b.QuantityRemaining > 0, ct);

            if (!hasBatches)
            {
                _logger.LogDebug("No non-zero batches found for product {ProductId}, returning 0 for latest cost", productId);
                return Result<decimal>.Success(0m);
            }

            // Get the most recent batch (by CreatedAt DESC) with non-zero quantity
            var latestBatches = await _uow.InventoryBatches.ToListAsync(
                b => b.ProductId == productId && b.QuantityRemaining > 0,
                q => q.OrderByDescending(b => b.CreatedAt).Take(1),
                ct);

            var latest = latestBatches.FirstOrDefault();

            if (latest == null)
                return Result<decimal>.Success(0m);

            _logger.LogDebug(
                "Latest cost for product {ProductId}: {UnitCost} (from batch #{BatchId}, batch no: {BatchNo})",
                productId, latest.UnitCost, latest.Id, latest.BatchNo);

            return Result<decimal>.Success(latest.UnitCost);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error computing latest cost for product {ProductId}", productId);
            return Result<decimal>.Failure("حدث خطأ أثناء حساب آخر تكلفة.");
        }
    }
}
