using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Accounting.Entities;

/// <summary>
/// Maps system-wide default accounts used by the accounting engine.
/// Only one row typically exists (BranchId = null for single-branch setups).
/// </summary>
public class SystemAccountMappings : BaseEntity
{
    public int? BranchId { get; private set; }

    // ─── Asset Accounts ──────────────────────────────
    public int DefaultCashAccountId { get; private set; }
    public int DefaultBankAccountId { get; private set; }
    public int InventoryAssetAccountId { get; private set; }
    public int AccountsReceivableAccountId { get; private set; }

    // ─── Liability Accounts ───────────────────────────
    public int AccountsPayableAccountId { get; private set; }
    public int VatOutputAccountId { get; private set; }
    public int VatInputAccountId { get; private set; }

    // ─── Equity Accounts ──────────────────────────────
    public int CapitalAccountId { get; private set; }

    // ─── Revenue Accounts ─────────────────────────────
    public int SalesRevenueAccountId { get; private set; }
    public int SalesReturnAccountId { get; private set; }

    // ─── Expense Accounts ─────────────────────────────
    public int CogsAccountId { get; private set; }
    public int GeneralExpenseAccountId { get; private set; }
    public int SpoilageLossAccountId { get; private set; }

    private SystemAccountMappings() { } // EF Core

    public static SystemAccountMappings Create(
        int defaultCashAccountId,
        int defaultBankAccountId,
        int inventoryAssetAccountId,
        int accountsReceivableAccountId,
        int accountsPayableAccountId,
        int vatOutputAccountId,
        int vatInputAccountId,
        int capitalAccountId,
        int salesRevenueAccountId,
        int salesReturnAccountId,
        int cogsAccountId,
        int generalExpenseAccountId,
        int spoilageLossAccountId,
        int? branchId = null,
        int? createdByUserId = null)
    {
        // ─── Essential Account Guards ─────────────────
        if (defaultCashAccountId <= 0)
            throw new DomainException("رقم حساب الصندوق النقدي مطلوب");
        if (defaultBankAccountId <= 0)
            throw new DomainException("رقم الحساب البنكي مطلوب");
        if (inventoryAssetAccountId <= 0)
            throw new DomainException("رقم حساب أصل المخزون مطلوب");
        if (accountsReceivableAccountId <= 0)
            throw new DomainException("رقم حساب الذمم المدينة مطلوب");
        if (accountsPayableAccountId <= 0)
            throw new DomainException("رقم حساب الذمم الدائنة مطلوب");
        if (salesRevenueAccountId <= 0)
            throw new DomainException("رقم حساب إيرادات المبيعات مطلوب");
        if (cogsAccountId <= 0)
            throw new DomainException("رقم حساب تكلفة البضاعة المباعة مطلوب");

        var mappings = new SystemAccountMappings
        {
            BranchId = branchId,
            DefaultCashAccountId = defaultCashAccountId,
            DefaultBankAccountId = defaultBankAccountId,
            InventoryAssetAccountId = inventoryAssetAccountId,
            AccountsReceivableAccountId = accountsReceivableAccountId,
            AccountsPayableAccountId = accountsPayableAccountId,
            VatOutputAccountId = vatOutputAccountId,
            VatInputAccountId = vatInputAccountId,
            CapitalAccountId = capitalAccountId,
            SalesRevenueAccountId = salesRevenueAccountId,
            SalesReturnAccountId = salesReturnAccountId,
            CogsAccountId = cogsAccountId,
            GeneralExpenseAccountId = generalExpenseAccountId,
            SpoilageLossAccountId = spoilageLossAccountId
        };
        mappings.SetCreatedBy(createdByUserId);
        return mappings;
    }

    /// <summary>
    /// Returns the default account ID based on the payment method.
    /// </summary>
    public int GetPaymentAccountId(string paymentMethod)
    {
        if (string.Equals(paymentMethod, "Cash", StringComparison.OrdinalIgnoreCase))
            return DefaultCashAccountId;

        return DefaultBankAccountId;
    }
}
