using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Accounting.Entities;

/// <summary>
/// Key-Value mapping between a business function and a Chart of Accounts AccountId.
/// MappingKey is stored as nvarchar(100) — e.g. "SalesRevenue", "COGS".
/// Inherits Entity (no audit fields) — this is a pure junction/configuration table.
/// Schema: §4.10 SystemAccountMappings.
/// </summary>
public class SystemAccountMapping : Entity
{
    /// <summary>
    /// Business function key as string (nvarchar(100), unique).
    /// Values match SystemAccountKey enum names for reference.
    /// </summary>
    public string MappingKey { get; private set; } = string.Empty;

    /// <summary>
    /// Mapped AccountId from the Accounts table.
    /// </summary>
    public int AccountId { get; private set; }
    public Account? Account { get; private set; }

    /// <summary>
    /// Branch identifier for multi-branch override (nullable, schema smallint).
    /// </summary>
    public short? BranchId { get; private set; }

    private SystemAccountMapping() { } // EF Core

    /// <summary>
    /// Creates a new system account mapping with validation.
    /// </summary>
    public static SystemAccountMapping Create(
        string mappingKey,
        int accountId,
        short? branchId = null)
    {
        if (string.IsNullOrWhiteSpace(mappingKey))
            throw new DomainException("مفتاح الربط مطلوب");

        if (mappingKey.Trim().Length > 100)
            throw new DomainException("مفتاح الربط لا يمكن أن يتجاوز 100 حرف");

        if (accountId <= 0)
            throw new DomainException("رقم الحساب المحاسبي غير صالح");

        return new SystemAccountMapping
        {
            MappingKey = mappingKey.Trim(),
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
    }
}
