using SalesSystem.Domain.Accounting.Enums;

namespace SalesSystem.Contracts.DTOs;

public record UserDto(int Id, string UserName, string FullName, byte Role,
    byte Status, bool MustChangePassword, DateTime? PasswordChangedAt,
    string? Phone, string? Email, string? AvatarPath,
    DateTime? LastLoginAt, int LoginAttempts, int? DefaultCashBoxId)
{
    public bool IsActive => Status == 1;
}

public record AuditLogDto(long Id, int? UserId, string? UserName, string Action,
    string EntityType, int? EntityId, string? Details, string? IpAddress, DateTime Timestamp);

public record PermissionDto(int Id, string Name, string DisplayNameAr, string? Category, bool IsActive);

public record RolePermissionDto(byte Role, List<int> PermissionIds);

public record CurrentUserDto(int Id, string UserName, string FullName, byte Role,
    string? AvatarPath, List<string> Permissions);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword, string ConfirmPassword);

public record UnitDto(int Id, string Name, string? Symbol, bool IsActive);

public record CategoryDto(int Id, string Name, string? Description, bool IsActive);

public record ProductDto(
    int Id,
    string? Barcode,
    string Name,
    int? CategoryId,
    string? CategoryName,
    int? UnitId, // Legacy
    string? UnitName, // Legacy
    int? WholesaleUnitId,
    string? WholesaleUnitName,
    int? RetailUnitId,
    string? RetailUnitName,
    decimal ConversionFactor,
    decimal PurchasePrice,
    decimal SalePrice, // Legacy
    decimal WholesalePrice,
    decimal RetailPrice,
    decimal MinStock,
    string? Description,
    DateTime? ExpirationDate,
    string? ImagePath,  // مسار الصورة المحلي (اختياري)
    bool IsActive,
    decimal CurrentStock = 0)
{
    public bool IsOutOfStock => CurrentStock <= 0;
    public bool IsLowStock => CurrentStock > 0 && CurrentStock <= MinStock;
    public string StockStatusLabel => IsOutOfStock ? "نفذ" : IsLowStock ? "محدود" : "";
}

public record WarehouseDto(int Id, string Name, string? Location, bool IsDefault, bool IsActive);

public record WarehouseStockDto(
    int WarehouseId,
    string? WarehouseName,
    int ProductId,
    string ProductName,
    string? UnitName,
    decimal Quantity,
    decimal ReorderLevel);

public record SupplierDto(int Id, string Name, string? Phone, string? Email, string? Address, 
    string? TaxNumber, decimal OpeningBalance, decimal CurrentBalance, decimal CreditLimit, bool IsActive,
    int? AccountId = null, string? AccountName = null);
public record CustomerDto(int Id, string Name, string? Phone, string? Email, string? Address, 
    string? TaxNumber, decimal OpeningBalance, decimal CurrentBalance, decimal CreditLimit, bool IsActive,
    int? AccountId = null, string? AccountName = null,
    int? CustomerGroupId = null, string? CustomerGroupName = null)
{
    public bool IsBalanceNegative 
    { 
        get => CurrentBalance > 0; 
    }
}

public record CustomerGroupDto(int Id, string Name, string? Description, bool IsActive);


public record SalesInvoiceDto(
    int Id,
    int InvoiceNo,
    int? CustomerId,
    string? CustomerName,
    int WarehouseId,
    string WarehouseName,
    DateTime InvoiceDate,
    DateOnly? DueDate,
    byte PaymentType,
    decimal SubTotal,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal DueAmount,
    string? Notes,
    byte Status,
    int? TaxId,
    string? TaxName,
    decimal? TaxRate,
    int? CurrencyId,
    decimal? ExchangeRate,
    IReadOnlyList<SalesInvoiceItemDto> Items)
{
    public string PaymentTypeDisplay => PaymentType switch
    {
        1 => "نقدي",
        2 => "آجل",
        3 => "مختلط",
        _ => "غير معروف"
    };

    public string StatusDisplay => Status switch
    {
        1 => "مسودة",
        2 => "تم الترحيل",
        3 => "ملغي",
        _ => "غير معروف"
    };
}

