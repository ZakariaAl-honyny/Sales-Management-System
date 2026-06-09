using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Application.Services;

/// <summary>
/// تنفيذ خدمة المصاريف الإضافية لفواتير الشراء.
/// </summary>
public class AdditionalFeeService : IAdditionalFeeService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<AdditionalFeeService> _logger;

    public AdditionalFeeService(
        IUnitOfWork uow,
        ILogger<AdditionalFeeService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<Result<List<AdditionalFeeDto>>> GetFeesByInvoiceAsync(int purchaseInvoiceId, CancellationToken ct)
    {
        var invoice = await _uow.PurchaseInvoices.FirstOrDefaultAsync(
            i => i.Id == purchaseInvoiceId, ct, "AdditionalFees");

        if (invoice == null)
            return Result<List<AdditionalFeeDto>>.Failure("فاتورة الشراء غير موجودة", ErrorCodes.NotFound);

        var fees = await _uow.AdditionalFees.ToListAsync(
            f => f.PurchaseInvoiceId == purchaseInvoiceId && f.IsActive,
            ct: ct);

        var dtos = fees.Select(MapToDto).ToList();
        return Result<List<AdditionalFeeDto>>.Success(dtos);
    }

    public async Task<Result<AdditionalFeeDto>> CreateFeeAsync(
        CreateAdditionalFeeRequest request, int purchaseInvoiceId, CancellationToken ct)
    {
        var invoice = await _uow.PurchaseInvoices.GetByIdAsync(purchaseInvoiceId, ct);
        if (invoice == null)
            return Result<AdditionalFeeDto>.Failure("فاتورة الشراء غير موجودة", ErrorCodes.NotFound);

        if (invoice.Status != InvoiceStatus.Draft)
            return Result<AdditionalFeeDto>.Failure("يمكن إضافة مصاريف فقط للفواتير المسودة");

        try
        {
            var fee = AdditionalFee.Create(
                purchaseInvoiceId,
                request.FeeName,
                request.FeeAmount,
                (DistributionMethod)request.DistributionMethod,
                request.AccountId);

            await _uow.AdditionalFees.AddAsync(fee, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("تم إنشاء مصروف إضافي: {FeeName} بقيمة {FeeAmount} لفاتورة {InvoiceId}",
                fee.FeeName, fee.FeeAmount, purchaseInvoiceId);

            return Result<AdditionalFeeDto>.Success(MapToDto(fee));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "خطأ في المجال أثناء إنشاء المصروف الإضافي: {Message}", ex.Message);
            return Result<AdditionalFeeDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ أثناء إنشاء المصروف الإضافي لفاتورة {InvoiceId}", purchaseInvoiceId);
            return Result<AdditionalFeeDto>.Failure("حدث خطأ أثناء حفظ المصروف الإضافي");
        }
    }

    public async Task<Result> RemoveFeeAsync(int feeId, CancellationToken ct)
    {
        var fee = await _uow.AdditionalFees.GetByIdAsync(feeId, ct);
        if (fee == null)
            return Result.Failure("المصروف الإضافي غير موجود", ErrorCodes.NotFound);

        try
        {
            await _uow.AdditionalFees.SoftDeleteAsync(feeId, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("تم إزالة المصروف الإضافي: المعرف {FeeId}", feeId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ أثناء إزالة المصروف الإضافي {FeeId}", feeId);
            return Result.Failure("حدث خطأ أثناء إزالة المصروف الإضافي");
        }
    }

    private static AdditionalFeeDto MapToDto(AdditionalFee f)
    {
        return new AdditionalFeeDto(
            f.Id,
            f.FeeName,
            f.FeeAmount,
            (byte)f.DistributionMethod,
            f.AccountId,
            null // AccountName — would need navigation load
        );
    }
}
