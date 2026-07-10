using SalesSystem.Domain.Accounting.Enums;

namespace SalesSystem.Contracts.DTOs;

public record UserDto(int Id, string UserName, byte Role,
    bool MustChangePassword, bool IsLocked, bool IsActive,
    string? AvatarPath,
    DateTime? LastLoginAt, int LoginAttempts, int? DefaultCashBoxId)
{
    public string StatusDisplay => IsActive ? (IsLocked ? "مقفل" : "نشط") : "معطل";
}

public record AuditLogDto(long Id, int? UserId, string? UserName, string Action,
    string? EntityType, int? EntityId, string? OldValues, string? NewValues,
    string? ChangedColumns, string? IpAddress, DateTime Timestamp);

public record RoleDto(int Id, string Name, string? Description, bool IsActive, long PermissionsMask = 0);

public record UserSessionDto(
    long Id,
    int UserId,
    string? UserName,
    string? DeviceName,
    string? IpAddress,
    DateTime CreatedAt,
    DateTime LastActivityAt,
    DateTime ExpiresAt,
    bool IsRevoked)
{
    public bool IsActive => !IsRevoked && DateTime.UtcNow <= ExpiresAt;
    public string StatusDisplay => IsRevoked ? "ملغية" : DateTime.UtcNow > ExpiresAt ? "منتهية" : "نشطة";
}

public record UserRoleDto(int UserId, int RoleId, string? RoleName);

public record PermissionDto(int Id, string Code, string DisplayName, string? Category, bool IsActive);

public record RolePermissionDto(byte Role, List<int> PermissionIds);

public record CurrentUserDto(int Id, string UserName, byte Role,
    string? AvatarPath, List<string> Permissions);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword, string ConfirmPassword);

public record UnitDto(int Id, string Name, string? Symbol, bool IsActive);

public record ProductDto(
    int Id,
    string Name,
    int CategoryId,
    string? CategoryName,
    string? Description,
    decimal ReorderLevel,
    bool TrackExpiry,
    string? ImagePath,
    bool IsActive,
    string? Barcode = null,
    decimal CurrentStock = 0)
{
    public bool IsOutOfStock => CurrentStock <= 0;
    public bool IsLowStock => CurrentStock > 0 && CurrentStock <= ReorderLevel;
    public string StockStatusLabel => IsOutOfStock ? "نفذ" : IsLowStock ? "محدود" : "";
}

public record WarehouseDto(
    short Id,
    string Name,
    string? Phone,
    string? Address,
    string? Notes,
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
    string? TaxNumber, string? Notes, decimal CreditLimit, bool IsActive,
    int AccountId, string? AccountName = null, int? CategoryId = null)
{
    public bool HasCreditLimit => CreditLimit > 0;
}
public record CustomerDto(int Id, string Name, string? Phone, string? Email, string? Address,
    string? TaxNumber, decimal CreditLimit, bool IsActive,
    int AccountId, string? AccountName = null, int? CategoryId = null, string? Notes = null)
{
    public bool HasCreditLimit => CreditLimit > 0;
}


public record SalesInvoiceDto(
    int Id,
    int InvoiceNo,
    int CustomerId,
    string? CustomerName,
    int WarehouseId,
    string WarehouseName,
    DateTime InvoiceDate,
    byte PaymentType,
    decimal SubTotal,
    decimal DiscountAmount,
    byte DiscountType,
    decimal? DiscountRate,
    decimal TaxAmount,
    decimal OtherCharges,
    decimal NetTotal,
    decimal PaidAmount,
    decimal RemainingAmount,
    decimal? CostInBaseCurrency,
    string? Notes,
    byte Status,
    int? TaxId,
    string? TaxName,
    decimal? TaxRate,
    int? CashBoxId,
    string? CashBoxName,
    IReadOnlyList<SalesInvoiceLineDto> Items)
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

public record SalesInvoiceLineDto(int Id, int ProductId, string ProductName,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    int ProductUnitId,
    byte DiscountType = 0,
    decimal? DiscountRate = null,
    decimal DiscountAmount = 0,
    decimal? CostInBaseCurrency = null,
    decimal UnitCost = 0,
    decimal ProfitAmount = 0);