public record SalesInvoiceItemDto(int Id, int ProductId, string ProductName,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountAmount,
    decimal LineTotal,
    byte Mode);

public record PurchaseInvoiceDto(
    int Id,
    int InvoiceNo,
    int SupplierId,
    string SupplierName,
    int WarehouseId,
    string WarehouseName,
    DateTime InvoiceDate,
    DateOnly? DueDate,
    byte PaymentType,
    decimal SubTotal,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal DueAmount,
    string? SupplierInvoiceNo,
    string? Notes,
    byte Status,
    int? TaxId,
    string? TaxName,
    decimal? TaxRate,
    int? CurrencyId,
    decimal? ExchangeRate,
    IReadOnlyList<PurchaseInvoiceItemDto> Items)
{
    public string PaymentTypeDisplay => PaymentType switch
    {
        1 => "نقدي",
        2 => "آجل",
        3 => "مختلط",
        _ => "غير معروف"
    };

    public string StatusDisplay => Status switch
    {
        1 => "مسودة",
        2 => "تم الترحيل",
        3 => "ملغي",
        _ => "غير معروف"
    };
}

public record PurchaseInvoiceItemDto(int Id, int ProductId, string ProductName,
    decimal Quantity,
    decimal UnitCost,
    decimal DiscountAmount,
    decimal LineTotal,
    byte Mode);

public record SalesReturnDto(
    int Id,
    string ReturnNo,
    int WarehouseId,
    string WarehouseName,
    int? CustomerId,
    string CustomerName,
    int? SalesInvoiceId,
    DateTime ReturnDate,
    decimal SubTotal,
    decimal TaxAmount,
    decimal DiscountAmount,
    decimal TotalAmount,
    int? CurrencyId,
    decimal? ExchangeRate,
    string? Notes,
    byte Status, IReadOnlyList<SalesReturnItemDto> Items)
{
    public string StatusDisplay => Status switch
    {
        1 => "مسودة",
        2 => "تم الترحيل",
        3 => "ملغي",
        _ => "غير معروف"
    };
}

public record SalesReturnItemDto(int Id, int ProductId, string ProductName,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountAmount,
    decimal LineTotal,
    byte Mode);

public record PurchaseReturnDto(
    int Id,
    string ReturnNo,
    int WarehouseId,
    string WarehouseName,
    int SupplierId,
    string SupplierName,
    int? PurchaseInvoiceId,
    DateTime ReturnDate,
    decimal SubTotal,
    decimal TaxAmount,
    decimal DiscountAmount,
    decimal TotalAmount,
    int? CurrencyId,
    decimal? ExchangeRate,
    string? Notes,
    byte Status, IReadOnlyList<PurchaseReturnItemDto> Items)
{
    public string StatusDisplay => Status switch
    {
        1 => "مسودة",
        2 => "تم الترحيل",
        3 => "ملغي",
        _ => "غير معروف"
    };
}

public record PurchaseReturnItemDto(int Id, int ProductId, string ProductName,
    decimal Quantity,
    decimal UnitCost,
    decimal DiscountAmount,
    decimal LineTotal,
    byte Mode);

public record StockTransferDto(
    int Id,
    string TransferNo,
    int FromWarehouseId,
    string FromWarehouseName,
    int ToWarehouseId,
    string ToWarehouseName,
    DateTime TransferDate,
    string? Notes,
    byte Status, IReadOnlyList<StockTransferItemDto> Items)
{
    public string StatusDisplay => Status switch
    {
        1 => "مسودة",
        2 => "تم الترحيل",
        3 => "ملغي",
        _ => "غير معروف"
    };
}

public record StockTransferItemDto(int Id, int ProductId, string ProductName, decimal Quantity, byte Mode, string? Notes);

public record CustomerPaymentDto(
    int Id,
    string PaymentNo,
    int CustomerId,
    string CustomerName,
    decimal Amount,
    byte PaymentMethod,
    int? CurrencyId,
    decimal? ExchangeRate,
    DateTime PaymentDate,
    int? SalesInvoiceId,
    string? Notes)
{
    public string PaymentTypeDisplay => PaymentMethod switch
    {
        1 => "نقدي",
        2 => "آجل",
        3 => "مختلط",
        _ => "غير معروف"
    };
}

