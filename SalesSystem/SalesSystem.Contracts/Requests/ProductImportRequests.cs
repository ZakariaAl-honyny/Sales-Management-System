using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Enums;

namespace SalesSystem.Contracts.Requests;

/// <summary>
/// طلب تنفيذ استيراد المنتجات — يحدد وضع الاستيراد (إدراج جديد أو تحديث) وقائمة الصفوف
/// </summary>
public record ProductImportExecuteRequest(
    ImportMode Mode,
    List<ProductImportRowDto> Rows
);