public record PurchaseInvoiceDto(
    int Id,
    int InvoiceNo,
    int SupplierId,
    string SupplierName,
    int WarehouseId,
    string WarehouseName,
    DateOnly InvoiceDate,
    byte PaymentType,
    decimal SubTotal,
    decimal DiscountAmount,
    byte DiscountType,
    decimal? DiscountRate,
    decimal? CostInBaseCurrency,
    decimal TaxAmount,
    decimal OtherCharges,
    decimal NetTotal,
    decimal PaidAmount,
    decimal RemainingAmount,
    string? Notes,
    string? SupplierInvoiceNo,
    string? AttachmentPath,
    byte Status,
    int? TaxId,
    string? TaxName,
    decimal? TaxRate,
    int? CashBoxId,
    IReadOnlyList<PurchaseInvoiceLineDto> Items)
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

public record PurchaseInvoiceLineDto(int Id, int ProductId, string ProductName,
    int ProductUnitId,
    string? ProductUnitName,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    decimal LandedUnitCost,
    byte DiscountType,
    decimal? DiscountRate,
    decimal DiscountAmount,
    decimal? CostInBaseCurrency,
    decimal AdditionalFeesAmount);

public record SalesReturnDto(
    int Id,
    int ReturnNo,
    int WarehouseId,
    string WarehouseName,
    int CustomerId,
    string CustomerName,
    int? SalesInvoiceId,
    DateTime ReturnDate,
    decimal SubTotal,
    decimal TaxAmount,
    decimal DiscountAmount,
    decimal TotalAmount,
    decimal ReturnedDiscountAmount,
    decimal ReturnedTaxAmount,
    decimal ReturnedChargeAmount,
    short? TaxId,
    string? Notes,
    string? ReturnReason,
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

public record SalesReturnItemDto(int Id, int ProductId, int ProductUnitId, string ProductName,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountAmount,
    decimal LineTotal,
    byte Mode,
    decimal? CostInBaseCurrency = null);

public record PurchaseReturnDto(
    int Id,
    int ReturnNo,
    int WarehouseId,
    string WarehouseName,
    int SupplierId,
    string SupplierName,
    int? PurchaseInvoiceId,
    bool LinkToInvoice,
    DateOnly ReturnDate,
    decimal SubTotal,
    decimal TotalAmount,
    decimal ReturnedDiscountAmount,
    decimal ReturnedTaxAmount,
    decimal ReturnedChargeAmount,
    short? TaxId,
    byte DiscountType,
    decimal? DiscountRate,
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
    decimal LineTotal,
    int? PurchaseInvoiceLineId,
    decimal? CostInBaseCurrency);

// ═══════════════════════════════════════════════════════════════
// New Inventory Module DTOs (v4.10+)
// ═══════════════════════════════════════════════════════════════

public record InventoryTransactionDto(
    int Id,
    string TransactionNo,
    byte MovementType,
    short WarehouseId,
    string? WarehouseName,
    int? ReferenceId,
    byte? ReferenceType,
    string? Notes,
    DateTime CreatedAt,
    int CreatedByUserId,
    IReadOnlyList<InventoryTransactionLineDto> Lines)
{
    public string MovementTypeDisplay => MovementType switch
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
}

public record InventoryTransactionLineDto(
    int Id,
    int InventoryTransactionId,
    int ProductUnitId,
    string? ProductUnitName,
    decimal Quantity,
    decimal UnitCost,
    string? BatchNo,
    DateOnly? ExpiryDate,
    short? WarehouseId);

public record WarehouseTransferDto(
    int Id,
    string TransferNo,
    short SourceWarehouseId,
    string? SourceWarehouseName,
    short DestinationWarehouseId,
    string? DestinationWarehouseName,
    string? Notes,
    byte Status,
    DateTime CreatedAt,
    int CreatedByUserId,
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
    int WarehouseTransferId,
    int ProductUnitId,
    string? ProductUnitName,
    decimal Quantity,
    string? BatchNo);

// ═══════════════════════════════════════════════════════════════

public record SupplierPaymentDto(
    int Id,
    string PaymentNo,
    int SupplierId,
    string SupplierName,
    decimal Amount,
    byte PaymentMethod,
    DateOnly PaymentDate,
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
    string FooterNote = "",
    bool PrintBarcode = false,
    bool PrintQRCode = false,
    bool PrintCompanyAddress = true);

public record StoreSettingsDto(
    int Id,
    string StoreName,
    string? Phone,
    string? Address,
    string? LogoPath,
    string? Email,
    decimal DefaultTaxRate, // DEPRECATED
    bool IsTaxEnabled,      // DEPRECATED
    string? TaxNumber,
    bool EnableStockAlerts,
    bool AllowNegativeStock,
    string InvoicePrefix,    // DEPRECATED
    string? BackupPath = null,
    string? BackupScheduleTime = "02:00",
    int BackupRetentionDays = 30,
    string? UpdateServerUrl = null,
    string? SignaturePath = null);

public record DocumentSequenceDto(int Id, string DocumentType, int NextNumber);

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

// ─── System Log DTO ────────────────────────────────
public record SystemLogDto(
    long Id,
    byte Level,
    string Message,
    string? Exception,
    string? Source,
    string? ActionName,
    string? IpAddress,
    int? UserId,
    string? UserName,
    DateTime CreatedAt)
{
    public string LogLevel => Level switch
    {
        1 => "معلومات",
        2 => "تحذير",
        3 => "خطأ",
        4 => "حرج",
        _ => "غير معروف"
    };
}

// ═══════════════════════════════════════════════════════
// Phase 22 — Chart of Accounts DTOs
// ═══════════════════════════════════════════════════════

public record AccountDto(
    int Id,
    string AccountCode,
    string NameAr,
    string? NameEn,
    byte Nature,
    bool IsLeaf,
    int? ParentId,
    string? ParentAccountName,
    bool IsSystem,
    bool IsActive,
    short? CategoryId,
    byte Level,
    string? Description,
    string? ColorCode,
    string? Notes)
{
    public string NatureDisplay => Nature switch
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
        1 => "مجموعة رئيسية",
        2 => "مجموعة فرعية",
        3 => "فرعي",
        4 => "تفصيلي",
        _ => $"المستوى {Level}"
    };

    // Backward-compatible properties
    public byte AccountType => Nature;
    public string AccountTypeDisplay => NatureDisplay;
    public bool AllowTransactions => IsLeaf;
    public int? ParentAccountId => ParentId;
    public bool IsSystemAccount => IsSystem;
}