public record SupplierPaymentDto(
    int Id,
    string PaymentNo,
    int SupplierId,
    string SupplierName,
    decimal Amount,
    byte PaymentMethod,
    int? CurrencyId,
    decimal? ExchangeRate,
    DateTime PaymentDate,
    int? PurchaseInvoiceId,
    string? Notes)
{
    public string PaymentTypeDisplay => PaymentMethod switch
    {
        1 => "نقدي",
        2 => "آجل",
        3 => "مختلط",
        _ => "غير معروف"
    };
}

public record InventoryMovementDto(
    long Id,
    int ProductId,
    string ProductName,
    int WarehouseId,
    string WarehouseName,
    byte MovementType,
    decimal QuantityChange,
    decimal QuantityBefore,
    decimal QuantityAfter,
    string ReferenceType,
    int ReferenceId,
    DateTime MovementDate,
    string? Notes);

public record PrintSettingsDto(
    string ThermalPrinterName,
    string A4PrinterName,
    string LogoPath,
    string StoreTaxNumber,
    decimal TaxRate,
    bool AutoPrintOnPost,
    string ReceiptHeader,
    string ReceiptFooter,
    int EscPosCodePage,
    string PaperSize,
    int PrintCopies,
    bool ShowBalanceOnPrint,
    bool PrintSignature,
    bool ShowLogo = true,
    string FooterNote = "");

public record StoreSettingsDto(
    int Id,
    string StoreName,
    string? Phone,
    string? Address,
    string? LogoPath,
    string? Email,
    string CurrencyCode,
    decimal DefaultTaxRate, // DEPRECATED: DefaultTaxRate — use Tax entity instead (kept for backwards compat). Remove in Phase 20.
    bool IsTaxEnabled,      // DEPRECATED: IsTaxEnabled — use Tax entity instead (kept for backwards compat). Remove in Phase 20.
    string? TaxNumber,
    bool EnableStockAlerts,
    bool AllowNegativeStock,
    bool AutoUpdatePrices,
    string InvoicePrefix,    // DEPRECATED: InvoicePrefix — use InvoiceNo (int) instead (kept for backwards compat). Remove in Phase 20.
    int CostingMethod = 1,
    string? BackupPath = null,
    string? BackupScheduleTime = "02:00",
    int BackupRetentionDays = 30,
    string? UpdateServerUrl = null,
    string? SignaturePath = null);

public record DocumentSequenceDto(int Id, string DocumentType, string Prefix, int Year, int LastNumber);

public record DashboardSummaryDto(
    decimal TotalSalesToday,
    int NumberOfSalesToday,
    decimal TotalPurchasesToday,
    int LowStockItemsCount,
    int ActiveCustomersCount,
    int ActiveSuppliersCount,
    int TotalProductsCount,
    decimal TotalReceivables,
    decimal TotalPayables,
    decimal TotalSalesMonth,
    decimal TotalPurchasesMonth);

public record SalesReportDto(
    DateTime InvoiceDate,
    int Id,
    string CustomerName,
    decimal SubTotal,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal DueAmount
);

public record PurchaseReportDto(
    DateTime InvoiceDate,
    int Id,
    string SupplierName,
    decimal SubTotal,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal DueAmount
);

public record StockReportDto(
    int ProductId,
    string ProductName,
    string CategoryName,
    string UnitName,
    string WarehouseName,
    decimal CurrentStock,
    decimal ReorderLevel,
    decimal PurchasePrice,
    decimal TotalValue
);

public record CustomerFinancialBalanceDto(
    int CustomerId,
    string CustomerName,
    decimal OpeningBalance,
    decimal TotalSales,
    decimal TotalReturns,
    decimal TotalPayments,
    decimal TotalCredit,
    decimal CurrentBalance
);

public record SupplierBalanceReportDto(
    int SupplierId,
    string SupplierName,
    decimal OpeningBalance,
    decimal TotalPurchases,
    decimal TotalReturns,
    decimal TotalPayments,
    decimal TotalDebit,
    decimal CurrentBalance
);

