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

public record RoleDto(int Id, string Name, string? Description, bool IsActive);

public record UserSessionDto(
    long Id,
    int UserId,
    string? UserName,
    string FullName,
    string? DeviceName,
    string? IpAddress,
    DateTime LoginAt,
    DateTime LastActivityAt,
    DateTime ExpiresAt,
    bool IsRevoked)
{
    public bool IsActive => !IsRevoked && DateTime.UtcNow <= ExpiresAt;
    public string StatusDisplay => IsRevoked ? "ملغية" : DateTime.UtcNow > ExpiresAt ? "منتهية" : "نشطة";
}

public record UserRoleDto(int UserId, int RoleId, string? RoleName);
public record UserBranchDto(int UserId, short BranchId, string? BranchName);

public record PermissionDto(int Id, string Name, string DisplayNameAr, string? Category, bool IsActive);

public record RolePermissionDto(byte Role, List<int> PermissionIds);

public record CurrentUserDto(int Id, string UserName, string FullName, byte Role,
    string? AvatarPath, List<string> Permissions);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword, string ConfirmPassword);

public record UnitDto(int Id, string Name, string? Symbol, bool IsActive);

public record ProductDto(
    int Id,
    string Name,
    int CategoryId,
    string? CategoryName,
    string? Barcode,
    string? Description,
    decimal ReorderLevel,
    bool TrackExpiry,
    string? ImagePath,
    string? Notes,
    bool IsActive,
    decimal CurrentStock = 0)
{
    public bool IsOutOfStock => CurrentStock <= 0;
    public bool IsLowStock => CurrentStock > 0 && CurrentStock <= ReorderLevel;
    public string StockStatusLabel => IsOutOfStock ? "نفذ" : IsLowStock ? "محدود" : "";
}

public record WarehouseDto(
    int Id,
    string Code,
    string Name,
    byte Type,
    string? Location,
    string? Phone,
    string? Address,
    string? ManagerName,
    bool IsActive);

public record WarehouseStockDto(
    int WarehouseId,
    string? WarehouseName,
    int ProductId,
    string ProductName,
    string? UnitName,
    decimal Quantity,
    decimal AvgCost);

public record SupplierDto(int Id, string Name, string? Phone, string? Email, string? Address,
    string? TaxNumber, bool IsActive,
    int AccountId, string? AccountName = null, string? PaymentTerms = null, string? Notes = null);
public record CustomerDto(int Id, string Name, string? Phone, string? Email, string? Address,
    string? TaxNumber, decimal CreditLimit, bool IsActive,
    int AccountId, string? AccountName = null, DateTime? CustomerSince = null, byte? PriceLevel = null, string? Notes = null)
{
    public bool HasCreditLimit => CreditLimit > 0;
}


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
    decimal OtherCharges,
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
    int? CashBoxId,
    string? CashBoxName,
    decimal? TotalCost,
    decimal? TotalProfit,
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
    byte Mode,
    decimal? CostInBaseCurrency = null,
    decimal? Profit = null,
    bool IsPriceOverridden = false,
    int? ProductUnitId = null);

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
    decimal NetTotal,
    decimal PaidAmount,
    decimal RemainingAmount,
    string? Notes,
    byte Status,
    int? TaxId,
    string? TaxName,
    decimal? TaxRate,
    int? CurrencyId,
    decimal? ExchangeRate,
    string? AttachmentPath,
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
    int ProductUnitId,
    string? ProductUnitName,
    decimal Quantity,
    decimal UnitCost,
    decimal LineTotal);

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
    byte Status,
    int? CashBoxId,
    string? CashBoxName,
    decimal RefundAmount,
    IReadOnlyList<SalesReturnItemDto> Items)
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
    int ReturnNo,
    int WarehouseId,
    string WarehouseName,
    int SupplierId,
    string SupplierName,
    int? PurchaseInvoiceId,
    DateTime ReturnDate,
    decimal SubTotal,
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
    int ProductUnitId,
    string? ProductUnitName,
    decimal Quantity,
    decimal UnitCost,
    decimal LineTotal);

// ═══════════════════════════════════════════════════════════════
// New Inventory Module DTOs (v4.10+)
// ═══════════════════════════════════════════════════════════════

