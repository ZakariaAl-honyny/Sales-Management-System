using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;

namespace SalesSystem.Application.Services;

public class AdditionalFeeService : IAdditionalFeeService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<AdditionalFeeService> _logger;

    public AdditionalFeeService(IUnitOfWork uow, ILogger<AdditionalFeeService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<Result<List<AdditionalFeeDto>>> GetFeesByInvoiceAsync(int purchaseInvoiceId, CancellationToken ct)
    {
        var invoice = await _uow.PurchaseInvoices.GetByIdAsync(purchaseInvoiceId, ct);
        if (invoice == null)
            return Result<List<AdditionalFeeDto>>.Failure("فاتورة المشتريات غير موجودة", ErrorCodes.NotFound);

        var fees = await _uow.AdditionalFees.ToListAsync(
            f => f.PurchaseInvoiceId == purchaseInvoiceId,
            q => q.OrderBy(f => f.Id),
            ct,
            false);

        var dtos = fees.Select(f => new AdditionalFeeDto(
            f.Id, f.PurchaseInvoiceId, f.FeeName, f.FeeAmount,
            (byte)f.DistributionMethod, f.AccountId)).ToList();

        return Result<List<AdditionalFeeDto>>.Success(dtos);
    }

    public async Task<Result<AdditionalFeeDto>> CreateFeeAsync(CreateAdditionalFeeRequest request, int purchaseInvoiceId, CancellationToken ct)
    {
        var invoice = await _uow.PurchaseInvoices.GetByIdAsync(purchaseInvoiceId, ct);
        if (invoice == null)
            return Result<AdditionalFeeDto>.Failure("فاتورة المشتريات غير موجودة", ErrorCodes.NotFound);

        if (invoice.Status != InvoiceStatus.Draft)
            return Result<AdditionalFeeDto>.Failure("يمكن إضافة رسوم إضافية للفواتير المسودة فقط");

        try
        {
            var fee = AdditionalFee.Create(
                purchaseInvoiceId: purchaseInvoiceId,
                feeName: request.FeeName,
                feeAmount: request.FeeAmount,
                distributionMethod: (DistributionMethod)request.DistributionMethod,
                accountId: request.AccountId
            );

            await _uow.AdditionalFees.AddAsync(fee, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Additional fee created: {FeeName} ({Amount}) for purchase invoice {InvoiceId}",
                request.FeeName, request.FeeAmount, purchaseInvoiceId);

            return Result<AdditionalFeeDto>.Success(new AdditionalFeeDto(
                fee.Id, fee.PurchaseInvoiceId, fee.FeeName, fee.FeeAmount,
                (byte)fee.DistributionMethod, fee.AccountId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create additional fee for purchase invoice {InvoiceId}", purchaseInvoiceId);
            return Result<AdditionalFeeDto>.Failure("فشل في إضافة الرسوم الإضافية");
        }
    }

    public async Task<Result> RemoveFeeAsync(int feeId, CancellationToken ct)
    {
        var fee = await _uow.AdditionalFees.GetByIdAsync(feeId, ct);
        if (fee == null)
            return Result.Failure("الرسوم الإضافية غير موجودة", ErrorCodes.NotFound);

        try
        {
            // Remove associated allocations first
            var allocations = await _uow.AdditionalFeeAllocations.ToListAsync(
                a => a.AdditionalFeeId == feeId, ct: ct);
            if (allocations.Any())
                _uow.AdditionalFeeAllocations.DeleteRange(allocations);

            _uow.AdditionalFees.DeleteRange(new[] { fee });
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Additional fee removed: ID {FeeId}", feeId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove additional fee {FeeId}", feeId);
            return Result.Failure("فشل في حذف الرسوم الإضافية");
        }
    }

    public async Task<Result> DistributeFeesAsync(int purchaseInvoiceId, CancellationToken ct)
    {
        var invoice = await _uow.PurchaseInvoices.FirstOrDefaultAsync(
            i => i.Id == purchaseInvoiceId, ct, "Items");

        if (invoice == null)
            return Result.Failure("فاتورة المشتريات غير موجودة", ErrorCodes.NotFound);

        if (invoice.Status != InvoiceStatus.Draft)
            return Result.Failure("يمكن توزيع الرسوم الإضافية للفواتير المسودة فقط");

        var fees = await _uow.AdditionalFees.ToListAsync(
            f => f.PurchaseInvoiceId == purchaseInvoiceId && f.IsActive,
            ct: ct);

        if (!fees.Any())
            return Result.Failure("لا توجد رسوم إضافية للتوزيع");

        var items = invoice.Items;
        if (!items.Any())
            return Result.Failure("لا توجد أصناف في الفاتورة لتوزيع الرسوم عليها");

        // Remove existing allocations for this invoice
        var existingAllocations = await _uow.AdditionalFeeAllocations.ToListAsync(
            a => fees.Select(f => f.Id).Contains(a.AdditionalFeeId), ct: ct);
        if (existingAllocations.Any())
            _uow.AdditionalFeeAllocations.DeleteRange(existingAllocations);

        // Reset additional fees amounts on items
        foreach (var item in items)
            item.SetAdditionalFeesAmount(0);

        List<AdditionalFeeAllocation> newAllocations = new();

        foreach (var fee in fees)
        {
            if (fee.DistributionMethod == DistributionMethod.ByCost)
            {
                // Distribute proportional to item LineTotal
                var totalCost = items.Sum(i => i.LineTotal);
                if (totalCost <= 0)
                {
                    // Equal distribution if total cost is zero
                    var equalShare = fee.FeeAmount / items.Count;
                    foreach (var item in items)
                    {
                        var allocation = AdditionalFeeAllocation.Create(
                            fee.Id, item.Id, equalShare);
                        newAllocations.Add(allocation);
                        item.SetAdditionalFeesAmount(item.AdditionalFeesAmount + equalShare);
                    }
                }
                else
                {
                    foreach (var item in items)
                    {
                        var share = fee.FeeAmount * (item.LineTotal / totalCost);
                        var allocation = AdditionalFeeAllocation.Create(
                            fee.Id, item.Id, share);
                        newAllocations.Add(allocation);
                        item.SetAdditionalFeesAmount(item.AdditionalFeesAmount + share);
                    }
                }
            }
            else // ByQuantity
            {
                // Distribute proportional to item Quantity
                var totalQuantity = items.Sum(i => i.Quantity);
                if (totalQuantity <= 0)
                {
                    var equalShare = fee.FeeAmount / items.Count;
                    foreach (var item in items)
                    {
                        var allocation = AdditionalFeeAllocation.Create(
                            fee.Id, item.Id, equalShare);
                        newAllocations.Add(allocation);
                        item.SetAdditionalFeesAmount(item.AdditionalFeesAmount + equalShare);
                    }
                }
                else
                {
                    foreach (var item in items)
                    {
                        var share = fee.FeeAmount * (item.Quantity / totalQuantity);
                        var allocation = AdditionalFeeAllocation.Create(
                            fee.Id, item.Id, share);
                        newAllocations.Add(allocation);
                        item.SetAdditionalFeesAmount(item.AdditionalFeesAmount + share);
                    }
                }
            }
        }

        foreach (var alloc in newAllocations)
            await _uow.AdditionalFeeAllocations.AddAsync(alloc, ct);

        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Additional fees distributed for purchase invoice {InvoiceId}: {FeeCount} fees", purchaseInvoiceId, fees.Count);

        return Result.Success();
    }
}