public record ProductMovementReportDto(
    DateTime Date,
    string WarehouseName,
    string MovementType,
    string ReferenceNo,
    decimal QuantityChange,
    decimal QuantityAfter
);

public record StockWriteOffDto(
    int Id,
    int ProductId,
    string? ProductName,
    int WarehouseId,
    string? WarehouseName,
    decimal Quantity,
    DateTime WriteOffDate,
    string Reason,
    int? UnitId,
    int CreatedByUserId,
    DateTime CreatedAt);

public record ExpiredProductDto(
    int ProductId,
    string ProductName,
    string? CategoryName,
    string? WarehouseName,
    decimal CurrentStock,
    DateTime ExpirationDate,
    int DaysExpired);

public record LowStockReportDto(
    int     ProductId,
    string  ProductName,
    string? CategoryName,
    string  WarehouseName,
    decimal CurrentRetailQty,
    decimal ReorderLevelRetailQty,
    decimal DeficitRetailQty,
    decimal SuggestedWholesaleBoxes,   // Product.ConvertRetailToWholesaleBoxes()
    decimal SuggestedRetailRemainder,  // Product.GetRemainingRetailAfterWholesale()
    string  WholesaleUnitName,
    string  RetailUnitName,
    decimal ConversionFactor
);

// Financial Reports DTOs
public record IncomeStatementDto(
    string Category,
    string Description,
    decimal Amount);

public record CashFlowItemDto(
    string Category,
    decimal Amount);

public record CashFlowReportDto(
    decimal OpeningBalance,
    decimal TotalIncome,
    decimal TotalExpense,
    decimal NetCashFlow,
    decimal ClosingBalance,
    List<CashFlowItemDto> IncomeItems,
    List<CashFlowItemDto> ExpenseItems);

public record VatReportDto(
    string InvoiceNumber,
    DateTime InvoiceDate,
    string? PartyName,
    decimal TaxableAmount,
    decimal TaxRate,
    decimal TaxAmount);

public record TaxDto(int Id, string Name, decimal Rate, bool IsDefault, bool IsActive);

public record CurrencyDto(
    int Id,
    string Name,
    string Code,
    string Symbol,
    decimal ExchangeRateToBase,
    bool IsBaseCurrency,
    string? FractionName,
    bool IsSystem,
    bool IsActive);

public record ExchangeRateHistoryDto(
    int Id,
    int CurrencyId,
    decimal OldRate,
    decimal NewRate,
    DateOnly EffectiveDate,
    string? RateType,
    string? Notes,
    int? ChangedByUserId);

// ═══════════════════════════════════════════════════════
// Phase 22 — Chart of Accounts DTOs
// ═══════════════════════════════════════════════════════

public record AccountDto(
    int Id,
    string AccountCode,
    string NameAr,
    string NameEn,
    byte AccountType,
    int Level,
    int? ParentAccountId,
    string? ParentAccountName,
    bool IsSystemAccount,
    bool IsActive,
    string? Description,
    string? ColorCode,
    bool AllowTransactions,
    decimal? OpeningBalance,
    string? Explanation,
    string? Notes)
{
    public string AccountTypeDisplay => AccountType switch
    {
        1 => "أصل",
        2 => "خصم",
        3 => "حق ملكية",
        4 => "إيراد",
        5 => "مصروف",
        _ => "غير معروف"
    };

    public string LevelDisplay => Level switch
    {
        1 => "رئيسي (مجموعة)",
        2 => "فرعي",
        3 => "فرعي فرعي",
        4 => "تفصيلي",
        _ => "غير معروف"
    };
}

public record AccountTreeNodeDto(
    int Id,
    string AccountCode,
    string NameAr,
    byte AccountType,
    int Level,
    string? ColorCode,
    bool AllowTransactions,
    decimal? OpeningBalance,
    string? Explanation,
    List<AccountTreeNodeDto> Children);

// ─── Accounting DTOs ─────────────────────────────────
public record AccountBalanceDto(
    int AccountId,
    string AccountCode,
    string AccountNameAr,
    AccountType AccountType,
    decimal TotalDebit,
    decimal TotalCredit,
    decimal Balance,
    bool IsDebitNormal
);