public record AccountTreeNodeDto(
    int Id,
    string AccountCode,
    string NameAr,
    byte Nature,
    bool IsLeaf,
    short? CategoryId,
    byte Level,
    string? ColorCode,
    string? Description,
    List<AccountTreeNodeDto> Children)
{
    // Backward-compatible properties
    public byte AccountType => Nature;
    public bool AllowTransactions => IsLeaf;
}

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
    byte Nature,
    decimal TotalDebit,
    decimal TotalCredit,
    decimal Balance,
    bool IsDebitNormal
)
{
    public AccountType AccountType => (AccountType)Nature;
}

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
    DateTime EntryDate,
    string Description,
    string EntryType,
    string? ReferenceType,
    int? ReferenceId,
    string? ReferenceNumber,
    decimal TotalDebit,
    decimal TotalCredit,
    int Status,
    string? StatusDisplay,
    DateTime CreatedAt,
    int? CreatedByUserId
);

public record JournalEntryDetailDto(
    int Id,
    string EntryNumber,
    DateTime EntryDate,
    string Description,
    string EntryType,
    string? ReferenceType,
    int? ReferenceId,
    string? ReferenceNumber,
    int Status,
    string? StatusDisplay,
    int? ReversedByEntryId,
    DateTime CreatedAt,
    int? CreatedByUserId,
    List<JournalEntryLineDetailDto> Lines
);

public record JournalEntryLineDetailDto(
    int Id,
    int AccountId,
    string? AccountName,
    string? AccountCode,
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
    short? BaseUnitId,
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
    string? LogoPath
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

public record SalesQuotationDto(
    int Id,
    int QuotationNo,
    int CustomerId,
    string? CustomerName,
    int WarehouseId,
    string? WarehouseName,
    DateTime QuotationDate,
    DateTime ValidUntil,
    byte Status,
    decimal SubTotal,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal TotalAmount,
    string? Notes,
    string? TermsAndConditions,
    string? CreatedByUserName,
    DateTime CreatedAt,
    bool IsActive,
    List<SalesQuotationItemDto>? Items = null)
{
    public string StatusDisplay => Status switch
    {
        1 => "مسودة",
        2 => "مرسلة",
        3 => "مقبولة",
        4 => "محولة لفاتورة",
        5 => "مرفوضة",
        _ => "غير معروف"
    };
}

public record SalesQuotationItemDto(
    int Id,
    int QuotationId,
    int ProductId,
    string? ProductName,
    int ProductUnitId,
    string? UnitName,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal LineTotal,
    string? Notes);

