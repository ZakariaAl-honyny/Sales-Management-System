using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Application.Interfaces.Services;

/// <summary>
/// خدمة عروض الأسعار (أوامر البيع) — إدارة دورة حياة عرض السعر من الإنشاء إلى التحويل لفاتورة.
/// </summary>
public interface ISalesQuotationService
{
    /// <summary>الحصول على عرض سعر بالمعرف.</summary>
    Task<Result<SalesQuotationDto>> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>الحصول على قائمة عروض الأسعار مع الترشيح.</summary>
    Task<Result<List<SalesQuotationDto>>> GetAllAsync(
        int? customerId = null,
        byte? status = null,
        string? search = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken ct = default);

    /// <summary>إنشاء عرض سعر جديد (مسودة).</summary>
    Task<Result<SalesQuotationDto>> CreateAsync(CreateSalesQuotationRequest request, int userId, CancellationToken ct = default);

    /// <summary>تحديث عرض سعر موجود (المسودة فقط).</summary>
    Task<Result<SalesQuotationDto>> UpdateAsync(int id, UpdateSalesQuotationRequest request, int userId, CancellationToken ct = default);

    /// <summary>إلغاء عرض سعر.</summary>
    Task<Result<bool>> DeleteAsync(int id, CancellationToken ct = default);

    /// <summary>تأكيد عرض السعر — ينقل الحالة من مسودة إلى مؤكد.</summary>
    Task<Result<SalesQuotationDto>> ConfirmAsync(int id, CancellationToken ct = default);

    /// <summary>إنهاء صلاحية عرض السعر.</summary>
    Task<Result<SalesQuotationDto>> ExpireAsync(int id, CancellationToken ct = default);

    /// <summary>تحويل عرض السعر المؤكد إلى فاتورة بيع.</summary>
    Task<Result<SalesQuotationDto>> ConvertToInvoiceAsync(int id, ConvertQuotationToInvoiceRequest request, int userId, CancellationToken ct = default);
}