public record AccountLedgerDto(
    string AccountCode,
    string AccountNameAr,
    decimal OpeningBalance,
    List<AccountLedgerLineDto> Lines,
    decimal TotalDebit,
    decimal TotalCredit,
    decimal ClosingBalance
);

public record AccountLedgerLineDto(
    DateTime Date,
    string EntryNumber,
    string Description,
    string? ReferenceNumber,
    decimal Debit,
    decimal Credit,
    decimal RunningBalance
);

// ─── Journal Entry List/Detail DTOs ──────────────────

public record JournalEntryListDto(
    int Id,
    string EntryNumber,
    DateTime TransactionDate,
    string Description,
    string EntryType,
    string? ReferenceType,
    int? ReferenceId,
    string? ReferenceNumber,
    decimal TotalDebit,
    decimal TotalCredit,
    bool IsPosted,
    bool IsReversed,
    DateTime CreatedAt,
    int? CreatedByUserId
);

public record JournalEntryDetailDto(
    int Id,
    string EntryNumber,
    DateTime TransactionDate,
    string Description,
    string EntryType,
    string? ReferenceType,
    int? ReferenceId,
    string? ReferenceNumber,
    bool IsPosted,
    bool IsReversed,
    int? ReversedByEntryId,
    DateTime CreatedAt,
    int? CreatedByUserId,
    List<JournalEntryLineDetailDto> Lines
);

public record JournalEntryLineDetailDto(
    int Id,
    int AccountId,
    string AccountCode,
    string AccountNameAr,
    decimal Debit,
    decimal Credit,
    string? Description
);

public record AccountStatementDto(
    DateTime Date,
    string Description,
    string ReferenceNumber,
    decimal Debit,
    decimal Credit,
    decimal Balance
);

public record SystemAccountMappingsDto(
    int Id,
    int DefaultCashAccountId,
    string? DefaultCashAccountName,
    string? DefaultCashAccountCode,
    int DefaultBankAccountId,
    string? DefaultBankAccountName,
    string? DefaultBankAccountCode,
    int InventoryAssetAccountId,
    string? InventoryAssetAccountName,
    string? InventoryAssetAccountCode,
    int AccountsReceivableAccountId,
    string? AccountsReceivableAccountName,
    string? AccountsReceivableAccountCode,
    int AccountsPayableAccountId,
    string? AccountsPayableAccountName,
    string? AccountsPayableAccountCode,
    int VatOutputAccountId,
    string? VatOutputAccountName,
    string? VatOutputAccountCode,
    int VatInputAccountId,
    string? VatInputAccountName,
    string? VatInputAccountCode,
    int CapitalAccountId,
    string? CapitalAccountName,
    string? CapitalAccountCode,
    int SalesRevenueAccountId,
    string? SalesRevenueAccountName,
    string? SalesRevenueAccountCode,
    int SalesReturnAccountId,
    string? SalesReturnAccountName,
    string? SalesReturnAccountCode,
    int CogsAccountId,
    string? CogsAccountName,
    string? CogsAccountCode,
    int GeneralExpenseAccountId,
    string? GeneralExpenseAccountName,
    string? GeneralExpenseAccountCode,
    int SpoilageLossAccountId,
    string? SpoilageLossAccountName,
    string? SpoilageLossAccountCode,
    int? OpeningBalanceEquityAccountId,
    string? OpeningBalanceEquityAccountName,
    string? OpeningBalanceEquityAccountCode,
    int? BranchId
);

// ─── Audit ────────────────────────────────────────────

public record AuditLogQuery
{
    public int? UserId { get; init; }
    public string? Action { get; init; }
    public string? EntityType { get; init; }
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}

public record PaginatedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

// ═══════════════════════════════════════════════════════
// Phase 23 — Customer Reports DTOs
// ═══════════════════════════════════════════════════════

public record CustomerBalanceReportDto(
    int Id,
    string Name,
    string? Phone,
    string? GroupName,
    decimal CurrentBalance,
    decimal CreditLimit,
    string BalanceStatus
);

public record CustomerAgingReportDto(
    int Id,
    string Name,
    string? Phone,
    decimal CurrentBalance,
    string AgingBucket,
    DateTime CalculationDate
);

