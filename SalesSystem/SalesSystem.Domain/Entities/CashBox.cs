using SalesSystem.Domain.Accounting.Entities;
using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Represents a cash register location. Balance is tracked on the linked Account
/// in the Chart of Accounts. This entity is a lightweight register identifier
/// with metadata for operational use.
/// Schema: §4.3 CashBoxes — Id, AccountId (int not null FK),
/// Name, Description (nullable), IsActive, CreatedByUserId, UpdatedByUserId, CreatedAt, UpdatedAt.
/// </summary>
public class CashBox : ActivatableEntity
{
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// FK to the Chart of Accounts Account that holds this cash box's balance.
    /// Required — the service layer resolves the account before entity creation.
    /// </summary>
    public int AccountId { get; private set; }
    public Account? Account { get; private set; }

    /// <summary>
    /// Optional description or notes about this cash box.
    /// </summary>
    public string? Description { get; private set; }

    private CashBox() { } // EF Core

    /// <summary>
    /// Creates a new cash box with the specified name, account, and currency.
    /// AccountId is required — the service layer resolves auto-creation before calling this factory.
    /// </summary>
    public static CashBox Create(
        string name,
        int accountId,
        string? description = null,
        int? createdByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم الصندوق مطلوب");

        if (accountId <= 0)
            throw new DomainException("الحساب المحاسبي مطلوب");

        var box = new CashBox
        {
            Name = name.Trim(),
            AccountId = accountId,
            Description = description?.Trim(),
        };
        box.SetCreatedBy(createdByUserId);
        return box;
    }

    // ─── Domain Methods ───────────────────────────

    /// <summary>
    /// Updates the mutable fields of the cash box.
    /// </summary>
    public void Update(
        string name,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم الصندوق مطلوب");

        Name = name.Trim();
        Description = description?.Trim();
        UpdateTimestamp();
    }
}
