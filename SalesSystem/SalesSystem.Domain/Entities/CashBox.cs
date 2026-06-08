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
public class CashBox : BaseEntity
{
    public string BoxName { get; private set; } = string.Empty;

    /// <summary>
    /// FK to the Chart of Accounts Account that holds this cash box's balance.
    /// Nullable to support migration of existing data — auto-created by service layer
    /// when not provided. Domain validation requires a valid AccountId for new boxes.
    /// </summary>
    public int? AccountId { get; private set; }
    public Account? Account { get; private set; }

    /// <summary>
    /// FK to Categories table for classifying the cash box type
    /// (e.g., cashier, fund, bank, representative custody).
    /// </summary>
    public int? CategoryId { get; private set; }
    public Category? Category { get; private set; }

    public int? BranchId { get; private set; }
    public int? CurrencyId { get; private set; }
    public Currency? Currency { get; private set; }
    public int? AssignedUserId { get; private set; } // NULL = shared box
    public string? PhoneNumber { get; private set; }
    public string? TaxNumber { get; private set; }
    public string? Address { get; private set; }
    public string? Notes { get; private set; }

    // Navigation
    private readonly List<CashTransaction> _transactions = new();
    public IReadOnlyCollection<CashTransaction> Transactions => _transactions.AsReadOnly();

    private CashBox() { } // EF Core

    /// <summary>
    /// Creates a new cash box. AccountId may be omitted — the service layer
    /// auto-creates a Chart of Accounts sub-account under "1110 — النقدية".
    /// </summary>
    public static CashBox Create(
        string boxName,
        int? accountId = null,
        int? categoryId = null,
        int? branchId = null,
        int? assignedUserId = null,
        int? currencyId = null,
        string? phoneNumber = null,
        string? taxNumber = null,
        string? address = null,
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(boxName))
            throw new DomainException("اسم الصندوق مطلوب");

        // AccountId is validated at the service layer — auto-created if null

        return new CashBox
        {
            BoxName = boxName.Trim(),
            AccountId = accountId,
            CategoryId = categoryId,
            BranchId = branchId,
            AssignedUserId = assignedUserId,
            CurrencyId = currencyId,
            PhoneNumber = phoneNumber?.Trim(),
            TaxNumber = taxNumber?.Trim(),
            Address = address?.Trim(),
            Notes = notes?.Trim(),
            IsActive = true
        };
    }

    // ─── Domain Methods ───────────────────────────

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
        int? categoryId,
        int? branchId,
        int? assignedUserId,
        int? currencyId)
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

        if (categoryId.HasValue)
            CategoryId = categoryId.Value > 0 ? categoryId : null;

        if (branchId.HasValue)
            BranchId = branchId.Value > 0 ? branchId : null;

        if (assignedUserId.HasValue)
            AssignedUserId = assignedUserId.Value > 0 ? assignedUserId : null;

        if (currencyId.HasValue)
            CurrencyId = currencyId.Value > 0 ? currencyId : null;

        UpdateTimestamp();
    }
}