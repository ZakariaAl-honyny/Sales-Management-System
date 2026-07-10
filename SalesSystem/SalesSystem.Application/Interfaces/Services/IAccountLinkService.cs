using SalesSystem.Contracts.Common;

namespace SalesSystem.Application.Interfaces.Services;

/// <summary>
/// Centralized service for linking operational entities (Customer, Supplier, Employee, CashBox, Bank)
/// to Chart of Accounts. Every entity that has an AccountId FK MUST go through this service
/// for account creation, name sync, activate, deactivate, and permanent delete.
/// Per accounts summry.md: the operational entity is the Source of Truth; Account is a reflection.
/// </summary>
public interface IAccountLinkService
{
    /// <summary>Creates a Level 4 detail account under parent "1130 — العملاء" for a customer.</summary>
    Task<Result<int>> CreateCustomerAccountAsync(string customerName, int createdByUserId, CancellationToken ct);

    /// <summary>Creates a Level 4 detail account under parent "2101 — الموردون" for a supplier.</summary>
    Task<Result<int>> CreateSupplierAccountAsync(string supplierName, int createdByUserId, CancellationToken ct);

    /// <summary>Creates a Level 4 detail account under parent "1107 — عهد الموظفين" for an employee.</summary>
    Task<Result<int>> CreateEmployeeAccountAsync(string employeeName, int? createdByUserId, CancellationToken ct);

    /// <summary>Syncs Account.NameAr + Account.NameEn when the linked entity's Name changes.</summary>
    Task SyncNameAsync(int accountId, string newName, CancellationToken ct);

    /// <summary>Activates the linked Account (Account.Activate()).</summary>
    Task ActivateAsync(int accountId, CancellationToken ct);

    /// <summary>Deactivates the linked Account (Account.Deactivate()).</summary>
    Task DeactivateAsync(int accountId, CancellationToken ct);

    /// <summary>Permanently deletes the linked Account (Account.MarkAsDeleted()).</summary>
    Task MarkAsDeletedAsync(int accountId, CancellationToken ct);
}
