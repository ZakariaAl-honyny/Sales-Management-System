using SalesSystem.Domain.Accounting.Enums;
using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Accounting.Entities;

/// <summary>
/// Key-Value mapping between a business function and a Chart of Accounts AccountId.
/// Each <see cref="SystemAccountKey"/> maps to exactly one Account per Branch.
/// Inherits AuditableEntity for CreatedBy/UpdatedBy tracking without soft delete.
/// </summary>
public class SystemAccountMapping : AuditableEntity
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
    /// Branch identifier for multi-branch support (nullable, schema smallint).
    /// </summary>
    public short? BranchId { get; private set; }

    private SystemAccountMapping() { } // EF Core

    /// <summary>
    /// Creates a new system account mapping with validation.
    /// </summary>
    public static SystemAccountMapping Create(
        SystemAccountKey mappingKey,
        int accountId,
        short? branchId = null)
    {
        if (accountId <= 0)
            throw new DomainException("رقم الحساب المحاسبي غير صالح");

        if (!Enum.IsDefined(typeof(SystemAccountKey), mappingKey))
            throw new DomainException("مفتاح الربط غير صالح");

        return new SystemAccountMapping
        {
            MappingKey = mappingKey,
            AccountId = accountId,
            BranchId = branchId
        };
    }

    /// <summary>
    /// Updates the account mapping to a different AccountId.
    /// </summary>
    public void Update(int accountId)
    {
        if (accountId <= 0)
            throw new DomainException("رقم الحساب المحاسبي غير صالح");

        AccountId = accountId;
        UpdateTimestamp();
    }
}