public record InventoryTransactionDto(
    int Id,
    int TransactionNo,
    DateTime TransactionDate,
    byte TransactionType,
    short WarehouseId,
    string? WarehouseName,
    int? ReferenceId,
    byte? ReferenceType,
    string? Notes,
    byte Status,
    IReadOnlyList<InventoryTransactionLineDto> Lines)
{
    public string TransactionTypeDisplay => TransactionType switch
    {
        1 => "مشتريات",
        2 => "مرتجع مشتريات",
        3 => "مبيعات",
        4 => "مرتجع مبيعات",
        5 => "تحويل خارج",
        6 => "تحويل داخل",
        7 => "جرد",
        8 => "تسوية",
        9 => "تلف",
        10 => "رصيد افتتاحي",
        11 => "صرف داخلي",
        12 => "استلام داخلي",
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

public record InventoryTransactionLineDto(
    int Id,
    int ProductId,
    string? ProductName,
    int ProductUnitId,
    string? ProductUnitName,
    decimal Quantity,
    decimal UnitCost,
    decimal TotalCost,
    int? BatchId);

public record WarehouseTransferDto(
    int Id,
    int TransferNo,
    short SourceWarehouseId,
    string? SourceWarehouseName,
    short DestinationWarehouseId,
    string? DestinationWarehouseName,
    DateTime TransferDate,
    string? Notes,
    byte Status,
    IReadOnlyList<WarehouseTransferLineDto> Lines)
{
    public string StatusDisplay => Status switch
    {
        1 => "مسودة",
        2 => "تم الترحيل",
        3 => "ملغي",
        _ => "غير معروف"
    };
}

public record WarehouseTransferLineDto(
    int Id,
    int ProductId,
    string? ProductName,
    int ProductUnitId,
    string? ProductUnitName,
    decimal Quantity,
    decimal UnitCost,
    decimal TotalCost,
    int? BatchId);

// ═══════════════════════════════════════════════════════════════

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
    decimal DefaultTaxRate, // DEPRECATED
    bool IsTaxEnabled,      // DEPRECATED
    string? TaxNumber,
    bool EnableStockAlerts,
    bool AllowNegativeStock,
    bool AutoUpdatePrices,
    string InvoicePrefix,    // DEPRECATED
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

public record ExpiredProductDto(
    int ProductId,
    string ProductName,
    string? CategoryName,
    string? WarehouseName,
    decimal CurrentStock,
    DateTime ExpirationDate,
    int DaysExpired);

public record StockBalanceReportDto(
    int ProductId,
    string ProductName,
    string? CategoryName,
    int WarehouseId,
    string WarehouseName,
    decimal CurrentStock,
    decimal ReorderLevel,
    decimal Cost,
    decimal TotalValue
)
{
    public string BalanceStatus => CurrentStock < ReorderLevel ? "منخفض" : "طبيعي";
    public bool IsLowStock => CurrentStock < ReorderLevel;
}

public record WarehouseMovementReportDto(
    DateTime Date,
    int ProductId,
    string ProductName,
    string WarehouseName,
    string MovementType,
    decimal QuantityChange,
    decimal QuantityBefore,
    decimal QuantityAfter,
    string? ReferenceType,
    int? ReferenceId
);

public record LowStockReportDto(
    int     ProductId,
    string  ProductName,
    string? CategoryName,
    string  WarehouseName,
    decimal CurrentRetailQty,
    decimal ReorderLevelRetailQty,
    decimal DeficitRetailQty,
    decimal SuggestedWholesaleBoxes,
    decimal SuggestedRetailRemainder,
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

public record TaxDto(int Id, string Name, string Code, decimal Rate, byte TaxType, bool IsDefault, bool IsActive);

public record CurrencyDto(
    int Id,
    string Name,
    string Code,
    string Symbol,
    decimal ExchangeRateToBase,
    bool IsBaseCurrency,
    string? FractionName,
    int DecimalPlaces,
    bool IsSystem,
    bool IsActive);

// ─── System Log DTO ────────────────────────────────
public record SystemLogDto(
    long Id,
    string LogLevel,
    byte? Level,
    string Message,
    string? Exception,
    string? StackTrace,
    string? Source,
    string? Context,
    string? MachineName,
    DateTime CreatedAt);

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

// ═══════════════════════════════════════════════════════
// Customer/Supplier Contact DTOs
// ═══════════════════════════════════════════════════════

public record CustomerContactDto(int Id, int CustomerId, string? CustomerName, string Name,
    string? Phone, string? Email, string? Position, string? Notes, bool IsActive);

public record SupplierContactDto(int Id, int SupplierId, string? SupplierName, string Name,
    string? Phone, string? Email, string? Position, string? Notes, bool IsActive);

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
    int Status,
    string? StatusDisplay,
    int? CurrencyId,
    decimal? ExchangeRate,
    string? AttachmentPath,
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
    int Status,
    string? StatusDisplay,
    int? CurrencyId,
    decimal? ExchangeRate,
    string? AttachmentPath,
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

// Product Import DTOs
// ──────────────────────────────────────────────
public record ProductImportRowDto(
    string ProductName,
    string? CategoryName,
    string? Barcode,
    int? BaseUnitId,
    decimal? MinStockLevel,
    string? Description
);

public record ProductImportResultDto(
    int TotalRows,
    int SuccessCount,
    int FailureCount,
    List<ProductImportErrorDto> Errors
);

public record ProductImportErrorDto(
    int RowNumber,
    string ProductName,
    string ErrorMessage
);

// ═══════════════════════════════════════════════════════
// Configuration Screens DTOs
// ═══════════════════════════════════════════════════════

public record CompanySettingsDto(
    int Id,
    string CompanyName,
    string? Phone,
    string? Email,
    string? Address,
    string? TaxNumber,
    string? LogoPath,
    short DefaultCurrencyId,
    string? CurrencyName
);

// ═══════════════════════════════════════════════════════
// 7 New Report DTOs
// ═══════════════════════════════════════════════════════

/// <summary>
/// Detailed stock ledger entry — shows inventory movements with full audit trail.
/// </summary>
public record DetailedStockLedgerDto(
    DateTime Date,
    string ReferenceNo,
    string ReferenceType,
    string MovementType,
    decimal QuantityBefore,
    decimal QuantityChange,
    decimal QuantityAfter,
    decimal UnitCost,
    decimal TotalCost,
    string? CreatedBy);

/// <summary>
/// Product profitability — sales revenue vs COGS per product.
/// </summary>
public record ProductProfitabilityDto(
    string ProductName,
    string? Category,
    decimal TotalSoldQty,
    decimal TotalSalesAmount,
    decimal TotalCOGS,
    decimal GrossProfit,
    decimal ProfitMargin);

/// <summary>
/// Profit broken down by customer.
/// </summary>
public record ProfitByCustomerDto(
    string CustomerName,
    decimal TotalSales,
    decimal TotalCost,
    decimal GrossProfit,
    decimal ProfitMargin,
    int InvoiceCount);

/// <summary>
/// Combined sales/purchase returns report.
/// </summary>
public record ReturnsReportDto(
    string ReturnNo,
    DateTime Date,
    string Type,
    string? PartyName,
    string? ProductName,
    decimal Quantity,
    decimal Amount,
    string? Reason,
    string Status);

/// <summary>
/// Aging report — customer/supplier balance aging buckets.
/// </summary>
public record AgingReportDto(
    string Name,
    decimal TotalBalance,
    decimal Current,
    decimal Days1To30,
    decimal Days31To60,
    decimal Days61To90,
    decimal Days90Plus,
    decimal TotalDue);

/// <summary>
/// Working capital summary — current assets, liabilities, ratio.
/// </summary>
public record WorkingCapitalSummaryDto(
    decimal CurrentAssets,
    decimal CurrentLiabilities,
    decimal WorkingCapital,
    decimal CurrentRatio,
    List<WorkingCapitalAccountDto> Accounts);

/// <summary>
/// A single account in the working capital breakdown.
/// </summary>
public record WorkingCapitalAccountDto(
    string AccountName,
    string AccountCode,
    decimal Balance,
    string Type);

/// <summary>
/// Account balance report line — all accounts with debit/credit/net.
/// </summary>
public record AccountBalanceReportDto(
    string AccountCode,
    string AccountName,
    string AccountTypeDisplay,
    int Level,
    decimal DebitBalance,
    decimal CreditBalance,
    decimal NetBalance);

