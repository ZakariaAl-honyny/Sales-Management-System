using SalesSystem.Domain.Accounting.Enums;
using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Accounting.Entities;

/// <summary>
/// Key-Value mapping between a business function and a Chart of Accounts AccountId.
/// Each <see cref="SystemAccountKey"/> maps to exactly one Account per Branch.
/// </summary>
public class SystemAccountMapping : Entity
{
    /// <summary>
    /// Mapped AccountId from the Accounts table.
    /// </summary>
    public int AccountId { get; private set; }
    public Account? Account { get; private set; }

    /// <summary>
    /// The business function this mapping represents.
    /// </summary>
    public SystemAccountKey MappingKey { get; private set; }

    /// <summary>
    /// Branch identifier for multi-branch support (default = 0 = Main Branch).
    /// </summary>
    public int BranchId { get; private set; }

    /// <summary>
    /// Optional description in Arabic.
    /// </summary>
    public string? DescriptionAr { get; private set; }

    /// <summary>
    /// Optional description in English.
    /// </summary>
    public string? DescriptionEn { get; private set; }

    /// <summary>
    /// Whether this mapping is active.
    /// </summary>
    public bool IsActive { get; private set; } = true;

    private SystemAccountMapping() { } // EF Core

    /// <summary>
    /// Creates a new system account mapping with validation.
    /// </summary>
    public static SystemAccountMapping Create(
        SystemAccountKey mappingKey,
        int accountId,
        int branchId = 0,
        string? descriptionAr = null,
        string? descriptionEn = null)
    {
        if (accountId <= 0)
            throw new DomainException("رقم الحساب المحاسبي غير صالح");

        if (!Enum.IsDefined(typeof(SystemAccountKey), mappingKey))
            throw new DomainException("مفتاح الربط غير صالح");

        if (branchId < 0)
            throw new DomainException("رقم الفرع غير صالح");

        return new SystemAccountMapping
        {
            MappingKey = mappingKey,
            AccountId = accountId,
            BranchId = branchId,
            DescriptionAr = descriptionAr?.Trim(),
            DescriptionEn = descriptionEn?.Trim(),
            IsActive = true
        };
    }

    /// <summary>
    /// Updates the account mapping to a different AccountId.
    /// </summary>
    public void Update(int accountId, string? descriptionAr = null, string? descriptionEn = null)
    {
        if (accountId <= 0)
            throw new DomainException("رقم الحساب المحاسبي غير صالح");

        AccountId = accountId;

        if (descriptionAr != null)
            DescriptionAr = string.IsNullOrWhiteSpace(descriptionAr) ? null : descriptionAr.Trim();

        if (descriptionEn != null)
            DescriptionEn = string.IsNullOrWhiteSpace(descriptionEn) ? null : descriptionEn.Trim();
    }

    /// <summary>
    /// Soft-delete this mapping.
    /// </summary>
    public void MarkAsDeleted()
    {
        IsActive = false;
    }

    /// <summary>
    /// Restore a soft-deleted mapping.
    /// </summary>
    public void Restore()
    {
        IsActive = true;
    }

    /// <summary>
    /// Sets the branch for this mapping.
    /// </summary>
    public void SetBranchId(int branchId)
    {
        if (branchId < 0)
            throw new DomainException("رقم الفرع غير صالح");
        BranchId = branchId;
    }
}
