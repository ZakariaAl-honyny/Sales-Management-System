using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Represents a Bill of Materials — links a finished (assembly) product to its component (raw material) product.
/// One BOM entry per component per assembly. Supports simple single-level BOM.
/// </summary>
public class BillOfMaterials : BaseEntity
{
    /// <summary>FK to Product — the finished/assembly product (المنتج المُجمَّع)</summary>
    public int AssemblyProductId { get; private set; }

    /// <summary>FK to Product — the component/raw material (المكوّن)</summary>
    public int ComponentProductId { get; private set; }

    /// <summary>FK to ProductUnit — the unit of measure for the component</summary>
    public int ComponentUnitId { get; private set; }

    /// <summary>Quantity of the component required to produce one unit of the assembly (decimal(18,3))</summary>
    public decimal QuantityRequired { get; private set; }

    /// <summary>
    /// Waste percentage (e.g., 5 means 5% extra is needed). Stored as decimal(18,2). Default 0.
    /// </summary>
    public decimal WastePercentage { get; private set; }

    // Navigation properties
    public Product AssemblyProduct { get; private set; } = null!;
    public Product ComponentProduct { get; private set; } = null!;
    public ProductUnit ComponentUnit { get; private set; } = null!;

    private BillOfMaterials() { } // EF Core

    /// <summary>
    /// Factory method to create a new BillOfMaterials entry.
    /// </summary>
    /// <param name="assemblyProductId">معرف المنتج المُجمَّع</param>
    /// <param name="componentProductId">معرف المكوّن</param>
    /// <param name="componentUnitId">معرف وحدة المكوّن</param>
    /// <param name="quantityRequired">الكمية المطلوبة من المكوّن</param>
    /// <param name="wastePercentage">نسبة الهالك (اختياري)</param>
    /// <param name="createdByUserId">معرف المستخدم المنشئ</param>
    /// <returns>كيان فاتورة المواد</returns>
    public static BillOfMaterials Create(
        int assemblyProductId,
        int componentProductId,
        int componentUnitId,
        decimal quantityRequired,
        decimal wastePercentage = 0,
        int? createdByUserId = null)
    {
        if (assemblyProductId <= 0)
            throw new DomainException("معرف المنتج المُجمَّع مطلوب.");
        if (componentProductId <= 0)
            throw new DomainException("معرف المكوّن مطلوب.");
        if (assemblyProductId == componentProductId)
            throw new DomainException("لا يمكن أن يكون المنتج المُجمَّع هو نفسه المكوّن.");
        if (componentUnitId <= 0)
            throw new DomainException("معرف وحدة المكوّن مطلوب.");
        if (quantityRequired <= 0)
            throw new DomainException("الكمية المطلوبة يجب أن تكون أكبر من الصفر.");
        if (wastePercentage < 0)
            throw new DomainException("نسبة الهالك لا يمكن أن تكون سالبة.");

        var bom = new BillOfMaterials
        {
            AssemblyProductId = assemblyProductId,
            ComponentProductId = componentProductId,
            ComponentUnitId = componentUnitId,
            QuantityRequired = quantityRequired,
            WastePercentage = wastePercentage,
            IsActive = true
        };
        bom.SetCreatedBy(createdByUserId);
        return bom;
    }

    /// <summary>
    /// Returns the effective quantity required including waste.
    /// مثال: لو الكمية المطلوبة 2 ونسبة الهالك 5% → الكمية الفعلية = 2 × (1 + 0.05) = 2.1
    /// </summary>
    public decimal EffectiveQuantityRequired => QuantityRequired * (1 + WastePercentage / 100m);

    /// <summary>
    /// Updates the bill of materials entry.
    /// </summary>
    public void Update(
        int componentUnitId,
        decimal quantityRequired,
        decimal wastePercentage = 0)
    {
        if (componentUnitId <= 0)
            throw new DomainException("معرف وحدة المكوّن مطلوب.");
        if (quantityRequired <= 0)
            throw new DomainException("الكمية المطلوبة يجب أن تكون أكبر من الصفر.");
        if (wastePercentage < 0)
            throw new DomainException("نسبة الهالك لا يمكن أن تكون سالبة.");

        ComponentUnitId = componentUnitId;
        QuantityRequired = quantityRequired;
        WastePercentage = wastePercentage;
        UpdateTimestamp();
    }
}
