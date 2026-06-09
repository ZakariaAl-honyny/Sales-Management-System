using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Application.Interfaces.Services;

/// <summary>
/// خدمة أوامر الشراء — إدارة دورة حياة أمر الشراء من الإنشاء إلى الإلغاء.
/// </summary>
public interface IPurchaseOrderService
{
    /// <summary>الحصول على أمر شراء بالمعرف.</summary>
    Task<Result<PurchaseOrderDto>> GetByIdAsync(int id, CancellationToken ct);

    /// <summary>الحصول على قائمة أوامر الشراء مع الترشيح.</summary>
    Task<Result<List<PurchaseOrderDto>>> GetAllAsync(
        int? supplierId,
        byte? status,
        string? search = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken ct = default);

    /// <summary>الحصول على أوامر الشراء المعلقة (غير مستلمة بالكامل).</summary>
    Task<Result<List<PurchaseOrderDto>>> GetPendingOrdersAsync(CancellationToken ct);

    /// <summary>إنشاء أمر شراء جديد (مسودة).</summary>
    Task<Result<PurchaseOrderDto>> CreateAsync(CreatePurchaseOrderRequest request, int userId, CancellationToken ct);

    /// <summary>تحديث أمر شراء موجود (المسودة فقط).</summary>
    Task<Result<PurchaseOrderDto>> UpdateAsync(int id, UpdatePurchaseOrderRequest request, int userId, CancellationToken ct);

    /// <summary>إلغاء أمر شراء.</summary>
    Task<Result> CancelAsync(int id, int userId, CancellationToken ct);
}
