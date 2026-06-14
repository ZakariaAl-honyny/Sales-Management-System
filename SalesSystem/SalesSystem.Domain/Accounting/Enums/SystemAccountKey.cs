namespace SalesSystem.Domain.Accounting.Enums;

/// <summary>
/// Defines the set of system account mapping keys.
/// Each key maps a business function to a Chart of Accounts AccountId.
/// This replaces the old fixed-column <c>SystemAccountMappings</c> table.
/// </summary>
public enum SystemAccountKey : byte
{
    /// <summary>Default cash (صندوق) — CashBox default linked account.</summary>
    DefaultCash = 1,

    /// <summary>Default bank account.</summary>
    DefaultBank = 2,

    /// <summary>Accounts Receivable (عملاء).</summary>
    AccountsReceivable = 3,

    /// <summary>Accounts Payable (موردين).</summary>
    AccountsPayable = 4,

    /// <summary>Inventory asset account.</summary>
    Inventory = 5,

    /// <summary>Cost of Goods Sold (تكلفة المبيعات).</summary>
    CostOfGoodsSold = 6,

    /// <summary>Sales Revenue (إيرادات المبيعات).</summary>
    SalesRevenue = 7,

    /// <summary>Sales Returns (مردودات المبيعات).</summary>
    SalesReturns = 8,

    /// <summary>Purchase Returns (مردودات المشتريات).</summary>
    PurchaseReturns = 9,

    /// <summary>VAT Output (ضريبة المبيعات).</summary>
    VatOutput = 10,

    /// <summary>VAT Input (ضريبة المشتريات).</summary>
    VatInput = 11,

    /// <summary>Capital account (رأس المال).</summary>
    Capital = 12,

    /// <summary>Opening Balance Equity (أرصدة افتتاحية).</summary>
    OpeningBalanceEquity = 13,

    /// <summary>Retained Earnings (الأرباح المحتجزة).</summary>
    RetainedEarnings = 14,

    /// <summary>Undistributed profits (أرباح وفاق — used for annual closing).</summary>
    UndistributedProfits = 15,

    /// <summary>Inventory Shortage / عجز مخزون (expense for physical count shortages).</summary>
    InventoryShortage = 16,

    /// <summary>Inventory Surplus / زيادة مخزون (revenue for physical count surpluses).</summary>
    InventorySurplus = 17,
}
