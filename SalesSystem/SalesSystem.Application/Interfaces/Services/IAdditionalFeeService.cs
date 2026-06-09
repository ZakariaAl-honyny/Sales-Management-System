using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Application.Interfaces.Services;

/// <summary>
/// خدمة المصاريف الإضافية لفواتير الشراء — إنشاء وعرض وإزالة الرسوم.
/// </summary>
public interface IAdditionalFeeService
{
    /// <summary>الحصول على قائمة المصاريف الإضافية لفاتورة شراء.</summary>
    Task<Result<List<AdditionalFeeDto>>> GetFeesByInvoiceAsync(int purchaseInvoiceId, CancellationToken ct);

    /// <summary>إنشاء مصروف إضافي جديد لفاتورة شراء.</summary>
    Task<Result<AdditionalFeeDto>> CreateFeeAsync(CreateAdditionalFeeRequest request, int purchaseInvoiceId, CancellationToken ct);

    /// <summary>إزالة مصروف إضافي (حذف ناعم).</summary>
    Task<Result> RemoveFeeAsync(int feeId, CancellationToken ct);
}
