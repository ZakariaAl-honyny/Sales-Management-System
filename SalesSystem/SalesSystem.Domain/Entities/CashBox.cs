using SalesSystem.Domain.Accounting.Entities;
using SalesSystem.Domain.Common;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Represents a cash register location. Balance is tracked on the linked Account
/// in the Chart of Accounts. This entity is a lightweight register identifier
/// with metadata (category, contact info) for operational use.
/// </summary>
public class CashBox : ActivatableEntity
{
    public string BoxName { get; private set; } = string.Empty;

    /// <summary>
    /// FK to the Chart of Accounts Account that holds this cash box's balance.
    /// If null at creation time, the service layer auto-creates a sub-account under "1110 — النقدية".
    /// </summary>
    public int? AccountId { get; private set; }
    public Account? Account { get; private set; }

    public short? BranchId { get; private set; }

    /// <summary>
    /// FK to Currencies table (required). The currency this cash box operates in.
    /// </summary>
    public short CurrencyId { get; private set; }
    public Currency? Currency { get; private set; }
    public int? AssignedUserId { get; private set; } // NULL = shared box
    public string? PhoneNumber { get; private set; }
    public string? TaxNumber { get; private set; }
    public string? Address { get; private set; }
    public string? Notes { get; private set; }

    private CashBox() { } // EF Core

    /// <summary>
    /// Creates a new cash box with required CurrencyId.
    /// AccountId can be null — the service layer auto-creates a
    /// Chart of Accounts sub-account under "1110 — النقدية" and calls SetAccountId.
    /// </summary>
    public static CashBox Create(
        string boxName,
        short currencyId,
        int? accountId = null,
        short? branchId = null,
        int? assignedUserId = null,
        string? phoneNumber = null,
        string? taxNumber = null,
        string? address = null,
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(boxName))
            throw new DomainException("اسم الصندوق مطلوب");

        if (currencyId <= 0)
            throw new DomainException("عملة الصندوق مطلوبة");

        return new CashBox
        {
            BoxName = boxName.Trim(),
            AccountId = accountId,
            BranchId = branchId,
            AssignedUserId = assignedUserId,
            CurrencyId = currencyId,
            PhoneNumber = phoneNumber?.Trim(),
            TaxNumber = taxNumber?.Trim(),
            Address = address?.Trim(),
            Notes = notes?.Trim(),
        };
    }

    // ─── Domain Methods ───────────────────────────

    /// <summary>
    /// Sets the Chart of Accounts AccountId for this cash box.
    /// Called by the service layer after auto-creating a sub-account under "1110 — النقدية".
    /// Only allowed when the current AccountId is null (not yet set).
    /// </summary>
    public void SetAccountId(int accountId)
    {
        if (accountId <= 0)
            throw new DomainException("معرف الحساب غير صالح");
        if (AccountId.HasValue)
            throw new DomainException("لا يمكن تغيير الحساب المحاسبي للصندوق بعد تعيينه");

        AccountId = accountId;
        UpdateTimestamp();
    }

    /// <summary>
    /// Validates that a user can access this box.
    /// Shared boxes (AssignedUserId = null) can be accessed by anyone.
    /// </summary>
    public void ValidateUserAccess(int userId)
    {
        if (AssignedUserId.HasValue && AssignedUserId.Value != userId)
            throw new DomainException(
                $"ليس لديك صلاحية الوصول إلى الصندوق '{BoxName}'. " +
                $"تواصل مع المدير لتغيير الصلاحيات.");
    }

    /// <summary>
    /// Updates the box name.
    /// </summary>
    public void UpdateName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new DomainException("اسم الصندوق مطلوب");

        BoxName = newName.Trim();
    }

    /// <summary>
    /// Updates all mutable fields of the cash box.
    /// Only non-null values are applied — null means "keep current value".
    /// </summary>
    public void Update(
        string? boxName,
        string? phoneNumber,
        string? taxNumber,
        string? address,
        string? notes,
        short? branchId,
        int? assignedUserId,
        short currencyId)
    {
        if (boxName != null)
        {
            if (string.IsNullOrWhiteSpace(boxName))
                throw new DomainException("اسم الصندوق مطلوب");
            BoxName = boxName.Trim();
        }

        if (phoneNumber != null)
            PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();

        if (taxNumber != null)
            TaxNumber = string.IsNullOrWhiteSpace(taxNumber) ? null : taxNumber.Trim();

        if (address != null)
            Address = string.IsNullOrWhiteSpace(address) ? null : address.Trim();

        if (notes != null)
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();

        if (branchId.HasValue)
            BranchId = branchId.Value > 0 ? branchId : null;

        if (assignedUserId.HasValue)
            AssignedUserId = assignedUserId.Value > 0 ? assignedUserId : null;

        if (currencyId <= 0)
            throw new DomainException("عملة الصندوق مطلوبة");
        CurrencyId = currencyId;

        UpdateTimestamp();
    }
}